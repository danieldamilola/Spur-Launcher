# Copies the canonical 256px app icon to Assets\arc.ico for legacy tooling.
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$src  = Join-Path $PSScriptRoot "arc-launcher-256x256.ico"
$dest = Join-Path $root "Assets\arc.ico"

if (-not (Test-Path $src)) {
    Write-Error "Missing source icon: $src"
}

New-Item -ItemType Directory -Path (Split-Path $dest) -Force | Out-Null
Copy-Item $src $dest -Force
Write-Host "Synced $src -> $dest"
