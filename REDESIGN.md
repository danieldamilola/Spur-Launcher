# Arc — UX Redesign v2

## Architecture: Two Windows

| Window | Purpose | Behavior |
|---|---|---|
| **Launcher bar** | Search, browse, actions, AI | Frameless, floating, `Alt+Space`, hides on blur |
| **Settings** | Configuration | Standard window, title bar, close button, opened via `Ctrl+,` / tray / search |

Closing Settings never closes the Launcher bar. Independent windows, shared config. Changes apply instantly.

---

## Information Architecture

### Three Categories (Apps removed)

Windows has a Start Menu for browsing all apps. Arc is for *fast access*, not app discovery. Apps are searched, not browsed.

| Category | Icon | Purpose |
|---|---|---|
| **Files** | Folder | Recent files, filter by type |
| **Clipboard** | Clipboard | Dedicated clipboard history only |
| **Actions** | Zap | Calculator, currency, timer, color, IP, AI, password, notes, system commands |

Clipboard items are **never shown in general search.** They belong in the Clipboard category only. This keeps search results clean — apps, files, and actions only.

---

## Screen 1: Hub — Idle, No Hover

```
┌──────────────────────────────────────────────────────┐
│                                                      │
│   🔍  Search apps, files, clipboard...               │
│                                                      │
│   ┌──────────────────────────────────────────────┐  │
│   │  ⊡  Google Chrome               ↵           │  │
│   │  ⊡  Project Docs/               ↵           │  │
│   │  ⊡  Calculator                  ↵           │  │
│   └──────────────────────────────────────────────┘  │
│                                                      │
│     ⊡ Settings                                    │  │
│                                                      │
└──────────────────────────────────────────────────────┘
```

- 3 recent items max (from FrequencyService)
- No section labels, no emoji, no quick actions
- Category icons hidden
- Settings link in footer
- **Empty state (first launch):** "Your recent items will appear here."

---

## Screen 1b: Hub — Hover Over Bar

```
┌──────────────────────────┬────────────────────────────┐
│                          │                            │
│  🔍  Search anything...  │  ○ Files  ○ Clipboard  ⚡  │
│                          │                            │
│  ┌───────────────────┐   │                            │
│  │  ⊡  Chrome     ↵ │   │                            │
│  │  ⊡  Docs/      ↵ │   │                            │
│  │  ⊡  Calculator ↵ │   │                            │
│  └───────────────────┘   │                            │
│                          │                            │
└──────────────────────────┴────────────────────────────┘
     ~468px                     ~212px
```

- Bar shrinks by ~212px on hover
- Three category icons slide in from right
- 44px glass circles with icon
- Onboarding teaches: "Hover here to reveal modes →"
- No cursor tracking — pure hover-on-bar trigger

---

## Screen 2: Search Results

```
┌──────────────────────────────────────────────────────┐
│   🔍  chr                                            │
│   ─────────────────────────────────────────────────  │
│                                                      │
│   Applications                                       │
│   ▶  Google Chrome                        ↵         │
│      Chromium                                       │
│                                                      │
│   Files                                              │
│      chrome_installer.exe                 ↵         │
│                                                      │
│   Actions                                            │
│      Search web for chrome                          │
│                                                      │
│   ─────────────────────────────────────────────────  │
│     ⊡ Settings                                      │
│                                                      │
└──────────────────────────────────────────────────────┘
```

**Result row rules:**
- **Apps:** Name only. No "Web Browser" / "Application" subtitle.
- **Files:** Name + extension. Path on hover tooltip only.
- **Actions:** Name + instruction subtitle.
- **Clipboard:** Never shown here.
- `↵` hint on selected row only.
- Section labels: title case, muted, 10px.

---

## Screen 3: Browse — Files

```
┌──────────────────────────────────────────────────────┐
│   ○ Files  ○ Clipboard  ⚡ Actions                   │
│   ─────────────────────────────────────────────────  │
│                                                      │
│   All  Documents  Images  PDF  Video  Music  Code   │
│                                                      │
│   ⊡  Project Proposal.docx       Documents/    ↵   │
│   ⊡  budget-2026.xlsx            Documents/    ↵   │
│   ⊡  profile-photo.jpg           Pictures/     ↵   │
│   ⊡  arc-logo.png                Desktop/      ↵   │
│                                                      │
└──────────────────────────────────────────────────────┘
```

- Category icons in header (not floating)
- Filter chips across top
- File extension as subtitle
- Folder path as secondary text

---

## Screen 4: Browse — Clipboard

```
┌──────────────────────────────────────────────────────┐
│   ○ Files  ○ Clipboard  ⚡ Actions                   │
│   ─────────────────────────────────────────────────  │
│                                                      │
│   12 items                              [Clear all]  │
│                                                      │
│   ⊡  Copied text preview here...      2m ago   ↵   │
│   ⊡  Another copied snippet...        5m ago   ↵   │
│   ⊡  [Image thumbnail]                10m ago  ↵   │
│   ⊡  https://example.com/long-url...  1h ago   ↵   │
│                                                      │
└──────────────────────────────────────────────────────┘
```

- Header with item count and clear action
- Text entries: preview truncated at 60 chars
- Image entries: small thumbnail
- Time shown as right-aligned muted text
- Enter reuses the clipboard item

---

## Screen 5: Browse — Actions

```
┌──────────────────────────────────────────────────────┐
│   ○ Files  ○ Clipboard  ⚡ Actions                   │
│   ─────────────────────────────────────────────────  │
│                                                      │
│   ⊡  Calculator         Type "2 + 2"                │
│   ⊡  Currency           Type "100 usd to eur"       │
│   ⊡  Timer              Type "timer 5m"             │
│   ⊡  Color Picker       Type "#ff0000"              │
│   ⊡  IP Address         Type "ip"                   │
│   ⊡  AI Assistant       Type "ai" then question     │
│   ⊡  Password Gen       Type "pw 16"                │
│   ⊡  Quick Note         Type "note" then text       │
│                                                      │
│   System                                            │
│   ⊡  Kill Process       Type "kill" then name       │
│   ⊡  Screenshot         Type "screenshot"           │
│   ⊡  Lock                                             │
│   ⊡  Sleep                                            │
│   ⊡  Restart                                          │
│   ⊡  Shutdown                                         │
│   ⊡  Empty Recycle                                    │
│                                                      │
└──────────────────────────────────────────────────────┘
```

**Actions list:**

| Action | Trigger | Behavior |
|---|---|---|
| Calculator | `2 + 2` | Evaluates, shows result in preview |
| Currency | `100 usd to eur` | Converts, shows in preview, copies on Enter |
| Timer | `timer 5m` | Starts countdown, notification when done |
| Color Picker | `#ff0000` | Shows swatch + hex/RGB/HSL |
| IP Address | `ip` | Shows local + public IP |
| AI Assistant | `ai [question]` | Full-width chat, streaming response |
| Password Gen | `pw 16` | Generates, copies on Enter |
| Quick Note | `note [text]` | Saves plain-text note locally |
| Kill Process | `kill [name]` | Force-closes process |
| Screenshot | `screenshot` | Captures screen, saves to Desktop |
| Lock | `lock` | Locks workstation |
| Sleep | `sleep` | Puts computer to sleep |
| Restart | `restart` | Restarts computer |
| Shutdown | `shutdown` | Shuts down computer |
| Empty Recycle | `empty recycle` | Empties the bin |

---

## Screen 6: Action Preview — Calculator

```
┌─────────────────────────────┬─────────────────────────┐
│   🔍  15 * 3 + 2            │                         │
│   ──────────────────────────┤                         │
│                             │        47               │
│   Calculator                │                         │
│   ▶  = 47                   │   15 × 3 + 2            │
│                             │                         │
│                             │   [Copy]                │
│                             │                         │
└─────────────────────────────┴─────────────────────────┘
```

Split view. Large mono result. Expression shown below. Copy button.

---

## Screen 7: AI Chat — Full Width

```
┌──────────────────────────────────────────────────────┐
│  ✨ AI Assistant                          [⇠]  [📋]  │
│  ─────────────────────────────────────────────────── │
│                                                      │
│  ┌──────────────────────────────────────────────┐   │
│  │                                              │   │
│  │  You                                         │   │
│  │  What's the capital of France?               │   │
│  │                                              │   │
│  │  Arc                                         │   │
│  │  The capital of France is Paris. It's also   │   │
│  │  the largest city in France with a           │   │
│  │  population of approximately 2.1 million.    │   │
│  │                                              │   │
│  └──────────────────────────────────────────────┘   │
│                                                      │
│  ┌──────────────────────────────────────────────┐   │
│  │  Type a follow-up...                     [↑]  │   │
│  └──────────────────────────────────────────────┘   │
│                                                      │
└──────────────────────────────────────────────────────┘
```

- `⇠` icon = Back to Hub (conversation cleared)
- `📋` icon = Copy entire conversation
- `↑` icon = Send (Enter also works)
- Shift+Enter inserts newline in composer
- Streaming text appears character by character

---

## Screen 8: Settings — Separate Window

```
┌─────────────────────────────────────────────────────┐
│  Arc Settings                                  — □ ✕ │
│  ─────────────────────────────────────────────────── │
│                                                      │
│  ┌──────────────┬──────────────────────────────────┐ │
│  │              │                                   │ │
│  │ ● Appearance │  Theme                           │ │
│  │   Search     │  ○ Dark  ● Light  ○ System       │ │
│  │   Hotkey     │                                   │ │
│  │   AI         │  Opacity                         │ │
│  │   Privacy    │  [══════════○────] 85%            │ │
│  │   About      │                                   │ │
│  │              │  Accent Color                    │ │
│  │              │  [#5B7EFF] ▪                     │ │
│  │              │                                   │ │
│  │              │  Compact Mode                    │ │
│  │              │  Show only search bar when idle  │ │
│  │              │  [Off]                            │ │
│  │              │                                   │ │
│  └──────────────┴──────────────────────────────────┘ │
│                                                      │
│                                        Arc v0.2.0    │
└─────────────────────────────────────────────────────┘
```

**Settings: 6 sections**

| Section | Settings |
|---|---|
| **Appearance** | Theme, Opacity, Background Color, Accent Color, Font, Compact Mode |
| **Search** | Indexed Folders, File Extensions, Max File Depth |
| **Hotkey** | Arc Shortcut, Launch on Startup, Show Tray Icon, Custom Shortcuts |
| **AI** | Provider, Model, API Key |
| **Privacy** | Clear Clipboard on Exit, Clear Usage Data |
| **Actions** | Enable/disable each action independently: Calculator, Currency, Timer, Color Picker, IP, AI, Password Gen, Quick Note, Kill Process, Screenshot, Lock, Sleep, Restart, Shutdown, Empty Recycle |
| **About** | Version, Credits, License (read-only) |

**Access:** `Ctrl+,` · Tray right-click · Type "settings" · Settings icon in Hub footer

---

## Onboarding: First Launch (3 Slides)

**Slide 1: Welcome**
```
┌───────────────────────────────────────────────┐
│                                               │
│            Welcome to Arc                    │
│                                               │
│     The fastest way to launch anything       │
│     on Windows.                              │
│                                               │
│     Press Alt+Space to open Arc.             │
│     Type to search. Enter to launch.         │
│                                               │
│                    [Next →]                   │
└───────────────────────────────────────────────┘
```

**Slide 2: Modes**
```
┌───────────────────────────────────────────────┐
│                                               │
│            Three Modes                        │
│                                               │
│     Hover over the bar to reveal:            │
│                                               │
│     ○ Files    Recent files                  │
│     ○ Clipboard  Copy history                │
│     ⚡ Actions  Calculator, AI, Timer...     │
│                                               │
│     Or just type — Arc searches everything.  │
│                                               │
│                    [Next →]                   │
└───────────────────────────────────────────────┘
```

**Slide 3: Done**
```
┌───────────────────────────────────────────────┐
│                                               │
│            You're Ready                       │
│                                               │
│     Press Ctrl+, anytime for Settings.       │
│                                               │
│     Press Alt+Space now to start.            │
│                                               │
│                  [Get Started]                │
└───────────────────────────────────────────────┘
```

---

## User Flows

### Flow 1: First Launch

```
Install → Open Arc → Onboarding (3 slides, skippable) → Hub
    → Types "chrome" → Enter → Chrome launches
    → Chrome appears in recent items on next open
```

### Flow 2: Power User

```
Alt+Space → "chr" → Enter                          (launch app)
Alt+Space → "ai what is rust" → Enter → chat       (AI)
Alt+Space → Hover → Click Actions → Timer → "5m"   (timer)
Alt+Space → Hover → Click Clipboard → find item    (clipboard)
Alt+Space → "note meeting at 3pm" → Enter          (quick note)
Alt+Space → "pw 20" → Enter                        (password)
```

### Flow 3: Escape Unwind

```
Typing → Esc → Query cleared → Hub with recent items
AI chat → Esc → Back to Hub (conversation cleared)
Settings window → Esc/Close → Settings closes (bar unaffected)
```

### Flow 4: Settings

```
Ctrl+, → Settings window opens → Change theme → Bar updates instantly → Close
Tray → Right-click → Settings → Same window
Type "settings" → Enter → Settings window opens
```

---

## Animation Spec

| Event | Duration | Easing |
|---|---|---|
| Window open | 160ms | Ease out |
| Window close | 100ms | Ease in |
| Bar shrink on hover | 200ms | Ease out |
| Category icons slide in | 200ms | Ease out (delayed 50ms) |
| Category icons slide out | 150ms | Ease in |
| Pill ↔ Panel corner radius | 180ms | Ease out |
| Results appear | 120ms | Ease out |
| Row hover bg | 80ms | Linear |
| Row selection | 100ms | Ease out |
| Preview panel open | 180ms | Ease out |
| AI full-width enter/exit | 200ms | Ease out |
| Settings section switch | 150ms | Ease out |

---

## Empty & Error States

| State | Message |
|---|---|
| No recent items | Your recent items will appear here. |
| No search results | No results. |
| AI key missing | API key not set. [Open Settings] |
| AI network error | Couldn't connect. [Retry] |
| Catalog loading | Loading... |
| Clipboard empty | Nothing copied yet. |

---

## Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Alt+Space` | Open/close Arc |
| `Esc` | Back one level (unwind) |
| `↑` / `↓` | Move selection |
| `Enter` | Open selected |
| `Ctrl+Enter` | Open folder / copy value |
| `Ctrl+Shift+Enter` | Run as admin |
| `Ctrl+,` | Open Settings window |
| `Ctrl+1` | Browse Files |
| `Ctrl+2` | Browse Clipboard |
| `Ctrl+3` | Browse Actions |
| `Ctrl+P` | Pin/unpin selected |
| `Ctrl+R` | Refresh app catalog |
| `Tab` | Cycle categories |
| `Shift+Enter` | (AI composer) Insert newline |

---

## Scoring: Frequency-Weighted Search

The search engine uses two signals:

1. **Fuzzy match score** — how well the query matches the target name (character runs, word boundaries, exact case)
2. **Frequency score** — how often the user launches this item

The scoring formula:

```
finalScore = fuzzyScore + frequencyBoost
frequencyBoost = log₂(launchCount + 1) × 0.5
```

| Launches | Boost | Effect |
|---|---|---|
| 1 | +0.5 | Slight edge over equal fuzzy matches |
| 3 | +1.0 | Noticeable preference |
| 7 | +1.5 | Clear winner for short queries |
| 15 | +2.0 | Dominates fuzzy score for one-char queries |
| 31 | +2.5 | Always appears first |

This means: type "w" → WhatsApp (launched 20 times) beats Windsurf (launched 2 times), even if Windsurf has a slightly better fuzzy match.

---

## Trade-offs

| Decision | Optimizes for | Sacrifices |
|---|---|---|
| Hub with 3 recents (not empty pill) | Speed, first-launch clarity | Empty-pill aesthetic |
| Bar shrinks to reveal categories | Clean idle + playful discovery | Requires onboarding |
| 3 categories, no Apps | Search purity, no redundant browsing | App grid browsing |
| Clipboard: dedicated category only | Clean search results | Quick clipboard access from search |
| Separate settings window | Bar purity, settings usability | Two-window architecture |
| No app subtitles | Visual cleanliness | Type info for unknown apps |
| AI conversation clears on Esc | Mental model consistency | Conversation persistence |
| Compact mode as opt-in | Power user preference | Additional setting |
| Frequency-weighted scoring | Speed for frequent actions | Predictable alphabetical ordering |

---

## Deferred

| Item | Reason |
|---|---|
| Extensions/plugins | Later version |
| Conversation save/export | Requires file format decisions |
| Rich AI responses (markdown) | AI returns plain text by design |
| Per-app shortcut editor UI | Ship basic shortcuts first |
