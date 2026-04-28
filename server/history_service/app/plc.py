from __future__ import annotations

import asyncio
import struct
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Iterable

from .config import settings
from .db import db

try:
    import snap7  # type: ignore
except ImportError:  # pragma: no cover
    snap7 = None


@dataclass(frozen=True)
class PointBinding:
    point_id: str  # TDengine point_id tag: "plc_id.device_name.address"
    pt_table: str  # point_samples subtable name
    plc_id: str
    device_name: str
    shared_ip_address: str
    display_name: str
    address: str
    data_type: int


@dataclass(frozen=True)
class S7Address:
    db_number: int
    start_byte: int
    bit_index: int | None
    width: int


@dataclass
class ReadChunk:
    db_number: int
    start_byte: int
    size: int
    points: list[tuple[PointBinding, S7Address]]


def parse_s7_address(address: str, data_type: int) -> S7Address:
    import re

    pattern = re.compile(r"^DB(?P<db>\d+)\.DB(?P<kind>[XWDB])(?P<offset>\d+)(?:\.(?P<bit>\d+))?$")
    match = pattern.match(address.strip())
    if not match:
        raise ValueError(f"Unsupported S7 address: {address}")

    db_number = int(match.group("db"))
    kind = match.group("kind")
    offset = int(match.group("offset"))
    bit_text = match.group("bit")

    if kind == "X":
        return S7Address(db_number=db_number, start_byte=offset, bit_index=int(bit_text or 0), width=1)
    if data_type in {1, 2} or kind == "D":
        return S7Address(db_number=db_number, start_byte=offset, bit_index=None, width=4)
    if data_type == 3 or kind == "W":
        return S7Address(db_number=db_number, start_byte=offset, bit_index=None, width=2)
    if data_type == 4:
        return S7Address(db_number=db_number, start_byte=offset, bit_index=None, width=8)

    raise ValueError(f"Unsupported data type for address {address}: {data_type}")


def decode_value(buffer: bytes, s7: S7Address, data_type: int) -> tuple[float | None, str | None]:
    start = s7.start_byte
    if data_type == 0:
        raw = bool(buffer[start] & (1 << int(s7.bit_index or 0)))
        return (1.0 if raw else 0.0), "1" if raw else "0"
    if data_type == 1:
        value = struct.unpack(">i", buffer[start : start + 4])[0]
        return float(value), str(value)
    if data_type == 2:
        value = struct.unpack(">f", buffer[start : start + 4])[0]
        return float(value), str(value)
    if data_type == 3:
        value = struct.unpack(">h", buffer[start : start + 2])[0]
        return float(value), str(value)
    if data_type == 4:
        value = struct.unpack(">q", buffer[start : start + 8])[0]
        return float(value), str(value)
    return None, None


def build_read_chunks(bindings: Iterable[PointBinding]) -> dict[str, list[ReadChunk]]:
    by_ip: dict[str, list[tuple[PointBinding, S7Address]]] = {}
    for binding in bindings:
        s7 = parse_s7_address(binding.address, binding.data_type)
        by_ip.setdefault(binding.shared_ip_address, []).append((binding, s7))

    result: dict[str, list[ReadChunk]] = {}
    max_size = max(1, settings.plc_max_read_bytes)

    for ip, items in by_ip.items():
        items.sort(key=lambda item: (item[1].db_number, item[1].start_byte))
        chunks: list[ReadChunk] = []
        current: ReadChunk | None = None

        for binding, s7 in items:
            if current is None:
                current = ReadChunk(s7.db_number, s7.start_byte, s7.width, [(binding, s7)])
                continue

            current_end = current.start_byte + current.size
            new_end = max(current_end, s7.start_byte + s7.width)
            new_size = new_end - current.start_byte
            same_db = current.db_number == s7.db_number

            if same_db and new_size <= max_size:
                current.size = new_size
                current.points.append((binding, s7))
            else:
                chunks.append(current)
                current = ReadChunk(s7.db_number, s7.start_byte, s7.width, [(binding, s7)])

        if current is not None:
            chunks.append(current)

        result[ip] = chunks

    return result


def _escape(s: str) -> str:
    return s.replace("'", "\\'")


def _subtable_name(prefix: str, *parts: str) -> str:
    raw = "_".join([prefix] + list(parts))
    return "".join(c if (c.isascii() and c.isalnum()) or c == "_" else "_" for c in raw).lower()[:190]


async def load_enabled_points(plc_id: str | None = None) -> list[PointBinding]:
    db_name = settings.tdengine_database
    where_parts = ["is_enabled = true", "shared_ip_address != ''"]
    if plc_id:
        where_parts.append(f"plc_id = '{_escape(plc_id)}'")
    where_sql = " AND ".join(where_parts)

    rows = await db.fetch(
        f"SELECT DISTINCT tbname, device_name, plc_id, display_name, address, "
        f"data_type, shared_ip_address "
        f"FROM {db_name}.history_points "
        f"WHERE {where_sql}"
    )
    result: list[PointBinding] = []
    for row in rows:
        p_plc_id = row["plc_id"]
        dev_name = row["device_name"]
        address = row["address"]
        pt_table = _subtable_name("pt", p_plc_id, dev_name, address)
        point_id = f"{p_plc_id}.{dev_name}.{address}"
        result.append(
            PointBinding(
                point_id=point_id,
                pt_table=pt_table,
                plc_id=p_plc_id,
                device_name=dev_name,
                shared_ip_address=row["shared_ip_address"],
                display_name=row["display_name"],
                address=address,
                data_type=int(row["data_type"]),
            )
        )
    return result


def _ensure_snap7() -> None:
    if snap7 is None:
        raise RuntimeError("python-snap7 is not installed")


def _read_ip(ip: str, chunks: list[ReadChunk]) -> list[tuple[PointBinding, float | None, str | None]]:
    _ensure_snap7()
    client = snap7.client.Client()
    client.set_connection_type(3)
    client.connect(ip, settings.plc_rack, settings.plc_slot, settings.plc_connect_timeout_seconds * 1000)

    records: list[tuple[PointBinding, float | None, str | None]] = []
    try:
        for chunk in chunks:
            payload = bytes(client.db_read(chunk.db_number, chunk.start_byte, chunk.size))
            for binding, s7 in chunk.points:
                relative = S7Address(
                    db_number=s7.db_number,
                    start_byte=s7.start_byte - chunk.start_byte,
                    bit_index=s7.bit_index,
                    width=s7.width,
                )
                numeric_value, text_value = decode_value(payload, relative, binding.data_type)
                records.append((binding, numeric_value, text_value))
    finally:
        client.disconnect()
        client.destroy()

    return records


async def collect_once(plc_id: str | None = None) -> int:
    bindings = await load_enabled_points(plc_id)
    if not bindings:
        return 0

    grouped = build_read_chunks(bindings)
    loop = asyncio.get_running_loop()
    all_records: list[tuple[PointBinding, float | None, str | None]] = []

    for ip, chunks in grouped.items():
        records = await loop.run_in_executor(None, _read_ip, ip, chunks)
        all_records.extend(records)

    if not all_records:
        return 0

    db_name = settings.tdengine_database
    now = datetime.now(timezone.utc)
    ts_str = now.strftime("%Y-%m-%d %H:%M:%S.") + f"{now.microsecond // 1000:03d}"

    # 拼接单条 INSERT 语句 (TDengine 多表写入)
    parts: list[str] = []
    for binding, numeric_value, text_value in all_records:
        if numeric_value is None:
            continue
        tv = _escape(text_value or str(numeric_value))
        parts.append(
            f"{db_name}.{binding.pt_table} USING {db_name}.point_samples "
            f"TAGS ('{_escape(binding.point_id)}', '{_escape(binding.device_name)}', "
            f"'{_escape(binding.plc_id)}', '{_escape(binding.display_name)}', "
            f"{binding.data_type}) "
            f"VALUES ('{ts_str}', {numeric_value}, '{tv}', 0)"
        )

    if parts:
        sql = "INSERT INTO " + " ".join(parts)
        await db.batch_insert_sql(sql)

    return len(all_records)
