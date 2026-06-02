# phases.md — Build Phases & Test Gates

> **Rule:** A phase is not complete until every test in its gate passes.
> Do not start Phase N+1 if Phase N has a failing test.
> Each phase builds on the last — a broken foundation compounds forward.

---

## Phase Overview

```
Phase 0  — Project Setup & Dependencies
Phase 1  — Frameless Window (visual foundation)
Phase 2  — Global Hotkey (Alt+Space)
Phase 3  — Search Bar UI
Phase 4  — Hover State & Category Circles
Phase 5  — App Index (search engine, no UI)
Phase 6  — Search Results List
Phase 7  — Execute & Dismiss
Phase 8  — Detail Strip & Quick Actions
Phase 9  — File Search
Phase 10 — Dynamic Filter Chips
Phase 11 — System Commands
Phase 12 — Clipboard Manager
Phase 13 — Inline Calculator
Phase 14 — Web Search Shortcuts
Phase 15 — Module System
Phase 16 — Settings Window
Phase 17 — Polish, Performance & Distribution
```

---

## Phase 0 — Project Setup & Dependencies

**Goal:** Solution builds. All packages resolve. Folder structure matches the spec.

### Steps
1. Create solution: `NovaLauncher.sln`
2. Create projects:
   - `NovaLauncher.App` — WPF, `net9.0-windows`
   - `NovaLauncher.Core` — Class Library, `net9.0-windows`
   - `NovaLauncher.Tests` — xUnit, `net9.0`
3. Add project references: App → Core, Tests → Core
4. Install all NuGet packages (see `technologies.md` §12)
5. Create the full folder structure (see `technologies.md` §13)
6. Add `.gitignore`, `README.md`
7. Commit: `init: project structure`

### Test Gate ✓
```
□  dotnet build → 0 errors, 0 warnings
□  dotnet test  → 0 tests found (empty, that's fine), exits 0
□  All folders from technologies.md §13 exist
□  No package restore errors
```

---

## Phase 1 — Frameless Window

**Goal:** A borderless, transparent window appears at screen center with Mica/Acrylic blur,
correct border radius, and correct shadow. Nothing inside it yet.

### Steps
1. Configure `MainWindow.xaml`:
   - `WindowStyle="None"` `AllowsTransparency="True"` `Background="Transparent"`
   - Width `640`, no fixed height
   - `ResizeMode="NoResize"`
2. Implement `WindowBlur.cs` — apply Mica Alt on Win11, Acrylic fallback on Win10
3. Apply window border radius via a `Border` root element (`CornerRadius="14"`)
4. Set window drop shadow via `Effect` or Win32 `DwmExtendFrameIntoClientArea`
5. Position window at screen center using `SystemParameters.WorkArea`
6. Window starts visible for now (hotkey comes next phase)

### Test Gate ✓
```
□  Window appears centered on screen
□  Window has no title bar, no native chrome
□  Background is frosted / blurred (Mica or Acrylic — not solid black or white)
□  Corners are visibly rounded (14px radius)
□  Drop shadow visible around the window
□  Window is correct width (640px)
□  Tested on: Windows 11 (Mica) AND Windows 10 (Acrylic fallback)
□  Alt+F4 closes the app (temporary close method)
```

---

## Phase 2 — Global Hotkey (Alt+Space)

**Goal:** `Alt+Space` shows and hides the window from anywhere on the system.
The window must already exist in memory — just shown/hidden, never re-created.

### Steps
1. Implement `HotkeyService.cs`:
   - Register `Alt+Space` with Win32 `RegisterHotKey` on startup
   - Handle `WM_HOTKEY` in a hidden helper window's `WndProc`
2. On hotkey fire:
   - If window is hidden → `Show()`, bring to foreground, focus input
   - If window is visible → `Hide()`
3. Implement `NotifyIcon` (system tray):
   - Small icon in tray
   - Right-click menu: "Open" (same as hotkey), "Settings" (placeholder), "Quit"
4. App starts minimised to tray — window hidden by default
5. `Escape` key in the window → `Hide()` (wire up in `MainWindow.xaml.cs`)
6. Window loses focus → `Hide()` (handle `Deactivated` event)

### Test Gate ✓
```
□  App launches with no visible window
□  Tray icon appears
□  Alt+Space → window appears, input is focused
□  Alt+Space again → window hides
□  Escape → window hides
□  Click anywhere outside window → window hides
□  Alt+Space works while another app is in the foreground (true global shortcut)
□  Quit from tray → app exits cleanly, hotkey unregistered
□  After hide and re-show: window is still centered, still focused
□  No memory leak: open/close 20 times, Task Manager RAM stays stable
```

---

## Phase 3 — Search Bar UI

**Goal:** The search bar looks exactly as designed. Input works. Placeholder text.
Search icon. Correct typography. No results yet — just the bar.

### Steps
1. Create `SearchBarView.xaml` — a `UserControl`
2. Style the `TextBox`:
   - Remove native border, background
   - Apply `body-lg` / Inter Variable 500
   - Left padding `20px`, vertical center
   - Placeholder text via `TextBox` attached property or overlay `TextBlock`
3. Add search icon (Lucide `Search`, 18px, `--text-tertiary`) left of input
4. Wire `SearchBarView` into `MainWindow.xaml`
5. Set up `Colors.xaml` and `Typography.xaml` resource dictionaries (all tokens from `design.md`)
6. Confirm window height is exactly `56px` with just the bar

### Test Gate ✓
```
□  Window is exactly 56px tall with only the bar
□  Placeholder text "Search apps, files, commands..." visible in correct color
□  Placeholder disappears on first keypress
□  Typed text appears in correct font, size, weight, color
□  Search icon is visible and correctly positioned
□  Backspace, delete, cursor movement all work normally in the input
□  Ctrl+A selects all text
□  Ctrl+Backspace clears entire input
□  Light mode and dark mode both render correctly (toggle system theme to test)
□  Window renders correctly at 100%, 125%, 150% DPI scaling
```

---

## Phase 4 — Hover State & Category Circles

**Goal:** 3 circles appear when the mouse enters the bar. They animate in with stagger.
They animate out when the mouse leaves. Each circle shows a label on hover.

### Steps
1. Implement mouse enter/leave detection on `MainWindow`
2. Create `CategoryCircle.xaml` — a reusable `UserControl`:
   - 34×34 circle, `--bg-elevated` background
   - Lucide icon centered, 18px, `--text-secondary`
   - Caption label below (hidden by default, visible on hover)
   - Hover: background → `--bg-elevated` + brighten, cursor → hand
   - Active: background → `--accent-subtle`, icon → `--accent`
3. Create `CategoryCirclesView.xaml` — horizontal row of 3 circles:
   - `📄 Files` `⚙ Commands` `📋 Clipboard`
   - Gap: 8px between circles, 16px right margin
4. Animate circles in on mouse enter:
   - Stagger using `BeginTime` on each `Storyboard`
   - opacity 0→1, ScaleTransform 0.85→1.0, 150ms spring easing
5. Animate circles out on mouse leave:
   - All together, 100ms ease-out
6. Circle click → store which circle is active, placeholder text changes
   (results panel comes next phase — just wire the state for now)

### Test Gate ✓
```
□  Mouse enters bar → 3 circles fade in with stagger (left to right)
□  Mouse leaves bar → circles fade out together
□  Circle stagger timing feels natural (not too fast, not too slow)
□  Hovering a circle → label appears below it ("Files", "Commands", "Clipboard")
□  Label disappears when cursor leaves that circle
□  Clicking a circle → it gets accent background + accent icon
□  Clicking active circle → it returns to default state
□  Circles do NOT appear when user is typing (test: open bar, start typing, hover over bar area — circles must not show)
□  Reduce Motion: if Windows "reduce motion" is enabled, circles appear/disappear instantly (no animation)
```

---

## Phase 5 — App Index (Core, No UI)

**Goal:** The app index builds, stores apps in memory, and returns fuzzy-matched results.
This phase is **pure C# in NovaLauncher.Core** — no UI, tested via xUnit.

### Steps
1. Implement `AppSearchProvider.cs`:
   - Scan Start Menu (user + system) for `.lnk` files, resolve via `IShellLink` COM
   - Read registry `App Paths` keys
   - Read UWP packages via `PackageManager`
   - Deduplicate by executable path
   - Extract icon via `System.Drawing.Icon.ExtractAssociatedIcon`
   - Store in `List<AppResult>` in memory
2. Implement `FuzzyMatcher.cs` wrapping `FuzzySharp`:
   - `Score(string query, string candidate) → int`
   - `Match(string query, IEnumerable<string> candidates) → IEnumerable<(string, int)>`
3. Implement `FrecencyScorer.cs`:
   - Read/write frecency from SQLite (`DatabaseService.cs`)
   - `GetScore(string id) → double`
   - `RecordUse(string id)`
4. Implement `DatabaseService.cs`:
   - Create SQLite db at `%APPDATA%\NovaLauncher\data.db`
   - Run schema migrations on startup
5. Wire `FileSystemWatcher` to re-index when Start Menu folder changes
6. Write unit tests in `NovaLauncher.Tests`:
   - `FuzzyMatcher_ReturnsCorrectTopResult`
   - `FuzzyMatcher_HandlesCaseInsensitive`
   - `FuzzyMatcher_HandlesEmptyInput`
   - `FrecencyScorer_BoostsFrequentlyUsedItems`
   - `AppSearchProvider_IndexContainsInstalledApp`

### Test Gate ✓
```
□  dotnet test → all 5+ tests pass
□  Index builds in < 500ms (measure with Stopwatch in a test)
□  FuzzyMatcher: "vsc" → "Visual Studio Code" scores higher than "Visual Studio 2022"
□  FuzzyMatcher: "ps" → "Photoshop" if installed, not just "Windows PowerShell"
□  AppSearchProvider: known installed app (e.g. Notepad) is in the index
□  Frecency: RecordUse 5× on item A, 1× on item B → A scores higher than B
□  DatabaseService: db file created at correct path, schema correct
□  FileSystemWatcher: pin a new shortcut to Start Menu → index refreshes within 2s
```

---

## Phase 6 — Search Results List

**Goal:** Typing in the bar runs the search and shows results. Keyboard navigation works.

### Steps
1. Create `SearchViewModel.cs` with `[ObservableProperty]`:
   - `Query`, `Results`, `SelectedIndex`
   - `OnQueryChanged` → calls `SearchAsync`
2. `SearchAsync`: calls `AppSearchProvider`, merges results, sorts by score
3. Create `ResultListView.xaml`:
   - `ListBox` with `VirtualizingStackPanel`
   - Bound to `Results`
   - Custom `ItemContainerStyle` — no native selection highlight
4. Create `ResultItemView.xaml` (DataTemplate):
   - App icon (32×32, 8px radius)
   - App name (`body-lg` / 500)
   - Subtitle: path or category (`body` / 400, `--text-secondary`)
   - Category tag right-aligned (`caption`, `--text-tertiary`) — only shown in mixed results
   - Hover: background → `--bg-elevated`, 80ms
   - Selected: background → `--accent-subtle`
5. Window expands downward when results appear (animate height)
6. `↑ / ↓` keyboard navigation in `MainWindow` `KeyDown` handler
7. First result pre-selected when results load

### Test Gate ✓
```
□  Type "note" → Notepad appears in results
□  Type "vsc" → VS Code appears (fuzzy match)
□  Results appear within 50ms of keystroke
□  First result is always pre-selected
□  ↑ / ↓ moves selection, wraps at top/bottom
□  Result icon, name, and subtitle all display correctly
□  Hover state on result rows works (background change)
□  Selected state is visually distinct (accent background)
□  Window height expands smoothly as results appear
□  Window height collapses when input is cleared
□  Backspace to empty → results disappear
□  Typing fast (10+ chars/sec) doesn't cause flicker or wrong results
□  100 results in list — scrolling is smooth, no jank (VirtualizingStackPanel working)
```

---

## Phase 7 — Execute & Dismiss

**Goal:** Pressing Enter on a selected app result opens it. Launcher dismisses. Frecency recorded.

### Steps
1. Handle `Enter` key in `MainWindow` → call `ExecuteSelected()` on ViewModel
2. `ExecuteSelected()`:
   - Get selected `AppResult`
   - `Process.Start(result.ExecutablePath)`
   - Call `FrecencyScorer.RecordUse(result.Id)`
   - Hide window, clear query
3. Handle `Ctrl+Enter` → `Process.Start` with `Verb = "runas"` (Run as Admin)
4. Handle `Ctrl+I` → open file location in Explorer
5. Handle `Ctrl+C` → copy app path to clipboard

### Test Gate ✓
```
□  Select Notepad in results, press Enter → Notepad opens
□  Launcher hides immediately after Enter
□  Query is cleared when launcher reopens (Alt+Space after launch)
□  Open same app 5× → it moves higher in results on next search
□  Ctrl+Enter → UAC prompt appears (Run as Admin working)
□  Ctrl+I → Explorer opens showing the app's folder
□  Ctrl+C → app path copied to clipboard (verify with Ctrl+V in Notepad)
□  Launching a non-existent path (broken shortcut) → no crash, silent fail or toast error
```

---

## Phase 8 — Detail Strip & Quick Actions

**Goal:** Selecting a result shows a 48px detail strip pinned to the bottom of the results panel.
It updates as selection changes. Quick actions are clickable.

### Steps
1. Create `DetailStripView.xaml` — 48px UserControl:
   - Item name (caption / 500)
   - Metadata (caption / 400, `--text-secondary`)
   - Action shortcuts row: `↵ Open · ⌘R Admin · ⌘I Explorer · ⌘C Path`
   - 1px `--border` top separator
2. Bind to `SelectedResult` in `SearchViewModel`
3. Animate in: slideY(4px→0) + opacity(0→1), 120ms spring
4. Animate out: slideY(0→4px) + opacity(1→0), 80ms on deselect
5. Crossfade when selection changes (don't animate out then in — crossfade)
6. `Ctrl+K` → show all available actions as an overlay inside the results panel

### Test Gate ✓
```
□  Detail strip appears when a result is selected
□  Detail strip updates immediately when selection changes (no flicker)
□  Name and metadata are correct for the selected item
□  Action shortcuts visible: ↵ Open · ⌘R Admin · ⌘I Explorer · ⌘C Path
□  Each action shortcut executes correctly when keyboard shortcut pressed
□  Strip animates in on first selection smoothly
□  Strip crossfades (not flicker) when moving between results
□  Ctrl+K opens action overlay — all available actions listed
□  Strip absent when no result is selected (e.g. empty input)
```

---

## Phase 9 — File Search

**Goal:** File results appear below app results when the query matches filenames.
File circle (📄) browse mode shows recent files.

### Steps
1. Implement `FileSearchProvider.cs`:
   - Primary: Windows Search Index via COM (`ISearchManager` / `ISearchQueryHelper`)
   - Fallback: Everything SDK via P/Invoke if `Everything64.dll` present
   - Graceful disable if neither available
2. Merge file results into `SearchOrchestrator.cs` (after app results)
3. File result items get file-specific icons (extension-based) and metadata (size, date)
4. 📄 circle click → `Category Browse` state:
   - Show recent files (Windows Search `ORDER BY System.DateModified DESC LIMIT 20`)
   - Placeholder text changes to "Search files..."
5. Typing while in file browse → filters files only

### Test Gate ✓
```
□  Type a filename → correct file appears in results below any app matches
□  File result shows: icon, filename, folder path, modified date
□  Results return in < 100ms for filename queries (Windows Search Index)
□  If Everything is installed: results return in < 20ms
□  If neither Windows Search nor Everything available: file results silently absent (no crash)
□  📄 circle click → shows recent files list
□  Placeholder text shows "Search files..." in file browse mode
□  Typing in file browse mode → filters the file results
□  ← circle click → collapses file results, returns to hover state
□  File result: Enter → opens file in default app
□  File result: Ctrl+R → opens containing folder in Explorer
□  File result: Ctrl+C → copies full path to clipboard
```

---

## Phase 10 — Dynamic Filter Chips

**Goal:** When results span Files + Apps + Web, filter chips appear below the search bar.
Clicking a chip narrows results to that category.

### Steps
1. Create `FilterChipsView.xaml`:
   - Horizontal `ItemsControl` bound to `ActiveFilters` (list of categories with results)
   - Each chip: 26px height, 13px radius, icon + label + count badge
   - "All" chip always first
2. Logic in `SearchViewModel`:
   - After search, compute which categories have results
   - Expose `ActiveFilters` as `ObservableCollection<FilterChip>`
   - `SelectedFilter` property — null means "All"
3. Chips animate in: fade+slideY(4px), 120ms, staggered 30ms
4. Chips hidden when only 1 category has results (no need to filter)
5. `Tab` key cycles through chips from search bar

### Test Gate ✓
```
□  Type "note" (has app + file results) → chips appear: [ 📄 Files N ]
□  Type "vsc" (app only) → no chips appear (one category)
□  Chip count badge is accurate
□  Click "Files" chip → only file results shown
□  Click "All" chip → mixed results return
□  Tab key cycles through chips correctly
□  Chips disappear instantly when query is cleared
□  Chips animate in smoothly (no layout jump)
□  Active chip has accent styling, inactive has muted styling
```

---

## Phase 11 — System Commands

**Goal:** System commands (sleep, shutdown, dark mode, etc.) appear in results and execute.

### Steps
1. Implement `CommandSearchProvider.cs` with hardcoded command list:
   ```
   sleep, lock, restart, shutdown, sign out,
   empty trash, toggle dark mode,
   volume up, volume down, mute,
   screenshot
   ```
2. Each command has: name, description, icon, `Action Execute()`
3. Commands rank below apps but above files in results
4. Fuzzy match command names and descriptions
5. Implement each command's execution (Win32 or .NET APIs):
   - Sleep: `SetSuspendState`
   - Lock: `LockWorkStation`
   - Shutdown/Restart: `InitiateSystemShutdownEx`
   - Dark mode: toggle `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize`
   - Empty trash: `SHEmptyRecycleBin`
   - Screenshot: `SendKeys` to Win+Shift+S
6. Destructive commands (shutdown, restart) show a confirm step in the detail strip before executing

### Test Gate ✓
```
□  Type "sleep" → Sleep command appears in results
□  Type "dark" → "Toggle Dark Mode" appears
□  Type "shut" → "Shutdown" appears with ⚠ destructive indicator
□  Shutdown/Restart: pressing Enter shows confirm step ("Press Enter again to confirm")
□  Second Enter confirms and executes; Escape cancels
□  Toggle Dark Mode: executes, system theme visibly changes
□  Empty Trash: executes, Recycle Bin empties
□  Lock: screen locks
□  ⚙ circle click → shows full command list pre-filtered, no query needed
□  Commands do not appear when query is empty (only on explicit search or circle click)
```

---

## Phase 12 — Clipboard Manager

**Goal:** Clipboard history is captured, stored, browsable, and pasteable.

### Steps
1. Implement `ClipboardService.cs`:
   - Register `AddClipboardFormatListener` on the main window handle
   - Handle `WM_CLIPBOARDUPDATE` in `WndProc`
   - On change: read clipboard (text / image / file paths), store in SQLite
   - Detect sensitive patterns (password managers) → skip storing
2. `ClipboardViewModel.cs`:
   - Load history from SQLite on open
   - Filter by search query
   - Pin/unpin, delete operations
3. 📋 circle click → Clipboard browse mode:
   - Show full history (newest first, pinned at top)
   - Placeholder: "Filter clipboard..."
4. `Win+Shift+V` global hotkey → opens launcher directly in clipboard mode
5. `Enter` on clipboard item → `Clipboard.SetText(item.Content)` then `SendKeys Ctrl+V` to paste into previously focused window
6. Store image entries as PNG path references (not inline in SQLite)

### Test Gate ✓
```
□  Copy text in any app → appears in clipboard history after Alt+Space
□  Copy an image → appears as [IMG] entry with dimensions
□  Copy a file path → appears as a path entry
□  📋 circle → shows clipboard history
□  Filter: type in clipboard mode → list filters correctly
□  Pinned items appear at top with 📌 marker
□  Ctrl+P → pins/unpins selected item
□  Ctrl+D → deletes selected item with no crash
□  Enter → pastes item into the previously focused app
□  Win+Shift+V → opens launcher directly on clipboard list
□  After 50+ items: oldest unpinned items dropped (respects max-items setting)
□  Password manager copy (test with KeePass or Bitwarden): item NOT stored in history
□  Clipboard history persists across launcher restarts (SQLite working)
```

---

## Phase 13 — Inline Calculator

**Goal:** Typing a math expression shows the result inline as a result row. Enter copies it.

### Steps
1. Implement `CalculatorProvider.cs`:
   - Detect math input: regex for operators, `sqrt`, `%`, unit/currency keywords
   - Evaluate via `meval` (arithmetic) or custom parser for `15% of 300`
   - Return a `CalculatorResult` with the evaluated value
2. Currency conversion:
   - On first session use, fetch rates from `api.exchangerate-api.com` (free tier)
   - Cache rates in SQLite for the session
   - Parse: `100 USD to NGN`, `50 EUR to GBP`
3. Unit conversion: hardcoded conversion table (km↔miles, °F↔°C, kg↔lb, ml↔cups, etc.)
4. Calculator result renders as a special result row:
   - Large mono result value on right
   - Expression label on left
   - `Enter` copies value to clipboard
5. `↑` in calculator mode → cycles last 10 expressions

### Test Gate ✓
```
□  Type "2 + 2" → "= 4" result row appears
□  Type "15% of 2400" → "= 360" appears
□  Type "sqrt(144)" → "= 12" appears
□  Type "100 USD to NGN" → correct converted amount (within 5% of current rate)
□  Type "10km to miles" → "= 6.21 miles"
□  Type "32°F to °C" → "= 0°C"
□  Enter on calc result → value copied to clipboard, launcher dismisses
□  Type a normal app name ("calculator") → no false positive calc detection
□  Invalid expression ("2 + * 3") → no crash, no result row, treated as normal search
□  ↑ key in calculator → cycles through last expressions correctly
□  Currency rates cached: second query in same session is instant (no HTTP call)
```

---

## Phase 14 — Web Search Shortcuts

**Goal:** Web search row always appears as the last result. Keyword shortcuts work.

### Steps
1. `WebSearchProvider.cs`:
   - Always returns 1 result: "Search [engine] for [query]"
   - Default engine: Google
   - Keyword detection: `g `, `yt `, `gh `, `wiki `, `npm `, `mdn `, `reddit `
   - Each keyword maps to a URL template
2. Execute → `Process.Start(url)` opens in default browser
3. User-defined shortcuts stored in SQLite settings table

### Test Gate ✓
```
□  Any query → "Search Google for..." row appears last
□  Enter on web row → browser opens with correct Google search URL
□  Type "yt lo-fi music" → "Search YouTube for lo-fi music" row appears
□  Type "gh tauri" → "Search GitHub for tauri" row appears
□  Type "npm react" → "Search npm for react"
□  Opens in the user's default browser (not hardcoded to Chrome or Edge)
□  Custom shortcut added in settings → works correctly after save
□  Web row is always last, never above app or file results
```

---

## Phase 15 — Module System

**Goal:** A module can be dropped into the modules folder, loaded at runtime,
and its results appear in the launcher via keyword.

### Steps
1. Implement `IModule.cs` interface in `NovaLauncher.Core`
2. Implement `ModuleLoader.cs`:
   - Scans `%APPDATA%\NovaLauncher\modules\` on startup
   - Loads each valid `.dll` via `AssemblyLoadContext`
   - Instantiates `IModule` implementations
3. Keyword dispatch in `SearchOrchestrator`:
   - If query starts with a module's `TriggerKeyword` → route to that module only
4. Build one example first-party module: `NovaLauncher.Module.Emoji`
   - Trigger: `:`
   - Query: fuzzy search emoji names
   - Execute: copy emoji to clipboard
5. Module results render in the standard result list (no special UI)

### Test Gate ✓
```
□  Emoji module .dll placed in modules folder
□  App restarts → emoji module loaded (no crash)
□  Type ":smile" → 😊 and related emoji appear as results
□  Enter on emoji → emoji copied to clipboard
□  Type ":fire" → 🔥 appears
□  Type "fire" (no colon) → emoji module NOT activated (keyword isolation works)
□  Remove module .dll → app restarts without it, no crash
□  Malformed .dll in modules folder → skipped gracefully, other modules still load
□  Module with bad IModule implementation → caught, logged, skipped
```

---

## Phase 16 — Settings Window

**Goal:** Settings window opens, all settings save instantly and apply without restart.

### Steps
1. Create `SettingsWindow.xaml` — separate `Window`, not overlay
2. Sections: General, Search, Clipboard, Web Search, Modules, About
3. Implement each setting with instant-save:
   - **Hotkey:** rebind (unregister old, register new) — show conflict warning if taken
   - **Theme:** toggle system/light/dark/midnight → apply immediately to launcher
   - **Accent color:** 8 presets → apply immediately
   - **Launch at login:** write/remove registry key `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
   - **Everything SDK:** toggle file provider
   - **Clipboard max items:** write to settings table, trim history if reducing
   - **Web search shortcuts:** add/edit/delete table
   - **Modules:** list with toggle switch and "Open folder" button
4. About section: version number, "Check for updates" button (placeholder)

### Test Gate ✓
```
□  Ctrl+, opens Settings window
□  Settings window is independent of launcher bar (both can be open)
□  Theme change → launcher background changes in real time (no restart)
□  Accent color change → all accent elements update in real time
□  Hotkey change → new hotkey works, old one stops working
□  Hotkey conflict (e.g. Alt+Space taken by another app) → warning shown, not saved
□  Launch at login → toggle on, reboot, app starts in tray automatically
□  Clipboard max items → set to 5, add 6 items, 6th pushes oldest out correctly
□  Web shortcut added → works in launcher immediately after save
□  Module toggle off → module no longer responds to its keyword
□  All settings persist after app quit and relaunch
□  Escape closes settings window
```

---

## Phase 17 — Polish, Performance & Distribution

**Goal:** All performance targets from `technologies.md §14` are met.
App is ready to package and distribute.

### Steps

**Performance:**
1. Profile with Visual Studio Diagnostic Tools — identify any >10ms operations on the UI thread
2. Move any slow operations to background threads (`Task.Run`)
3. Publish with NativeAOT: `dotnet publish -c Release -r win-x64 --self-contained true`
4. Measure RAM: open Task Manager, run launcher, verify < 35MB idle / < 70MB open
5. Measure startup: time from `Alt+Space` to window visible — must be < 80ms

**Animation polish:**
6. Review all transitions against `design.md §6` — spring curves, timing, stagger
7. Verify `prefers-reduced-motion` (Windows "reduce motion") disables all animations
8. Check all animations at 60fps on an average machine (no jank)

**Edge cases:**
9. Test on a machine with no Everything installed → file search gracefully degrades
10. Test with 0 clipboard items → clipboard mode shows empty state gracefully
11. Test with no internet → calculator currency shows "rates unavailable" gracefully
12. Test with a corrupt SQLite db → app recreates db, no crash
13. Test with 1000+ installed apps → search still returns in < 50ms
14. Test DPI scaling: 100%, 125%, 150%, 200% — no layout breaks

**Distribution:**
15. Set up Inno Setup script → generates `NovaLauncher-Setup.exe`
16. Build portable: single `.exe` no installer needed
17. Sign both with Authenticode certificate (prevents SmartScreen warning)
18. Write `README.md` with install instructions and keyboard shortcuts reference

### Final Test Gate ✓
```
□  Hotkey → window visible:           < 80ms  (measure 10× average)
□  First results shown after typing:  < 50ms
□  RAM at idle (tray, hidden):        < 35MB
□  RAM when open with results:        < 70MB
□  CPU at idle:                       < 0.1%  (Task Manager, 30s average)
□  Installer size:                    < 15MB
□  App index rebuild:                 < 300ms

□  All 13 previous phase test gates still pass
□  No crashes in 30 min of normal use
□  prefers-reduced-motion: all animations disabled, only opacity fades
□  DPI 100/125/150/200: no layout breaks, no blurry text
□  Installer runs on a clean Windows 10 VM (no .NET pre-installed) → app works
□  Installer runs on a clean Windows 11 VM → app works, Mica effect active
□  SmartScreen: signed build shows no warning
□  Tray icon: right-click menu works correctly
□  All keyboard shortcuts from ux.md §11 work correctly
```

---

## Quick Reference: What Each Phase Produces

| Phase | Deliverable                                  | Runnable? |
|-------|----------------------------------------------|-----------|
| 0     | Empty solution, all deps installed           | ✓ builds  |
| 1     | Frosted glass window at screen center        | ✓         |
| 2     | Alt+Space shows/hides window + tray          | ✓         |
| 3     | Styled search bar, input works               | ✓         |
| 4     | 3 circles animate in/out on hover            | ✓         |
| 5     | App index + fuzzy search (tests only)        | tests     |
| 6     | Typing returns app results in a list         | ✓         |
| 7     | Enter launches apps, frecency learns         | ✓         |
| 8     | Detail strip shows on selection              | ✓         |
| 9     | File results + file browse circle            | ✓         |
| 10    | Filter chips narrow results by category      | ✓         |
| 11    | System commands work (sleep, dark mode...)   | ✓         |
| 12    | Clipboard history captured and pasteable     | ✓         |
| 13    | Math expressions evaluate inline             | ✓         |
| 14    | Web search row + keyword shortcuts           | ✓         |
| 15    | Module system + emoji module                 | ✓         |
| 16    | Settings window, all settings persist        | ✓         |
| 17    | Signed installer, performance targets met    | 🚀 ship   |
