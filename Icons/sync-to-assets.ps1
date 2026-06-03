# Copies the canonical 256px app icon to Assets\spur.ico for legacy tooling.
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$src  = Join-Path $PSScriptRoot "spur-launcher-256x256.ico"
$dest = Join-Path $root "Assets\spur.ico"

if (-not (Test-Path $src)) {
    Write-Error "Missing source icon: $src"
}

New-Item -ItemType Directory -Path (Split-Path $dest) -Force | Out-Null
Copy-Item $src $dest -Force
Write-Host "Synced $src -> $dest"
