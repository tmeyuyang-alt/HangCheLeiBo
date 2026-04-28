from __future__ import annotations

from datetime import datetime, timedelta, timezone
from typing import Any, Iterable

from dateutil.relativedelta import relativedelta

from .config import settings
from .db import db
from .schemas import AggregateType, HistoryQueryRequest, QueryColumn, TimeUnit


def _escape(s: str) -> str:
    return s.replace("'", "\\'")


def _ts_str(dt: datetime) -> str:
    return dt.strftime("%Y-%m-%d %H:%M:%S.") + f"{dt.microsecond // 1000:03d}"


def resolve_duration(start_at: datetime, value: int, unit: TimeUnit) -> datetime:
    if unit == TimeUnit.seconds:
        return start_at + timedelta(seconds=value)
    if unit == TimeUnit.minutes:
        return start_at + timedelta(minutes=value)
    if unit == TimeUnit.hours:
        return start_at + timedelta(hours=value)
    if unit == TimeUnit.days:
        return start_at + timedelta(days=value)
    if unit == TimeUnit.months:
        return start_at + relativedelta(months=value)
    if unit == TimeUnit.years:
        return start_at + relativedelta(years=value)
    if unit == TimeUnit.shift:
        return start_at + timedelta(hours=settings.shift_length_hours * value)
    raise ValueError(f"Unsupported duration unit: {unit}")


def interval_to_tdengine(value: int, unit: TimeUnit) -> str:
    """Convert interval to TDengine INTERVAL syntax like '1m', '1h', '1d'."""
    if unit == TimeUnit.shift:
        return f"{value * settings.shift_length_hours}h"
    mapping = {
        TimeUnit.seconds: "s",
        TimeUnit.minutes: "m",
        TimeUnit.hours: "h",
        TimeUnit.days: "d",
        TimeUnit.months: "n",  # TDengine uses 'n' for months
        TimeUnit.years: "y",
    }
    return f"{value}{mapping[unit]}"


def interval_to_label(value: int, unit: TimeUnit) -> str:
    """Human-readable label."""
    if unit == TimeUnit.shift:
        return f"{value * settings.shift_length_hours} hours"
    name_map = {
        TimeUnit.seconds: "seconds",
        TimeUnit.minutes: "minutes",
        TimeUnit.hours: "hours",
        TimeUnit.days: "days",
        TimeUnit.months: "months",
        TimeUnit.years: "years",
    }
    return f"{value} {name_map[unit]}"


def aggregate_expr(aggregate: AggregateType) -> str:
    """Return TDengine aggregate function for numeric_value."""
    if aggregate == AggregateType.last:
        return "LAST(numeric_value)"
    if aggregate == AggregateType.min:
        return "MIN(numeric_value)"
    if aggregate == AggregateType.max:
        return "MAX(numeric_value)"
    return "AVG(numeric_value)"


def normalize_filter_values(values: Iterable[str] | None) -> list[str] | None:
    if not values:
        return None

    result: list[str] = []
    seen: set[str] = set()
    for raw_value in values:
        for item in raw_value.split(","):
            value = item.strip()
            if not value or value in seen:
                continue
            seen.add(value)
            result.append(value)
    return result or None


async def run_history_query(request: HistoryQueryRequest) -> dict[str, Any]:
    start_at = request.start_at.astimezone(timezone.utc)
    end_at = resolve_duration(start_at, request.duration_value, request.duration_unit)
    keyword_filter = request.device_name_contains.strip() if request.device_name_contains else None

    db_name = settings.tdengine_database

    # 查询匹配的点位
    where_parts = [f"plc_id = '{_escape(request.plc_id)}'", "is_enabled = 1"]

    # 旧版兼容：device_name_contains 单关键词模糊匹配
    if keyword_filter:
        kf = _escape(keyword_filter)
        where_parts.append(
            f"(device_name LIKE '%{kf}%' OR display_name LIKE '%{kf}%')"
        )

    # 新版：devices 列表匹配 device_name（多选 OR），conditions 匹配 display_name（多选 OR）
    # 两个列表之间是 AND 关系：必须同时满足设备条件和指标条件
    if request.devices:
        device_parts = [f"device_name LIKE '%{_escape(d)}%'" for d in request.devices]
        where_parts.append(f"({' OR '.join(device_parts)})")

    if request.conditions:
        cond_parts = [f"display_name LIKE '%{_escape(c)}%'" for c in request.conditions]
        where_parts.append(f"({' OR '.join(cond_parts)})")

    # 旧版兼容：device_name_contains_any 逐条 AND（保持向后兼容）
    legacy_filters = normalize_filter_values(request.device_name_contains_any)
    if legacy_filters and not request.devices and not request.conditions:
        for lf in legacy_filters:
            where_parts.append(
                f"(CONCAT(device_name, '.', display_name) LIKE '%{_escape(lf)}%')"
            )

    where_sql = " AND ".join(where_parts)
    point_rows = await db.fetch(
        f"SELECT DISTINCT tbname, device_name, plc_id, display_name, address, data_type "
        f"FROM {db_name}.history_points "
        f"WHERE {where_sql}"
    )

    # 构建 columns 列信息
    columns = [QueryColumn(field="ts", label="ts")]
    point_meta: list[dict[str, Any]] = []
    point_fields: list[str] = []

    for row in point_rows:
        plc_id = row["plc_id"]
        dev_name = row["device_name"]
        address = row["address"]
        display_name = row["display_name"]
        pt_table = _subtable_name("pt", plc_id, dev_name, address)
        point_id_str = f"{plc_id}.{dev_name}.{address}"
        field = f"p_{pt_table}"
        label = f"{dev_name}.{display_name}"

        point_fields.append(field)
        columns.append(
            QueryColumn(
                field=field,
                label=label,
                point_id=hash(point_id_str) & 0x7FFFFFFFFFFFFFFF,
                device_name=dev_name,
                point_name=display_name,
            )
        )
        point_meta.append({
            "pt_table": pt_table,
            "field": field,
            "device_name": dev_name,
            "display_name": display_name,
        })

    interval_label = interval_to_label(request.interval_value, request.interval_unit)

    if not point_meta:
        return {
            "start_at": start_at,
            "end_at": end_at,
            "interval_label": interval_label,
            "aggregate": request.aggregate,
            "columns": columns,
            "rows": [],
        }

    # 对每个点位分别查询, 然后合并成宽表
    interval_str = interval_to_tdengine(request.interval_value, request.interval_unit)
    agg_expr = aggregate_expr(request.aggregate)
    ts_start = _ts_str(start_at)
    ts_end = _ts_str(end_at)

    wide_rows: dict[str, dict[str, Any]] = {}

    for pm in point_meta:
        pt_table = pm["pt_table"]
        field = pm["field"]
        sql = (
            f"SELECT _WSTART AS bucket_ts, {agg_expr} AS agg_val "
            f"FROM {db_name}.{pt_table} "
            f"WHERE ts >= '{ts_start}' AND ts < '{ts_end}' "
            f"INTERVAL({interval_str})"
        )
        try:
            rows = await db.fetch(sql)
        except Exception as exc:
            print(f"[query] fetch failed for {pt_table}: {exc}\nSQL: {sql}")
            continue

        for row in rows:
            bucket_ts = row["bucket_ts"]
            value = row["agg_val"]
            # 统一 key 为 ISO 字符串
            if isinstance(bucket_ts, datetime):
                ts_key = bucket_ts.isoformat()
            else:
                ts_key = str(bucket_ts)

            if ts_key not in wide_rows:
                wide_rows[ts_key] = {"ts": ts_key, **{name: None for name in point_fields}}
            wide_rows[ts_key][field] = value

    result_rows = [wide_rows[key] for key in sorted(wide_rows.keys())]
    return {
        "start_at": start_at,
        "end_at": end_at,
        "interval_label": interval_label,
        "aggregate": request.aggregate,
        "columns": columns,
        "rows": result_rows,
    }


def _subtable_name(prefix: str, *parts: str) -> str:
    raw = "_".join([prefix] + list(parts))
    return "".join(c if (c.isascii() and c.isalnum()) or c == "_" else "_" for c in raw).lower()[:190]
