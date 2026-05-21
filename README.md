# Volt

A fast, minimal app launcher for Windows — inspired by Raycast and Alfred, built with WPF and .NET 9.

![WPF](https://img.shields.io/badge/WPF-.NET%209-512BD4?style=flat&logo=dotnet)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4?style=flat&logo=windows)
![License](https://img.shields.io/github/license/danieldamilola/Volt)

---

## Features

- **Instant search** — apps, files, and system actions with fuzzy matching
- **Extensions** — built-in calculator, color picker, clipboard manager, timer, IP address, and AI assistant
- **AI integration** — ask questions directly via Groq API
- **Settings panel** — theme, font size, border radius, hotkey, and more
- **Global hotkey** — `Alt + Space` to show/hide from anywhere
- **Minimal UI** — floating window, dark-first design, no taskbar clutter

---

## Getting Started

### Requirements

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Run

```bash
git clone https://github.com/danieldamilola/Volt.git
cd Volt
dotnet run
```

---

## Usage

| Key | Action |
|---|---|
| `Alt + Space` | Toggle window |
| `↑ / ↓` | Navigate results |
| `Enter` | Open selected result |
| `Esc` | Dismiss |
| Type `settings` | Open settings panel |
| Type `calc ...` | Calculator |
| Type `color` | Color picker |
| Type `clip` | Clipboard history |
| Type `timer` | Timer |
| Type `ip` | Show IP address |
| Type `ai ...` | Ask AI (requires Groq API key) |

---

## Tech Stack

- **WPF** (.NET 9, C#)
- **CommunityToolkit.Mvvm** — MVVM pattern
- **ResourceDictionary** — token-based theming
- **Groq API** — AI assistant

---

## Configuration

Settings are stored at `%AppData%\Flow\config.json`. You can also edit them via the in-app settings panel (`type "settings"`).

---

## License

MIT
