# ============================================================
# Docker image build & export script (Windows PowerShell)
# Requirements: Docker Desktop installed and running
# Usage: powershell -ExecutionPolicy Bypass -File build_docker.ps1
# ============================================================

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OutputDir = Join-Path $ScriptDir "docker_export"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  History Service - Docker Build Tool" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# check Docker
Write-Host "[INFO] checking Docker..." -ForegroundColor Green
try {
    docker info 2>&1 | Out-Null
    Write-Host "[INFO] Docker is running" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Docker is not running, please start Docker Desktop" -ForegroundColor Red
    exit 1
}

# create output dir
if (Test-Path $OutputDir) { Remove-Item -Recurse -Force $OutputDir }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# ============================================================
# 1. build history-service image
# ============================================================
Write-Host ""
Write-Host "[INFO] ===== 1. build history-service image =====" -ForegroundColor Green
Write-Host "[INFO] this may take a few minutes on first build..." -ForegroundColor Cyan
Write-Host ""

Set-Location $ScriptDir
docker build -t history-service:latest .

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] history-service build failed" -ForegroundColor Red
    exit 1
}
Write-Host "[INFO] history-service image built OK" -ForegroundColor Green

# ============================================================
# 2. pull TDengine image
# ============================================================
Write-Host ""
Write-Host "[INFO] ===== 2. pull TDengine image =====" -ForegroundColor Green
Write-Host ""

$tdTag = "3.3.6.0"
# try mirrors in order
$tdPulled = $false
$tdMirrors = @(
    "docker.1ms.run/tdengine/tdengine:$tdTag",
    "docker.xuanyuan.me/tdengine/tdengine:$tdTag",
    "docker.m.daocloud.io/tdengine/tdengine:$tdTag",
    "tdengine/tdengine:$tdTag"
)
foreach ($mirror in $tdMirrors) {
    Write-Host "[INFO] trying: $mirror" -ForegroundColor Cyan
    docker pull $mirror
    if ($LASTEXITCODE -eq 0) {
        docker tag $mirror "tdengine/tdengine:$tdTag"
        $tdPulled = $true
        break
    }
    Write-Host "[WARN] failed, trying next mirror..." -ForegroundColor Yellow
}
if (-not $tdPulled) {
    Write-Host "[ERROR] all mirrors failed for TDengine" -ForegroundColor Red
    exit 1
}

Write-Host "[INFO] TDengine image ready (tag: $tdTag)" -ForegroundColor Green

# ============================================================
# 3. export images to tar files
# ============================================================
Write-Host ""
Write-Host "[INFO] ===== 3. export images =====" -ForegroundColor Green

Write-Host "[INFO] exporting history-service.tar ..." -ForegroundColor Cyan
docker save -o "$OutputDir\history-service.tar" history-service:latest

Write-Host "[INFO] exporting tdengine.tar ..." -ForegroundColor Cyan
docker save -o "$OutputDir\tdengine.tar" "tdengine/tdengine:$tdTag"

Write-Host "[INFO] export done" -ForegroundColor Green

# ============================================================
# 4. copy config files
# ============================================================
Write-Host ""
Write-Host "[INFO] ===== 4. copy config files =====" -ForegroundColor Green

Copy-Item (Join-Path $ScriptDir "docker-compose.yml") "$OutputDir\docker-compose.yml" -Force
Copy-Item (Join-Path $ScriptDir ".env.example") "$OutputDir\.env" -Force

# ============================================================
# 5. write deploy.sh
# ============================================================
Write-Host ""
Write-Host "[INFO] ===== 5. writing deploy.sh =====" -ForegroundColor Green

$deployScript = @'
#!/bin/bash
# deploy.sh - run this on the target Ubuntu server (no internet needed)
# Usage: sudo bash deploy.sh

set -e
log_info()  { echo -e "\033[0;32m[INFO]\033[0m  $*"; }
log_error() { echo -e "\033[0;31m[ERROR]\033[0m $*"; }

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# check Docker
if ! command -v docker > /dev/null 2>&1; then
    log_error "Docker not found. Install it first: apt install -y docker.io"
    exit 1
fi

# check docker compose
if docker compose version > /dev/null 2>&1; then
    COMPOSE_CMD="docker compose"
elif command -v docker-compose > /dev/null 2>&1; then
    COMPOSE_CMD="docker-compose"
else
    log_error "docker-compose not found. Install it: apt install -y docker-compose"
    exit 1
fi

# load images
log_info "loading TDengine image..."
docker load -i "$SCRIPT_DIR/tdengine.tar"

log_info "loading history-service image..."
docker load -i "$SCRIPT_DIR/history-service.tar"

log_info "loaded images:"
docker images | grep -E "tdengine|history-service"

# start services
log_info "starting services..."
cd "$SCRIPT_DIR"
$COMPOSE_CMD up -d

# wait for TDengine
log_info "waiting for TDengine (up to 60s)..."
for i in $(seq 1 60); do
    if docker exec history-service-tdengine taos -s "SELECT 1" > /dev/null 2>&1; then
        log_info "TDengine is ready"
        break
    fi
    printf "."
    sleep 1
done
echo ""

# create database
log_info "initializing database..."
docker exec history-service-tdengine taos -s \
    "CREATE DATABASE IF NOT EXISTS history_service PRECISION 'ms' KEEP 730d;" || true

# verify
sleep 3
log_info "service status:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep -E "NAME|tdengine|history"

if curl -s http://localhost:8000/health 2>/dev/null | grep -q "ok"; then
    log_info "API health check passed"
fi

echo ""
echo "============================================================"
log_info "Deploy complete!"
echo "============================================================"
echo ""
echo "  API:          http://SERVER_IP:8000"
echo "  Health check: http://SERVER_IP:8000/health"
echo "  TDengine UI:  http://SERVER_IP:6060"
echo ""
echo "  Commands:"
echo "    View logs:   docker logs history-service-app -f"
echo "    Status:      docker ps"
echo "    Stop:        $COMPOSE_CMD down"
echo "    Restart:     $COMPOSE_CMD restart"
echo ""
echo "============================================================"
'@

[System.IO.File]::WriteAllText(
    "$OutputDir\deploy.sh",
    $deployScript.Replace("`r`n", "`n"),
    [System.Text.UTF8Encoding]::new($false)
)

# ============================================================
# summary
# ============================================================
$totalFiles = (Get-ChildItem $OutputDir -File).Count
$totalBytes = (Get-ChildItem $OutputDir -File | Measure-Object -Property Length -Sum).Sum
$totalGB = "{0:N2} GB" -f ($totalBytes / 1GB)

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "[INFO] All done!" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Output dir : $OutputDir"
Write-Host "  File count : $totalFiles"
Write-Host "  Total size : $totalGB"
Write-Host ""
Write-Host "  docker_export/"
Write-Host "    history-service.tar  - app image"
Write-Host "    tdengine.tar         - TDengine image"
Write-Host "    docker-compose.yml   - compose config"
Write-Host "    .env                 - environment variables"
Write-Host "    deploy.sh            - server deploy script"
Write-Host ""
Write-Host "  Next steps:" -ForegroundColor Yellow
Write-Host ""
Write-Host "    1. copy to server:"
Write-Host "       scp -r docker_export root@SERVER_IP:/opt/history_service/"
Write-Host ""
Write-Host "    2. run on server:"
Write-Host "       cd /opt/history_service"
Write-Host "       sudo bash deploy.sh"
Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
