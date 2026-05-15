from __future__ import annotations

import asyncio
import contextlib
from datetime import datetime, timedelta, timezone
from typing import Any
from urllib.parse import parse_qs

from fastapi import FastAPI, HTTPException, Query, Request

from .config import settings
from .config_import import import_assets, import_config_json, get_latest_config_snapshot
from .db import db
from .fake_data import generate_random_history_data
from .plc import collect_once
from .query import run_history_query
from .alarm import insert_alarm_record, query_alarm_records
from .feed import get_daily_stats, get_shift_summary_compat
from .operation_log import insert_operation_log, query_operation_logs
from .schemas import (
    AggregateType,
    AlarmOrderType,
    AlarmRecordQueryRequest,
    AlarmRecordQueryResponse,
    AlarmRecordRequest,
    DailyFeedResponse,
    HistoryQueryRequest,
    HistoryQueryResponse,
    HourWindowResponse,
    ImportAssetsRequest,
    ImportAssetsResponse,
    OperationLogQueryRequest,
    OperationLogQueryResponse,
    OperationLogRequest,
    RandomDataGenerateRequest,
    RandomDataGenerateResponse,
    TimeUnit,
)

app = FastAPI(title="History Service", version="0.2.0")
collector_task: asyncio.Task | None = None


async def _collector_loop() -> None:
    interval = max(1, settings.collect_interval_seconds)
    next_run = asyncio.get_running_loop().time()
    while True:
        started_at = asyncio.get_running_loop().time()
        try:
            count = await collect_once()
            elapsed = asyncio.get_running_loop().time() - started_at
            print(f"[collector] collected {count} rows in {elapsed:.3f}s")
        except Exception as exc:  # pragma: no cover
            print(f"[collector] collect failed: {exc}")
        next_run += interval
        sleep_seconds = next_run - asyncio.get_running_loop().time()
        if sleep_seconds <= 0:
            print(f"[collector] collect cycle is slower than interval={interval}s")
            next_run = asyncio.get_running_loop().time()
            continue
        await asyncio.sleep(sleep_seconds)


@app.on_event("startup")
async def startup() -> None:
    global collector_task

    await db.connect()
    if settings.auto_init_schema:
        await db.init_schema()

    if settings.auto_start_collector:
        collector_task = asyncio.create_task(_collector_loop())
        print(f"[collector] auto started, interval={settings.collect_interval_seconds}s")
    else:
        print("[collector] auto start disabled")


@app.on_event("shutdown")
async def shutdown() -> None:
    global collector_task

    if collector_task is not None:
        collector_task.cancel()
        with contextlib.suppress(asyncio.CancelledError):
            await collector_task
        collector_task = None

    await db.disconnect()


@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/api/v1/config/import-assets", response_model=ImportAssetsResponse)
async def import_assets_endpoint(request: ImportAssetsRequest) -> ImportAssetsResponse:
    try:
        imported_devices, imported_points = await import_assets(request.folder, request.plc_id)
    except FileNotFoundError as exc:
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    return ImportAssetsResponse(
        imported_devices=imported_devices,
        imported_points=imported_points,
    )


@app.post("/api/v1/config/import-json", response_model=ImportAssetsResponse)
async def import_json_endpoint(request: dict[str, Any]) -> ImportAssetsResponse:
    plc_id = str(request.get("plcId") or "plc01").strip() or "plc01"
    config_payload = request.get("config")
    if not isinstance(config_payload, dict):
        raise HTTPException(status_code=400, detail="Missing config payload")

    try:
        imported_devices, imported_points = await import_config_json(config_payload, plc_id)
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    return ImportAssetsResponse(
        imported_devices=imported_devices,
        imported_points=imported_points,
    )


@app.get("/api/v1/config/latest")
async def get_latest_config(plc_id: str = Query(default="plc01")) -> dict[str, Any]:
    """获取指定 PLC 最新上传的完整 JSON 配置快照（从本地文件读取）。"""
    snapshot = get_latest_config_snapshot(plc_id)
    if snapshot is None:
        raise HTTPException(status_code=404, detail=f"No config snapshot found for plc_id={plc_id}")
    return snapshot


@app.get("/api/v1/debug/query-history-points")
async def debug_query_history_points(
    plc_id: str = Query(default="plc01"),
) -> dict[str, Any]:
    """调试接口：直接执行多种查询，返回 Python 实际看到的结果。"""
    db_name = settings.tdengine_database
    results: dict[str, Any] = {}

    queries = {
        "count": f"SELECT COUNT(*) FROM {db_name}.history_points",
        "count_filtered": f"SELECT COUNT(*) FROM {db_name}.history_points WHERE plc_id = '{plc_id}'",
        "count_enabled": f"SELECT COUNT(*) FROM {db_name}.history_points WHERE plc_id = '{plc_id}' AND is_enabled = 1",
        "sample_rows": f"SELECT tbname, device_name, plc_id, display_name, address, data_type FROM {db_name}.history_points LIMIT 3",
        "partition_rows": (
            f"SELECT tbname, device_name, plc_id, display_name, address, data_type "
            f"FROM {db_name}.history_points "
            f"WHERE plc_id = '{plc_id}' AND is_enabled = 1 "
            f"PARTITION BY tbname LIMIT 1"
        ),
    }

    for key, sql in queries.items():
        try:
            rows = await db.fetch(sql)
            results[key] = {"rows": rows, "count": len(rows)}
        except Exception as exc:
            results[key] = {"error": str(exc)}

    return results


@app.post("/api/v1/debug/generate-random-data", response_model=RandomDataGenerateResponse)
async def generate_random_data(request: RandomDataGenerateRequest) -> RandomDataGenerateResponse:
    try:
        payload = await generate_random_history_data(request)
    except ValueError as exc:
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    return RandomDataGenerateResponse(**payload)


@app.post("/api/v1/debug/generate-random-data-quick", response_model=RandomDataGenerateResponse)
async def generate_random_data_quick(
    plc_id: str = Query(default="plc01"),
) -> RandomDataGenerateResponse:
    """无需传参：自动生成当前时间前 6 小时的随机数据，每秒一条，覆盖写入。"""
    now = datetime.now(tz=timezone.utc)
    request = RandomDataGenerateRequest(
        start_at=now - timedelta(hours=6),
        duration_value=6,
        duration_unit=TimeUnit.hours,
        plc_id=plc_id,
        step_seconds=1,
        overwrite=True,
    )
    try:
        payload = await generate_random_history_data(request)
    except ValueError as exc:
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    return RandomDataGenerateResponse(**payload)


@app.post("/api/v1/collector/run-once")
async def run_collector_once(plc_id: str | None = None) -> dict[str, int]:
    try:
        count = await collect_once(plc_id)
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc
    return {"written_rows": count}


@app.post("/api/v1/history/query", response_model=HistoryQueryResponse)
async def history_query(request: HistoryQueryRequest) -> HistoryQueryResponse:
    try:
        payload = await run_history_query(request)
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    return HistoryQueryResponse(**payload)


@app.get("/data/hour_window", response_model=HourWindowResponse)
async def hour_window(
    request: Request,
    plc: str = Query(default="plc01"),
    ts: datetime = Query(..., description="start time"),
    device_name_contains: str | None = Query(default=None),
    device_name_contains_any: str | None = Query(default=None),
    aggregate: AggregateType = Query(default=AggregateType.last),
) -> HourWindowResponse:
    parsed_query = parse_qs(request.scope.get("query_string", b"").decode("utf-8", errors="ignore"), keep_blank_values=False)
    parsed_any = parsed_query.get("device_name_contains_any") or []
    if not parsed_any and device_name_contains_any:
        parsed_any = [device_name_contains_any]

    history_request = HistoryQueryRequest(
        start_at=ts if ts.tzinfo else ts.replace(tzinfo=timezone.utc),
        duration_value=1,
        duration_unit=TimeUnit.hours,
        interval_value=1,
        interval_unit=TimeUnit.seconds,
        aggregate=aggregate,
        plc_id=plc,
        device_name_contains=device_name_contains,
        device_name_contains_any=parsed_any or None,
    )
    payload = await run_history_query(history_request)
    return HourWindowResponse(**payload)


@app.post("/api/v1/alarm/record")
async def create_alarm_record(request: AlarmRecordRequest) -> dict[str, str]:
    """Unity 端提交一条已处理的报警记录。"""
    try:
        await insert_alarm_record(
            trigger_time=request.trigger_time,
            recovery_time=request.recovery_time,
            device_name=request.device_name,
            alarm_content=request.alarm_content,
            handling_method=request.handling_method,
            operator_name=request.operator_name,
            alarm_level=request.alarm_level,
            plc_id=request.plc_id,
        )
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    return {"status": "ok"}


@app.post("/api/v1/operation/log")
async def create_operation_log(request: OperationLogRequest) -> dict[str, str]:
    """Unity 端提交一条 PLC 写操作日志（fire-and-forget，不影响主流程）。"""
    try:
        await insert_operation_log(
            op_time=request.op_time,
            operator_name=request.operator_name,
            address=request.address,
            value=request.value,
            description=request.description,
            plc_id=request.plc_id,
        )
    except Exception as exc:
        # 日志上传失败不应中断客户端，仅服务端打印错误
        print(f"[operation_log] 插入失败: {exc}")
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    return {"status": "ok"}


@app.post("/api/v1/operation/history", response_model=OperationLogQueryResponse)
async def query_operation_history(request: OperationLogQueryRequest) -> OperationLogQueryResponse:
    """查询 PLC 写操作日志，支持时间范围、操作人员、地址模糊匹配、分页、排序。"""
    try:
        payload = await query_operation_logs(
            start_at=request.start_at,
            end_at=request.end_at,
            operator_name=request.operator_name,
            address_contains=request.address_contains,
            plc_id=request.plc_id,
            order=request.order.value,
            page=request.page,
            page_size=request.page_size,
        )
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    return OperationLogQueryResponse(**payload)


@app.post("/api/v1/alarm/history", response_model=AlarmRecordQueryResponse)
async def query_alarm_history(request: AlarmRecordQueryRequest) -> AlarmRecordQueryResponse:
    """查询报警历史记录，支持设备名过滤和正序/倒序排列。"""
    print(
        f"[alarm/history] 收到查询请求 | "
        f"start_at={request.start_at} | "
        f"end_at={request.end_at} | "
        f"device_name={request.device_name!r} | "
        f"plc_id={request.plc_id!r} | "
        f"order={request.order} | "
        f"page={request.page} | "
        f"page_size={request.page_size}"
    )
    try:
        payload = await query_alarm_records(
            start_at=request.start_at,
            end_at=request.end_at,
            device_name=request.device_name,
            plc_id=request.plc_id,
            order=request.order.value,
            page=request.page,
            page_size=request.page_size,
        )
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    return AlarmRecordQueryResponse(**payload)


# ---------------------------------------------------------------------------
# 进料统计接口
# ---------------------------------------------------------------------------

@app.get("/api/v1/feed/daily-stats", response_model=DailyFeedResponse)
async def daily_feed_stats(
    date: str   = Query(..., description="报表日期 yyyy-MM-dd（程序内部用 date+1 传入）"),
    plc_id: str = Query(default="plc01"),
) -> DailyFeedResponse:
    """
    查询指定日期的日进料统计（白班 + 夜班 + 全日合计）。

    - date 参数含义：与 /shift/summary 保持一致。
      客户端实际显示日期 = date - 1 天。
      例如想查 2024-01-15 的数据，传 date=2024-01-16。
    - 班进料 = 该班时间窗口内 PLC 累计值的最大值（值归零后分段求和）。
    - 日进料 = 白班 + 夜班。
    """
    try:
        from datetime import date as _date
        report_date = _date.fromisoformat(date)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=f"日期格式错误，应为 yyyy-MM-dd: {exc}") from exc

    try:
        payload = await get_daily_stats(report_date, plc_id)
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc

    return DailyFeedResponse(**payload)


@app.get("/shift/summary")
async def shift_summary_compat(
    date: str   = Query(..., description="报表日期 yyyy-MM-dd"),
    plc: str    = Query(default="plc01"),
) -> dict:
    """
    兼容旧版 DailyFeedReportExporter.cs 的接口。
    响应格式：{ plc, date, record_ts, rows:[{furnace,material,current_shift,previous_shift}] }
    """
    try:
        from datetime import date as _date
        report_date = _date.fromisoformat(date)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=f"日期格式错误: {exc}") from exc

    try:
        payload = await get_shift_summary_compat(report_date, plc)
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc

    return payload
