# ux.md — User Experience & Screen Flows

> One bar. No persistent chrome. Everything reveals itself only when needed.
> Inspired by macOS Tahoe Spotlight — dynamic, clean, contextual.

---

## Mental Model

The launcher is a **single bar** that lives in three states:

```
IDLE          → invisible, running in background
EMPTY         → bar visible, search input focused, no results
HOVER         → 3 floating category circles appear inside the bar
ACTIVE SEARCH → bar expands downward, results list appears, dynamic filter chips show
```

The bar **never has a persistent sidebar**. No Apps circle — Windows Start Menu handles
app browsing. No Calculator circle — math auto-detects from typing. Only the 3 things
Windows gives you no fast path to: **Files, Commands, Clipboard**.

```
①  📄  Files       — instant file search (Explorer is slow)
②  ⚙   Commands    — system actions with no Windows shortcut
③  📋  Clipboard   — replaces the basic Win+V experience
```

App launching is the **default result of typing** — not a browsable category.

---

## 1. Idle State

**Description:** Launcher is invisible. Silently running in the system tray.

**Entry points:** OS startup / user logs in
**Exit points:** `Alt+Space` → Empty State

**System tray:** Small icon, right-click → Settings / Quit / About. No badges, no animations.

---

## 2. Empty State

**Description:** Bar appears, search input focused, nothing typed yet. Just the bar — clean.

**What's visible:**
```
┌──────────────────────────────────────────────────────┐
│  🔍  Search apps, files, commands...                 │
└──────────────────────────────────────────────────────┘
Width: 640px  Height: 56px  Border-radius: 14px
```

**Behaviors:**
- Window appears at screen center with spring animation (scale 0.96→1, opacity 0→1, 180ms)
- Search icon is the only element — no results, no icons, no hints
- Input cursor is blinking and focused immediately
- `Escape` → dismisses launcher → Idle
- Mouse enters the window → transitions to Hover State
- Any character typed → transitions to Active Search State

**Entry points:** `Alt+Space` from Idle
**Exit points:**
- Mouse enters window → Hover State
- Typing → Active Search State
- `Escape` → Idle

---

## 3. Hover State

**Description:** Mouse is over the bar. Three floating category circles fade in on the right
side of the input row. Each one covers something Windows gives you no fast path to.

**What's visible:**
```
┌──────────────────────────────────────────────────────┐
│  🔍  Search apps, files, commands...    ⬤   ⬤   ⬤   │
└──────────────────────────────────────────────────────┘
                                          📄   ⚙   📋
                                    (label shows on hover of each circle)
Width: 640px  Height: 56px
```

**Category circles (left to right):**
```
①  📄  Files        — instant file search, Explorer is too slow
②  ⚙   Commands     — sleep, shutdown, dark mode, volume, screenshot
③  📋  Clipboard    — full history, replaces the basic Win+V
```

Apps are not a circle — Windows Start Menu handles that.
Calculator is not a circle — type any math expression and it activates automatically.

**Circle animation in:**
- Staggered: opacity 0→1, scale 0.85→1.0
- Each circle 150ms spring, delayed 30ms per circle
- Full sequence done in ~210ms

**Circle animation out (mouse leaves):**
- All together: opacity 1→0, scale 1.0→0.85, 100ms ease-out

**Individual circle hover:**
- Label appears below the circle (caption / 11px / `--text-secondary`)
- Background shifts to `--bg-elevated`
- Cursor: pointer

**Clicking a circle:**
- That circle gets accent state (accent background, accent icon)
- Bar expands downward → Category Browse State
- Active circle shows `←` to return

**Behaviors:**
- Mouse leaves window → circles fade out
- Typing starts → circles fade out instantly → Active Search State
- `Escape` → Idle

**Entry points:** Mouse enters bar from Empty State
**Exit points:**
- Click a circle → Category Browse State
- Start typing → Active Search State
- Mouse leaves → Empty State
- `Escape` → Idle

---

## 4. Category Browse State

**Description:** User clicked one of the 3 circles. Bar expands downward showing that
category's content pre-loaded. No query needed. The active circle shows `←` to go back.

**What's visible (example: Files circle clicked):**
```
┌──────────────────────────────────────────────────────┐
│  🔍  Search files...             ←  ⬤   ⬤   ⬤       │
├──────────────────────────────────────────────────────┤
│  📄  Files                                           │
│  ─────────────────────────────────────────────────   │
│  📄  project-notes.md    C:/Projects/    2m ago      │
│  📁  nova-launcher/      C:/Dev/         1h ago      │
│  📄  design.md           C:/Dev/         today       │
│  📄  figma-export.pdf    ~/Downloads/    yesterday   │
│  📁  Documents/          C:/Users/...    3 days ago  │
│  ─────────────────────────────────────────────────   │
│  project-notes.md  ·  12KB  ·  Modified 2m ago       │
│  ↵ Open  ·  ⌘R Reveal  ·  ⌘C Copy Path              │
└──────────────────────────────────────────────────────┘
```

**Per-category default content:**
| Circle      | Default content shown                                  |
|-------------|--------------------------------------------------------|
| 📄 Files    | Recent 20 files/folders, sorted by modified date       |
| ⚙ Commands  | All system commands listed (sleep, shutdown, etc.)     |
| 📋 Clipboard| Full clipboard history, newest first, pinned at top    |

**Behaviors:**
- Placeholder text in search bar changes to match context: "Search files...",
  "Search commands...", "Filter clipboard..."
- `↑ / ↓` navigates results
- `Enter` on selected result → executes → Idle
- Typing → filters within this category first (active circle stays lit)
- Click `←` on active circle → collapses results → Hover State
- `Escape` → collapses results → Hover State

**Entry points:** Clicking a circle from Hover State
**Exit points:**
- `Enter` on result → Action Executed → Idle
- Click `←` → Hover State
- Typing → Active Search (this category pre-selected as filter)
- `Escape` → Hover State

---

## 5. Active Search State

**Description:** User is typing. Results update live. App results come first naturally —
no circle needed, this is just how typing works. Dynamic filter chips appear below
the bar for the categories that have matches.

**What's visible:**
```
┌──────────────────────────────────────────────────────┐
│  🔍  figma_                                          │
│  [ 📄 Files  1 ]  [ 🌐 Web ]                        │
├──────────────────────────────────────────────────────┤
│  ████  Figma                      ↵ Open             │
│  📄   figma-designs.fig           C:/Projects/       │
│  🌐   Search Google for "figma"                      │
│  ─────────────────────────────────────────────────   │
│  Figma  ·  v116.x  ·  Last opened 1h ago             │
│  ↵ Open  ·  ⌘R Admin  ·  ⌘I Explorer  ·  ⌘C Path    │
└──────────────────────────────────────────────────────┘
```

**Result ranking order (always):**
```
1. Apps          — highest priority, always floats to top
2. Commands      — if query matches a system command
3. Files         — file and folder matches
4. Calculator    — if query is math (shown as a result row, not a section)
5. Web search    — always last, always present as fallback
```

Apps appear at the top automatically — they don't need a circle or a category label.
The user gets app launching for free just by typing.

**Dynamic Filter Chips:**
- Only appear when results span more than one category
- Only show categories with at least 1 result — zero-result categories stay hidden
- "All" chip sits first — resets to mixed view
- Number badge on each chip (max `9+`)
- Chips for Apps are never shown — app results always stay at the top, unfiltered

**Behaviors:**
- Results within ~50ms (debounce 30ms)
- First result always pre-selected
- `↑ / ↓` navigates
- `Enter` → execute → Idle
- `Ctrl+Enter` → secondary action
- `Ctrl+K` → all actions overlay
- Backspace to empty → chips disappear, results collapse → Empty State
- `Escape` → clears query → Empty State

**Entry points:** Typing from any state
**Exit points:**
- `Enter` → Action Executed → Idle
- Backspace to empty → Empty State
- Click a filter chip → Filtered Search State
- `Escape` → Empty State

---

## 6. Filtered Search State

**Description:** User clicked a filter chip. Results are narrowed to one category.
The active chip is highlighted. Everything else is the same as Active Search.

**What's visible:**
```
┌────────────────────────────────────────────────────────────────┐
│  🔍  figma_                                                    │
│  [ All ]  [ 🖥 Apps ● 1 ]  [ 📄 Files  2 ]                   │
├────────────────────────────────────────────────────────────────┤
│  ████  Figma                         ↵ Open                   │
│  ─────────────────────────────────────────────────────────     │
│  Figma  ·  v116.x  ·  Last opened 1h ago                      │
│  ↵ Open  ·  ⌘R Admin  ·  ⌘I Explorer  ·  ⌘C Path             │
└────────────────────────────────────────────────────────────────┘
```

**Behaviors:**
- Click "All" chip → back to mixed results
- Click another chip → switch category filter
- Results update live as query changes
- Everything else identical to Active Search State

**Entry points:** Clicking a filter chip from Active Search State
**Exit points:**
- Click "All" chip → Active Search State
- `Escape` → clears text → Empty State
- `Enter` → Action Executed → Idle

---

## 7. Clipboard Mode (direct)

**Description:** Clipboard category is jumped to directly via hotkey or circle click.
Shows clipboard history as the full result list.

**Trigger:** `Win+Shift+V` global hotkey, OR click 📋 circle in Hover State

**What's visible:**
```
┌────────────────────────────────────────────────────────────────┐
│  🔍  Filter clipboard...                ← ⬤  ⬤  ⬤  ⬤  ⬤      │
├────────────────────────────────────────────────────────────────┤
│  Just now    console.log("hello world")                       │
│  2m ago      https://github.com/user/repo                     │
│  5m ago      [IMG]  screenshot.png  640×480                   │
│  12m ago     const x = require('express')                     │
│  1h ago   📌 john@example.com   PINNED                        │
│  ─────────────────────────────────────────────────────────     │
│  https://github.com/user/repo  ·  2 min ago                   │
│  ↵ Paste  ·  ⌘P Pin  ·  ⌘D Delete                            │
└────────────────────────────────────────────────────────────────┘
```

**Behaviors:**
- Typing filters clipboard items in real-time
- `Enter` pastes selected item into the previously focused window, launcher dismisses
- `Ctrl+P` pins/unpins item (pinned items float to top, never expire)
- `Ctrl+D` deletes item
- `Escape` → collapses to Hover State

**Entry points:** `Win+Shift+V`, or 📋 circle click from Hover State
**Exit points:**
- Paste → Idle
- `Escape` → Hover State

---

## 8. Calculator Mode (direct)

**Description:** Calculator category jumped to directly, or auto-activates when input looks like math.

**Trigger:** Click 🧮 circle in Hover State, OR type a math expression anywhere (auto-detected)

**Auto-detection:** Any input containing `+`, `-`, `*`, `/`, `%`, `sqrt`, ` to ` (conversions), currency codes

**What's visible:**
```
┌────────────────────────────────────────────────────────────────┐
│  🔍  15% of 2400                        ← ⬤  ⬤  ⬤  ⬤  ⬤      │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│                =   360.00                                      │
│                                                                │
│  ─────────────────────────────────────────────────────────     │
│  100 USD → ₦163,800.00  ·  live rate                          │
│  10km → 6.21 miles  ·  32°F → 0°C                             │
└────────────────────────────────────────────────────────────────┘
```

**Behaviors:**
- Result shown immediately, no debounce
- Large mono result dominates the panel
- `Enter` copies result to clipboard, launcher dismisses
- `↑` cycles through last 10 expressions
- Conversion quick-refs always shown as contextual hints below the result

**Entry points:** 🧮 circle click, or typing a math expression in any state
**Exit points:**
- `Enter` → copy → Idle
- `Escape` → Hover State

---

## 9. Module Mode

**Description:** A Module's keyword is typed and it takes over the result panel.
No new circles appear for modules — they are keyword-activated only.

**Trigger:** Type the module's registered keyword prefix (e.g., `sp` for Spotify)

**What's visible:**
```
┌────────────────────────────────────────────────────────────────┐
│  🔍  sp _                                                      │
│  [ 🎵 Spotify ]                                               │
├────────────────────────────────────────────────────────────────┤
│  ▶  Now playing: Tems - Free Mind                             │
│  ⏭  sp next        Skip track                                 │
│  ⏸  sp pause       Pause playback                             │
│  🔍  sp <song>     Search & play                              │
│  ─────────────────────────────────────────────────────────     │
│  sp next  ·  Skip to next track                               │
│  ↵ Execute                                                    │
└────────────────────────────────────────────────────────────────┘
```

**Behaviors:**
- A single chip for the module appears in the filter chips row
- Module result items navigate identically to regular results
- `Escape` exits module context → clears text → Empty State

**Entry points:** Typing a module keyword prefix in Active Search
**Exit points:**
- `Enter` on action → Idle
- `Escape` → Empty State

---

## 10. Settings

**Trigger:** `Ctrl+,` or right-click tray icon → Settings

Opens as a **separate native window** (not overlaying the launcher bar).
Standard settings window, not a panel. The launcher bar is independent from it.

**Sections:**
```
General       → Hotkey, launch at login, theme, accent color
Search        → Sources, Everything SDK toggle, excluded folders
Clipboard     → Max items, sensitive data toggle, pin manager
Web Search    → Default engine, custom keyword shortcuts
Modules       → Installed list, enable/disable, open folder
About         → Version, update check, shortcuts reference
```

All settings save instantly. No Apply button.

---

## 11. Keyboard Navigation Map

| Key                   | Context                   | Action                                        |
|-----------------------|---------------------------|-----------------------------------------------|
| `Alt+Space`           | Global                    | Open launcher                                 |
| `Escape`              | Empty / Hover             | Dismiss launcher → Idle                       |
| `Escape`              | Active Search (text)      | Clear text → Empty State                      |
| `Escape`              | Category Browse           | Collapse results → Hover State                |
| `↑ / ↓`              | Result list               | Navigate results                              |
| `Enter`               | Selected result           | Execute primary action                        |
| `Ctrl+Enter`          | Selected result           | Execute secondary action (Run as admin)       |
| `Ctrl+K`              | Selected result           | Show all actions overlay                      |
| `Ctrl+C`              | Selected result           | Copy value / path to clipboard                |
| `Ctrl+Backspace`      | Search bar                | Clear entire input                            |
| `↑` (no text)         | Calculator mode           | Cycle expression history                      |
| `Win+Shift+V`         | Global                    | Open launcher directly on Clipboard           |
| `Ctrl+P`              | Clipboard item selected   | Pin / unpin item                              |
| `Ctrl+D`              | Clipboard item selected   | Delete item                                   |
| `Ctrl+,`              | Any                       | Open Settings window                          |
| `Tab`                 | Filter chips row          | Cycle through filter chips                    |

---

## 12. State Machine Summary

```
                     ┌──────────────────────┐
                     │        IDLE          │
                     │   (invisible)        │
                     └──────────┬───────────┘
                                │ Alt+Space
                     ┌──────────▼───────────┐
                     │     EMPTY STATE      │◄──────────────────────────────┐
                     │   (bar only, 56px)   │                               │
                     └──┬───────────┬───────┘                               │
                        │ hover     │ type                                   │
               ┌────────▼───┐    ┌──▼─────────────────────┐                │
               │   HOVER    │    │    ACTIVE SEARCH        │                │
               │  (circles  │    │  (results + chips)      │◄─── backspace ─┤
               │  appear)   │    └──┬──────────┬───────────┘                │
               └──┬────┬────┘       │ chip     │ Enter                      │
                  │    │ type       │ click     │                            │
                  │    └────────────┼───────┐   │                            │
                  │ click circle   ▼       │   │                            │
               ┌──▼───────────────────┐   │   │                            │
               │   CATEGORY BROWSE    │  ┌▼──────────────┐                 │
               │  (pre-typed browse)  │  │   FILTERED     │                 │
               └──────────┬───────────┘  │   SEARCH       │                 │
                          │ type         └──────┬──────────┘                │
                          └──────────────────── │ Escape ────────────────────┘
                                                │ Enter
                                     ┌──────────▼───────────┐
                                     │   ACTION EXECUTED    │
                                     │  (launcher dismisses)│
                                     └──────────┬───────────┘
                                                │
                                                ▼
                                             IDLE


  Win+Shift+V → opens directly on Clipboard Mode (skips Hover)
  Math input  → auto-activates Calculator Mode from Active Search
  Module keyword → Module Mode activates inside Active Search
  Escape always steps back one level, never skips levels
```
