from __future__ import annotations

from datetime import datetime

from .config import settings
from .db import db


def _escape(s: str) -> str:
    return s.replace("\\", "\\\\").replace("'", "\\'")


def _to_local_naive(dt: datetime) -> datetime:
    """将 UTC-aware datetime 转为本地 naive datetime，与 TDengine 本地时间保持一致。"""
    if dt.tzinfo is None:
        return dt
    return dt.astimezone().replace(tzinfo=None)


async def insert_operation_log(
    op_time: str,
    operator_name: str,
    address: str,
    value: str,
    description: str = "",
    plc_id: str = "plc01",
) -> None:
    """插入一条 PLC 写操作日志。

    Args:
        op_time:       操作时间字符串，支持 "yyyy-MM-dd HH:mm:ss" 或含毫秒的
                       "yyyy-MM-dd HH:mm:ss.fff"（本地时间，由 Unity 客户端生成）
        operator_name: 操作人员姓名（来自 LoginManager.CurrentUser.name）
        address:       写入的 PLC 地址，如 "DB1.DBX0.0"
        value:         写入的值（字符串形式）
        description:   操作描述，如 "1#称 启动"
        plc_id:        PLC 编号
    """
    db_name = settings.tdengine_database
    sub_table = f"oplog_{_escape(plc_id)}"
    sql = (
        f"INSERT INTO {db_name}.{sub_table} USING {db_name}.operation_logs "
        f"TAGS ('{_escape(plc_id)}') "
        f"(ts, operator_name, address, write_value, description) "
        f"VALUES ("
        f"'{_escape(op_time)}', "
        f"'{_escape(operator_name)}', "
        f"'{_escape(address)}', "
        f"'{_escape(value)}', "
        f"'{_escape(description)}'"
        f")"
    )
    await db.execute(sql)


async def query_operation_logs(
    start_at: datetime,
    end_at: datetime | None = None,
    operator_name: str | None = None,
    address_contains: str | None = None,
    plc_id: str = "plc01",
    order: str = "desc",       # "asc" | "desc"
    page: int = 1,
    page_size: int = 50,
) -> dict:
    """分页查询操作日志。

    Args:
        start_at:         查询开始时间
        end_at:           查询结束时间（默认当前时间）
        operator_name:    精确匹配操作人员姓名（为空则不过滤）
        address_contains: 模糊匹配 PLC 地址（为空则不过滤）
        plc_id:           PLC 编号
        order:            "asc" 正序 / "desc" 倒序
        page:             页码（从 1 开始）
        page_size:        每页条数
    """
    db_name = settings.tdengine_database

    start_local = _to_local_naive(start_at)
    end_local   = _to_local_naive(end_at) if end_at is not None else datetime.now()

    start_str = start_local.strftime("%Y-%m-%d %H:%M:%S")
    end_str   = end_local.strftime("%Y-%m-%d %H:%M:%S")

    where = f"ts >= '{start_str}' AND ts <= '{end_str}'"
    if plc_id:
        where += f" AND plc_id = '{_escape(plc_id)}'"
    if operator_name and operator_name.strip():
        where += f" AND operator_name = '{_escape(operator_name.strip())}'"
    if address_contains and address_contains.strip():
        where += f" AND address LIKE '%{_escape(address_contains.strip())}%'"

    order_sql = "DESC" if order.lower() != "asc" else "ASC"

    # 总条数
    count_sql = f"SELECT COUNT(*) AS cnt FROM {db_name}.operation_logs WHERE {where}"
    print(f"[operation_log] count SQL: {count_sql}")
    count_rows = await db.fetch(count_sql)
    total = 0
    if count_rows and count_rows[0].get("cnt") is not None:
        total = int(count_rows[0]["cnt"])

    # 分页数据
    offset   = (page - 1) * page_size
    data_sql = (
        f"SELECT ts, operator_name, address, write_value, description "
        f"FROM {db_name}.operation_logs WHERE {where} "
        f"ORDER BY ts {order_sql} LIMIT {page_size} OFFSET {offset}"
    )
    rows = await db.fetch(data_sql)

    records = []
    for row in rows:
        ts_val = row.get("ts", "")
        if hasattr(ts_val, "strftime"):
            ts_val = ts_val.strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]  # 保留毫秒（去掉微秒）
        records.append({
            "op_time":       str(ts_val),
            "operator_name": str(row.get("operator_name", "")),
            "address":       str(row.get("address", "")),
            "value":         str(row.get("write_value", "")),
            "description":   str(row.get("description", "") or ""),
        })

    return {
        "total":     total,
        "page":      page,
        "page_size": page_size,
        "records":   records,
    }
