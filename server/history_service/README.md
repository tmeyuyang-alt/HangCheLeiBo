# History Service

用于采集并查询 `DeviceSignalConfigAsset` 中 `isHistoryData = true` 的点位历史数据。

技术选型：

- API: FastAPI
- 时序库: PostgreSQL + TimescaleDB
- PLC: `python-snap7`
- 部署: Docker Compose 或本机 Python

## 为什么这样设计

每秒采集一次时，原始表会非常大。这里把数据分成两层：

- 原始秒级数据写入 `point_samples`
- 查询分钟/小时/天时，优先命中连续聚合视图 `point_samples_1m`、`point_samples_1h`、`point_samples_1d`

这样能兼顾：

- 秒级写入简单
- 短时间窗口还能查原始数据
- 大时间范围查询走聚合，速度稳定

## 目录

- `app/`: FastAPI 服务代码
- `sql/001_init.sql`: TimescaleDB 初始化脚本
- `docker-compose.yml`: 本地数据库启动模板
- `.env.example`: 环境变量示例

## 数据模型

### 1. 设备表 `devices`

- `plc_id`: PLC 分组标识，默认可用 `plc01`
- `name`: 设备名称
- `shared_ip_address`: PLC IP

### 2. 点位表 `history_points`

- 只保存需要采集的历史点位
- 唯一键：`(device_id, address)`

### 3. 原始采样表 `point_samples`

- 主键：`(ts, point_id)`
- `numeric_value`: 统一保存数值
- `text_value`: 保留原始字符串
- `quality`: 质量码，`0` 正常，非 `0` 代表异常

## 查询接口

主接口：`POST /api/v1/history/query`

支持参数：

- `start_at`: 起始时间
- `duration_value`: 时间长度数值
- `duration_unit`: `seconds|minutes|hours|days|months|years|shift`
- `interval_value`: 聚合步长数值
- `interval_unit`: `seconds|minutes|hours|days|months|years|shift`
- `aggregate`: `last|min|max|avg`
- `plc_id`: PLC 标识
- `device_name_contains`: 设备名称包含过滤

返回结果是宽表格式：

- `columns`: 列定义
- `rows`: 每个时间桶一行，字段为 `ts + 每个点位列`

## 班次定义

默认班次长度 12 小时，默认起始时间 `08:00`。

- 白班：`08:00 - 20:00`
- 夜班：`20:00 - 08:00`

可通过环境变量修改：

- `SHIFT_START_HOUR`
- `SHIFT_LENGTH_HOURS`

## 配置导入

### 方式 1：直接读取 Unity `.asset`

`POST /api/v1/config/import-assets`

请求体：

```json
{
  "folder": "D:/GIT/HuangLin1203/Assets/Configs",
  "plc_id": "plc01"
}
```

服务会扫描所有 `.asset`，只导入 `isHistoryData = 1` 的点位。

## 与现有 Unity 前端对接

当前服务已提供：

- `POST /api/v1/history/query`
- `GET /data/hour_window`

其中 `/data/hour_window` 是给现有历史表先落一个兼容入口。

## 启动

### 1. 启数据库

```bash
docker compose up -d
```

### 2. 安装依赖

```bash
pip install -r requirements.txt
```

### 3. 启服务

```bash
uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```

### 4. 导入配置

```bash
curl -X POST http://127.0.0.1:8000/api/v1/config/import-assets \
  -H "Content-Type: application/json" \
  -d '{"folder":"D:/GIT/HuangLin1203/Assets/Configs","plc_id":"plc01"}'
```

## 采集策略

服务启动后，如果 `AUTO_START_COLLECTOR=true`：

- 每秒读取一次所有已启用历史点位
- 按 PLC IP 聚合
- 按 DB 块和地址范围批量读取
- 批量写入数据库

## 索引与性能要点

- 原始表做 hypertable
- 建立 `(point_id, ts DESC)` 索引
- 连续聚合覆盖分钟/小时/天
- 原始表 7 天后压缩
- 查询时按 interval 自动选择原始表或聚合视图

## 注意

- 当前代码假定地址格式类似 `DB1.DBX308.0`、`DB1.DBD0`、`DB1.DBW2`
- `LINT` 以 8 字节有符号整数解析
- 若后续存在更多地址格式，需要在 `app/plc.py` 中补充解析
