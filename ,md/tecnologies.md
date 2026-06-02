# technologies.md — Tech Stack

> **Principles:** Low RAM. Low CPU. Fast to open. Fast to build.
> Native feel. Windows-first. The standard way Windows apps are built.

---

## Stack Overview

```
┌──────────────────────────────────────────────────────┐
│                   UI Layer                           │
│              WPF (Windows Presentation Foundation)   │
│              XAML + C# + .NET 9                      │
│              CommunityToolkit.Mvvm (MVVM pattern)    │
└──────────────────────────┬───────────────────────────┘
                           │
┌──────────────────────────▼───────────────────────────┐
│                  Core / Business Logic               │
│              C# — Search, Indexing, Hotkeys          │
│              Windows APIs via P/Invoke + COM         │
│              Everything SDK (file search)            │
└──────────────────────────┬───────────────────────────┘
                           │
┌──────────────────────────▼───────────────────────────┐
│                  Persistence Layer                   │
│              SQLite via Microsoft.Data.Sqlite        │
│              Clipboard history · Frecency · Settings │
└──────────────────────────────────────────────────────┘
```

---

## Why C# + WPF + .NET 9

WPF is the proven standard for Windows productivity apps.
**Flow Launcher** — the best Windows launcher right now — is built entirely in C# + WPF.
That means a real reference codebase exists, the patterns are established, and the Windows
API access is first-class with no extra wiring.

| What you need                  | C# + WPF                        | Tauri + Rust                      |
|--------------------------------|---------------------------------|-----------------------------------|
| Global hotkeys                 | `RegisterHotKey` — 3 lines      | Needs FFI bindings                |
| Clipboard chain hook           | `AddClipboardFormatListener`    | Manual Win32 FFI                  |
| Mica / Acrylic blur            | `DwmSetWindowAttribute` — built in | Manual P/Invoke calls          |
| App icon extraction            | `System.Drawing.Icon` — native  | Needs extra crate                 |
| Windows Search index           | COM `ISearchManager` — native   | Via FFI                           |
| System tray                    | `NotifyIcon` — built in         | Tauri tray plugin                 |
| RAM at idle                    | ~25–50MB (.NET 9 NativeAOT)     | ~30–50MB                          |
| Learning curve                 | C# is easy, huge community      | Rust is hard                      |
| Reference launcher to learn from | Flow Launcher (open source)   | —                                 |

.NET 9 also ships **NativeAOT** — compile to a native Windows binary with no JIT warmup,
faster startup, and a smaller memory footprint than previous .NET versions.

---

## 1. Language — C# 13 (.NET 9)

- **C# 13** — latest version, ships with .NET 9
- Modern features used: `record` types for immutable search results, `async/await` everywhere,
  pattern matching for result type dispatch, `Span<T>` for string slicing in the fuzzy matcher
- Target framework: `net9.0-windows` — gives access to all Windows-specific APIs
- NativeAOT publishing for production builds (faster startup, smaller RAM footprint)

---

## 2. UI Framework — WPF (.NET 9)

WPF renders via DirectX. It is hardware-accelerated, supports custom control templates,
and gives full control over every pixel — which is exactly what a launcher needs.

**Why not WinUI 3:**
WinUI 3 is the future direction from Microsoft but it is still maturing — debugging tools
are weaker, there are known issues with borderless windows and transparency, and the
community is much smaller. WPF has 15+ years of answers on Stack Overflow. Use WPF now,
consider WinUI 3 in v2 once the ecosystem stabilises.

**Key WPF capabilities used:**
- `AllowsTransparency="True"` + `WindowStyle="None"` — frameless window with custom chrome
- `Window.Background` set to transparent brush — Acrylic/Mica drawn via Win32 beneath
- `RenderOptions.BitmapScalingMode` — crisp app icon rendering at any DPI
- `VirtualizingStackPanel` — only renders visible result rows, keeps scroll performant
  even with thousands of results
- `ControlTemplate` — every single control is reskinned to match the design system
- `Storyboard` + `DoubleAnimation` with `EasingFunction` — spring-like animations
- `SystemParameters.WorkArea` — position window at screen center correctly on any monitor setup

---

## 3. MVVM Pattern — CommunityToolkit.Mvvm

The app follows strict MVVM (Model–View–ViewModel).
XAML = View. C# ViewModels = state and logic. Models = data shapes.
The View never talks to Windows APIs directly — everything goes through ViewModels.

**CommunityToolkit.Mvvm** (from Microsoft) replaces boilerplate:
- `[ObservableProperty]` — auto-generates `INotifyPropertyChanged` for any field
- `[RelayCommand]` — auto-generates `ICommand` for any method
- `ObservableCollection<T>` — result list that WPF binds to, updates UI automatically
- `WeakReferenceMessenger` — loose messaging between ViewModels without coupling them

```csharp
// Example ViewModel slice
public partial class SearchViewModel : ObservableObject
{
    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SearchResult> _results = [];

    [ObservableProperty]
    private SearchResult? _selectedResult;

    [ObservableProperty]
    private Category _activeCategory = Category.Recent;

    partial void OnQueryChanged(string value) =>
        _ = SearchAsync(value);
}
```

---

## 4. Search — Fuzzy Matching

**Library:** `FuzzySharp` (C# port of the fzf algorithm)
- Handles typos, partial matches, transpositions
- Returns a score 0–100 for each candidate
- Fast enough on lists of 10,000+ items without async (sub-millisecond)

**Frecency scoring layered on top:**
```csharp
double FinalScore(double fuzzyScore, FrecencyRecord frecency)
{
    double decayedFrequency = frecency.UseCount
        / (1 + (DateTime.UtcNow - frecency.LastUsed).TotalHours * 0.1);
    return fuzzyScore * 0.7 + decayedFrequency * 0.3;
}
```
Apps you open often and recently float to the top naturally.

**Search providers run in parallel:**
```csharp
var results = await Task.WhenAll(
    _appProvider.SearchAsync(query),
    _fileProvider.SearchAsync(query),
    _settingsProvider.SearchAsync(query),
    _commandProvider.SearchAsync(query)
);
```

---

## 5. App Indexing — Windows Shell + Registry

App discovery uses three sources, merged and deduplicated:

**Start Menu shortcuts:**
```csharp
// Scans both user and system Start Menu
var startMenuPaths = new[]
{
    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
};
// Walk .lnk files recursively, resolve shortcut targets via IShellLink COM
```

**Registry (Win32 apps):**
```
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths
HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths
```

**Package manager apps (UWP / Microsoft Store):**
```csharp
// Windows.Management.Deployment namespace
var packageManager = new PackageManager();
var packages = packageManager.FindPackagesForUser(string.Empty);
```

Index is built on startup (~200ms) and refreshed via `FileSystemWatcher` on the Start Menu folder.

---

## 6. File Search — Everything SDK

For instant filename search, integrate **voidtools Everything** via its SDK.

Everything indexes the entire NTFS file system in seconds and answers queries in under 5ms.
It runs as a background service — the launcher just queries it.

```csharp
// Everything SDK via P/Invoke
[DllImport("Everything64.dll")]
private static extern void Everything_SetSearch(string lpSearchString);

[DllImport("Everything64.dll")]
private static extern bool Everything_Query(bool bWait);

[DllImport("Everything64.dll")]
private static extern uint Everything_GetNumResults();

[DllImport("Everything64.dll")]
private static extern void Everything_GetResultFullPathName(
    uint nIndex, StringBuilder lpString, uint nMaxCount);
```

Fallback if Everything is not installed: Windows Search Index via COM `ISearchManager`.
File search is disabled gracefully if neither is available.

---

## 7. Clipboard Manager — Win32 Clipboard Chain

```csharp
// Register as clipboard listener (no polling, event-driven)
[DllImport("user32.dll")]
private static extern bool AddClipboardFormatListener(IntPtr hwnd);

// Handle WM_CLIPBOARDUPDATE in WndProc
protected override void WndProc(ref Message m)
{
    if (m.Msg == WM_CLIPBOARDUPDATE)
        OnClipboardChanged();
    base.WndProc(ref m);
}
```

On every clipboard change, read content, detect type (text / image / file path),
store in SQLite with timestamp. No polling. Zero CPU at idle.

---

## 8. Global Hotkey — Win32 RegisterHotKey

```csharp
public class HotkeyService
{
    private const int WM_HOTKEY = 0x0312;

    public void Register(IntPtr hwnd, int id, ModifierKeys modifiers, Key key)
    {
        RegisterHotKey(hwnd, id, (uint)modifiers, (uint)KeyInterop.VirtualKeyFromKey(key));
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(
        IntPtr hWnd, int id, uint fsModifiers, uint vk);
}
```

Default: `Alt+Space`. User-configurable. Works even when the app window is not focused.

---

## 9. Acrylic / Mica Window Blur

```csharp
public static class WindowBlur
{
    // Windows 11 — Mica Alt (best quality)
    public static void EnableMica(IntPtr hwnd)
    {
        int backdropType = 4; // DWMSBT_MAINWINDOW (Mica Alt)
        DwmSetWindowAttribute(hwnd, 38, ref backdropType, sizeof(int));
    }

    // Windows 10 fallback — Acrylic blur behind
    public static void EnableAcrylic(IntPtr hwnd)
    {
        var accent = new AccentPolicy { AccentState = 4 }; // ACCENT_ENABLE_ACRYLICBLURBEHIND
        // ... SetWindowCompositionAttribute
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}
```

Detected at runtime — Mica on Win11, Acrylic fallback on Win10. No user configuration needed.

---

## 10. Database — SQLite via Microsoft.Data.Sqlite

Used for clipboard history, frecency data, and user settings.

```csharp
// Clipboard history schema
CREATE TABLE clipboard_history (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    content   TEXT,
    type      TEXT CHECK(type IN ('text', 'image', 'path')),
    timestamp INTEGER NOT NULL,
    pinned    INTEGER DEFAULT 0
);

CREATE TABLE frecency (
    id        TEXT PRIMARY KEY,
    use_count INTEGER DEFAULT 0,
    last_used INTEGER NOT NULL,
    score     REAL DEFAULT 0.0
);

CREATE TABLE settings (
    key   TEXT PRIMARY KEY,
    value TEXT NOT NULL
);
```

**Why not JSON files:**
SQLite handles concurrent reads, has proper indexing for clipboard search,
and never corrupts on an unexpected shutdown. The db file stays under 10MB for years of use.

---

## 11. Module System — .NET Assembly Loading

Modules are C# class libraries (`.dll`) or scripts that implement a shared interface.

```csharp
// IModule interface — every module implements this
public interface IModule
{
    string Id { get; }
    string Name { get; }
    string TriggerKeyword { get; }
    IEnumerable<SearchResult> Query(string input);
    Task ExecuteAsync(SearchResult result);
}
```

Modules are loaded at startup via `AssemblyLoadContext`:
```csharp
var context = new AssemblyLoadContext(module.Id, isCollectible: true);
var assembly = context.LoadFromAssemblyPath(module.DllPath);
var moduleType = assembly.GetTypes()
    .First(t => typeof(IModule).IsAssignableFrom(t));
var instance = (IModule)Activator.CreateInstance(moduleType)!;
```

`isCollectible: true` means modules can be unloaded without restarting the app.

---

## 12. NuGet Packages

```xml
<ItemGroup>
  <!-- MVVM -->
  <PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />

  <!-- Fuzzy search -->
  <PackageReference Include="FuzzySharp" Version="2.*" />

  <!-- SQLite -->
  <PackageReference Include="Microsoft.Data.Sqlite" Version="9.*" />

  <!-- Icon extraction -->
  <PackageReference Include="System.Drawing.Common" Version="9.*" />

  <!-- HTTP (currency rates) -->
  <PackageReference Include="System.Net.Http.Json" Version="9.*" />

  <!-- Logging -->
  <PackageReference Include="Serilog" Version="4.*" />
  <PackageReference Include="Serilog.Sinks.File" Version="6.*" />

  <!-- Unit testing -->
  <PackageReference Include="xunit" Version="2.*" />
  <PackageReference Include="FluentAssertions" Version="6.*" />
</ItemGroup>
```

No unnecessary packages. Everything else is in the .NET 9 BCL.

---

## 13. Project Structure

```
NovaLauncher/
├── NovaLauncher.App/              ← WPF app project (.csproj)
│   ├── App.xaml / App.xaml.cs    ← App entry, DI setup, tray icon
│   ├── MainWindow.xaml           ← Launcher window (frameless)
│   ├── Views/
│   │   ├── SearchBarView.xaml
│   │   ├── SidebarView.xaml
│   │   ├── ResultListView.xaml
│   │   ├── ClipboardView.xaml
│   │   ├── CalculatorView.xaml
│   │   └── SettingsWindow.xaml
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   ├── SearchViewModel.cs
│   │   ├── SidebarViewModel.cs
│   │   ├── ClipboardViewModel.cs
│   │   ├── CalculatorViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Styles/
│   │   ├── Colors.xaml           ← Design token resource dictionary
│   │   ├── Typography.xaml
│   │   ├── Controls.xaml         ← Button, TextBox, ListBox templates
│   │   └── Animations.xaml
│   └── Assets/
│       └── Fonts/
│           └── Inter-Variable.ttf
│
├── NovaLauncher.Core/             ← Class library — pure C#, no WPF
│   ├── Search/
│   │   ├── ISearchProvider.cs
│   │   ├── SearchOrchestrator.cs
│   │   ├── FuzzyMatcher.cs
│   │   └── FrecencyScorer.cs
│   ├── Providers/
│   │   ├── AppSearchProvider.cs
│   │   ├── FileSearchProvider.cs
│   │   ├── SettingsSearchProvider.cs
│   │   └── CommandSearchProvider.cs
│   ├── Clipboard/
│   │   └── ClipboardService.cs
│   ├── Hotkey/
│   │   └── HotkeyService.cs
│   ├── Modules/
│   │   ├── IModule.cs
│   │   └── ModuleLoader.cs
│   ├── Database/
│   │   └── DatabaseService.cs
│   └── Models/
│       ├── SearchResult.cs
│       ├── ClipboardItem.cs
│       └── FrecencyRecord.cs
│
├── NovaLauncher.Modules/          ← First-party modules (each a separate project)
│   ├── NovaLauncher.Module.Spotify/
│   ├── NovaLauncher.Module.VSCode/
│   └── NovaLauncher.Module.Emoji/
│
├── NovaLauncher.Tests/            ← xUnit test project
│   ├── Search/
│   │   ├── FuzzyMatcherTests.cs
│   │   └── FrecencyScorerTests.cs
│   └── Providers/
│       └── AppSearchProviderTests.cs
│
└── NovaLauncher.sln
```

Core is separated from App so it can be tested without a UI and reused by modules.

---

## 14. Performance Targets

| Metric                        | Target       | How                                                  |
|-------------------------------|--------------|------------------------------------------------------|
| Hotkey → window visible       | < 80ms       | Window pre-created and hidden, just `Show()` called  |
| First results shown           | < 50ms       | In-memory app index, parallel async providers        |
| RAM at idle (background)      | < 35MB       | .NET 9 NativeAOT, no JIT overhead                   |
| RAM when open                 | < 70MB       | VirtualizingStackPanel keeps result DOM lean         |
| CPU at idle                   | < 0.1%       | No polling — all event-driven (clipboard, hotkey)    |
| Installer size                | < 15MB       | NativeAOT self-contained, no .NET runtime required   |
| App index rebuild             | < 300ms      | Incremental via FileSystemWatcher                    |

---

## 15. IDE & Tooling Setup (Zed)

Since you're using **Zed** as your editor:

**Zed extensions to install:**
- `C#` — Roslyn language server (OmniSharp / csharp-ls)
- `XML` — for XAML syntax highlighting (Zed treats XAML as XML)
- `TOML` / `JSON` — for config files

**Note on Zed + WPF XAML:**
Zed's XAML support is basic — no WPF-specific IntelliSense for bindings.
For XAML editing specifically, keep **Visual Studio Community 2022** (free) available
as a secondary tool. You can write all C# in Zed and switch to VS only when designing complex XAML.
Alternatively, **JetBrains Rider** has excellent XAML support and works well with Zed workflows.

**DeepSeek prompting tips for this stack:**
- Always paste the relevant ViewModel + its XAML binding together when asking about data binding issues
- For Win32 P/Invoke signatures, ask DeepSeek to reference `pinvoke.net` for the exact signature
- When asking about WPF animations, specify "WPF Storyboard" not just "animation" to avoid CSS/web answers
- Paste your `.csproj` when asking about NuGet or build issues

---

## 16. Distribution

- **Build:** `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true`
- **Installer:** **Inno Setup** (free, simple, generates a clean `.exe` installer)
- **Portable:** Single `.exe` drop — NativeAOT means no .NET runtime install needed on user's machine
- **Auto-update:** `Squirrel.Windows` or `NetSparkleUpdater` — checks GitHub Releases
- **Code signing:** Windows Authenticode via `signtool.exe` (prevents SmartScreen warning)
- **Distribution:** GitHub Releases → `.exe` installer + portable `.zip`
