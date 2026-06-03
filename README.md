# Spur

Spur your workflow.

A fast, minimal launcher for Windows — one search field, Spotlight-style categories, and instant actions. Built with **WPF** and **.NET 9**.

## Features

- Global hotkey (`Alt+Space` by default) to open a floating search surface
- App, file, clipboard, and command search with keyboard-first navigation
- Three in-bar categories on hover: **Files**, **Commands**, **Clipboard**
- Built-in actions: calculator, web search, system commands, and more
- Acrylic glassmorphism background with system-aware light/dark themes
- Command palette (`Ctrl+Shift+P`) for quick actions
- Optional system tray icon and Velopack updates

## Requirements

- Windows 10 (17763+) or Windows 11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) for development

## Quick start

```powershell
git clone <repo-url> C:\dev\Spur
cd C:\dev\Spur
dotnet build Spur.csproj
dotnet run --project Spur.csproj
```

> **Note:** Project/assembly rename from Spur → Spur is in progress. The .csproj and folder names still say `Spur` — this is a temporary state.

Press **Alt+Space** to open the launcher (after first-run onboarding).

## Icons

Brand icons live in **[`Icons/`](Icons/README.md)** — not to be confused with in-app **Lucide** UI icons.

| Purpose | File |
|---------|------|
| Executable / taskbar pinned app | `Icons/spur-launcher-256x256.ico` |
| Notification tray | `Icons/spur-launcher-16x16.ico` |
| Installer (Velopack) | `Icons/spur-launcher-256x256.ico` |

Legacy `spur-launcher-*` icons have been replaced by the `spur-launcher-*` set above.

After changing icons, run:

```powershell
.\Icons\sync-to-assets.ps1
dotnet build Spur.csproj
```

Full details: **[Icons/README.md](Icons/README.md)**.

## Release build

```powershell
.\publish.ps1
```

Produces installers under `dist/` using Velopack. Requires `vpk` (`dotnet tool install -g vpk`).

## Project layout

```
Spur/
├── Icons/              # Windows .ico brand assets (see Icons/README.md)
├── Assets/             # spur.ico (synced from Icons/)
├── Themes/             # Dark/light XAML themes + design tokens
├── Views/              # WPF UI (search bar, results, settings, command palette)
├── ViewModels/         # MVVM (MainViewModel, SettingsViewModel, CommandPaletteViewModel)
├── Services/           # Search, clipboard, hotkey, icons, commands
├── Extensions/         # IAction plugins (calc, system, AI, …)
├── new/                # UX/design specs (ux.md, design.md, …)
├── Spur.Tests/          # Unit tests
└── handoff.md          # Session handoff notes
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
| `Ctrl+Shift+P` | Command palette |
| `Win+Shift+V` | Clipboard mode *(when configured)* |

## Design docs

Product and visual specs are in [`new/ux.md`](new/ux.md) and [`new/design.md`](new/design.md).

## License

See [LICENSE](LICENSE).

