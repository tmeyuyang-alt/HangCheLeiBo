from __future__ import annotations

from datetime import datetime
from enum import Enum
from typing import Any

from pydantic import BaseModel, Field


class TimeUnit(str, Enum):
    seconds = "seconds"
    minutes = "minutes"
    hours = "hours"
    days = "days"
    months = "months"
    years = "years"
    shift = "shift"


class AggregateType(str, Enum):
    last = "last"
    min = "min"
    max = "max"
    avg = "avg"


class ImportAssetsRequest(BaseModel):
    folder: str
    plc_id: str = "plc01"


class ImportAssetsResponse(BaseModel):
    imported_devices: int
    imported_points: int


class RandomDataGenerateRequest(BaseModel):
    start_at: datetime
    duration_value: int = Field(ge=1)
    duration_unit: TimeUnit
    plc_id: str = "plc01"
    device_name_contains: str | None = None
    device_name_contains_any: list[str] | None = None  # 兼容旧版
    devices: list[str] | None = None     # 设备名过滤列表（OR），匹配 device_name
    conditions: list[str] | None = None  # 指标名过滤列表（OR），匹配 display_name
    step_seconds: int = Field(default=1, ge=1, le=3600)
    overwrite: bool = True
    seed: int | None = None


class RandomDataGenerateResponse(BaseModel):
    plc_id: str
    points_count: int
    rows_inserted: int
    start_at: datetime
    end_at: datetime
    step_seconds: int
    overwrite: bool


class HistoryQueryRequest(BaseModel):
    start_at: datetime
    duration_value: int = Field(ge=1)
    duration_unit: TimeUnit
    interval_value: int = Field(ge=1)
    interval_unit: TimeUnit
    aggregate: AggregateType = AggregateType.last
    plc_id: str = "plc01"
    device_name_contains: str | None = None
    device_name_contains_any: list[str] | None = None  # 兼容旧版
    devices: list[str] | None = None     # 设备名过滤列表（OR），匹配 device_name
    conditions: list[str] | None = None  # 指标名过滤列表（OR），匹配 display_name


class QueryColumn(BaseModel):
    field: str
    label: str
    point_id: int | None = None
    device_name: str | None = None
    point_name: str | None = None


class HistoryQueryResponse(BaseModel):
    start_at: datetime
    end_at: datetime
    interval_label: str
    aggregate: AggregateType
    columns: list[QueryColumn]
    rows: list[dict[str, Any]]


class HourWindowResponse(HistoryQueryResponse):
    pass


class AlarmOrderType(str, Enum):
    desc = "desc"   # 倒序（默认，最新在前）
    asc  = "asc"    # 正序（最早在前）


class AlarmRecordRequest(BaseModel):
    """Unity 端提交已处理的报警记录"""
    trigger_time: str          # 报警触发时间
    recovery_time: str         # 报警恢复时间
    device_name: str           # 报警设备名称
    alarm_content: str         # 报警内容
    handling_method: str       # 处理方法
    operator_name: str         # 操作人员
    alarm_level: str = ""      # 报警级别（None / Level1 / Level2 / Level3）
    plc_id: str = "plc01"


class AlarmRecordQueryRequest(BaseModel):
    """查询报警历史"""
    start_at: datetime
    end_at: datetime | None = None
    device_name: str | None = None    # 精确匹配设备名（为空则不过滤）
    plc_id: str = "plc01"
    order: AlarmOrderType = AlarmOrderType.desc   # 排序方式
    page: int = Field(default=1, ge=1)
    page_size: int = Field(default=50, ge=1, le=500)


class AlarmRecordItem(BaseModel):
    trigger_time: str
    recovery_time: str
    device_name: str
    alarm_content: str
    handling_method: str
    operator_name: str
    alarm_level: str = ""


class AlarmRecordQueryResponse(BaseModel):
    total: int
    page: int
    page_size: int
    records: list[AlarmRecordItem]


# ---------------------------------------------------------------------------
# 操作日志
# ---------------------------------------------------------------------------

class OperationLogRequest(BaseModel):
    """Unity 端提交一条 PLC 写操作日志"""
    op_time: str            # 操作时间，格式 "yyyy-MM-dd HH:mm:ss"（客户端本地时间）
    operator_name: str      # 操作人员姓名
    address: str            # PLC 写入地址，如 "DB1.DBX0.0"
    value: str              # 写入的值（字符串）
    description: str = ""   # 操作描述，如 "1#称 启动"
    plc_id: str = "plc01"


class OperationLogQueryRequest(BaseModel):
    """查询操作日志"""
    start_at: datetime
    end_at: datetime | None = None
    operator_name: str | None = None    # 精确匹配操作人员（为空则不过滤）
    address_contains: str | None = None # 地址模糊匹配（为空则不过滤）
    plc_id: str = "plc01"
    order: AlarmOrderType = AlarmOrderType.desc
    page: int = Field(default=1, ge=1)
    page_size: int = Field(default=50, ge=1, le=500)


class OperationLogItem(BaseModel):
    op_time: str
    operator_name: str
    address: str
    value: str
    description: str = ""


class OperationLogQueryResponse(BaseModel):
    total: int
    page: int
    page_size: int
    records: list[OperationLogItem]


# ---------------------------------------------------------------------------
# 进料统计
# ---------------------------------------------------------------------------

class FeedScaleValue(BaseModel):
    """单个称的进料量"""
    name: str           # 称名称，如 "1#称统计"
    value: float        # 进料量（kg 或 t，与 PLC 原始单位一致）


class FeedDeviceStats(BaseModel):
    """单台设备（炉/称系统）的进料统计"""
    device_name:  str                    # 如 "1#炉"
    scales:       list[FeedScaleValue]   # 各称明细
    device_total: float                  # 本设备各称之和


class FeedShiftStats(BaseModel):
    """单个班次的进料统计"""
    shift_name: str                    # "白班" / "夜班"
    shift_key:  str                    # "previous" / "current"（兼容旧客户端）
    start_time: str
    end_time:   str
    devices:    list[FeedDeviceStats]


class DailyFeedResponse(BaseModel):
    """日进料统计响应"""
    date:         str                    # 显示日期，如 "2024-01-15"
    plc_id:       str
    shifts:       list[FeedShiftStats]   # 各班次明细
    daily_totals: list[FeedDeviceStats]  # 全日合计（各班之和）
