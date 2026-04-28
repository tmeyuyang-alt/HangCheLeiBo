from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Any

import json

import yaml

from .config import settings
from .db import db  # 仍用于 upsert_imported_devices


@dataclass
class ImportedPoint:
    display_name: str
    address: str
    data_type: int


@dataclass
class ImportedDevice:
    plc_id: str
    name: str
    shared_ip_address: str
    source_path: str
    points: list[ImportedPoint]


def _normalize_yaml_text(text: str) -> str:
    lines = text.splitlines()
    for index, line in enumerate(lines):
        if line.startswith("MonoBehaviour:"):
            return "\n".join(lines[index:])
    raise ValueError("Unity asset does not contain MonoBehaviour section")


def _as_bool(value: Any) -> bool:
    if isinstance(value, bool):
        return value
    if isinstance(value, int):
        return value != 0
    if isinstance(value, str):
        return value.strip().lower() in {"1", "true", "yes"}
    return False


def _escape(value: str) -> str:
    """Escape single quotes for TDengine NCHAR values."""
    return value.replace("'", "\\'")


def _subtable_name(prefix: str, *parts: str) -> str:
    """Generate a valid TDengine subtable name from parts."""
    raw = "_".join([prefix] + list(parts))
    # Only keep alphanumeric and underscores, lowercase
    return "".join(c if (c.isascii() and c.isalnum()) or c == "_" else "_" for c in raw).lower()[:190]


async def upsert_imported_devices(devices: list[ImportedDevice]) -> tuple[int, int]:
    imported_devices = 0
    imported_points = 0
    db_name = settings.tdengine_database

    for device in devices:
        if not device.points:
            continue

        # 用 devices 超级表记录设备信息 (使用 INSERT 自动建子表)
        dev_table = _subtable_name("dev", device.plc_id, device.name)
        await db.execute(
            f"INSERT INTO {db_name}.{dev_table} USING {db_name}.devices "
            f"TAGS ('{_escape(device.plc_id)}', '{_escape(device.name)}', "
            f"'{_escape(device.shared_ip_address)}', '{_escape(device.source_path)}') "
            f"VALUES (NOW, 1)"
        )
        imported_devices += 1

        for point in device.points:
            # 每个点位一张子表，子表名: pt_{plc_id}_{device_name}_{address}
            pt_table = _subtable_name("pt", device.plc_id, device.name, point.address)
            point_id = f"{device.plc_id}.{device.name}.{point.address}"

            # 在 history_points 超级表中记录元数据
            hp_table = _subtable_name("hp", device.plc_id, device.name, point.address)
            await db.execute(
                f"INSERT INTO {db_name}.{hp_table} USING {db_name}.history_points "
                f"TAGS ('{_escape(device.name)}', '{_escape(device.plc_id)}', "
                f"'{_escape(point.display_name)}', '{_escape(point.address)}', "
                f"{point.data_type}, true, '{_escape(device.shared_ip_address)}') "
                f"VALUES (NOW, 1)"
            )

            # 预创建 point_samples 子表 (插入一条占位数据，使用 NOW 避免超出保留窗口)
            await db.execute(
                f"INSERT INTO {db_name}.{pt_table} USING {db_name}.point_samples "
                f"TAGS ('{_escape(point_id)}', '{_escape(device.name)}', "
                f"'{_escape(device.plc_id)}', '{_escape(point.display_name)}', "
                f"{point.data_type}) "
                f"VALUES (NOW, 0, '0', 0)"
            )
            imported_points += 1

    return imported_devices, imported_points


def parse_unity_asset(asset_path: Path, plc_id: str) -> ImportedDevice:
    raw_text = asset_path.read_text(encoding="utf-8")
    parsed = yaml.safe_load(_normalize_yaml_text(raw_text))
    mono = parsed["MonoBehaviour"]

    device_name = str(mono.get("m_Name") or asset_path.stem)
    shared_ip_address = str(mono.get("sharedIpAddress") or "").strip()

    imported_points: list[ImportedPoint] = []
    for item in mono.get("points") or []:
        if not _as_bool(item.get("isHistoryData")):
            continue

        address = str(item.get("address") or "").strip()
        if not address:
            continue

        imported_points.append(
            ImportedPoint(
                display_name=str(item.get("displayName") or address).strip(),
                address=address,
                data_type=int(item.get("dataType") or 0),
            )
        )

    return ImportedDevice(
        plc_id=plc_id,
        name=device_name,
        shared_ip_address=shared_ip_address,
        source_path=str(asset_path),
        points=imported_points,
    )


async def import_assets(folder: str, plc_id: str) -> tuple[int, int]:
    base = Path(folder)
    if not base.exists() or not base.is_dir():
        raise FileNotFoundError(f"Folder not found: {folder}")

    asset_paths = sorted(base.glob("*.asset"))
    imported_devices_payload: list[ImportedDevice] = []

    for asset_path in asset_paths:
        imported_devices_payload.append(parse_unity_asset(asset_path, plc_id))

    return await upsert_imported_devices(imported_devices_payload)


def parse_config_json(payload: dict[str, Any], plc_id: str, source_path: str = "editor-upload") -> list[ImportedDevice]:
    imported_devices: list[ImportedDevice] = []
    shared_ip_address = str(payload.get("sharedIpAddress") or "").strip()

    for item in payload.get("devices") or []:
        if not isinstance(item, dict):
            continue

        device_name = str(item.get("deviceName") or "").strip()
        if not device_name:
            continue

        imported_points: list[ImportedPoint] = []
        for point in item.get("points") or []:
            if not isinstance(point, dict):
                continue

            if not _as_bool(point.get("isHistoryData")):
                continue

            address = str(point.get("address") or "").strip()
            if not address:
                continue

            imported_points.append(
                ImportedPoint(
                    display_name=str(point.get("displayName") or address).strip(),
                    address=address,
                    data_type=int(point.get("dataType") or 0),
                )
            )

        imported_devices.append(
            ImportedDevice(
                plc_id=plc_id,
                name=device_name,
                shared_ip_address=shared_ip_address,
                source_path=source_path,
                points=imported_points,
            )
        )

    return imported_devices


def _snapshot_path(plc_id: str) -> Path:
    """返回指定 PLC 快照文件路径。"""
    snapshot_dir = Path(settings.config_snapshot_dir)
    snapshot_dir.mkdir(parents=True, exist_ok=True)
    safe_id = "".join(c if (c.isascii() and c.isalnum()) or c == "_" else "_" for c in plc_id)
    return snapshot_dir / f"{safe_id}_latest.json"


def save_config_snapshot(payload: dict[str, Any], plc_id: str, source: str = "editor-upload") -> None:
    """将完整的 JSON 配置快照写入服务器本地文件，文件名: {plc_id}_latest.json。"""
    snapshot = {
        "plc_id": plc_id,
        "source": source,
        "config": payload,
    }
    path = _snapshot_path(plc_id)
    path.write_text(json.dumps(snapshot, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[config_import] snapshot saved to {path}")


def get_latest_config_snapshot(plc_id: str) -> dict[str, Any] | None:
    """从本地文件读取指定 PLC 最新配置快照。"""
    path = _snapshot_path(plc_id)
    if not path.exists():
        return None
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
        return {
            "plc_id": data.get("plc_id", plc_id),
            "source": data.get("source", ""),
            "config_json": data.get("config", {}),
        }
    except Exception as e:
        print(f"[config_import] failed to read snapshot {path}: {e}")
        return None


async def import_config_json(payload: dict[str, Any], plc_id: str) -> tuple[int, int]:
    # 保存完整 JSON 快照到本地文件（失败不阻断主流程）
    try:
        save_config_snapshot(payload, plc_id)
    except Exception as e:
        print(f"[config_import] save_config_snapshot failed (non-fatal): {e}")
    imported_devices = parse_config_json(payload, plc_id)
    return await upsert_imported_devices(imported_devices)
