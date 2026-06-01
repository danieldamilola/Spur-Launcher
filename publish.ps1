#!/usr/bin/env pwsh
# Arc — Release publish + Velopack installer
# Usage: .\publish.ps1
# Requires: dotnet tool install -g vpk  (run once)

param(
    [string]$Version = "1.2.0",
    [string]$OutputDir = ".\dist"
)

$ErrorActionPreference = "Stop"
$ProjectDir = $PSScriptRoot

Write-Host ""
Write-Host "  Arc Release Build v$Version" -ForegroundColor Cyan
Write-Host "  ─────────────────────────────" -ForegroundColor DarkGray
Write-Host ""

# ── 1. Kill running instance ─────────────────────────────────────────────────
Write-Host "  [1/4] Stopping running Arc..." -ForegroundColor Yellow
taskkill /IM Arc.exe /F 2>$null
Start-Sleep -Milliseconds 400

# ── 2. Clean previous publish ────────────────────────────────────────────────
$PublishDir = Join-Path $ProjectDir "publish\app"
Write-Host "  [2/4] Cleaning previous build..." -ForegroundColor Yellow
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
if (Test-Path $OutputDir)  { Remove-Item $OutputDir  -Recurse -Force }

# ── 3. Publish (self-contained, single-file, win-x64, Release) ──────────────
Write-Host "  [3/4] Publishing Arc..." -ForegroundColor Yellow
dotnet publish "$ProjectDir\Arc.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "$PublishDir" `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Publish failed." -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Published to: $PublishDir" -ForegroundColor Green

# ── 4. Package with Velopack ─────────────────────────────────────────────────
Write-Host "  [4/4] Packaging installer with Velopack..." -ForegroundColor Yellow

# Check vpk is installed
if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    Write-Host ""
    Write-Host "  vpk tool not found. Installing..." -ForegroundColor Yellow
    dotnet tool install -g vpk
}

vpk pack `
    --packId "Arc" `
    --packVersion "$Version" `
    --packDir "$PublishDir" `
    --outputDir "$OutputDir" `
    --packTitle "Arc Launcher" `
    --icon "$ProjectDir\Icons\arc-launcher-256x256.ico" `
    --mainExe "Arc.exe"

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Velopack packaging failed." -ForegroundColor Red
    Write-Host "    Tip: make sure vpk is installed: dotnet tool install -g vpk" -ForegroundColor DarkGray
    exit 1
}

Write-Host ""
Write-Host "  ✓ Done! Installer at: $OutputDir\ArcSetup.exe" -ForegroundColor Green
Write-Host ""

# Show output files
Get-ChildItem $OutputDir | ForEach-Object {
    $size = if ($_.Length -ge 1MB) { "$([math]::Round($_.Length/1MB,1)) MB" } else { "$([math]::Round($_.Length/1KB,0)) KB" }
    Write-Host "    $($_.Name)  ($size)" -ForegroundColor DarkGray
}
Write-Host ""
