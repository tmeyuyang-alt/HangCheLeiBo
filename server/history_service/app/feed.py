from __future__ import annotations

from datetime import date, datetime, time, timedelta
from typing import Any

from .config import settings
from .db import db


# ---------------------------------------------------------------------------
# 配置：与 DeviceSignalConfigs.json 保持一致
# ---------------------------------------------------------------------------

# (TDengine 中的 device_name TAG, 响应中使用的短名称)
FEED_DEVICES: list[tuple[str, str]] = [
    ("班进料统计_1#炉", "1#炉"),
    ("班进料统计_2#炉", "2#炉"),
    ("班进料统计_称",   "称"),
]

FEED_POINTS: list[str] = [
    "1#称统计", "2#称统计", "3#称统计",
    "4#称统计", "5#称统计", "6#称统计",
]


def _escape(s: str) -> str:
    return s.replace("\\", "\\\\").replace("'", "\\'")


def _fmt(dt: datetime) -> str:
    """格式化为 TDengine 接受的本地时间字符串（毫秒精度）。"""
    return dt.strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]


# ---------------------------------------------------------------------------
# 班次窗口计算
# ---------------------------------------------------------------------------

def shift_windows(report_date: date) -> list[dict[str, Any]]:
    """
    给定"报表日期" D，返回两个班次的时间窗口。

    约定（与现有 DailyFeedReportExporter 客户端一致）：
        白班（previous）: D-1  08:00 → D-1  20:00
        夜班（current） : D-1  20:00 → D    08:00

    report_date 对应客户端传入的 date+1。
    """
    shift_h = settings.shift_start_hour   # 默认 8
    shift_l = settings.shift_length_hours  # 默认 12

    d_prev = report_date - timedelta(days=1)

    day_start  = datetime.combine(d_prev, time(shift_h, 0, 0))
    day_end    = day_start + timedelta(hours=shift_l)
    night_end  = day_end + timedelta(hours=shift_l)

    return [
        {
            "shift_name": "白班",
            "shift_key":  "previous",
            "start": day_start,
            "end":   day_end,
        },
        {
            "shift_name": "夜班",
            "shift_key":  "current",
            "start": day_end,
            "end":   night_end,
        },
    ]


# ---------------------------------------------------------------------------
# 核心计算：从 point_samples 取单点单班的进料量
# ---------------------------------------------------------------------------

async def _point_shift_total(
    device_name: str,
    display_name: str,
    start: datetime,
    end: datetime,
    plc_id: str,
) -> float:
    """
    计算某个称在某个班次窗口内的进料量。

    算法：
      - 班次开始时 PLC 归零，期间单调递增，班次结束时 = 本班总量。
      - 正常情况：shift_total = MAX(numeric_value) in window
      - 若班内发生意外归零（断电等），用"分段求和"：
        遍历时序数据，检测到大幅下降时视为归零，累加各段 MAX。

    Reset 判定：新值 < 上一段 MAX × reset_ratio
    """
    db_name = settings.tdengine_database

    sql = (
        f"SELECT ts, numeric_value "
        f"FROM {db_name}.point_samples "
        f"WHERE ts >= '{_fmt(start)}' AND ts < '{_fmt(end)}' "
        f"AND device_name = '{_escape(device_name)}' "
        f"AND display_name = '{_escape(display_name)}' "
        f"AND plc_id = '{_escape(plc_id)}' "
        f"ORDER BY ts ASC"
    )

    rows = await db.fetch(sql)
    if not rows:
        return 0.0

    values = [
        float(r["numeric_value"])
        for r in rows
        if r.get("numeric_value") is not None
    ]
    if not values:
        return 0.0

    # --- 分段求和（鲁棒版 MAX）---
    RESET_RATIO = 0.5   # 下降超过上一段最大值的 50% 视为归零
    total   = 0.0
    seg_max = values[0]
    prev    = values[0]

    for v in values[1:]:
        if v < prev * RESET_RATIO and prev > 0:
            total  += seg_max
            seg_max = v
        else:
            if v > seg_max:
                seg_max = v
        prev = v

    total += seg_max
    return round(total, 4)


# ---------------------------------------------------------------------------
# 对外接口
# ---------------------------------------------------------------------------

async def get_daily_stats(
    report_date: date,
    plc_id: str = "plc01",
) -> dict[str, Any]:
    """
    计算报表日期 D 的日进料统计。

    返回结构：
    {
      "date":        "2024-01-15",   # 显示日期 = report_date - 1
      "plc_id":      "plc01",
      "shifts": [
        {
          "shift_name": "白班",
          "start_time": "...",
          "end_time":   "...",
          "devices": [
            {
              "device_name": "1#炉",
              "scales": [{"name":"1#称统计","value":123.4}, ...],
              "device_total": 789.0
            },
            ...
          ]
        },
        { "shift_name": "夜班", ... }
      ],
      "daily_totals": [
        {
          "device_name": "1#炉",
          "scales": [{"name":"1#称统计","value":246.8}, ...],
          "device_total": 1578.0
        },
        ...
      ]
    }
    """
    windows = shift_windows(report_date)
    display_date = (report_date - timedelta(days=1)).strftime("%Y-%m-%d")

    # 所有班次数据：device_key -> point_name -> shift_key -> value
    # device_key = short name ("1#炉" etc.)
    accumulated: dict[str, dict[str, dict[str, float]]] = {
        short: {pt: {} for pt in FEED_POINTS}
        for _, short in FEED_DEVICES
    }

    shifts_result = []
    for win in windows:
        devices_result = []
        for db_name_dev, short_name in FEED_DEVICES:
            scales = []
            device_total = 0.0
            for pt in FEED_POINTS:
                val = await _point_shift_total(
                    db_name_dev, pt, win["start"], win["end"], plc_id
                )
                scales.append({"name": pt, "value": val})
                device_total += val
                accumulated[short_name][pt][win["shift_key"]] = val

            devices_result.append({
                "device_name":  short_name,
                "scales":       scales,
                "device_total": round(device_total, 4),
            })

        shifts_result.append({
            "shift_name": win["shift_name"],
            "shift_key":  win["shift_key"],
            "start_time": win["start"].strftime("%Y-%m-%d %H:%M:%S"),
            "end_time":   win["end"].strftime("%Y-%m-%d %H:%M:%S"),
            "devices":    devices_result,
        })

    # 日合计
    daily_totals = []
    for _, short_name in FEED_DEVICES:
        scales = []
        dev_total = 0.0
        for pt in FEED_POINTS:
            day_val = sum(accumulated[short_name][pt].values())
            day_val = round(day_val, 4)
            scales.append({"name": pt, "value": day_val})
            dev_total += day_val

        daily_totals.append({
            "device_name":  short_name,
            "scales":       scales,
            "device_total": round(dev_total, 4),
        })

    return {
        "date":         display_date,
        "plc_id":       plc_id,
        "shifts":       shifts_result,
        "daily_totals": daily_totals,
    }


async def get_shift_summary_compat(
    report_date: date,
    plc_id: str = "plc01",
) -> dict[str, Any]:
    """
    兼容旧版 DailyFeedReportExporter 客户端的 /shift/summary 响应格式。

    response:
    {
      "plc":       "plc01",
      "date":      "2024-01-15",
      "record_ts": "...",
      "rows": [
        { "furnace":"1#炉", "material":"1#称统计",
          "current_shift": 夜班值, "previous_shift": 白班值 },
        ...
      ]
    }
    """
    stats = await get_daily_stats(report_date, plc_id)
    rows = []

    for shift in stats["shifts"]:
        for dev in shift["devices"]:
            for scale in dev["scales"]:
                # 找到对应的 rows 条目，按 furnace+material 唯一
                key = (dev["device_name"], scale["name"])
                existing = next((r for r in rows if (r["furnace"], r["material"]) == key), None)
                if existing is None:
                    existing = {
                        "furnace":        dev["device_name"],
                        "material":       scale["name"],
                        "current_shift":  0.0,
                        "previous_shift": 0.0,
                    }
                    rows.append(existing)
                existing[f"{shift['shift_key']}_shift"] = scale["value"]

    return {
        "plc":       plc_id,
        "date":      stats["date"],
        "record_ts": datetime.now().strftime("%Y-%m-%dT%H:%M:%S"),
        "rows":      rows,
    }
