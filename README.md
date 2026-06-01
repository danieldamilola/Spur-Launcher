# Arc

A fast, minimal launcher for Windows — one search field, Spotlight-style categories, and instant actions. Built with **WPF** and **.NET 9**.

## Features

- Global hotkey (`Alt+Space` by default) to open a floating search surface
- App, file, clipboard, and command search with keyboard-first navigation
- Three in-bar categories on hover: **Files**, **Commands**, **Clipboard**
- Built-in actions: calculator, web search, system commands, and more
- Light / dark themes with system accent support
- Optional system tray icon and Velopack updates

## Requirements

- Windows 10 (17763+) or Windows 11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) for development

## Quick start

```powershell
git clone <repo-url> C:\dev\Arc
cd C:\dev\Arc
dotnet build Arc.csproj
dotnet run --project Arc.csproj
```

Press **Alt+Space** to open the launcher (after first-run onboarding).

## Icons

Brand icons live in **[`Icons/`](Icons/README.md)** — not to be confused with in-app **Lucide** UI icons.

| Purpose | File |
|---------|------|
| Executable / taskbar pinned app | `Icons/arc-launcher-256x256.ico` |
| Notification tray | `Icons/arc-launcher-16x16.ico` |
| Installer (Velopack) | `Icons/arc-launcher-256x256.ico` |

After changing icons, run:

```powershell
.\Icons\sync-to-assets.ps1
dotnet build Arc.csproj
```

Full details: **[Icons/README.md](Icons/README.md)**.

## Release build

```powershell
.\publish.ps1
```

Produces installers under `dist/` using Velopack. Requires `vpk` (`dotnet tool install -g vpk`).

## Project layout

```
Arc/
├── Icons/              # Windows .ico brand assets (see Icons/README.md)
├── Assets/             # Legacy arc.ico (synced from Icons/)
├── Themes/             # Dark/light XAML themes + design tokens
├── Views/              # WPF UI (search bar, results, settings)
├── ViewModels/         # MVVM (MainViewModel, settings)
├── Services/           # Search, clipboard, hotkey, icons
├── Extensions/         # IAction plugins (calc, system, AI, …)
├── new/                # UX/design specs (ux.md, design.md, …)
└── Arc.Tests/          # Unit tests
```

## Keyboard shortcuts

| Key | Action |
|-----|--------|
| `Alt+Space` | Toggle launcher |
| `Escape` | Step back / dismiss |
| `↑` / `↓` | Move selection |
| `Enter` | Open / run |
| `Ctrl+1` / `2` / `3` | Files / Clipboard / Commands |
| `Ctrl+,` | Settings |
| `Win+Shift+V` | Clipboard mode *(when configured)* |

## Design docs

Product and visual specs are in [`new/ux.md`](new/ux.md) and [`new/design.md`](new/design.md).

## License

See [LICENSE](LICENSE).
