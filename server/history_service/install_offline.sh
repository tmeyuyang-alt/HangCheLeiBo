#!/bin/bash
# ============================================================
# 离线安装脚本 (在无外网的 Ubuntu 服务器上运行)
# 用法: sudo bash install_offline.sh
# ============================================================

set -e

log_info()  { echo -e "\033[0;32m[INFO]\033[0m  $*"; }
log_warn()  { echo -e "\033[1;33m[WARN]\033[0m  $*"; }
log_error() { echo -e "\033[0;31m[ERROR]\033[0m $*"; }

if [ "$(id -u)" -ne 0 ]; then
    log_error "please run as root: sudo bash $0"
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
VENV_DIR="/opt/history_service_venv"

# ---------- 1. install TDengine ----------
log_info "===== 1. install TDengine ====="

TAR_FILE=$(ls "$SCRIPT_DIR/tdengine/"*.tar.gz 2>/dev/null | head -1)
DEB_FILE=$(ls "$SCRIPT_DIR/tdengine/"*.deb 2>/dev/null | head -1)

if [ -n "$TAR_FILE" ]; then
    log_info "install from tar.gz: $(basename "$TAR_FILE")"
    TMPDIR=$(mktemp -d)
    tar -xzf "$TAR_FILE" -C "$TMPDIR"
    cd "$TMPDIR"/TDengine-*/ && bash install.sh -e no
    cd - > /dev/null
    rm -rf "$TMPDIR"
elif [ -n "$DEB_FILE" ]; then
    log_info "install from deb: $(basename "$DEB_FILE")"
    dpkg -i "$DEB_FILE" || apt-get install -f -y
else
    log_error "no TDengine package found in tdengine/ folder"
    exit 1
fi

# ---------- 2. configure and start TDengine ----------
log_info "===== 2. configure and start TDengine ====="

grep -q "^fqdn" /etc/taos/taos.cfg 2>/dev/null || echo "fqdn    127.0.0.1" >> /etc/taos/taos.cfg
grep -q "$(hostname)" /etc/hosts 2>/dev/null || echo "127.0.0.1 $(hostname)" >> /etc/hosts

rm -rf /var/lib/taos/* /var/log/taos/*
systemctl daemon-reload
systemctl enable taosd
systemctl start taosd

log_info "waiting for taosd..."
for i in $(seq 1 15); do
    if taos -s "SELECT 1" > /dev/null 2>&1; then
        break
    fi
    sleep 1
done

if systemctl is-active --quiet taosd; then
    log_info "taosd is running"
else
    log_error "taosd failed to start"
    /usr/bin/taosd 2>&1 | tail -5
    exit 1
fi

if command -v taosadapter > /dev/null 2>&1; then
    systemctl enable taosadapter
    systemctl start taosadapter
    sleep 2
    if systemctl is-active --quiet taosadapter; then
        log_info "taosAdapter is running (port 6041)"
    fi
fi

taos -s "CREATE DATABASE IF NOT EXISTS history_service PRECISION 'ms' KEEP 730d;" || true
log_info "database history_service created"

# ---------- 3. create venv and install Python packages ----------
log_info "===== 3. install Python packages (venv) ====="

# ensure python3-venv is available
if ! python3 -m venv --help > /dev/null 2>&1; then
    log_warn "python3-venv not found, trying apt install..."
    apt-get install -y python3-venv 2>/dev/null || true
fi

# create virtual environment
if [ -d "$VENV_DIR" ]; then
    log_info "removing old venv: $VENV_DIR"
    rm -rf "$VENV_DIR"
fi

log_info "creating venv: $VENV_DIR"
python3 -m venv "$VENV_DIR"

# activate venv
source "$VENV_DIR/bin/activate"

# upgrade pip inside venv (offline)
if [ -f "$SCRIPT_DIR/system_deps/get-pip.py" ]; then
    python "$SCRIPT_DIR/system_deps/get-pip.py" --no-index --find-links="$SCRIPT_DIR/python_packages/" 2>/dev/null || true
fi

# install packages offline
pip install --no-index --find-links="$SCRIPT_DIR/python_packages/" \
    -r "$SCRIPT_DIR/python_packages/requirements.txt"

log_info "Python packages installed in venv"

# create convenience symlinks
ln -sf "$VENV_DIR/bin/python" /usr/local/bin/history-python
ln -sf "$VENV_DIR/bin/uvicorn" /usr/local/bin/history-uvicorn

# ---------- 4. firewall ----------
if command -v ufw > /dev/null 2>&1; then
    ufw allow 6030/tcp 2>/dev/null || true
    ufw allow 6041/tcp 2>/dev/null || true
    ufw allow 8000/tcp 2>/dev/null || true
fi

# ---------- 5. verify ----------
log_info "===== 4. verify ====="

taos -s "SELECT server_version();" 2>/dev/null || true
"$VENV_DIR/bin/python" -c "import taosws; print('taosws OK')" 2>/dev/null || true
"$VENV_DIR/bin/python" -c "import fastapi; print('fastapi OK')" 2>/dev/null || true

echo ""
echo "============================================================"
log_info "ALL DONE"
echo "============================================================"
echo ""
echo "  TDengine: taosws://root:taosdata@127.0.0.1:6041"
echo "  Account:  root / taosdata"
echo ""
echo "  Python venv: $VENV_DIR"
echo ""
echo "  Start history_service:"
echo "    cd /path/to/history_service"
echo "    $VENV_DIR/bin/uvicorn app.main:app --host 0.0.0.0 --port 8000"
echo ""
echo "  Or use shortcut:"
echo "    cd /path/to/history_service"
echo "    history-uvicorn app.main:app --host 0.0.0.0 --port 8000"
echo ""
echo "============================================================"
