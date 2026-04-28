#!/bin/bash
# ============================================================
# 离线依赖包下载脚本
# 在有网络的机器上运行，把所有依赖下载到本地
# 然后打包传到无外网的 Ubuntu 服务器上安装
#
# 用法: bash download_offline_packages.sh
# 输出: offline_packages.tar.gz (所有依赖打包)
# ============================================================

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info()  { echo -e "${GREEN}[INFO]${NC}  $*"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC}  $*"; }
log_error() { echo -e "${RED}[ERROR]${NC} $*"; }

# 输出目录
OUTDIR="offline_packages"
rm -rf "$OUTDIR"
mkdir -p "$OUTDIR/python_packages"
mkdir -p "$OUTDIR/tdengine"
mkdir -p "$OUTDIR/system_deps"

# ============================================================
# 1. 下载 TDengine deb 包
# ============================================================
log_info "===== 1. 下载 TDengine ====="

TDENGINE_VERSION="3.3.6.0"
DEB_NAME="TDengine-server-${TDENGINE_VERSION}-Linux-x64.deb"
TAR_NAME="TDengine-server-${TDENGINE_VERSION}-Linux-x64.tar.gz"

URLS=(
    "https://www.taosdata.com/assets-download/3.0/${TAR_NAME}"
    "https://www.tdengine.com/assets-download/3.0/${TAR_NAME}"
)

DOWNLOADED=false
for url in "${URLS[@]}"; do
    log_info "尝试: $url"
    if curl -L --connect-timeout 15 --max-time 600 -o "$OUTDIR/tdengine/${TAR_NAME}" "$url" 2>/dev/null; then
        if [ -s "$OUTDIR/tdengine/${TAR_NAME}" ]; then
            DOWNLOADED=true
            log_info "TDengine tar.gz 下载成功"
            break
        fi
    fi
    log_warn "下载失败，尝试下一个源..."
done

if [ "$DOWNLOADED" = false ]; then
    log_warn "TDengine 自动下载失败"
    log_warn "请手动下载 tar.gz 放入 $OUTDIR/tdengine/ 目录"
    log_warn "下载地址: https://docs.taosdata.com/get-started/"
fi

# ============================================================
# 2. 下载 Python 依赖包
# ============================================================
log_info "===== 2. 下载 Python 依赖包 ====="

# 创建 requirements.txt
cat > "$OUTDIR/python_packages/requirements.txt" <<'REQEOF'
fastapi==0.103.2
uvicorn[standard]==0.23.2
taospy[ws]
PyYAML==6.0.2
python-dateutil==2.8.2
python-snap7==2.0.2
pydantic==1.10.15
REQEOF

# 下载所有 wheel 包 (指定 linux 平台, python 3.11)
pip3 download \
    -r "$OUTDIR/python_packages/requirements.txt" \
    -d "$OUTDIR/python_packages/" \
    --platform manylinux2014_x86_64 \
    --platform linux_x86_64 \
    --platform any \
    --python-version 311 \
    --only-binary=:all: \
    2>/dev/null || true

# 有些包没有预编译 wheel，需要下载源码包
pip3 download \
    -r "$OUTDIR/python_packages/requirements.txt" \
    -d "$OUTDIR/python_packages/" \
    --no-binary=:none: \
    2>/dev/null || true

# 去重 (保留最新版本)
log_info "下载的 Python 包:"
ls -1 "$OUTDIR/python_packages/"*.whl "$OUTDIR/python_packages/"*.tar.gz "$OUTDIR/python_packages/"*.zip 2>/dev/null | head -50

# ============================================================
# 3. 下载 Ubuntu 系统依赖 deb 包
# ============================================================
log_info "===== 3. 下载 Ubuntu 系统依赖 ====="

# 如果当前是 Ubuntu/Debian 系统，可以用 apt 下载
if command -v apt-get &>/dev/null; then
    cd "$OUTDIR/system_deps"

    # Python3 和 pip
    apt-get download python3 python3-pip python3-venv python3-dev 2>/dev/null || true

    # 编译工具 (部分 Python 包可能需要)
    apt-get download gcc g++ make 2>/dev/null || true

    # snap7 依赖
    apt-get download libsnmp-dev 2>/dev/null || true

    cd - > /dev/null
    log_info "系统 deb 包已下载"
else
    log_warn "当前不是 Ubuntu/Debian 系统，跳过系统依赖下载"
    log_warn "请确保目标服务器已安装: python3 python3-pip"

    # 下载 get-pip.py 作为备用
    curl -sL https://bootstrap.pypa.io/get-pip.py -o "$OUTDIR/system_deps/get-pip.py" 2>/dev/null || true
    log_info "已下载 get-pip.py 备用安装脚本"
fi

# ============================================================
# 4. 生成离线安装脚本
# ============================================================
log_info "===== 4. 生成离线安装脚本 ====="

cat > "$OUTDIR/install_offline.sh" <<'INSTALLEOF'
#!/bin/bash
# ============================================================
# 离线安装脚本 (在无外网的 Ubuntu 服务器上运行)
# 用法: sudo bash install_offline.sh
# ============================================================

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

log_info()  { echo -e "${GREEN}[INFO]${NC}  $*"; }
log_error() { echo -e "${RED}[ERROR]${NC} $*"; }

if [ "$(id -u)" -ne 0 ]; then
    log_error "请使用 root 权限运行: sudo bash $0"
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# ---------- 1. 安装系统依赖 ----------
log_info "===== 1. 安装系统依赖 ====="

if ls "$SCRIPT_DIR/system_deps/"*.deb &>/dev/null; then
    dpkg -i "$SCRIPT_DIR/system_deps/"*.deb 2>/dev/null || true
    log_info "系统 deb 包已安装"
fi

# 确保 pip3 可用
if ! command -v pip3 &>/dev/null; then
    if [ -f "$SCRIPT_DIR/system_deps/get-pip.py" ]; then
        python3 "$SCRIPT_DIR/system_deps/get-pip.py" --no-index
    fi
fi

# ---------- 2. 安装 TDengine ----------
log_info "===== 2. 安装 TDengine ====="

# 优先 tar.gz 安装 (兼容性最好)
TAR_FILE=$(ls "$SCRIPT_DIR/tdengine/"*.tar.gz 2>/dev/null | head -1)
DEB_FILE=$(ls "$SCRIPT_DIR/tdengine/"*.deb 2>/dev/null | head -1)

if [ -n "$TAR_FILE" ]; then
    log_info "使用 tar.gz 安装: $TAR_FILE"
    TMPDIR=$(mktemp -d)
    tar -xzf "$TAR_FILE" -C "$TMPDIR"
    cd "$TMPDIR"/TDengine-*/ && bash install.sh -e no
    cd - > /dev/null
    rm -rf "$TMPDIR"
elif [ -n "$DEB_FILE" ]; then
    log_info "使用 deb 安装: $DEB_FILE"
    dpkg -i "$DEB_FILE" || apt-get install -f -y
else
    log_error "未找到 TDengine 安装包，请放入 tdengine/ 目录"
    exit 1
fi

# 配置 FQDN
grep -q "^fqdn" /etc/taos/taos.cfg 2>/dev/null || echo "fqdn    127.0.0.1" >> /etc/taos/taos.cfg

# 确保主机名可解析
grep -q "$(hostname)" /etc/hosts 2>/dev/null || echo "127.0.0.1 $(hostname)" >> /etc/hosts

# 清理旧数据并启动
rm -rf /var/lib/taos/* /var/log/taos/*
systemctl daemon-reload
systemctl enable taosd
systemctl start taosd

sleep 3

if systemctl is-active --quiet taosd; then
    log_info "taosd 启动成功"
else
    log_error "taosd 启动失败，查看日志: journalctl -u taosd"
    # 尝试手动启动看报错
    /usr/bin/taosd 2>&1 | tail -5
    exit 1
fi

# 启动 taosAdapter
if command -v taosadapter &>/dev/null; then
    systemctl enable taosadapter
    systemctl start taosadapter
    sleep 2
    if systemctl is-active --quiet taosadapter; then
        log_info "taosAdapter 启动成功 (端口 6041)"
    fi
fi

# 创建数据库
taos -s "CREATE DATABASE IF NOT EXISTS history_service PRECISION 'ms' KEEP 730d;" || true
log_info "数据库 history_service 已创建"

# ---------- 3. 安装 Python 依赖 ----------
log_info "===== 3. 安装 Python 依赖 ====="

pip3 install --no-index --find-links="$SCRIPT_DIR/python_packages/" \
    -r "$SCRIPT_DIR/python_packages/requirements.txt"

log_info "Python 依赖安装完成"

# ---------- 4. 配置防火墙 ----------
log_info "===== 4. 配置防火墙 ====="

if command -v ufw &>/dev/null; then
    ufw allow 6030/tcp 2>/dev/null || true
    ufw allow 6041/tcp 2>/dev/null || true
    ufw allow 8000/tcp 2>/dev/null || true
    log_info "UFW 端口已开放: 6030, 6041, 8000"
fi

# ---------- 5. 验证 ----------
log_info "===== 5. 验证安装 ====="

taos -s "SELECT server_version();" 2>/dev/null || true
python3 -c "import taosws; print('taosws 导入成功')" 2>/dev/null || true
python3 -c "import fastapi; print('fastapi 导入成功')" 2>/dev/null || true

echo ""
echo "============================================================"
log_info "全部安装完成!"
echo "============================================================"
echo ""
echo "  TDengine 连接地址: taosws://root:taosdata@127.0.0.1:6041"
echo "  默认账号: root / taosdata"
echo ""
echo "  启动 history_service:"
echo "    cd /path/to/history_service"
echo "    uvicorn app.main:app --host 0.0.0.0 --port 8000"
echo ""
echo "============================================================"
INSTALLEOF

chmod +x "$OUTDIR/install_offline.sh"

# ============================================================
# 5. 打包
# ============================================================
log_info "===== 5. 打包所有文件 ====="

tar -czf offline_packages.tar.gz "$OUTDIR"/

# 统计
TOTAL_SIZE=$(du -sh offline_packages.tar.gz | cut -f1)
PKG_COUNT=$(find "$OUTDIR" -type f | wc -l)

echo ""
echo "============================================================"
log_info "打包完成!"
echo "============================================================"
echo ""
echo "  输出文件:  offline_packages.tar.gz ($TOTAL_SIZE)"
echo "  包含文件:  $PKG_COUNT 个"
echo ""
echo "  目录结构:"
echo "    offline_packages/"
echo "    ├── tdengine/          TDengine 安装包"
echo "    ├── python_packages/   Python 依赖 wheel 包"
echo "    ├── system_deps/       系统依赖 / get-pip.py"
echo "    └── install_offline.sh 离线安装脚本"
echo ""
echo "  使用方法:"
echo "    1. 将 offline_packages.tar.gz 传到目标服务器"
echo "    2. tar -xzf offline_packages.tar.gz"
echo "    3. cd offline_packages"
echo "    4. sudo bash install_offline.sh"
echo ""
echo "============================================================"
