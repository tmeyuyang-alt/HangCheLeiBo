# ============================================================
# 离线依赖包下载脚本 (Windows PowerShell)
# 前提: 本机已安装 Python 3.11+
# 用法: powershell -ExecutionPolicy Bypass -File download_offline.ps1
# ============================================================

$ErrorActionPreference = "Continue"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OutputDir = Join-Path $ScriptDir "offline_packages"

Write-Host "[INFO] start downloading offline packages..." -ForegroundColor Green
Write-Host ""

# clean old
if (Test-Path $OutputDir) { Remove-Item -Recurse -Force $OutputDir }
New-Item -ItemType Directory -Path (Join-Path $OutputDir "tdengine") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $OutputDir "python_packages") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $OutputDir "system_deps") -Force | Out-Null

# ============================================================
# 1. download TDengine tar.gz
# ============================================================
Write-Host "[INFO] ===== 1. download TDengine =====" -ForegroundColor Green

$tdVersion = "3.3.6.0"
$tdFileName = "TDengine-server-$tdVersion-Linux-x64.tar.gz"
$tdFile = Join-Path $OutputDir "tdengine\$tdFileName"
$tdUrls = @(
    "https://www.taosdata.com/assets-download/3.0/$tdFileName",
    "https://www.tdengine.com/assets-download/3.0/$tdFileName"
)
$tdOK = $false

foreach ($url in $tdUrls) {
    Write-Host "[INFO] trying: $url" -ForegroundColor Cyan
    try {
        $wc = New-Object System.Net.WebClient
        $wc.DownloadFile($url, $tdFile)
        if ((Get-Item $tdFile).Length -gt 1MB) {
            Write-Host "[INFO] TDengine downloaded OK" -ForegroundColor Green
            $tdOK = $true
            break
        }
    } catch {
        Write-Host "[WARN] failed, try next..." -ForegroundColor Yellow
    }
}

if (-not $tdOK) {
    Write-Host "[WARN] TDengine auto download failed" -ForegroundColor Yellow
    Write-Host "[WARN] please download manually from: https://docs.taosdata.com/get-started/" -ForegroundColor Yellow
    Write-Host "[WARN] put tar.gz file into: $OutputDir\tdengine\" -ForegroundColor Yellow
}

# ============================================================
# 2. download Python packages
# ============================================================
Write-Host ""
Write-Host "[INFO] ===== 2. download Python packages =====" -ForegroundColor Green

$reqFile = Join-Path $OutputDir "python_packages\requirements.txt"
$reqContent = "fastapi==0.103.2`nuvicorn[standard]==0.23.2`ntaospy[ws]`nPyYAML==6.0.2`npython-dateutil==2.8.2`npython-snap7==2.0.2`npydantic==1.10.15"
[System.IO.File]::WriteAllText($reqFile, $reqContent, [System.Text.UTF8Encoding]::new($false))

$pkgDir = Join-Path $OutputDir "python_packages"

Write-Host "[INFO] downloading wheel packages (may take a few minutes)..." -ForegroundColor Cyan
Write-Host ""

# 直接调用 python, 能看到完整输出和错误
& py -m pip download -r $reqFile -d $pkgDir --platform manylinux2014_x86_64 --platform manylinux_2_17_x86_64 --platform linux_x86_64 --platform any --python-version 311 --only-binary=:all:

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[WARN] wheel download had errors, trying with source packages..." -ForegroundColor Yellow
    Write-Host ""
    & py -m pip download -r $reqFile -d $pkgDir
}

Write-Host ""
$pkgCount = (Get-ChildItem $pkgDir -Include *.whl, *.tar.gz, *.zip -File).Count
Write-Host "[INFO] downloaded $pkgCount Python packages" -ForegroundColor Green

# ============================================================
# 3. download get-pip.py
# ============================================================
Write-Host ""
Write-Host "[INFO] ===== 3. download get-pip.py =====" -ForegroundColor Green

$getPipFile = Join-Path $OutputDir "system_deps\get-pip.py"
try {
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile("https://bootstrap.pypa.io/get-pip.py", $getPipFile)
    Write-Host "[INFO] get-pip.py downloaded OK" -ForegroundColor Green
} catch {
    Write-Host "[WARN] get-pip.py download failed" -ForegroundColor Yellow
}

# ============================================================
# 4. copy install script
# ============================================================
Write-Host ""
Write-Host "[INFO] ===== 4. copy install script =====" -ForegroundColor Green

$srcInstall = Join-Path $ScriptDir "install_offline.sh"
$dstInstall = Join-Path $OutputDir "install_offline.sh"
if (Test-Path $srcInstall) {
    Copy-Item $srcInstall $dstInstall -Force
    Write-Host "[INFO] install_offline.sh copied" -ForegroundColor Green
} else {
    Write-Host "[WARN] install_offline.sh not found in script directory" -ForegroundColor Yellow
}

# ============================================================
# 5. summary
# ============================================================
Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "[INFO] DONE!" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Cyan

$totalFiles = (Get-ChildItem $OutputDir -Recurse -File).Count
$totalBytes = (Get-ChildItem $OutputDir -Recurse -File | Measure-Object -Property Length -Sum).Sum
$totalMB = "{0:N1} MB" -f ($totalBytes / 1MB)

Write-Host ""
Write-Host "  folder:  $OutputDir"
Write-Host "  files:   $totalFiles"
Write-Host "  size:    $totalMB"
Write-Host ""
Write-Host "  offline_packages/"
Write-Host "  |- tdengine/            TDengine tar.gz"
Write-Host "  |- python_packages/     Python wheel packages"
Write-Host "  |- system_deps/         get-pip.py"
Write-Host "  |- install_offline.sh   install script for Ubuntu"
Write-Host ""
Write-Host "  Next steps:" -ForegroundColor Yellow
Write-Host "    1. copy to server:"
Write-Host "       scp -r offline_packages root@SERVER_IP:/tmp/"
Write-Host ""
Write-Host "    2. run on server:"
Write-Host "       cd /tmp/offline_packages"
Write-Host "       sudo bash install_offline.sh"
Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
