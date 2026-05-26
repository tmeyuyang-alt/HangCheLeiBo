from __future__ import annotations

from datetime import datetime, timedelta, timezone, tzinfo
from zoneinfo import ZoneInfo, ZoneInfoNotFoundError

from .config import settings


def app_timezone() -> tzinfo:
    """Return the configured business timezone, defaulting to China local time."""
    name = (settings.app_timezone or "Asia/Shanghai").strip() or "Asia/Shanghai"

    try:
        return ZoneInfo(name)
    except ZoneInfoNotFoundError:
        if name.upper() in {"UTC", "Z"}:
            return timezone.utc
        # Some slim/offline deployments may not include tzdata. Keep China time correct.
        return timezone(timedelta(hours=8), name)


def now_local() -> datetime:
    return datetime.now(app_timezone())


def to_local_aware(value: datetime) -> datetime:
    if value.tzinfo is None:
        return value.replace(tzinfo=app_timezone())
    return value.astimezone(app_timezone())


def to_local_naive(value: datetime) -> datetime:
    return to_local_aware(value).replace(tzinfo=None)


def format_tdengine_timestamp(value: datetime) -> str:
    local_value = to_local_naive(value)
    return local_value.strftime("%Y-%m-%d %H:%M:%S.") + f"{local_value.microsecond // 1000:03d}"


def format_local_seconds(value: datetime) -> str:
    return to_local_naive(value).strftime("%Y-%m-%d %H:%M:%S")
