# features.md — Core Feature Set

> **Rule:** If less than 30% of users would use it weekly, it doesn't ship as a core feature.
> Everything optional lives in the **Modules** system.

---

## Feature Tiers

```
CORE     — built-in, always present, can't be removed
BUILT-IN — ships with the app but can be toggled off in settings
MODULE   — installable by user, you build and publish them yourself
```

> **On categories:** Only 3 things get a dedicated circle in the hover state —
> the things Windows gives you no fast shortcut to: **Files, Commands, Clipboard**.
> App launching is the default result of typing (not a category — Windows has Start Menu).
> Calculator is auto-detected from your input (type math, it appears — no circle needed).

---

## CORE Features

### 1. App Launcher
**The primary reason anyone opens a launcher — and it just works by typing.**

> No dedicated category circle. No browse mode. Windows has Start Menu for that.
> You type, apps come up. That's it.

- Searches installed apps instantly (indexes Win32, UWP, Microsoft Store, Steam, etc.)
- Fuzzy matching — type `"vsc"` → VS Code, `"ps"` → Photoshop
- Frecency learning — apps you open most float to top automatically
- App results always rank first above files, commands, and web results
- `Ctrl+Enter` opens app as Administrator
- No "browse all apps" mode — that's what the Start Menu is for

---

### 2. Universal Search
**Files, folders, and settings in one bar.**

- File search powered by Windows Search Index (instant, no extra indexing)
- Optional deep search via **Everything SDK** integration (filename search, sub-100ms)
- Search Windows Settings panels directly (type `"wifi"` → opens WiFi settings)
- Results grouped by type: Apps → Files → Settings → Web
- File preview on select (image thumbnail, text snippet, size/date metadata)

---

### 3. Clipboard History
**Consistently the most-loved feature in every launcher survey.**

- Stores last 50 copied items by default (text, images, file paths)
- Triggered by `Win+Shift+V` or typing `clip` in the search bar
- Search within clipboard history
- Pin important items (they never expire)
- One-click to paste, or press `Enter`
- Items stored locally, never synced or sent anywhere — privacy first
- Respects sensitive fields: clipboard cleared after copying passwords (detects password manager patterns)

---

### 4. Inline Calculator
**Type math, get answers. No circle, no mode switch — just type.**

> Auto-activates when input looks like math. You never have to think about it.
> Windows has a Calculator app — this isn't replacing it, it's faster for quick math.

- Activates automatically: `2 + 2`, `15% of 300`, `sqrt(144)`, `100 USD to NGN`
- Appears as a result row inline — not a separate panel
- Supports: arithmetic, percentages, square roots, powers, parentheses
- `Enter` copies result to clipboard
- History of last 10 calculations (press `↑` to cycle)
- Currency conversion (live rates, fetched once per session)
- Unit conversion: `10km to miles`, `32°F to °C`, `500ml to cups`

---

### 5. System Commands
**The commands you type 10x a day.**

- `sleep` / `lock` / `restart` / `shutdown` / `sign out`
- `empty trash` — empties Recycle Bin
- `toggle dark mode` — flips system theme
- `volume up/down/mute`
- `screenshot` — triggers Windows snipping
- All commands are fuzzy-searchable — type `"dark"` → shows "Toggle Dark Mode"
- Confirmations only for destructive actions (shutdown, restart)

---

### 6. Quick Web Search
**Search any site without opening a browser.**

- Default: Google, DuckDuckGo (user-configurable)
- Keyword shortcuts: `g something` → Google, `yt something` → YouTube, `gh something` → GitHub
- User can define custom shortcuts (e.g., `tw something` → Twitter search)
- Results open in default browser
- Built-in shortcuts ship with the app: Google, YouTube, GitHub, Wikipedia, MDN, npm, Reddit

---

## BUILT-IN Features (toggleable)

### 7. Process Switcher
**Jump to any open window, fast.**

- `Alt+Tab` replacement — shows open windows with thumbnails
- Type to filter by window title or app name
- Bring window to focus or close it from here
- Off by default — opt-in in settings

---

### 8. Quick Notes (Scratch Pad)
**Fleeting thought capture, not a notes app.**

- Triggered by `Ctrl+Shift+Space` or typing `note`
- Single floating note window — plain text only
- Note persists between sessions (one note, always the same)
- Purpose: jot something fast, not manage a whole note system
- Export as `.txt` or copy all

---

### 9. Color Picker
**For developers and designers.**

- Triggered by typing `color` or `pick color`
- Activates system-wide eyedropper cursor
- Outputs: HEX, RGB, HSL — user picks preferred format
- Copies to clipboard on click
- Shows last 5 picked colors

---

## MODULE System

> Modules are what you (the developer) build and distribute.
> They are not called "plugins" — they are **Modules**.
> Clean name. Clean concept.

### How Modules Work
- Each Module is a self-contained folder: `module.json` + optional Rust binary or Node script + optional UI
- Loaded by the app at startup — no restart needed for most module types
- Modules appear as searchable commands, result providers, or both
- Modules run sandboxed — no file system access beyond what they declare in `module.json`
- A Module can register a keyword trigger: `spotify play` → Spotify module handles it

### module.json schema (minimal)
```json
{
  "id": "com.yourname.modulename",
  "name": "Module Name",
  "version": "1.0.0",
  "description": "One line. What does this do?",
  "trigger": "sp",
  "permissions": ["network", "clipboard_write"],
  "entry": "main.js"
}
```

### Built-in First-Party Modules (you build these yourself later)
| Module              | Trigger   | Description                            |
|---------------------|-----------|----------------------------------------|
| Spotify Controls    | `sp`      | Play/pause, skip, search tracks        |
| GitHub              | `gh`      | Search repos, open issues, PRs         |
| VS Code Projects    | `code`    | Open recent workspaces in VS Code      |
| Timer               | `timer`   | Set a countdown from the bar           |
| IP / Network Info   | `ip`      | Show local IP, copy to clipboard       |
| Emoji Picker        | `:`       | Fuzzy search all emoji                 |
| UUID Generator      | `uuid`    | Generate and copy a UUID instantly     |

---

## What Was Left Out (and Why)

| Feature              | Reason cut                                                    |
|----------------------|---------------------------------------------------------------|
| Window Manager       | Belongs in a dedicated tool (PowerToys FancyZones, etc.)      |
| AI Chat              | Adds weight; AI lives in Modules if the user wants it         |
| Browser Bookmark Search | Too platform-specific; lives as a Module                 |
| Calendar integration | Overhead without universal value; Module territory            |
| Contacts search      | Low usage frequency; confuses the search surface              |
| Cloud sync           | Privacy risk, complexity; local-first is the principle        |
| Custom Themes marketplace | Scope creep; 8 presets + dark/light is enough          |
| Workflow builder (visual) | Too complex for v1; reduces to just scripts in Modules  |
