#!/usr/bin/env python3
"""Test YunKuaiDao order shipment API.

Usage:
  set YKD_COOKIE=eweishop-user=...; is_expired=0; shopId=1; warehose_id=0; storeId=
  python server/tools/ykd_auto_send_order.py 118667 --remark test
  python server/tools/ykd_auto_send_order.py 118667 --remark test --send
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import urllib.error
import urllib.parse
import urllib.request
from typing import Any


BASE_URL = "https://shop.yunkuaidao.com"
SEND_PATH = "/shop/manage/order/op/send"


def build_headers(cookie: str) -> dict[str, str]:
    return {
        "Accept": "application/json, text/plain, */*",
        "Accept-Language": "zh-CN,zh;q=0.9,en;q=0.8",
        "Content-Type": "application/json",
        "Cookie": cookie,
        "Origin": BASE_URL,
        "Referer": f"{BASE_URL}/shop",
        "Shop-Id": "undefined",
        "User-Agent": (
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
            "AppleWebKit/537.36 (KHTML, like Gecko) "
            "Chrome/148.0.0.0 Safari/537.36"
        ),
        "Version": "6.17.14",
        "Wap-Live": "wap-live-share",
        "X-Requested-With": "XMLHttpRequest",
    }


def request_json(
    url: str,
    headers: dict[str, str],
    method: str = "GET",
    payload: dict[str, Any] | None = None,
    timeout: int = 20,
) -> dict[str, Any]:
    body = None
    if payload is not None:
        body = json.dumps(payload, ensure_ascii=False, separators=(",", ":")).encode("utf-8")

    request = urllib.request.Request(url, data=body, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            charset = response.headers.get_content_charset() or "utf-8"
            text = response.read().decode(charset, errors="replace")
    except urllib.error.HTTPError as exc:
        error_text = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"HTTP {exc.code}: {error_text}") from exc
    except urllib.error.URLError as exc:
        raise RuntimeError(f"request failed: {exc.reason}") from exc

    try:
        data = json.loads(text)
    except json.JSONDecodeError as exc:
        raise RuntimeError(f"response is not JSON: {text[:500]}") from exc

    if not isinstance(data, dict):
        raise RuntimeError(f"unexpected JSON response: {data!r}")
    return data


def get_order_detail(order_id: str, headers: dict[str, str]) -> dict[str, Any]:
    query = urllib.parse.urlencode({"id": order_id})
    return request_json(f"{BASE_URL}{SEND_PATH}?{query}", headers)


def build_send_payload(order_id: str, detail: dict[str, Any], remark: str) -> dict[str, Any]:
    if str(detail.get("error", "")) not in ("0", ""):
        raise RuntimeError(f"order detail API returned error: {detail}")

    order_goods = detail.get("order_goods")
    if not isinstance(order_goods, list) or not order_goods:
        raise RuntimeError("order detail has no order_goods")

    send_goods: dict[str, int] = {}
    for item in order_goods:
        if not isinstance(item, dict):
            continue
        goods_id = item.get("id")
        can_send = int(item.get("can_send") or 0)
        can_send_count = int(item.get("can_send_count") or 0)
        if goods_id and can_send and can_send_count > 0:
            send_goods[str(goods_id)] = can_send_count

    if not send_goods:
        raise RuntimeError("no shippable goods found in this order")

    return {
        "id": str(order_id),
        "order_goods_id": send_goods,
        "order_goods_weight": {},
        "no_express": "0",
        "express_id": "",
        "express_sn": "",
        "remark": remark,
        "city_distribution_type": 0,
        "is_retry": "0",
        "subpackage": 1,
        "contains_gift": 2,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Fetch order detail and optionally submit shipment.")
    parser.add_argument("order_id", help="order id, for example 118667")
    parser.add_argument("--cookie", default=os.getenv("YKD_COOKIE"), help="login Cookie, or env YKD_COOKIE")
    parser.add_argument("--remark", default="test", help="shipment remark, default: test")
    parser.add_argument("--send", action="store_true", help="submit shipment; without it, only print payload")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if not args.cookie:
        print("missing Cookie: pass --cookie or set env YKD_COOKIE.", file=sys.stderr)
        return 2

    headers = build_headers(args.cookie)
    detail = get_order_detail(args.order_id, headers)
    payload = build_send_payload(args.order_id, detail, args.remark)

    print("Order detail summary:")
    print(f"  order_id: {args.order_id}")
    print(f"  buyer_name: {detail.get('buyer_name', '')}")
    print(f"  buyer_mobile: {detail.get('buyer_mobile', '')}")
    print(f"  address: {detail.get('address', '')}")
    print("POST JSON to submit:")
    print(json.dumps(payload, ensure_ascii=False, indent=2))

    if not args.send:
        print("Dry-run only. Add --send to submit shipment.")
        return 0

    result = request_json(f"{BASE_URL}{SEND_PATH}", headers, method="POST", payload=payload)
    print("Shipment API response:")
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if str(result.get("error", "0")) == "0" else 1


if __name__ == "__main__":
    raise SystemExit(main())
