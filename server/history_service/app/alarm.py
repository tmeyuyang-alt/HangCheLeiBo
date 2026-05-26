from __future__ import annotations

from datetime import datetime

from .config import settings
from .db import db
from .time_utils import format_local_seconds, now_local, to_local_naive


def _escape(s: str) -> str:
    return s.replace("\\", "\\\\").replace("'", "\\'")


def _to_local_naive(dt: datetime) -> datetime:
    """将 UTC-aware datetime 转为本地 naive datetime。
    TDengine 插入时用本地时间字符串，查询也必须用本地时间字符串，否则时间窗口偏移。
    """
    return to_local_naive(dt)


async def insert_alarm_record(
    trigger_time: str,
    recovery_time: str,
    device_name: str,
    alarm_content: str,
    handling_method: str,
    operator_name: str,
    alarm_level: str = "",
    plc_id: str = "plc01",
) -> None:
    db_name = settings.tdengine_database
    sub_table = f"alarm_{_escape(plc_id)}"
    sql = (
        f"INSERT INTO {db_name}.{sub_table} USING {db_name}.alarm_records "
        f"TAGS ('{_escape(plc_id)}') "
        f"VALUES ("
        f"'{_escape(trigger_time)}', "
        f"'{_escape(recovery_time)}', "
        f"'{_escape(device_name)}', "
        f"'{_escape(alarm_content)}', "
        f"'{_escape(handling_method)}', "
        f"'{_escape(operator_name)}', "
        f"'{_escape(alarm_level)}'"
        f")"
    )
    await db.execute(sql)


async def query_alarm_records(
    start_at: datetime,
    end_at: datetime | None = None,
    device_name: str | None = None,
    plc_id: str = "plc01",
    order: str = "desc",          # "asc" | "desc"
    page: int = 1,
    page_size: int = 50,
) -> dict:
    db_name = settings.tdengine_database

    # 统一转为本地 naive datetime，与 WarningNotify 插入的本地时间字符串保持一致
    start_local = to_local_naive(start_at)
    end_local   = to_local_naive(end_at) if end_at is not None else now_local().replace(tzinfo=None)

    start_str = start_local.strftime("%Y-%m-%d %H:%M:%S")
    end_str   = end_local.strftime("%Y-%m-%d %H:%M:%S")

    where = f"ts >= '{start_str}' AND ts <= '{end_str}'"
    if plc_id:
        where += f" AND plc_id = '{_escape(plc_id)}'"
    if device_name and device_name.strip():
        # 精确匹配设备名
        where += f" AND device_name = '{_escape(device_name.strip())}'"

    order_sql = "DESC" if order.lower() != "asc" else "ASC"

    # count
    count_sql = f"SELECT COUNT(*) AS cnt FROM {db_name}.alarm_records WHERE {where}"
    print(f"[alarm] count SQL: {count_sql}")
    count_rows = await db.fetch(count_sql)
    total = 0
    if count_rows and count_rows[0].get("cnt") is not None:
        total = int(count_rows[0]["cnt"])

    # data with paging
    offset = (page - 1) * page_size
    data_sql = (
        f"SELECT ts, recovery_time, device_name, alarm_content, "
        f"handling_method, operator_name, alarm_level "
        f"FROM {db_name}.alarm_records WHERE {where} "
        f"ORDER BY ts {order_sql} LIMIT {page_size} OFFSET {offset}"
    )
    rows = await db.fetch(data_sql)

    records = []
    for row in rows:
        ts_val = row.get("ts", "")
        if hasattr(ts_val, "strftime"):
            ts_val = format_local_seconds(ts_val)
        records.append({
            "trigger_time":    str(ts_val),
            "recovery_time":   str(row.get("recovery_time", "")),
            "device_name":     str(row.get("device_name", "")),
            "alarm_content":   str(row.get("alarm_content", "")),
            "handling_method": str(row.get("handling_method", "")),
            "operator_name":   str(row.get("operator_name", "")),
            "alarm_level":     str(row.get("alarm_level", "") or ""),
        })

    return {
        "total":     total,
        "page":      page,
        "page_size": page_size,
        "records":   records,
    }
