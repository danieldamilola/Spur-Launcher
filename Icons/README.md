# Arc brand icons

This folder contains the **official Arc launcher** Windows icon set. All files are `.ico` format with the `arc-launcher-{size}` naming convention.

## Files

| File | Nominal size | Primary use |
|------|----------------|-------------|
| `arc-launcher-16x16.ico` | 16×16 | **System tray** (notification area) |
| `arc-launcher-32x32.ico` | 32×32 | Small UI, legacy tray, alt contexts |
| `arc-launcher-64x64.ico` | 64×64 | Medium DPI shell surfaces |
| `arc-launcher-128x128.ico` | 128×128 | High-DPI shortcuts, thumbnails |
| `arc-launcher-256x256.ico` | 256×256 | **Application icon** (embedded in `Arc.exe`) |
| `arc-launcher-512x512.ico` | 512×512 | Store / marketing / large tiles |
| `arc-launcher-1024x1024.ico` | 1024×1024 | Source / export master |

## Where each icon is used in the repo

| Location | Icon file | Notes |
|----------|-----------|--------|
| `Arc.csproj` → `ApplicationIcon` | `arc-launcher-256x256.ico` | Baked into the executable at build time |
| `App.xaml.cs` → tray (`TaskbarIcon`) | `arc-launcher-16x16.ico` | Loaded via `IconLoader.LoadTrayIcon()` |
| `publish.ps1` → Velopack `--icon` | `arc-launcher-256x256.ico` | Installer / update package icon |
| `Assets/arc.ico` | Copy of **256×256** | Legacy path for scripts; run sync below |
| `installer.iss` | *(from built exe)* | `UninstallDisplayIcon` uses `Arc.exe` embedded icon |
| `Views/OnboardingWindow.xaml` | `arc-launcher-64x64.ico` | Welcome slide brand mark |
| `Views/SettingsWindow.xaml` | `arc-launcher-32x32.ico` | Settings window title-bar icon |

## Code references

- **`Arc.Models.AppIcons`** — file names and pack URI helpers (`pack://application:,,,/Icons/...`)
- **`Arc.Helpers.IconLoader`** — loads embedded icons for tray and UI

Do not hard-code icon paths in XAML; use **Lucide** stroke icons for in-app UI (search, categories, settings). This folder is for **Windows shell / branding** only.

## Updating icons

1. Replace the relevant `.ico` file(s) in this folder (keep names unchanged).
2. Sync the legacy asset and rebuild:

```powershell
.\Icons\sync-to-assets.ps1
dotnet build Arc.csproj
```

3. For a release installer:

```powershell
.\publish.ps1
```

## Adding a new size

1. Export `arc-launcher-{N}x{N}.ico` into this folder.
2. Add a constant in `AppIcons.cs`.
3. Add `<Resource Include="Icons\arc-launcher-{N}x{N}.ico" />` in `Arc.csproj`.
4. Document the use case in this README.

## Design notes

- In-app UI icons (Files, Clipboard, Commands, results) use **Lucide** via `LucideIconConverter` — not these `.ico` files.
- Prefer **monochrome** Lucide strokes in the launcher; brand color lives in the app icon and accent token (`#0A84FF`).
