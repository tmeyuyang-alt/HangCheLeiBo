-- TDengine 初始化脚本
-- 通过 Python 代码自动执行 (db.init_schema)
-- 此文件仅作参考, 实际建表逻辑在 app/db.py 中

-- 创建数据库 (保留 730 天数据, 毫秒精度)
CREATE DATABASE IF NOT EXISTS history_service PRECISION 'ms' KEEP 730d;
USE history_service;

-- 超级表: devices (设备配置信息)
CREATE STABLE IF NOT EXISTS devices (
    ts TIMESTAMP,
    _placeholder INT
) TAGS (
    plc_id NCHAR(128),
    name NCHAR(255),
    shared_ip_address NCHAR(64),
    source_path NCHAR(512)
);

-- 超级表: history_points (点位配置信息)
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
);

-- 普通表: config_snapshots (Unity 编辑器上传的完整 JSON 配置快照)
CREATE TABLE IF NOT EXISTS config_snapshots (
    ts TIMESTAMP,
    plc_id NCHAR(128),
    config_json NCHAR(16000),
    source NCHAR(64)
);

-- 超级表: point_samples (采样数据 - 核心时序数据)
-- 每个点位自动创建一张子表, 通过 tag 关联设备信息
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
);
