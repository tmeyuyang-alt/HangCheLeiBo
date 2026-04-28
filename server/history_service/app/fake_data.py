from __future__ import annotations

import math
import random
from datetime import datetime, timedelta, timezone
from typing import Sequence

from .config import settings
from .db import db
from .query import normalize_filter_values, resolve_duration
from .schemas import RandomDataGenerateRequest

BOOL_TYPE = 0
DINT_TYPE = 1
REAL_TYPE = 2
INT_TYPE = 3
LINT_TYPE = 4
BATCH_SIZE = 5000  # TDengine 单条 SQL 不宜过长, 分批拼接


def _float_value(point_id: int, ts: datetime, rng: random.Random) -> float:
    seconds = ts.timestamp()
    base = 20.0 + (point_id % 11) * 7.5
    wave = math.sin(seconds / 180.0 + point_id) * 8.0
    trend = math.cos(seconds / 1200.0 + (point_id % 5)) * 3.0
    noise = rng.uniform(-0.8, 0.8)
    return round(base + wave + trend + noise, 3)


def _int_value(point_id: int, ts: datetime, rng: random.Random, scale: int, amplitude: int) -> int:
    seconds = ts.timestamp()
    base = scale + (point_id % 17) * amplitude
    wave = math.sin(seconds / 240.0 + point_id) * amplitude
    noise = rng.randint(-max(1, amplitude // 8), max(1, amplitude // 8))
    return int(round(base + wave + noise))


def _bool_value(point_id: int, ts: datetime) -> float:
    bucket = int(ts.timestamp()) // 30
    return float(1 if (bucket + point_id) % 2 == 0 else 0)


def _value_for_type(data_type: int, point_id: int, ts: datetime, rng: random.Random) -> tuple[float, str]:
    """Return (numeric_value, text_value) where text_value is a pure numeric string."""
    if data_type == BOOL_TYPE:
        v = _bool_value(point_id, ts)
        return v, str(int(v))
    if data_type == REAL_TYPE:
        v = _float_value(point_id, ts, rng)
        return v, str(v)
    if data_type == INT_TYPE:
        v = _int_value(point_id, ts, rng, 80, 25)
        return float(v), str(v)
    if data_type == DINT_TYPE:
        v = _int_value(point_id, ts, rng, 1200, 300)
        return float(v), str(v)
    if data_type == LINT_TYPE:
        v = _int_value(point_id, ts, rng, 200000, 40000)
        return float(v), str(v)
    v = _float_value(point_id, ts, rng)
    return v, str(v)


def _escape(s: str) -> str:
    return s.replace("'", "\\'")


def _subtable_name(prefix: str, *parts: str) -> str:
    raw = "_".join([prefix] + list(parts))
    return "".join(c if (c.isascii() and c.isalnum()) or c == "_" else "_" for c in raw).lower()[:190]


def _ts_str(dt: datetime) -> str:
    """Format datetime to TDengine timestamp string with milliseconds."""
    return dt.strftime("%Y-%m-%d %H:%M:%S.") + f"{dt.microsecond // 1000:03d}"


async def generate_random_history_data(request: RandomDataGenerateRequest) -> dict[str, object]:
    start_at = request.start_at.astimezone(timezone.utc)
    end_at = resolve_duration(start_at, request.duration_value, request.duration_unit)
    keyword_filter = request.device_name_contains.strip() if request.device_name_contains else None

    db_name = settings.tdengine_database

    # 查询已配置的点位 (从 history_points 超级表的 tag 中读取)
    where_parts = [f"plc_id = '{_escape(request.plc_id)}'", "is_enabled = 1"]
    if keyword_filter:
        kf = _escape(keyword_filter)
        where_parts.append(
            f"(device_name LIKE '%{kf}%' OR display_name LIKE '%{kf}%')"
        )
    if request.devices:
        device_parts = [f"device_name LIKE '%{_escape(d)}%'" for d in request.devices]
        where_parts.append(f"({' OR '.join(device_parts)})")
    if request.conditions:
        cond_parts = [f"display_name LIKE '%{_escape(c)}%'" for c in request.conditions]
        where_parts.append(f"({' OR '.join(cond_parts)})")
    # 旧版兼容
    legacy_filters = normalize_filter_values(request.device_name_contains_any)
    if legacy_filters and not request.devices and not request.conditions:
        for lf in legacy_filters:
            where_parts.append(
                f"(CONCAT(device_name, '.', display_name) LIKE '%{_escape(lf)}%')"
            )

    where_sql = " AND ".join(where_parts)

    # PARTITION BY tbname + LIMIT 1 每个子表取一行，避免 DISTINCT 在 TDengine 超级表上的兼容性问题
    point_rows = await db.fetch(
        f"SELECT tbname, device_name, plc_id, display_name, address, data_type "
        f"FROM {db_name}.history_points "
        f"WHERE {where_sql} "
        f"PARTITION BY tbname LIMIT 1"
    )
    # 按 tbname 去重（PARTITION BY 已保证每个子表只有一行，这里做双重保险）
    seen: set[str] = set()
    unique_rows = []
    for r in point_rows:
        if r["tbname"] not in seen:
            seen.add(r["tbname"])
            unique_rows.append(r)
    point_rows = unique_rows
    if not point_rows:
        raise ValueError("No history points found for the given filters")

    # 构建点位元信息: (子表名, data_type, point_id_str, device_name, display_name)
    point_meta: list[tuple[str, int, str, str, str]] = []
    for row in point_rows:
        plc_id = row["plc_id"]
        dev_name = row["device_name"]
        address = row["address"]
        data_type = int(row["data_type"])
        display_name = row["display_name"]
        pt_table = _subtable_name("pt", plc_id, dev_name, address)
        point_id = f"{plc_id}.{dev_name}.{address}"
        point_meta.append((pt_table, data_type, point_id, dev_name, display_name))

    # 如果 overwrite, 删除该时间范围内的旧数据
    if request.overwrite:
        for pt_table, *_ in point_meta:
            try:
                await db.execute(
                    f"DELETE FROM {db_name}.{pt_table} "
                    f"WHERE ts >= '{_ts_str(start_at)}' AND ts < '{_ts_str(end_at)}'"
                )
            except Exception:
                pass

    rng = random.Random(request.seed if request.seed is not None else 20260328)

    inserted_rows = 0
    current = start_at
    step = timedelta(seconds=request.step_seconds)

    # 分批拼接 INSERT 语句
    # TDengine INSERT 支持: INSERT INTO tb1 VALUES (...) (...) tb2 VALUES (...) (...)
    # 按子表分组收集 values, 达到 BATCH_SIZE 后一次性写入
    values_by_table: dict[str, list[str]] = {pt[0]: [] for pt in point_meta}
    batch_count = 0

    while current < end_at:
        ts_str = _ts_str(current)
        # 给每个点位一个唯一的确定性 id (用于 _value_for_type 的 point_id 参数)
        for idx, (pt_table, data_type, point_id, dev_name, display_name) in enumerate(point_meta):
            numeric_value, text_value = _value_for_type(data_type, idx + 1, current, rng)
            values_by_table[pt_table].append(
                f"('{ts_str}', {numeric_value}, '{_escape(text_value)}', 0)"
            )
            batch_count += 1

        if batch_count >= BATCH_SIZE:
            await _flush_batch(db_name, point_meta, values_by_table)
            inserted_rows += batch_count
            batch_count = 0
            for k in values_by_table:
                values_by_table[k] = []

        current += step

    # 最后一批
    if batch_count > 0:
        await _flush_batch(db_name, point_meta, values_by_table)
        inserted_rows += batch_count

    return {
        "plc_id": request.plc_id,
        "points_count": len(point_meta),
        "rows_inserted": inserted_rows,
        "start_at": start_at,
        "end_at": end_at,
        "step_seconds": request.step_seconds,
        "overwrite": request.overwrite,
    }


async def _flush_batch(
    db_name: str,
    point_meta: list[tuple[str, int, str, str, str]],
    values_by_table: dict[str, list[str]],
) -> None:
    """Flush collected values into a single multi-table INSERT."""
    parts: list[str] = []
    for pt_table, data_type, point_id, dev_name, display_name in point_meta:
        vals = values_by_table.get(pt_table)
        if not vals:
            continue
        values_str = " ".join(vals)
        parts.append(
            f"{db_name}.{pt_table} USING {db_name}.point_samples "
            f"TAGS ('{_escape(point_id)}', '{_escape(dev_name)}', "
            f"'{_escape(point_meta[0][2].split('.')[0])}', "
            f"'{_escape(display_name)}', {data_type}) "
            f"VALUES {values_str}"
        )

    if parts:
        sql = "INSERT INTO " + " ".join(parts)
        await db.batch_insert_sql(sql)
