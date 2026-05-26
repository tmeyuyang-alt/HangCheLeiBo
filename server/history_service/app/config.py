from __future__ import annotations

import os
from dataclasses import dataclass


def _get_bool(name: str, default: bool) -> bool:
    value = os.getenv(name)
    if value is None:
        return default
    return value.strip().lower() in {"1", "true", "yes", "on"}


@dataclass(frozen=True)
class Settings:
    tdengine_url: str = os.getenv(
        "TDENGINE_URL",
        "taosws://root:taosdata@127.0.0.1:6041",
    )
    tdengine_database: str = os.getenv("TDENGINE_DATABASE", "history_service")
    auto_init_schema: bool = _get_bool("AUTO_INIT_SCHEMA", True)
    auto_start_collector: bool = _get_bool("AUTO_START_COLLECTOR", True)
    collect_interval_seconds: int = int(os.getenv("COLLECT_INTERVAL_SECONDS", "1"))
    plc_rack: int = int(os.getenv("PLC_RACK", "0"))
    plc_slot: int = int(os.getenv("PLC_SLOT", "1"))
    plc_tcp_port: int = int(os.getenv("PLC_TCP_PORT", "102"))
    plc_connect_timeout_seconds: int = int(os.getenv("PLC_CONNECT_TIMEOUT_SECONDS", "3"))
    plc_max_read_bytes: int = int(os.getenv("PLC_MAX_READ_BYTES", "200"))
    shift_start_hour: int = int(os.getenv("SHIFT_START_HOUR", "8"))
    shift_length_hours: int = int(os.getenv("SHIFT_LENGTH_HOURS", "12"))
    # 应用业务时区。TDengine 的时间戳字符串按本地业务时间写入/查询。
    app_timezone: str = os.getenv("APP_TIMEZONE", "Asia/Shanghai")
    # 数据保留天数
    data_keep_days: int = int(os.getenv("DATA_KEEP_DAYS", "730"))
    # 配置快照存储目录（JSON 文件，不写入数据库）
    config_snapshot_dir: str = os.getenv("CONFIG_SNAPSHOT_DIR", "/opt/config_snapshots")


settings = Settings()
