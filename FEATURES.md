# FEATURES.md — Arc

Arc is a keyboard-first app launcher for Windows. Press `Alt+Space`, type anything, get results instantly. It replaces the Windows Start Menu search for people who want something faster, cleaner, and smarter.

---

## How Search Works

Arc has two states:

**Idle (empty query):**
The window shows only the search bar and a row of 4 category icon buttons. Nothing else. Clean.

**Active (typing):**
Results expand below the search bar automatically. Results come from whichever category is active (or all, if none selected). Results update on every keystroke with no delay.

Search is fuzzy — you don't need to spell things exactly. "chr" finds Chrome. "vs c" finds VS Code.

Results are ranked by relevance + how often you've opened that item. The more you use something, the higher it appears.

---

## Category Buttons (the 4 icons)

These are the 4 circular icon buttons shown in the search bar row. Clicking one filters results to that category. Clicking the active one deselects it (shows all). Keyboard: `Tab` cycles through them.

| Icon | Category | What it searches |
|------|----------|-----------------|
| Apps icon | **Apps** | All installed applications from Start Menu |
| Folder icon | **Files** | Files and folders across the system |
| Stack/layers icon | **Clipboard** | Last 20 items copied to clipboard |
| Pages/docs icon | **Actions** | Built-in commands: calculator, timer, IP, color, AI |

This is the exact layout from the reference image — pill search bar on the left, 4 circular buttons on the right.

---

## Search Categories

### Apps
- Discovers all installed apps from Windows Start Menu shortcuts
- Shows app icon + app name
- Press `Enter` to launch
- Press `Ctrl+Enter` to open file location of the app

### Files
- Searches files and folders using Windows Search Index
- Shows file type icon + file name + folder path as subtitle
- Press `Enter` to open in default app
- Press `Ctrl+Enter` to open containing folder in Explorer

### Clipboard
- Shows last 20 items copied to clipboard, most recent first
- Each item shows a preview of the copied text (truncated at 60 chars)
- Press `Enter` to paste the selected item into the previously focused app
- Press `Ctrl+Enter` to copy it again without pasting

### Actions (built-in commands)
These activate automatically when you type specific patterns. No need to select the Actions category first — they just appear.

| What you type | What happens |
|--------------|-------------|
| Any math: `15 * 3`, `(100/4)+2` | Shows result instantly: `= 45` |
| `timer 10m` or `timer 30s` | Starts a countdown. Windows notification when done. |
| `#FF5733` or any hex color | Shows color swatch + hex, RGB, HSL values |
| `ip` | Shows your local IP and public IP |
| `ai [anything]` | Sends your question to Groq AI, streams the answer |

---

## AI Feature

- Type `ai ` followed by any question
- The answer streams word by word in the preview panel on the right
- No conversation history — each question is fresh
- Requires a Groq API key (free, set once in settings)
- Shows "Add your Groq API key in Settings" if key is not set
- Shows "No internet connection" if offline

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Alt+Space` | Open / close Arc (global, works anywhere) |
| `Escape` | Close Arc (or clear query first if typing) |
| `↓` / `↑` | Move through results |
| `Enter` | Open / run selected result |
| `Ctrl+Enter` | Secondary action (open folder, copy value) |
| `Tab` | Cycle through the 4 category buttons |
| `Ctrl+,` | Open Settings (from anywhere inside Arc) |
| `Backspace` | Clear last character (normal typing behavior) |

---

## Settings

Settings are accessed by pressing `Ctrl+,` inside Arc, or by typing `settings` in the search bar.

| Setting | Options |
|---------|---------|
| Theme | Light / Dark / Follow Windows |
| Shortcut | Change the global open shortcut (default: `Alt+Space`) |
| Groq API Key | Text input — paste your free Groq key here |
| Results count | How many results to show (5 / 8 / 10) |
| File search | Toggle file search on/off (on by default) |
| Clipboard history | Toggle clipboard tracking on/off (on by default) |

Settings are saved instantly. No save button. No restart required.

---

## What Arc Does NOT Do

- No browser history search
- No web search (AI can answer questions instead)
- No plugin marketplace or installable extensions
- No cloud sync — everything is local
- No telemetry or analytics
- No auto-update (manual for now)
- No window management commands
- No snippet manager
- No calculator history

