from __future__ import annotations

import asyncio
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime
from typing import Any, Sequence

import taosws

from .config import settings


class Database:
    """TDengine database wrapper using WebSocket (taosws) driver."""

    def __init__(self) -> None:
        self._conn: taosws.Connection | None = None
        self._executor = ThreadPoolExecutor(max_workers=10)

    # ------------------------------------------------------------------
    # 连接管理
    # ------------------------------------------------------------------

    async def connect(self, retries: int = 30, delay: float = 2.0) -> None:
        if self._conn is not None:
            return
        loop = asyncio.get_event_loop()
        last_error: Exception | None = None
        for attempt in range(1, retries + 1):
            try:
                self._conn = await loop.run_in_executor(
                    self._executor,
                    lambda: taosws.connect(settings.tdengine_url),
                )
                print(f"[db] connected to TDengine (attempt {attempt})")
                return
            except Exception as e:
                last_error = e
                print(f"[db] TDengine not ready (attempt {attempt}/{retries}), retry in {delay}s ...")
                await asyncio.sleep(delay)
        raise RuntimeError(f"[db] failed to connect after {retries} attempts: {last_error}")

    async def disconnect(self) -> None:
        if self._conn is None:
            return
        try:
            self._conn.close()
        except Exception:
            pass
        self._conn = None

    # ------------------------------------------------------------------
    # 初始化 schema
    # ------------------------------------------------------------------

    async def init_schema(self) -> None:
        db_name = settings.tdengine_database
        keep = settings.data_keep_days
        await self.execute(
            f"CREATE DATABASE IF NOT EXISTS {db_name} "
            f"PRECISION 'ms' KEEP {keep}d"
        )
        await self.execute(f"USE {db_name}")

        # 超级表: devices 配置
        await self.execute(
            """
            CREATE STABLE IF NOT EXISTS devices (
                ts TIMESTAMP,
                _placeholder INT
            ) TAGS (
                plc_id NCHAR(128),
                name NCHAR(255),
                shared_ip_address NCHAR(64),
                source_path NCHAR(512)
            )
            """
        )

        # 超级表: history_points 配置
        await self.execute(
            """
            CREATE STABLE IF NOT EXISTS history_points (
                ts TIMESTAMP,
                _placeholder INT
            ) TAGS (
                device_name NCHAR(255),
                plc_id NCHAR(128),
                display_name NCHAR(255),
                address NCHAR(128),
                data_type INT,
                is_enabled BOOL,
                shared_ip_address NCHAR(64)
            )
            """
        )

        # 超级表: point_samples 采样数据 — 核心时序表
        # 每个 point 一张子表, tag 关联设备信息
        await self.execute(
            """
            CREATE STABLE IF NOT EXISTS point_samples (
                ts TIMESTAMP,
                numeric_value DOUBLE,
                text_value NCHAR(64),
                quality SMALLINT
            ) TAGS (
                point_id NCHAR(64),
                device_name NCHAR(255),
                plc_id NCHAR(128),
                display_name NCHAR(255),
                data_type INT
            )
            """
        )

        # 超级表: alarm_records 报警记录
        await self.execute(
            """
            CREATE STABLE IF NOT EXISTS alarm_records (
                ts TIMESTAMP,
                recovery_time NCHAR(64),
                device_name NCHAR(255),
                alarm_content NCHAR(512),
                handling_method NCHAR(512),
                operator_name NCHAR(128),
                alarm_level NCHAR(32)
            ) TAGS (
                plc_id NCHAR(128)
            )
            """
        )
        # 兼容旧表：若 alarm_level 列不存在则添加（TDengine 列已存在时会报错，忽略即可）
        try:
            await self.execute(
                f"ALTER STABLE {db_name}.alarm_records ADD COLUMN alarm_level NCHAR(32)"
            )
        except Exception:
            pass  # 列已存在，忽略

        # 超级表: operation_logs PLC 写操作日志
        # 注意: value 是 TDengine 保留字，列名改为 write_value
        await self.execute(
            "CREATE STABLE IF NOT EXISTS operation_logs ("
            "ts TIMESTAMP,"
            "operator_name NCHAR(128),"
            "address NCHAR(256),"
            "write_value NCHAR(256),"
            "description NCHAR(512)"
            ") TAGS (plc_id NCHAR(128))"
        )

    # ------------------------------------------------------------------
    # 通用执行方法 (通过 executor 异步化)
    # ------------------------------------------------------------------

    async def execute(self, sql: str) -> None:
        loop = asyncio.get_event_loop()
        await loop.run_in_executor(self._executor, self._sync_execute, sql)

    async def fetch(self, sql: str) -> list[dict[str, Any]]:
        loop = asyncio.get_event_loop()
        return await loop.run_in_executor(self._executor, self._sync_fetch, sql)

    async def fetchrow(self, sql: str) -> dict[str, Any] | None:
        rows = await self.fetch(sql)
        return rows[0] if rows else None

    # ------------------------------------------------------------------
    # 批量插入 (用于 fake_data 和 plc collector)
    # ------------------------------------------------------------------

    async def batch_insert_sql(self, sql: str) -> None:
        """Execute a large INSERT SQL (TDengine supports multi-row INSERT)."""
        await self.execute(sql)

    # ------------------------------------------------------------------
    # 同步内部方法
    # ------------------------------------------------------------------

    def _sync_execute(self, sql: str) -> None:
        assert self._conn is not None, "Database not connected"
        self._conn.execute(sql)

    def _sync_fetch(self, sql: str) -> list[dict[str, Any]]:
        assert self._conn is not None, "Database not connected"
        # taosws: execute() 对 SELECT 返回 int(0)，用 query() 获取结果集
        # TaosResult 不支持 fetch_all()，直接迭代取行
        result = self._conn.query(sql)
        if result is None:
            return []
        fields = result.fields
        if not fields:
            return []
        col_names = [f.name() for f in fields]
        return [dict(zip(col_names, row)) for row in result]


db = Database()
