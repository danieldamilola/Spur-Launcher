<p align="center">
  <img src="Icons/spur-256x256.ico" alt="Spur" width="96" height="96" />
</p>

<h1 align="center">Spur</h1>

<p align="center">
  <b>Spur your workflow.</b><br/>
  A fast, keyboard-first launcher for Windows — one search field, Spotlight-style categories, and instant actions.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4?logo=windows11&logoColor=white" alt="Platform" />
  <img src="https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 9" />
  <img src="https://img.shields.io/badge/UI-WPF-68217A?logo=xaml&logoColor=white" alt="WPF" />
  <img src="https://img.shields.io/github/license/danieldamilola/Spur-Launcher" alt="License" />
</p>

---

## ✨ Features

| Category | Details |
|----------|---------|
| **🔍 Universal Search** | Apps, files, clipboard history, system settings, and web — all from one bar |
| **⚡ Instant Actions** | Calculator, timer, color picker, currency converter, password generator, IP lookup |
| **🤖 AI Assistant** | Built-in chat powered by Groq, Gemini, OpenRouter, or DeepSeek |
| **📋 Clipboard Manager** | Persistent clipboard history with search (`Win+Shift+V`) |
| **🎨 Glassmorphism UI** | Acrylic background with system-aware light / dark themes |
| **🧩 Add-on System** | Pluggable `IAddOn` architecture — enable, disable, or extend functionality |
| **🖥️ View Modes** | Compact (minimal) and Expanded (full-featured) window modes |
| **🔑 Global Hotkey** | `Alt+Space` (configurable) to summon the launcher instantly |

### Built-in Add-ons

| Add-on | Keyword | Description |
|--------|---------|-------------|
| Calculator | `=` or math expression | Inline math evaluation |
| Timer | `timer 5m` | Countdown timer with notification |
| Color Picker | `#ff0055` | Hex/RGB color preview |
| Currency | `100 usd to eur` | Real-time currency conversion |
| Password | `pw 16` | Secure random password generator |
| IP Address | `ip` | Public + local IP lookup |
| Quick Note | `note` | Save quick notes to `.txt` files |
| Screenshot | `ss` | Capture screen to clipboard/file |
| Shell | `>` | Run shell commands |
| System | `shutdown`, `restart`, etc. | Power and system commands |
| Kill Process | `kill` | Find and terminate running processes |
| AI Assistant | `ai` | Chat with AI models |

---

## 🚀 Getting Started

### Requirements

- **Windows 10** (build 17763+) or **Windows 11**
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Build & Run

```powershell
git clone https://github.com/danieldamilola/Spur-Launcher.git
cd Spur-Launcher
dotnet build Spur.csproj
dotnet run --project Spur.csproj
```

Press **`Alt+Space`** to open the launcher after first-run onboarding.

### Release Build

```powershell
.\publish.ps1
```

Produces installers under `dist/` using [Velopack](https://velopack.io). Requires `vpk` (`dotnet tool install -g vpk`).

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Alt+Space` | Toggle launcher |
| `Escape` | Step back / dismiss |
| `↑` / `↓` | Navigate results |
| `Enter` | Open / execute |
| `Tab` | Autocomplete |
| `Ctrl+1` / `2` / `3` | Switch to Files / Clipboard / Commands |
| `Ctrl+,` | Open Settings |
| `Ctrl+Shift+P` | Command Palette |
| `Win+Shift+V` | Clipboard Manager *(when configured)* |

---

## 🏗️ Architecture

```
Spur/
├── Extensions/         # IAddOn plugin system (calculator, AI, timer, …)
│   ├── AddOns/         #   Individual add-on implementations
│   ├── IAddOn.cs       #   Core add-on interface
│   └── AddOnRegistry.cs#   Runtime add-on discovery & management
├── Views/              # WPF UI (search bar, results, settings, add-on panels)
├── ViewModels/         # MVVM (MainViewModel, SettingsViewModel)
├── Models/             # Data models (SpurConfig, SearchResult, ClipboardEntry)
├── Services/           # Core services (search, clipboard, hotkey, indexing)
├── Behaviors/          # WPF attached behaviors (window positioning, keyboard)
├── Helpers/            # Utilities (markdown rendering, fuzzy matching)
├── Converters/         # XAML value converters
├── Themes/             # Dark / light theme XAML + design tokens + fonts
├── Icons/              # Windows .ico brand assets (16px → 1024px)
└── Assets/             # In-app PNG icons for add-ons and UI
```

### Key Design Decisions

- **Registry-based add-on system** — All add-ons implement `IAddOn` and are auto-discovered by `AddOnRegistry`. No hard-coded action maps.
- **Instant-save settings** — Every setting change persists immediately to `config.json` via `SaveAndApply()`.
- **WPF + .NET 9** — Modern WPF with `AllowsTransparency` for acrylic glassmorphism and drop shadows.
- **Keyboard-first** — Every action reachable without a mouse. Category switching, result selection, and execution all via keyboard.

---

## 🎨 Theming

Spur supports **Dark** and **Light** themes with automatic system detection. Theme files:

| File | Purpose |
|------|---------|
| `Themes/DarkTheme.xaml` | Dark mode colors and brushes |
| `Themes/LightTheme.xaml` | Light mode colors and brushes |
| `Themes/Tokens.xaml` | Design tokens (spacing, radii, fonts) |
| `Themes/CommonStyles.xaml` | Shared styles and templates |
| `Themes/Animations.xaml` | Micro-animation definitions |

---

## 🔌 Creating Add-ons

Implement the `IAddOn` interface and register in `AddOnRegistry`:

```csharp
public class MyAddOn : IAddOn
{
    public string Id => "myaddon";
    public string Name => "My Add-on";
    public string Keyword => "my";
    public string IconGlyph => "\ue113";
    public string IconPath => "/Assets/Icons/settings.png";
    public string Description => "Does something awesome";

    public bool CanHandle(string query) => query.StartsWith("my ");
    public Task<AddOnResult> ExecuteAsync(string query) => ...;
}
```

---

## 📦 Icons

Brand icons live in [`Icons/`](Icons/README.md) at standard Windows sizes:

| Size | File | Usage |
|------|------|-------|
| 16×16 | `spur-16x16.ico` | System tray |
| 24×24 | `spur-24x24.ico` | Small UI elements |
| 32×32 | `spur-32x32.ico` | Window title bar |
| 48×48 | `spur-48x48.ico` | Taskbar |
| 64×64 | `spur-64x64.ico` | Settings / About |
| 96×96 | `spur-96x96.ico` | High-DPI displays |
| 128×128 | `spur-128x128.ico` | Large icons |
| 256×256 | `spur-256x256.ico` | Application icon / installer |
| 512×512 | `spur-512x512.ico` | Store listing |
| 1024×1024 | `spur-1024x1024.ico` | Marketing |

After changing icons, sync to assets:

```powershell
.\Icons\sync-to-assets.ps1
dotnet build Spur.csproj
```

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

<p align="center">
  Built with ❤️ by <a href="https://github.com/danieldamilola">Daniel Adams</a>
</p>
