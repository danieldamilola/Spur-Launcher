# Handoff: Plugin Architecture — Flow-Like Multi-Project Redesign

**Date:** 2026-07-13
**Session type:** Coding / Architecture
**Status:** In Progress
**Project root:** `C:\dev\Spur`

---

## 1. Goal

Restructure Spur's monolithic search logic into a plugin-based architecture matching Flow Launcher's design: a separate `Spur.Plugin` SDK project defining `IPlugin`, `Spur.Core` for `PluginManager`, `Spur.Infrastructure` for shared services, and individual plugins (Apps, Files, Clipboard, Commands, Settings, Web) that each own their search domain. `MainViewModel` orchestrates via `PluginManager` and no longer holds forwarding properties — XAML binds directly to `ResultsVm.*`.

---

## 2. Current State

### Complete
- **Multi-project solution:** `Spur.Plugin` (net9.0, SDK contract), `Spur.Core` (net9.0-windows, PluginManager/PluginPair), `Spur.Infrastructure` (net9.0-windows, shared services)
- **`Spur.Plugin`** defines: `IPlugin`, `Query`, `PluginResult`, `PluginInitContext`, `PluginMetadata`, `PluginActionContext`, `IPublicAPI`
  - `PluginResult` has `Source: object?` property — built-in plugins set this to the full `SearchResult` so no properties are lost
- **`Spur.Core.PluginManager`** accepts `IEnumerable<IPlugin>` via constructor, registers by action keyword, parallel queries via `Task.WhenAll`
- **6 individual plugins** in `C:\dev\Spur\Plugins\`:
  - `AppsPlugin` — app launcher search (uses `PluginHelper.ToPluginResult` which sets `Source`)
  - `FilesPlugin` — file search (sets `Source`)
  - `ClipboardPlugin` — clipboard history search (creates `SearchResult` + sets `Source`)
  - `CommandsPlugin` — add-ons, URL detection, action catalog (sets `Source` for add-ons)
  - `SettingsPlugin` — Windows Settings pages (creates `SearchResult` + sets `Source`)
  - `WebPlugin` — web search
- **`PluginHelper`** — shared `MatchScore()`, `ToPluginResult()`, `LooksLikeUrl()`, `NormalizeUrl()`
- **`MainViewModel.SearchAsync`**: global search → `PluginManager.QueryAsync()`; scoped search → `SearchEngineService` directly
  - Plugin results with `Source` set: uses `SearchResult` directly (preserves Type, ExePath, ActionId, etc.)
  - Plugin results without `Source` (external plugins): fallback mapping to new `SearchResult` (limited properties)
- **`MainViewModel` forwarding properties removed** (`Results`, `ActiveScopeId`, `ScopeFilters`, `HasResults`, `IsScopeBarVisible`); XAML binds directly to `ResultsVm.*`
- **WPF project reference workaround** in `Spur.csproj`: builds deps via `<Target>` first, then references Spur.Core/Infrastructure as binary `<Reference>`
- **DI** in `App.xaml.cs` registers `PluginManager` singleton + all 6 `IPlugin` implementations
- **`LegacySearchAdapter`** still exists in code (and used by tests) but no longer registered in DI
- **Build:** 0 errors, 763 warnings (pre-existing CA1416, CS0067)
- **Tests:** 61/61 pass

### In Progress / Partial
- Scoped search (`ActiveCategory != null`) still routes through `SearchEngineService` directly — not yet migrated to plugin keywords
- `SearchEngineService` still referenced by scoped-search path — not yet removable
- `PluginResult.Action` delegate not yet wired into `OpenSelected` — external plugins won't execute when selected
- `Spur.Core` and `Spur.Infrastructure` exist but most services remain in the main project

### Not Started
- Migrate scoped search to plugin keywords so `SearchEngineService` can be deleted
- Move shared services (logger, image loader, startup, theme) into `Spur.Infrastructure`
- Move `PluginManager` and plugins back into `Spur.Core` once WPF project reference issue is resolved
- WPF `_wpftmp` intermediate project issue (CS0579/CS0433 with `<ProjectReference>`) — workaround via binary reference

### Last Action
Updated `PluginResult` with `Source: object?` property; all 6 plugins set `Source` to the underlying `SearchResult`; `SearchAsync` uses `Source` when available instead of re-building with lost properties.

---

## 3. What We're Currently Working On

Preserving full `SearchResult` data through the plugin pipeline. Previously, plugins returned `PluginResult` with limited properties and `MainViewModel.SearchAsync` re-created `SearchResult` from those fields, losing `Type`, `ExePath`, `ActionId`, `ClipContent`, `ClipImage`, etc. Now `PluginResult.Source` carries the original `SearchResult`, and `SearchAsync` uses it directly when available.

---

## 4. What We Tried That Failed

- **Direct `<ProjectReference>` from WPF project to Spur.Core/Infrastructure:** WPF's `_wpftmp` intermediate project duplicates sources, causing CS0579 (duplicate `AssemblyInfo`) and CS0433 (duplicate type). Workaround: build via `<Target>` + binary `<Reference>`.
- **Re-using `PluginResult` as the UI result item:** `PluginResult` is in `Spur.Plugin` (net9.0) and can't reference WPF types like `BitmapSource`. Cannot implement `IResultItem` without project coupling. Solution: keep `PluginResult` as transport, use `Source` to carry the real `SearchResult`.
- **Moving `SearchResult` into `Spur.Plugin`:** Requires WPF types (`BitmapSource`) which would force `Spur.Plugin` to `net9.0-windows`, breaking the clean SDK contract for external plugin authors.

---

## 5. Next Steps

1. Wire `PluginResult.Action` delegate into `OpenSelected` so external plugins can execute when selected
2. Migrate scoped search (`ActiveCategory != null`) to plugin action-keyword routing so `SearchEngineService` can be deleted
3. Move shared services (logger, `IconService`, startup, theme) into `Spur.Infrastructure`
4. Clean up `LegacySearchAdapter.cs` (dead code) once tests no longer reference it

### Longer Roadmap
- Move `PluginManager` and all plugins back into `Spur.Core` once WPF project reference issue is resolved
- Support external plugin loading from disk (`PluginManager.LoadFromDirectory()`)

---

## 6. Key Decisions & Constraints

- **`PluginResult.Source` pattern:** Instead of bloating `PluginResult` with all `SearchResult` properties (which would require WPF in `Spur.Plugin`), carry the full object as `Source: object?`. Built-in plugins always set it; external plugins won't.
- **`Spur.Plugin` stays `net9.0` (not `net9.0-windows`):** Keeps it clean for cross-platform plugin authors. WPF types stay in the main project.
- **Binary reference for project deps:** `Spur.csproj` builds Spur.Core/Infrastructure via `<Target Name="BuildDeps"...>` then adds as `<Reference>` to avoid WPF `_wpftmp` source duplication.
- **`IPlugin` interface is synchronous-style:** `QueryAsync` returns `Task<List<PluginResult>>` rather than writing to a channel. Simple, composable, testable.
- **Plugin action keywords are empty** for now (all plugins match global search). Future: `clip:` keyword → ClipboardPlugin only, etc.
- **No plugin metadata files** yet — metadata is hardcoded in each plugin's properties (Id, Name, Description, IconGlyph, ActionKeyword).

---

## 7. Open Questions & Blockers

- **How to wire `PluginResult.Action` into `OpenSelected`?** Currently `SearchResult` cases dispatch by `Type`/`ActionId`. External plugins provide a `Func<PluginActionContext, Task<bool>>` delegate — needs a fallback handler.
- **PluginManager constructor** takes `IEnumerable<IPlugin>` — should metadata be injected separately?
- **No external plugin loading** yet. Assembly loading from `%APPDATA%\Spur\Plugins` not implemented.

---

## 8. Files & Artifacts

| Item | Type | Location / Description |
|------|------|------------------------|
| `Spur.Plugin.csproj` | project | `C:\dev\Spur\Spur.Plugin\Spur.Plugin.csproj` — SDK contract (net9.0) |
| `IPlugin.cs` | interface | `C:\dev\Spur\Spur.Plugin\IPlugin.cs` |
| `PluginResult.cs` | class | `C:\dev\Spur\Spur.Plugin\PluginResult.cs` — result type with `Source` payload |
| `PluginInitContext.cs` | class | `C:\dev\Spur\Spur.Plugin\PluginInitContext.cs` |
| `PluginMetadata.cs` | class | `C:\dev\Spur\Spur.Plugin\PluginMetadata.cs` |
| `PluginActionContext.cs` | class | `C:\dev\Spur\Spur.Plugin\PluginActionContext.cs` |
| `IPublicAPI.cs` | interface | `C:\dev\Spur\Spur.Plugin\IPublicAPI.cs` |
| `Query.cs` | class | `C:\dev\Spur\Spur.Plugin\Query.cs` |
| `Spur.Core.csproj` | project | `C:\dev\Spur\Spur.Core\Spur.Core.csproj` (net9.0-windows) |
| `PluginManager.cs` | class | `C:\dev\Spur\Spur.Core\PluginManager.cs` |
| `PluginPair.cs` | class | `C:\dev\Spur\Spur.Core\PluginPair.cs` |
| `Spur.Infrastructure.csproj` | project | `C:\dev\Spur\Spur.Infrastructure\Spur.Infrastructure.csproj` (net9.0-windows, empty) |
| `AppsPlugin.cs` | plugin | `C:\dev\Spur\Plugins\AppsPlugin.cs` |
| `FilesPlugin.cs` | plugin | `C:\dev\Spur\Plugins\FilesPlugin.cs` |
| `ClipboardPlugin.cs` | plugin | `C:\dev\Spur\Plugins\ClipboardPlugin.cs` |
| `CommandsPlugin.cs` | plugin | `C:\dev\Spur\Plugins\CommandsPlugin.cs` |
| `SettingsPlugin.cs` | plugin | `C:\dev\Spur\Plugins\SettingsPlugin.cs` |
| `WebPlugin.cs` | plugin | `C:\dev\Spur\Plugins\WebPlugin.cs` |
| `PluginHelper.cs` | helper | `C:\dev\Spur\Plugins\PluginHelper.cs` |
| `LegacySearchAdapter.cs` | adapter | `C:\dev\Spur\Plugins\LegacySearchAdapter.cs` — wraps SearchEngineService as plugin (not in DI) |
| `MainViewModel.cs` | viewmodel | `C:\dev\Spur\ViewModels\MainViewModel.cs` — PluginManager-based SearchAsync:942 |
| `ResultsViewModel.cs` | viewmodel | `C:\dev\Spur\ViewModels\ResultsViewModel.cs` — owns Results collection + filter/scope |
| `MainWindow.xaml` | view | `C:\dev\Spur\Views\MainWindow.xaml` — binds to `ResultsVm.*` |
| `MainWindow.xaml.cs` | code-behind | `C:\dev\Spur\Views\MainWindow.xaml.cs` — layout-props relay |
| `UnifiedResultsView.xaml` | view | `C:\dev\Spur\Views\UnifiedResultsView.xaml` — binds to `ResultsVm.Results` |
| `ScopeBar.xaml` | view | `C:\dev\Spur\Views\ScopeBar.xaml` — binds to `ResultsVm.ScopeFilters` |
| `App.xaml.cs` | DI | `C:\dev\Spur\App.xaml.cs:89-95` — plugin registrations |
| `Spur.csproj` | build | `C:\dev\Spur\Spur.csproj` — binary reference workaround |
| `MainViewModelTests.cs` | tests | `C:\dev\Spur\Spur.Tests\MainViewModelTests.cs` — uses LegacySearchAdapter in PluginManager |

---

## 9. Context & Background

### Session history
- 2026-07-13 (earlier): Removed MainViewModel forwarding properties, wired XAML to ResultsVm.*, created LegacySearchAdapter, first multi-project layout with references
- 2026-07-13 (current): Created Spur.Plugin, Spur.Core, Spur.Infrastructure projects; defined IPlugin + supporting types; created 6 individual plugins; wired PluginManager into MainViewModel; added PluginResult.Source to preserve SearchResult data; build 0 errors, 61 tests pass

### Project background
Spur is a Windows launcher (alternative to Flow Launcher, Wox, etc.) built with WPF / .NET 9. Originally had a monolithic `SearchEngineService` that handled all search domains (apps, files, clipboard, add-ons, web) via a single `SearchAsync(ChannelWriter<IResultItem>)` method with a `string? category` parameter for scoping. The goal is to extract each domain into a standalone plugin so the system is extensible, testable, and mirrors Flow Launcher's architecture.

---

## 10. Paste-In Opener

> Spur is a Windows launcher app at C:\dev\Spur we're refactoring to Flow Launcher's plugin architecture. We've created Spur.Plugin (IPlugin SDK), Spur.Core (PluginManager), and 6 individual plugins (Apps, Files, Clipboard, Commands, Settings, Web). PluginManager is wired into MainViewModel for global search; PluginResult has a Source property carrying the underlying SearchResult to preserve type-specific data (ExePath, ActionId, ClipContent, etc.). Build is clean (0 errors) and all 61 tests pass. Next step: wire PluginResult.Action delegate into OpenSelected so external plugins can execute when selected, or migrate scoped search to plugin keywords to delete SearchEngineService.
