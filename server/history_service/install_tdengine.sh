#!/bin/bash
# ============================================================
# TDengine 一键安装脚本 (OpenCloudOS / CentOS / RHEL)
# 用法: sudo bash install_tdengine.sh
# ============================================================

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info()  { echo -e "${GREEN}[INFO]${NC}  $*"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC}  $*"; }
log_error() { echo -e "${RED}[ERROR]${NC} $*"; }

# ---- 检查 root 权限 ----
if [ "$(id -u)" -ne 0 ]; then
    log_error "请使用 root 权限运行: sudo bash $0"
    exit 1
fi

# ---- 系统信息 ----
log_info "系统信息:"
cat /etc/os-release 2>/dev/null | head -5
echo ""

# ============================================================
# 1. 安装 TDengine Server
# ============================================================
log_info "===== 第 1 步: 安装 TDengine Server ====="

# ---- 下载方式选择 ----
# 方式 1 (推荐): 使用涛思官方国内源 (最快)
# 方式 2: 使用 GitHub Releases 镜像
# 方式 3: 使用官方国际源 (海外服务器)
TDENGINE_VERSION="3.3.6.0"
ARCH=$(uname -m)

if [ "$ARCH" = "x86_64" ]; then
    PKG_SUFFIX="Linux-x64"
elif [ "$ARCH" = "aarch64" ]; then
    PKG_SUFFIX="Linux-aarch64"
else
    log_error "不支持的架构: $ARCH"
    exit 1
fi

RPM_NAME="TDengine-server-${TDENGINE_VERSION}-${PKG_SUFFIX}.rpm"

# 多个下载源, 依次尝试 (国内源在前)
URLS=(
    "https://www.taosdata.com/assets-download/3.0/${RPM_NAME}"
    "https://github.com/taosdata/TDengine/releases/download/ver-${TDENGINE_VERSION}/${RPM_NAME}"
    "https://www.tdengine.com/assets-download/3.0/${RPM_NAME}"
)

log_info "下载 TDengine Server v${TDENGINE_VERSION} (${ARCH})..."
cd /tmp
DOWNLOADED=false

for url in "${URLS[@]}"; do
    log_info "尝试下载源: $url"
    if command -v wget &>/dev/null; then
        wget -q --show-progress --timeout=30 --tries=2 -O tdengine-server.rpm "$url" && DOWNLOADED=true && break
    elif command -v curl &>/dev/null; then
        curl -L --connect-timeout 15 --max-time 300 --retry 2 -o tdengine-server.rpm "$url" && DOWNLOADED=true && break
    fi
    log_warn "该源下载失败, 尝试下一个..."
    rm -f tdengine-server.rpm
done

if [ "$DOWNLOADED" = true ] && [ -f tdengine-server.rpm ] && [ -s tdengine-server.rpm ]; then
    log_info "安装 RPM 包..."
    rpm -Uvh tdengine-server.rpm || yum localinstall -y tdengine-server.rpm
    rm -f tdengine-server.rpm
else
    log_warn "所有下载源均失败"
    log_info "=========================================="
    log_info "请手动下载后安装:"
    log_info "  1. 浏览器打开: https://docs.taosdata.com/get-started/"
    log_info "  2. 下载 RPM 包上传到服务器"
    log_info "  3. 运行: rpm -Uvh ${RPM_NAME}"
    log_info "=========================================="
    exit 1
fi

# ============================================================
# 2. 启动 taosd 服务
# ============================================================
log_info "===== 第 2 步: 启动 TDengine 服务 ====="

systemctl daemon-reload
systemctl enable taosd
systemctl start taosd

# 等待服务就绪
log_info "等待 taosd 启动..."
for i in $(seq 1 30); do
    if taos -s "SELECT server_version()" &>/dev/null; then
        break
    fi
    sleep 1
done

# 检查状态
if systemctl is-active --quiet taosd; then
    log_info "taosd 服务运行中"
else
    log_error "taosd 启动失败, 请检查: journalctl -u taosd"
    exit 1
fi

# ============================================================
# 3. 启动 taosAdapter (REST/WebSocket 接口)
# ============================================================
log_info "===== 第 3 步: 启动 taosAdapter (REST/WebSocket) ====="

if command -v taosadapter &>/dev/null; then
    systemctl enable taosadapter
    systemctl start taosadapter

    if systemctl is-active --quiet taosadapter; then
        log_info "taosAdapter 运行中 (端口 6041)"
    else
        log_warn "taosAdapter 启动失败, 请检查: journalctl -u taosadapter"
    fi
else
    log_warn "taosAdapter 未安装 (TDengine 3.x 通常自带)"
fi

# ============================================================
# 4. 安装 Python 依赖
# ============================================================
log_info "===== 第 4 步: 安装 Python 依赖 ====="

# 确保 pip3 存在
if ! command -v pip3 &>/dev/null; then
    log_info "安装 pip3..."
    dnf install -y python3-pip || yum install -y python3-pip
fi

pip3 install -U taospy[ws]
log_info "Python TDengine 驱动已安装"

# ============================================================
# 5. 创建数据库
# ============================================================
log_info "===== 第 5 步: 初始化数据库 ====="

taos -s "CREATE DATABASE IF NOT EXISTS history_service PRECISION 'ms' KEEP 730d;" || true
log_info "数据库 history_service 已创建"

# ============================================================
# 6. 配置防火墙
# ============================================================
log_info "===== 第 6 步: 配置防火墙 ====="

if command -v firewall-cmd &>/dev/null && systemctl is-active --quiet firewalld; then
    firewall-cmd --permanent --add-port=6030/tcp 2>/dev/null || true
    firewall-cmd --permanent --add-port=6041/tcp 2>/dev/null || true
    firewall-cmd --permanent --add-port=6043/tcp 2>/dev/null || true
    firewall-cmd --permanent --add-port=6060/tcp 2>/dev/null || true
    firewall-cmd --reload 2>/dev/null || true
    log_info "防火墙端口已开放: 6030, 6041, 6043, 6060"
else
    log_warn "firewalld 未运行, 跳过防火墙配置"
fi

# ============================================================
# 7. 验证安装
# ============================================================
log_info "===== 第 7 步: 验证安装 ====="

echo ""
taos -s "SELECT server_version();"
echo ""

VERSION=$(taos -s "SELECT server_version();" 2>/dev/null | grep -oP '\d+\.\d+\.\d+\.\d+' | head -1)

echo ""
echo "============================================================"
log_info "TDengine 安装完成!"
echo "============================================================"
echo ""
echo "  版本:        ${VERSION:-unknown}"
echo "  数据目录:    /var/lib/taos"
echo "  日志目录:    /var/log/taos"
echo "  配置文件:    /etc/taos/taos.cfg"
echo ""
echo "  服务端口:"
echo "    6030  - taosd (原生连接)"
echo "    6041  - taosAdapter (REST / WebSocket)"
echo "    6060  - taosExplorer (Web 管理界面)"
echo ""
echo "  默认账号:    root"
echo "  默认密码:    taosdata"
echo ""
echo "  Python 连接地址:"
echo "    taosws://root:taosdata@127.0.0.1:6041"
echo ""
echo "  常用命令:"
echo "    taos                          # 进入命令行"
echo "    systemctl status taosd        # 查看服务状态"
echo "    systemctl restart taosd       # 重启服务"
echo "    systemctl status taosadapter  # 查看 REST 服务状态"
echo ""
echo "  启动 history_service:"
echo "    cd /path/to/history_service"
echo "    pip3 install -r requirements.txt"
echo "    uvicorn app.main:app --host 0.0.0.0 --port 8000"
echo ""
echo "============================================================"
