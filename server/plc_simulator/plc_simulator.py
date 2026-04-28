#!/usr/bin/env python3
"""
S7 PLC Simulator — 模拟西门子 S7 PLC，供 Unity (S7.Net) 连接调试。

功能:
  1. 读取 DeviceSignalConfigs.json 配置文件，自动创建数据块
  2. 实现 ISO-on-TCP (TPKT/COTP/S7) 协议，处理读写请求
  3. 内置 Web 控制面板 (默认 :8080)，可在浏览器中查看/修改信号值
  4. 支持 BOOL 点位一键切换（用于测试报警）

使用方式:
  python plc_simulator.py --config path/to/DeviceSignalConfigs.json --port 102 --web-port 8080

  然后在 Unity 中将 PLC IP 设为本机 IP 即可。
"""

import argparse
import json
import os
import struct
import threading
import socket
import sys
import time
from http.server import HTTPServer, BaseHTTPRequestHandler
from urllib.parse import urlparse, parse_qs
import random
import re

# Windows stdout 默认可能 block-buffered，强制行刷新
sys.stdout.reconfigure(line_buffering=True)

# ======================================================================
# 数据块内存模型
# ======================================================================

class DataBlockManager:
    """管理所有 DB 的字节数组，线程安全。"""

    def __init__(self):
        self._lock = threading.RLock()
        self._blocks: dict[int, bytearray] = {}
        self._points: list[dict] = []  # 配置中的点位信息
        self._point_map: dict[str, dict] = {}  # key → point info

    def ensure_db(self, db_number: int, min_size: int):
        with self._lock:
            if db_number not in self._blocks:
                self._blocks[db_number] = bytearray(min_size)
            elif len(self._blocks[db_number]) < min_size:
                old = self._blocks[db_number]
                self._blocks[db_number] = bytearray(min_size)
                self._blocks[db_number][:len(old)] = old

    def read_bytes(self, db_number: int, start: int, count: int) -> bytes:
        with self._lock:
            block = self._blocks.get(db_number)
            if block is None:
                return bytes(count)
            end = start + count
            if end > len(block):
                # 扩展
                block.extend(bytearray(end - len(block)))
                self._blocks[db_number] = block
            return bytes(block[start:end])

    def write_bytes(self, db_number: int, start: int, data: bytes):
        with self._lock:
            self.ensure_db(db_number, start + len(data))
            block = self._blocks[db_number]
            block[start:start + len(data)] = data

    def write_bit(self, db_number: int, byte_addr: int, bit_addr: int, value: bool):
        with self._lock:
            self.ensure_db(db_number, byte_addr + 1)
            block = self._blocks[db_number]
            if value:
                block[byte_addr] |= (1 << bit_addr)
            else:
                block[byte_addr] &= ~(1 << bit_addr)

    def read_bit(self, db_number: int, byte_addr: int, bit_addr: int) -> bool:
        with self._lock:
            block = self._blocks.get(db_number)
            if block is None or byte_addr >= len(block):
                return False
            return bool(block[byte_addr] & (1 << bit_addr))

    def write_real(self, db_number: int, byte_addr: int, value: float):
        data = struct.pack(">f", value)
        self.write_bytes(db_number, byte_addr, data)

    def read_real(self, db_number: int, byte_addr: int) -> float:
        raw = self.read_bytes(db_number, byte_addr, 4)
        return struct.unpack(">f", raw)[0]

    def write_int16(self, db_number: int, byte_addr: int, value: int):
        data = struct.pack(">h", value)
        self.write_bytes(db_number, byte_addr, data)

    def write_int32(self, db_number: int, byte_addr: int, value: int):
        data = struct.pack(">i", value)
        self.write_bytes(db_number, byte_addr, data)

    def get_all_dbs(self) -> dict[int, int]:
        """返回 {db_number: size}"""
        with self._lock:
            return {k: len(v) for k, v in self._blocks.items()}

    def register_point(self, point_info: dict):
        self._points.append(point_info)
        self._point_map[point_info["key"]] = point_info

    def get_points(self) -> list[dict]:
        return self._points

    def get_point(self, key: str) -> dict | None:
        return self._point_map.get(key)


db_manager = DataBlockManager()

# ======================================================================
# 配置文件加载
# ======================================================================

DATA_TYPE_MAP = {0: "BOOL", 1: "DINT", 2: "REAL", 3: "INT", 4: "LINT"}
ALARM_LEVEL_MAP = {0: "None", 1: "Level1", 2: "Level2", 3: "Level3"}


def parse_address(address: str) -> dict:
    """解析 S7 地址 如 DB1.DBX312.0 → {db:1, byte:312, bit:0, type:'X'}"""
    parts = address.upper().split(".")
    result = {"db": 0, "byte": 0, "bit": 0, "type": "X"}

    for part in parts:
        # "DBX312" / "DBD318" / "DBW320" / "DBB322" — 含子类型标识
        if "DBX" in part:
            result["byte"] = int(part.replace("DBX", ""))
            result["type"] = "X"
        elif "DBD" in part:
            result["byte"] = int(part.replace("DBD", ""))
            result["type"] = "D"
        elif "DBW" in part:
            result["byte"] = int(part.replace("DBW", ""))
            result["type"] = "W"
        elif "DBB" in part:
            result["byte"] = int(part.replace("DBB", ""))
            result["type"] = "B"
        # "DB1" — 纯 DB 编号（必须放在 DBX/DBD/DBW/DBB 之后判断）
        elif re.match(r"^DB\d+$", part):
            result["db"] = int(part[2:])
        else:
            # 可能是 bit 号
            try:
                result["bit"] = int(part)
            except ValueError:
                pass

    return result


def load_config(config_path: str):
    """加载 DeviceSignalConfigs.json 并初始化数据块。"""
    with open(config_path, "r", encoding="utf-8-sig") as f:
        config = json.load(f)

    devices = config.get("devices", [])
    print(f"[Config] JSON 解析完成, 共 {len(devices)} 个设备", flush=True)
    total_points = 0

    for dev_idx, device in enumerate(devices):
        device_name = device.get("deviceName", "")
        points = device.get("points", [])
        print(f"  [{dev_idx+1}/{len(devices)}] {device_name}: {len(points)} 个点位", flush=True)

        for pt in points:
            display_name = pt.get("displayName", "")
            address = pt.get("address", "")
            data_type = pt.get("dataType", 0)
            alarm_level = pt.get("alarmLevel", 0)
            is_write = pt.get("isWrite", False)

            if not address:
                continue

            addr = parse_address(address)
            key = device_name + display_name

            # 计算所需字节大小
            type_sizes = {"X": 1, "B": 1, "W": 2, "D": 4}
            byte_size = type_sizes.get(addr["type"], 1)
            needed = addr["byte"] + byte_size

            db_manager.ensure_db(addr["db"], needed)

            # 为数值类型设置随机初始值
            data_type_name = DATA_TYPE_MAP.get(data_type, "BOOL")
            if data_type_name == "REAL":
                db_manager.write_real(addr["db"], addr["byte"], round(random.uniform(10, 100), 2))
            elif data_type_name in ("INT",):
                db_manager.write_int16(addr["db"], addr["byte"], random.randint(0, 1000))
            elif data_type_name in ("DINT",):
                db_manager.write_int32(addr["db"], addr["byte"], random.randint(0, 10000))
            # BOOL 默认 false (0)

            point_info = {
                "key": key,
                "device": device_name,
                "display": display_name,
                "address": address,
                "db": addr["db"],
                "byte": addr["byte"],
                "bit": addr["bit"],
                "addr_type": addr["type"],
                "data_type": data_type_name,
                "alarm_level": ALARM_LEVEL_MAP.get(alarm_level, "None"),
                "is_write": is_write,
            }
            db_manager.register_point(point_info)
            total_points += 1

    print(f"[Config] 已加载 {len(devices)} 个设备, {total_points} 个点位", flush=True)
    dbs = db_manager.get_all_dbs()
    for db_num, size in sorted(dbs.items()):
        print(f"  DB{db_num}: {size} bytes")


# ======================================================================
# S7 协议处理
# ======================================================================

def make_tpkt(payload: bytes) -> bytes:
    """封装 TPKT 报文"""
    length = 4 + len(payload)
    return bytes([0x03, 0x00, (length >> 8) & 0xFF, length & 0xFF]) + payload


def build_s7_ack_data(pdu_ref: int, param: bytes, data: bytes = b"", *, error_class: int = 0, error_code: int = 0) -> bytes:
    """Construct a full S7 Ack Data payload."""
    param_len = len(param)
    data_len = len(data)
    header = bytes([
        0x32,
        0x03,
        0x00, 0x00,
        (pdu_ref >> 8) & 0xFF, pdu_ref & 0xFF,
        (param_len >> 8) & 0xFF, param_len & 0xFF,
        (data_len >> 8) & 0xFF, data_len & 0xFF,
        error_class & 0xFF, error_code & 0xFF,
    ])
    return header + param + data


def get_s7_pdu_ref(s7_data: bytes) -> int:
    if len(s7_data) < 6:
        return 0
    return (s7_data[4] << 8) | s7_data[5]


def get_s7_function_code(s7_data: bytes) -> int:
    if len(s7_data) < 11:
        return -1
    return s7_data[10]


def get_s7_item_count(s7_data: bytes) -> int:
    if len(s7_data) < 12:
        return 0
    return s7_data[11]


def handle_cotp_connect(data: bytes) -> bytes:
    """处理 COTP 连接请求，返回连接确认"""
    # 构造 Connection Confirmed (0xD0)
    dst_ref = data[2:4] if len(data) >= 4 else bytes([0x00, 0x00])
    src_ref = data[4:6] if len(data) >= 6 else bytes([0x00, 0x00])
    src_tsap = bytes([0x01, 0x00])
    dst_tsap = bytes([0x03, 0x00])
    tpdu_size = 0x0A

    i = 7
    while i + 1 < len(data):
        code = data[i]
        size = data[i + 1]
        start = i + 2
        end = start + size
        if end > len(data):
            break
        value = data[start:end]
        if code == 0xC1 and size == 2:
            src_tsap = value
        elif code == 0xC2 and size == 2:
            dst_tsap = value
        elif code == 0xC0 and size == 1:
            tpdu_size = value[0]
        i = end

    response = bytearray([
        0x11,
        0xD0,
        src_ref[0], src_ref[1],
        dst_ref[0], dst_ref[1],
        0x00,
        0xC1, 0x02, src_tsap[0], src_tsap[1],
        0xC2, 0x02, dst_tsap[0], dst_tsap[1],
        0xC0, 0x01, tpdu_size,
    ])
    return make_tpkt(bytes(response))


def handle_s7_setup(s7_data: bytes) -> bytes:
    """处理 S7 通讯建立请求，返回设置确认"""
    # s7_data 是 COTP DT 之后的 S7 payload
    # 返回 S7 Communication Setup 确认
    cotp_dt = bytes([0x02, 0xF0, 0x80])
    pdu_ref = get_s7_pdu_ref(s7_data)
    param = bytes([
        0xF0,
        0x00,
        0x00, 0x03,
        0x00, 0x03,
        0x03, 0xC0,
    ])
    s7_response = build_s7_ack_data(pdu_ref, param)
    return make_tpkt(cotp_dt + s7_response)


# handle_s7_read / handle_s7_write / _make_error_response 定义在下方 S7 服务器节之后


# ======================================================================
# TCP 服务器 (S7 协议)
# ======================================================================

class S7ClientHandler:
    def __init__(self, conn: socket.socket, addr):
        self.conn = conn
        self.addr = addr

    def start(self):
        t = threading.Thread(target=self._run_safe, daemon=True)
        t.start()

    def _run_safe(self):
        print(f"[S7] 新连接: {self.addr}")
        try:
            self._handle()
        except (ConnectionResetError, BrokenPipeError, OSError):
            pass
        except Exception as e:
            print(f"[S7] 连接异常 {self.addr}: {e}")
        finally:
            try:
                self.conn.close()
            except Exception:
                pass
            print(f"[S7] 连接关闭: {self.addr}")

    def _recv_exactly(self, n: int) -> bytes:
        buf = b""
        while len(buf) < n:
            chunk = self.conn.recv(n - len(buf))
            if not chunk:
                raise ConnectionResetError("Connection closed")
            buf += chunk
        return buf

    def _recv_tpkt(self) -> bytes:
        """接收一个完整的 TPKT 包，返回 payload (不含 TPKT 头)"""
        header = self._recv_exactly(4)
        if header[0] != 0x03:
            raise ValueError(f"Invalid TPKT version: {header[0]:#x}")
        length = (header[2] << 8) | header[3]
        payload = self._recv_exactly(length - 4)
        return payload

    def _handle(self):
        # Step 1: COTP Connection Request
        payload = self._recv_tpkt()
        # payload[1] = 0xE0 (Connection Request)
        if len(payload) >= 2 and payload[1] == 0xE0:
            response = handle_cotp_connect(payload)
            self.conn.sendall(response)
        else:
            print(f"[S7] 非预期的 COTP 类型: {payload[1]:#x}")
            return

        # Step 2: S7 Communication Setup
        payload = self._recv_tpkt()
        # payload = COTP DT (3 bytes) + S7 data
        if len(payload) >= 4 and payload[1] == 0xF0:
            s7_data = payload[3:]  # 跳过 COTP DT header
            response = handle_s7_setup(s7_data)
            self.conn.sendall(response)
        else:
            print(f"[S7] 非预期的 COTP 类型: {payload[1]:#x}")
            return

        # Step 3: 持续处理读写请求
        while True:
            payload = self._recv_tpkt()
            if len(payload) < 4:
                continue

            # COTP DT header (3 bytes) + S7 data
            cotp_header_len = payload[0]
            s7_data = payload[cotp_header_len + 1:]

            if len(s7_data) < 11:
                continue

            # S7 Job: [0]=0x32 [1]=0x01 [2-3]=reserved [4-5]=PDU ref
            #         [6-7]=param_len [8-9]=data_len [10]=func [11]=item_count
            func_code = get_s7_function_code(s7_data)

            if func_code == 0x04:  # Read Var
                response = handle_s7_read(s7_data)
            elif func_code == 0x05:  # Write Var
                response = handle_s7_write(s7_data)
            else:
                print(f"[S7] 未知 function code: {func_code:#x}")
                response = _make_error_response(s7_data)

            self.conn.sendall(response)


def start_s7_server(port: int):
    """启动 S7 TCP 服务器"""
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind(("0.0.0.0", port))
    server.listen(5)
    print(f"[S7] 模拟器已启动，监听端口 {port}")

    while True:
        conn, addr = server.accept()
        handler = S7ClientHandler(conn, addr)
        handler.start()


# ======================================================================
# Web 控制面板
# ======================================================================

WEB_HTML = """<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<title>PLC Simulator - 控制面板</title>
<style>
* { margin: 0; padding: 0; box-sizing: border-box; }
body { font-family: 'Segoe UI', sans-serif; background: #1a1a2e; color: #e0e0e0; padding: 20px; }
h1 { color: #00d4ff; margin-bottom: 5px; }
.subtitle { color: #888; margin-bottom: 20px; font-size: 14px; }
.filters { margin-bottom: 15px; display: flex; gap: 10px; flex-wrap: wrap; }
.filters input, .filters select { padding: 6px 12px; border: 1px solid #333; border-radius: 4px;
  background: #16213e; color: #e0e0e0; font-size: 14px; }
.filters input { width: 250px; }
table { width: 100%; border-collapse: collapse; }
th { background: #16213e; color: #00d4ff; padding: 10px 8px; text-align: left;
  border-bottom: 2px solid #00d4ff; font-size: 13px; cursor: pointer; }
td { padding: 8px; border-bottom: 1px solid #2a2a4a; font-size: 13px; }
tr:hover { background: #16213e; }
.alarm-none { color: #666; }
.alarm-level1 { color: #ffd700; font-weight: bold; }
.alarm-level2 { color: #ff8c00; font-weight: bold; }
.alarm-level3 { color: #ff4444; font-weight: bold; }
.val-true { color: #00ff88; font-weight: bold; }
.val-false { color: #888; }
.btn { padding: 4px 12px; border: none; border-radius: 3px; cursor: pointer; font-size: 12px; }
.btn-toggle { background: #0066cc; color: white; }
.btn-toggle:hover { background: #0088ff; }
.btn-toggle.active { background: #00aa44; }
.btn-set { background: #444; color: white; }
.btn-set:hover { background: #666; }
.input-val { width: 80px; padding: 3px 6px; border: 1px solid #333; border-radius: 3px;
  background: #0a0a1a; color: #e0e0e0; font-size: 12px; }
.stats { display: flex; gap: 20px; margin-bottom: 15px; }
.stat-card { background: #16213e; padding: 12px 20px; border-radius: 6px; border: 1px solid #333; }
.stat-num { font-size: 24px; color: #00d4ff; font-weight: bold; }
.stat-label { font-size: 12px; color: #888; }
.refresh-info { color: #666; font-size: 12px; margin-top: 10px; }
</style>
</head>
<body>
<h1>PLC Simulator</h1>
<p class="subtitle">S7 协议模拟器 - Web 控制面板</p>

<div class="stats">
  <div class="stat-card"><div class="stat-num" id="totalPoints">-</div><div class="stat-label">总点位</div></div>
  <div class="stat-card"><div class="stat-num" id="alarmPoints">-</div><div class="stat-label">报警点位</div></div>
  <div class="stat-card"><div class="stat-num" id="activeAlarms">-</div><div class="stat-label">当前触发</div></div>
</div>

<div class="filters">
  <input type="text" id="searchInput" placeholder="搜索设备名 / 点位名..." oninput="filterTable()">
  <select id="filterType" onchange="filterTable()">
    <option value="">全部类型</option>
    <option value="BOOL">BOOL</option>
    <option value="REAL">REAL</option>
    <option value="INT">INT</option>
    <option value="DINT">DINT</option>
  </select>
  <select id="filterAlarm" onchange="filterTable()">
    <option value="">全部报警等级</option>
    <option value="Level1">Level1</option>
    <option value="Level2">Level2</option>
    <option value="Level3">Level3</option>
    <option value="None">无报警</option>
  </select>
</div>

<table>
<thead>
<tr>
  <th>设备</th><th>点位名</th><th>地址</th><th>类型</th><th>报警等级</th><th>当前值</th><th>操作</th>
</tr>
</thead>
<tbody id="pointsBody"></tbody>
</table>

<p class="refresh-info">自动刷新: 每 1 秒（仅更新值，不重建表格）</p>

<script>
let allPoints = [];
let filteredKeys = [];   // 当前过滤后的 key 列表（有序，与 DOM 行对应）
let tableBuilt = false;
let sidToKey = {};       // 'p0','p1'... → 原始 key（行号索引，不含中文，绝对不碰撞）
let keyToSid = {};       // 原始 key → 'p0','p1'...

// 首次加载：拉数据 + 建 DOM
async function initialLoad() {
  const resp = await fetch('/api/points');
  allPoints = await resp.json();
  applyFilter();
  setInterval(refreshValues, 1000);
}

// 筛选变化时重建 DOM
function applyFilter() {
  const search = document.getElementById('searchInput').value.toLowerCase();
  const typeF  = document.getElementById('filterType').value;
  const alarmF = document.getElementById('filterAlarm').value;
  const filtered = allPoints.filter(p => {
    if (search && !p.device.toLowerCase().includes(search) && !p.display.toLowerCase().includes(search)) return false;
    if (typeF  && p.data_type   !== typeF)  return false;
    if (alarmF && p.alarm_level !== alarmF) return false;
    return true;
  });
  filteredKeys = filtered.map(p => p.key);
  buildTable(filtered);
  updateStats(allPoints);
  tableBuilt = true;
}

// 用行号 idx 作为唯一 ID，彻底避免中文/特殊字符碰撞
function buildTable(filtered) {
  sidToKey = {};
  keyToSid = {};
  filtered.forEach((p, idx) => {
    const sid = 'p' + idx;
    sidToKey[sid] = p.key;
    keyToSid[p.key] = sid;
  });
  const tbody = document.getElementById('pointsBody');
  tbody.innerHTML = filtered.map((p, idx) => {
    const sid = 'p' + idx;
    const alarmClass = 'alarm-' + p.alarm_level.toLowerCase();
    let valCell = '', actionCell = '';
    if (p.data_type === 'BOOL') {
      const vc = p.current_value ? 'val-true' : 'val-false';
      const bc = p.current_value ? 'btn btn-toggle active' : 'btn btn-toggle';
      valCell    = `<span id="val_${sid}" class="${vc}">${p.current_value ? 'TRUE' : 'FALSE'}</span>`;
      actionCell = `<button id="btn_${sid}" class="${bc}" data-sid="${sid}" onclick="toggleBoolBtn(this)">切换</button>`;
    } else {
      valCell    = `<span id="val_${sid}">${p.current_value}</span>`;
      actionCell = `<input id="inp_${sid}" class="input-val" placeholder="${p.current_value}">
                    <button class="btn btn-set" data-sid="${sid}" onclick="setValBtn(this)">设置</button>`;
    }
    return `<tr>
      <td>${p.device}</td><td>${p.display}</td>
      <td style="font-family:monospace;color:#888">${p.address}</td>
      <td>${p.data_type}</td>
      <td class="${alarmClass}">${p.alarm_level}</td>
      <td>${valCell}</td><td>${actionCell}</td>
    </tr>`;
  }).join('');
}

// 定时刷新：只更新值，不碰 DOM 结构
async function refreshValues() {
  if (!tableBuilt) return;
  let data;
  try { const r = await fetch('/api/points'); data = await r.json(); } catch(e) { return; }
  allPoints = data;
  const map = {};
  data.forEach(p => map[p.key] = p);
  updateStats(data);
  filteredKeys.forEach(key => {
    const p = map[key]; if (!p) return;
    const sid = keyToSid[key]; if (!sid) return;
    const valEl = document.getElementById('val_' + sid); if (!valEl) return;
    if (p.data_type === 'BOOL') {
      valEl.className   = p.current_value ? 'val-true' : 'val-false';
      valEl.textContent = p.current_value ? 'TRUE' : 'FALSE';
      const btn = document.getElementById('btn_' + sid);
      if (btn) btn.className = p.current_value ? 'btn btn-toggle active' : 'btn btn-toggle';
    } else {
      const inp = document.getElementById('inp_' + sid);
      if (!inp || document.activeElement !== inp) {
        valEl.textContent = p.current_value;
        if (inp) inp.placeholder = p.current_value;
      }
    }
  });
}

function updateStats(pts) {
  document.getElementById('totalPoints').textContent  = pts.length;
  document.getElementById('alarmPoints').textContent  = pts.filter(p => p.alarm_level !== 'None').length;
  document.getElementById('activeAlarms').textContent = pts.filter(p => p.data_type === 'BOOL' && p.alarm_level !== 'None' && p.current_value).length;
}

// data-sid 存的是行号 sid，通过 sidToKey 反查原始 key，100% 精准
async function toggleBoolBtn(btn) {
  const sid = btn.dataset.sid;
  const key = sidToKey[sid]; if (!key) return;
  await fetch('/api/toggle', {method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({key})});
  const r = await fetch('/api/points'); allPoints = await r.json();
  const p = allPoints.find(x => x.key === key); if (!p) return;
  const valEl = document.getElementById('val_' + sid);
  if (valEl) { valEl.className = p.current_value?'val-true':'val-false'; valEl.textContent = p.current_value?'TRUE':'FALSE'; }
  btn.className = p.current_value?'btn btn-toggle active':'btn btn-toggle';
  updateStats(allPoints);
}

async function setValBtn(btn) {
  const sid = btn.dataset.sid;
  const key = sidToKey[sid]; if (!key) return;
  const inp = document.getElementById('inp_' + sid);
  const value = inp ? inp.value.trim() : '';
  if (value === '') return;
  await fetch('/api/set', {method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({key, value: parseFloat(value)})});
  if (inp) { inp.placeholder = value; inp.value = ''; }
}

initialLoad();
</script>
</body>
</html>"""


class WebHandler(BaseHTTPRequestHandler):
    def log_message(self, format, *args):
        pass  # 静默 HTTP 日志

    def do_GET(self):
        parsed = urlparse(self.path)

        if parsed.path == "/" or parsed.path == "/index.html":
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.end_headers()
            self.wfile.write(WEB_HTML.encode("utf-8"))

        elif parsed.path == "/api/points":
            points = db_manager.get_points()
            result = []
            for pt in points:
                val = self._read_value(pt)
                result.append({
                    "key": pt["key"],
                    "device": pt["device"],
                    "display": pt["display"],
                    "address": pt["address"],
                    "data_type": pt["data_type"],
                    "alarm_level": pt["alarm_level"],
                    "current_value": val,
                })
            self.send_response(200)
            self.send_header("Content-Type", "application/json; charset=utf-8")
            self.end_headers()
            self.wfile.write(json.dumps(result, ensure_ascii=False).encode("utf-8"))

        else:
            self.send_response(404)
            self.end_headers()

    def do_POST(self):
        parsed = urlparse(self.path)
        content_len = int(self.headers.get("Content-Length", 0))
        body = json.loads(self.rfile.read(content_len)) if content_len else {}

        if parsed.path == "/api/toggle":
            key = body.get("key", "")
            pt = db_manager.get_point(key)
            if pt and pt["data_type"] == "BOOL":
                cur = db_manager.read_bit(pt["db"], pt["byte"], pt["bit"])
                db_manager.write_bit(pt["db"], pt["byte"], pt["bit"], not cur)
                self._json_response({"ok": True, "value": not cur})
            else:
                self._json_response({"ok": False, "error": "Point not found or not BOOL"}, 400)

        elif parsed.path == "/api/set":
            key = body.get("key", "")
            value = body.get("value", 0)
            pt = db_manager.get_point(key)
            if pt:
                if pt["data_type"] == "REAL":
                    db_manager.write_real(pt["db"], pt["byte"], float(value))
                elif pt["data_type"] == "INT":
                    db_manager.write_int16(pt["db"], pt["byte"], int(value))
                elif pt["data_type"] == "DINT":
                    db_manager.write_int32(pt["db"], pt["byte"], int(value))
                elif pt["data_type"] == "BOOL":
                    db_manager.write_bit(pt["db"], pt["byte"], pt["bit"], bool(value))
                self._json_response({"ok": True})
            else:
                self._json_response({"ok": False, "error": "Point not found"}, 400)

        else:
            self.send_response(404)
            self.end_headers()

    def _json_response(self, data: dict, code: int = 200):
        self.send_response(code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.end_headers()
        self.wfile.write(json.dumps(data, ensure_ascii=False).encode("utf-8"))

    def _read_value(self, pt: dict):
        if pt["data_type"] == "BOOL":
            return db_manager.read_bit(pt["db"], pt["byte"], pt["bit"])
        elif pt["data_type"] == "REAL":
            return round(db_manager.read_real(pt["db"], pt["byte"]), 3)
        elif pt["data_type"] == "INT":
            raw = db_manager.read_bytes(pt["db"], pt["byte"], 2)
            return struct.unpack(">h", raw)[0]
        elif pt["data_type"] == "DINT":
            raw = db_manager.read_bytes(pt["db"], pt["byte"], 4)
            return struct.unpack(">i", raw)[0]
        return 0


def start_web_server(port: int):
    server = HTTPServer(("0.0.0.0", port), WebHandler)
    print(f"[Web] 控制面板已启动: http://localhost:{port}")
    server.serve_forever()


def handle_s7_read(s7_data: bytes) -> bytes:
    if len(s7_data) < 12:
        return _make_error_response(s7_data)

    item_count = get_s7_item_count(s7_data)
    offset = 12
    response_data = bytearray()

    for _ in range(item_count):
        if offset + 12 > len(s7_data):
            break

        transport_size = s7_data[offset + 3]
        count = (s7_data[offset + 4] << 8) | s7_data[offset + 5]
        db_number = (s7_data[offset + 6] << 8) | s7_data[offset + 7]
        area = s7_data[offset + 8]
        addr_bits = (s7_data[offset + 9] << 16) | (s7_data[offset + 10] << 8) | s7_data[offset + 11]
        byte_addr = addr_bits // 8
        offset += 12

        if area != 0x84:
            response_data.extend([0x05, 0x00, 0x00, 0x00])
            continue

        read_len = max(1, (count + 7) // 8) if transport_size == 0x01 else count
        read_data = db_manager.read_bytes(db_number, byte_addr, read_len)

        response_data.append(0xFF)
        response_data.append(0x04)
        data_len_bits = len(read_data) * 8
        response_data.append((data_len_bits >> 8) & 0xFF)
        response_data.append(data_len_bits & 0xFF)
        response_data.extend(read_data)

        if len(read_data) % 2 != 0:
            response_data.append(0x00)

    cotp_dt = bytes([0x02, 0xF0, 0x80])
    pdu_ref = get_s7_pdu_ref(s7_data)
    param = bytes([0x04, item_count])
    s7_payload = build_s7_ack_data(pdu_ref, param, bytes(response_data))
    return make_tpkt(cotp_dt + s7_payload)


def handle_s7_write(s7_data: bytes) -> bytes:
    if len(s7_data) < 12:
        return _make_error_response(s7_data)

    item_count = get_s7_item_count(s7_data)
    param_offset = 12
    items = []
    for _ in range(item_count):
        if param_offset + 12 > len(s7_data):
            break

        addr_bits = (s7_data[param_offset + 9] << 16) | (s7_data[param_offset + 10] << 8) | s7_data[param_offset + 11]
        items.append({
            "word_len": s7_data[param_offset + 3],
            "count": (s7_data[param_offset + 4] << 8) | s7_data[param_offset + 5],
            "db": (s7_data[param_offset + 6] << 8) | s7_data[param_offset + 7],
            "area": s7_data[param_offset + 8],
            "byte_addr": addr_bits // 8,
            "bit_addr": addr_bits % 8,
        })
        param_offset += 12

    data_offset = param_offset
    results = []

    for idx, item in enumerate(items):
        if data_offset + 4 > len(s7_data):
            results.append(0x05)
            continue

        transport = s7_data[data_offset + 1]
        data_len_raw = (s7_data[data_offset + 2] << 8) | s7_data[data_offset + 3]
        data_offset += 4

        if item["area"] != 0x84:
            results.append(0x05)
            continue

        if transport == 0x03:
            actual_bytes = 1
            if data_offset + actual_bytes > len(s7_data):
                results.append(0x05)
                continue
            db_manager.write_bit(item["db"], item["byte_addr"], item["bit_addr"], s7_data[data_offset] != 0)
            data_offset += actual_bytes
        else:
            actual_bytes = data_len_raw // 8 if transport == 0x04 else data_len_raw
            if actual_bytes <= 0:
                actual_bytes = item["count"]
            if data_offset + actual_bytes > len(s7_data):
                results.append(0x05)
                continue
            db_manager.write_bytes(item["db"], item["byte_addr"], s7_data[data_offset:data_offset + actual_bytes])
            data_offset += actual_bytes

        if idx < len(items) - 1 and actual_bytes % 2 != 0:
            data_offset += 1

        results.append(0xFF)

    cotp_dt = bytes([0x02, 0xF0, 0x80])
    pdu_ref = get_s7_pdu_ref(s7_data)
    param = bytes([0x05, item_count])
    s7_payload = build_s7_ack_data(pdu_ref, param, bytes(results))
    return make_tpkt(cotp_dt + s7_payload)


def _make_error_response(s7_data: bytes) -> bytes:
    cotp_dt = bytes([0x02, 0xF0, 0x80])
    pdu_ref = get_s7_pdu_ref(s7_data)
    func = get_s7_function_code(s7_data)
    if func < 0:
        func = 0x04
    item_count = get_s7_item_count(s7_data)
    s7_resp = build_s7_ack_data(
        pdu_ref,
        bytes([func, item_count]),
        bytes([0x05]),
        error_class=0x00,
        error_code=0x01,
    )
    return make_tpkt(cotp_dt + s7_resp)


# ======================================================================
# 主入口
# ======================================================================

def main():
    parser = argparse.ArgumentParser(description="S7 PLC Simulator")
    parser.add_argument("--config", "-c", default=None,
                        help="DeviceSignalConfigs.json 路径 (默认: 同级 StreamingAssets 目录)")
    parser.add_argument("--port", "-p", type=int, default=102,
                        help="S7 协议 TCP 端口 (默认: 102)")
    parser.add_argument("--web-port", "-w", type=int, default=8080,
                        help="Web 控制面板端口 (默认: 8080)")
    args = parser.parse_args()

    # 查找配置文件
    config_path = args.config
    if not config_path:
        # 尝试常见路径
        candidates = [
            os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "StreamingAssets", "DeviceSignalConfigs.json"),
            os.path.join(os.path.dirname(__file__), "DeviceSignalConfigs.json"),
            "DeviceSignalConfigs.json",
        ]
        for c in candidates:
            if os.path.exists(c):
                config_path = c
                break

    if not config_path or not os.path.exists(config_path):
        print("[Error] 找不到 DeviceSignalConfigs.json")
        print("  请使用 --config 参数指定路径")
        sys.exit(1)

    config_path = os.path.abspath(config_path)
    print(f"[Config] 加载配置: {config_path}")
    load_config(config_path)

    # 启动 Web 控制面板
    web_thread = threading.Thread(target=start_web_server, args=(args.web_port,), daemon=True)
    web_thread.start()

    # 启动 S7 服务器 (主线程)
    print(f"\n{'='*60}")
    print(f"  PLC 模拟器已就绪!")
    print(f"  S7 端口:      {args.port}")
    print(f"  Web 控制面板:  http://localhost:{args.web_port}")
    print(f"  Unity 中设置 PLC IP 为本机 IP 即可连接")
    print(f"{'='*60}\n")

    start_s7_server(args.port)


if __name__ == "__main__":
    main()
