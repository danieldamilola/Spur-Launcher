# Spur — Architecture

## Overview

Spur is a Windows desktop application launcher built with WPF and C# (.NET). It provides a keyboard-driven command palette for launching apps, running quick actions, managing clipboard history, and interacting with AI — all from a single hotkey-triggered overlay window.

It follows the **MVVM (Model-View-ViewModel)** pattern throughout, with a service layer handling all business logic and side effects.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Language | C# (.NET) |
| UI Framework | WPF (Windows Presentation Foundation) |
| Architecture Pattern | MVVM |
| Local Storage | File-based config (JSON via `SpurConfig`) |
| Secure Storage | Windows Credential Store (via `SecureStorageService`) |
| Search | Custom fuzzy search (`FuzzySearch.cs`) |
| AI | External API via `AiService` |
| Packaging | Squirrel (auto-update), Inno Setup (installer), Portable ZIP |
| Tests | xUnit / MSTest (`Spur.Tests`) |

---

## Project Structure

```
Spur/
├── Root files              # Entry points, project config, build scripts
├── Assets/                 # Static assets (currently empty)
├── Behaviors/              # Attached behaviors for WPF controls
├── Converters/             # IValueConverter implementations for XAML bindings
├── Extensions/             # Pluggable action modules (the extension system)
├── Helpers/                # Utility classes
├── Icons/                  # App icon assets at multiple resolutions
├── Models/                 # Data models and constants
├── Services/               # All business logic and system integrations
├── Themes/                 # XAML resource dictionaries (styles, themes, animations)
├── Tools/                  # Dev tooling (currently empty)
├── ViewModels/             # MVVM ViewModels
├── Views/                  # WPF XAML views and code-behind
└── Spur.Tests/             # Unit test project
```

---

## Entry Points

| File | Purpose |
|---|---|
| `App.xaml` / `App.xaml.cs` | Application entry point; bootstraps services and global resources |
| `MainWindow.xaml` / `MainWindow.xaml.cs` | Root window; hosts the launcher overlay |
| `GlobalUsings.cs` | Global `using` directives shared across the project |
| `SpurMotion.cs` | Window animation and motion system |
| `WindowBlur.cs` | Win32 interop for window blur/acrylic effect |

---

## Architecture Layers

### 1. Models (`Models/`)

Pure data structures with no behavior. No dependencies on services or ViewModels.

| File | Purpose |
|---|---|
| `SpurConfig.cs` | Root configuration object (user settings, persisted to disk) |
| `SpurConstants.cs` | App-wide constants |
| `SearchResult.cs` | Represents a single item in the results list |
| `CommandPaletteItem.cs` | Item shape for the command palette |
| `ClipboardEntry.cs` / `PinnedClipboardItem.cs` | Clipboard history data |
| `ActionSettings.cs` | Per-action configuration |
| `AppIcons.cs` | Icon cache model |
| `Category.cs` | Search result categorisation |
| `LauncherLayout.cs` | Layout/positioning config for the launcher window |
| `Messages.cs` | Inter-component messaging contracts |
| `ScopeFilterItem.cs` | Scope/filter bar item model |

---

### 2. Services (`Services/`)

All side effects, system access, and business logic live here. Services are injected into ViewModels and do not reference Views.

#### Core Search & Discovery
| Service | Responsibility |
|---|---|
| `AppDiscoveryService` | Scans the system for installed applications |
| `FileSearchService` | Searches the file system for files and folders |
| `FuzzySearch` | Custom fuzzy-match algorithm used across all search surfaces |
| `FrequencyService` | Tracks launch frequency to boost relevant results |
| `SearchEngineService` | Routes search queries to the correct backend (apps, files, web) |
| `CommandRegistry` / `ICommandRegistry` | Registers and resolves all available commands and actions |

#### System Integration
| Service | Responsibility |
|---|---|
| `HotkeyService` | Registers and listens for global hotkeys (Win32) |
| `ClipboardService` / `ClipboardWatcher` | Reads clipboard and watches for changes |
| `StartupService` | Manages Windows startup registration |
| `IconService` | Loads and caches app/file icons |
| `NotificationService` | Sends system tray notifications |

#### Configuration & Security
| Service | Responsibility |
|---|---|
| `ConfigService` | Reads/writes `SpurConfig` to disk |
| `ConfigValidator` | Validates config on load and surfaces errors |
| `SecureStorageService` / `ISecureStorageService` | Stores secrets (API keys) in Windows Credential Store |
| `ThemeManager` | Applies dark/light theme and responds to system theme changes |

#### AI
| Service | Responsibility |
|---|---|
| `AiService` | Handles requests to the configured AI API endpoint |

#### Utilities
| Service | Responsibility |
|---|---|
| `ILogger` | Logging abstraction |
| `SafeFireAndForget` | Extension for safely executing async fire-and-forget tasks |

---

### 3. Extensions (`Extensions/`)

Self-contained action modules that plug into the command palette via `IAction`. Each action handles its own input matching, execution, and result rendering.

| Action | What it does |
|---|---|
| `AiAction` | Routes queries to the AI chat surface |
| `CalculatorAction` | Evaluates math expressions inline |
| `ColorAction` | Parses and converts color values (hex, rgb, hsl) |
| `CurrencyAction` | Converts between currencies |
| `IpAction` | Looks up IP address information |
| `KillProcessAction` | Lists and terminates running processes |
| `PasswordGenAction` | Generates secure passwords |
| `QuickNoteAction` | Creates quick notes |
| `ScreenshotAction` | Triggers a screenshot |
| `SettingsAction` | Opens the Spur settings window |
| `SystemAction` | System commands (lock, shutdown, restart, sleep) |
| `TimerAction` | Sets countdown timers |
| `WindowBackdrop` | Shared helper for action window backdrop effects |

All actions implement `IAction`, which the `CommandRegistry` uses to discover and route queries at runtime.

---

### 4. ViewModels (`ViewModels/`)

Bind the service layer to the view layer. No direct UI dependencies — only commands, observable properties, and injected services.

| ViewModel | Owns |
|---|---|
| `MainViewModel` | Launcher lifecycle, search orchestration, result list state |
| `CommandPaletteViewModel` | Command palette mode and item selection |
| `ClipboardViewModel` | Clipboard history list and pinning |
| `AiChatViewModel` | AI chat session state |
| `SettingsViewModel` | Settings form state and save logic |
| `TimerViewModel` | Active timer state and countdown |

---

### 5. Views (`Views/`)

XAML-only UI with minimal code-behind. Code-behind is limited to view-specific logic that cannot be expressed in XAML (animations, focus management, Win32 interop).

| View | Purpose |
|---|---|
| `SearchBar` | Primary text input |
| `UnifiedResultsView` | Renders the combined results list |
| `CommandPalette` | Command mode overlay |
| `ScopeBar` | Scope/filter selector |
| `CategoryCircle` | Visual category indicator on results |
| `ClipboardManager` | Clipboard history panel |
| `OnboardingWindow` | First-run onboarding flow |
| `Settings/SettingsWindow` + `SettingsView` | Full settings UI |

---

### 6. Themes (`Themes/`)

All visual styling defined in XAML resource dictionaries. No hardcoded styles in code-behind.

| File | Purpose |
|---|---|
| `DesignTokens/` | Base design tokens (colors, spacing, radii) |
| `DarkTheme.xaml` / `LightTheme.xaml` | Theme-specific token overrides |
| `CommonStyles.xaml` | Shared control styles applied across all themes |
| `ResultTemplates.xaml` | `DataTemplate` definitions for result item types |
| `Animations.xaml` | Reusable storyboard animations |
| `Fonts/` | Bundled font files |

---

### 7. Converters (`Converters/`)

`IValueConverter` implementations used in XAML data bindings.

| Converter | Converts |
|---|---|
| `HexToColorConverter` | Hex string → `Color` |
| `IntEqConverter` | Integer equality → `bool` |
| `OpacityConverter` | Numeric value → opacity double |
| `PathToIconConverter` | File path → icon image source |
| `PathTrimConverter` | Long file path → trimmed display string |
| `ScopeEqualsConverter` | Scope enum equality → `bool` |
| `StringEqConverter` | String equality → `bool` |
| `VisibilityConverters` | Bool/null → `Visibility` |

---

### 8. Behaviors (`Behaviors/`)

| File | Purpose |
|---|---|
| `LauncherWindowBehavior` | Attached behavior controlling launcher window show/hide and positioning |

---

## Data Flow — Search Query

```
User types in SearchBar
        │
        ▼
MainViewModel.OnSearchChanged()
        │
        ├──► CommandRegistry.TryMatchExtension()  ──► Extension handles query
        │                                               (Calculator, AI, Color, etc.)
        │
        └──► SearchEngineService.SearchAsync()
                    │
                    ├──► AppDiscoveryService  (installed apps)
                    ├──► FileSearchService   (files & folders)
                    └──► FuzzySearch         (score & rank results)
                                │
                                ▼
                    FrequencyService.ApplyBoost()
                                │
                                ▼
                    MainViewModel.Results (ObservableCollection)
                                │
                                ▼
                    UnifiedResultsView renders list
```

---

## Data Flow — Action Execution

```
User presses Enter on a result
        │
        ▼
MainViewModel.ExecuteSelected()
        │
        ├── App result    ──► Process.Start()
        ├── File result   ──► Shell open
        ├── Extension     ──► IAction.ExecuteAsync()
        └── Command       ──► CommandRegistry.Invoke()
                │
                ▼
        FrequencyService.Record()   (boosts item in future searches)
        MainViewModel.HideLauncher()
```

---

## Data Flow — Clipboard

```
System clipboard changes
        │
        ▼
ClipboardWatcher (Win32 message hook)
        │
        ▼
ClipboardService.OnClipboardChange()
        │
        ▼
ClipboardViewModel.Entries (ObservableCollection<ClipboardEntry>)
        │
        ▼
ClipboardManager view renders history
```

---

## Configuration

User settings are stored as a JSON file on disk, managed by `ConfigService`. On load, `ConfigValidator` checks for missing or invalid values and applies defaults. Secrets (AI API keys) are stored separately in the Windows Credential Store via `SecureStorageService` and never written to the config file.

---

## Build & Distribution

| Output | Tool | Location |
|---|---|---|
| Debug build | MSBuild | `Bin/Debug/` |
| Self-contained publish | `publish.ps1` | `Out/app/` |
| Packaged app | `publish.ps1` | `Publish/app/` |
| Installer (Setup.exe) | Inno Setup (`installer.iss`) | `Dist/` |
| Portable ZIP | `publish.ps1` | `Dist/Spur-win-Portable.zip` |
| Auto-update package | Squirrel | `Dist/Spur-*-full.nupkg`, `Dist/RELEASES` |

The `publish.ps1` script orchestrates the full release pipeline: dotnet publish → Inno Setup → Squirrel packaging → output to `Dist/`.

---

## Tests (`Spur.Tests/`)

| File | Covers |
|---|---|
| `FuzzySearchTests.cs` | Fuzzy match accuracy and ranking |
| `MainViewModelTests.cs` | Search orchestration, result state |
| `SpurConfigValidationTests.cs` | Config edge cases and validation rules |