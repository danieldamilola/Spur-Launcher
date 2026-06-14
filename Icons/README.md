# Spur brand icons

This folder contains the **official Spur launcher** Windows icon set. All files are `.ico` format with the `spur-{size}` naming convention.

## Files

| File | Nominal size | Primary use |
|------|----------------|-------------|
| `spur-16x16.ico` | 16×16 | **System tray** (notification area) |
| `spur-32x32.ico` | 32×32 | Small UI, legacy tray, alt contexts |
| `spur-64x64.ico` | 64×64 | Medium DPI shell surfaces |
| `spur-128x128.ico` | 128×128 | High-DPI shortcuts, thumbnails |
| `spur-256x256.ico` | 256×256 | **Application icon** (embedded in `Spur.exe`) |
| `spur-512x512.ico` | 512×512 | Store / marketing / large tiles |
| `spur-1024x1024.ico` | 1024×1024 | Source / export master |

## Where each icon is used in the repo

| Location | Icon file | Notes |
|----------|-----------|--------|
| `Spur.csproj` → `ApplicationIcon` | `spur-256x256.ico` | Baked into the executable at build time *(pending csproj rename)* |
| `App.xaml.cs` → tray (`TaskbarIcon`) | `spur-16x16.ico` | Loaded via `IconLoader.LoadTrayIcon()` *(pending code rename)* |
| `publish.ps1` → Velopack `--icon` | `spur-256x256.ico` | Installer / update package icon |
| `Assets/spur.ico` | Copy of **256×256** | Legacy path for scripts; run sync below |
| `installer.iss` | *(from built exe)* | `UninstallDisplayIcon` uses `Spur.exe` embedded icon |
| `Views/OnboardingWindow.xaml` | `spur-64x64.ico` | Welcome slide brand mark *(pending XAML update)* |
| `Views/SettingsWindow.xaml` | `spur-32x32.ico` | Settings window title-bar icon *(pending XAML update)* |

## Code references

- **`Spur.Models.AppIcons`** — file names and pack URI helpers (`pack://application:,,,/Icons/...`)
- **`Spur.Helpers.IconLoader`** — loads embedded icons for tray and UI

Do not hard-code icon paths in XAML; use **Lucide** stroke icons for in-app UI (search, categories, settings). This folder is for **Windows shell / branding** only.

## Updating icons

1. Replace the relevant `.ico` file(s) in this folder (keep names unchanged).
2. Sync the legacy asset and rebuild:

```powershell
.\Icons\sync-to-assets.ps1
dotnet build Spur.csproj
```

3. For a release installer:

```powershell
.\publish.ps1
```

## Adding a new size

1. Export `spur-{N}x{N}.ico` into this folder.
2. Add a constant in `AppIcons.cs`.
3. Add `<Resource Include="Icons\spur-{N}x{N}.ico" />` in `Spur.csproj`.
4. Document the use case in this README.

## Design notes

- In-app UI icons (Files, Clipboard, Commands, results) use **Lucide** via `LucideIconConverter` — not these `.ico` files.
- Prefer **monochrome** Lucide strokes in the launcher; brand color lives in the app icon and accent token (`#0A84FF`).

