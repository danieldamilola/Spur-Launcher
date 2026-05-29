# DESIGN.md — Arc

> **Implementation note:** Arc is built with **.NET 9 + WPF** (not WebView2/Tauri).
> The CSS and `backdrop-filter` references in this document describe the visual target.
> The actual rendering uses WPF brushes, storyboards, and DWM acrylic (`DWMSBT_TRANSIENTWINDOW`)
> to approximate frosted glass on Windows 10/11.

## Design Direction

Arc is Spotlight brought to Windows — but not copied. It takes Spotlight's philosophy (one bar, total calm, results appear when needed), Raycast's information density and result row clarity, and adapts both to feel native on Windows 11.

The defining visual choice: **frosted glass**. The window has no solid background — it blurs whatever is behind it. On Windows 11, `backdrop-filter: blur()` is supported in WebView2. This makes Arc feel like it belongs to the OS rather than sitting on top of it. Spotlight does this on Mac. Arc does it on Windows.

**Three references:**
- **Spotlight (macOS)** — the overall philosophy: compact → expands on type, category pills, preview panel, frosted glass
- **Raycast** — result row density, big search bar, keyboard shortcut hints on the right, action bar concept
- **Flow Launcher** — Windows-native sensibility, flat result rows, monochrome-first palette

**What makes Arc different from all three:**
The idle state shows only the search bar + 4 category icon buttons in a single floating row — like the image reference provided. The window is as small as possible when you're not using it. It only grows when you need it to.

---

## Window Behavior

**Idle state (empty query):**
```
┌──────────────────────────────────────────────────────┐
│  🔍  Search apps, files, clipboard...    ⊞ 📁 ⊞ ⊞  │
└──────────────────────────────────────────────────────┘
```
- A single pill-shaped bar floating in the center of the screen
- Width: 680px. Height: 56px
- Frosted glass background — blurs whatever is behind it
- Search icon on the left, placeholder text, 4 category icon buttons on the right
- This is the entire window when idle. Nothing else.

**Active state (typing):**
```
┌──────────────────────────────────────────────────────┐
│  🔍  chrome                              ⊞ 📁 ⊞ ⊞  │
├──────────────────────────────────────────────────────┤
│  APPLICATIONS                                        │
│  ▶  Google Chrome          Web Browser         ↵   │  ← selected
│     Microsoft Edge         Web Browser              │
│  FILES                                               │
│     chrome_installer.exe   Downloads/          ↵   │
│     ...                                              │
└──────────────────────────────────────────────────────┘
```
- Window expands downward to show results
- Max height: 520px (scrollable inside)
- The search bar stays at the top, pinned
- Results appear below a hairline divider

**Active state with preview panel (AI, color, calculator):**
```
┌─────────────────────────┬────────────────────────────┐
│  🔍  ai what is ...     │                            │
├─────────────────────────┤   Preview Panel            │
│  Results list           │   (AI response, color      │
│  (left, 380px)          │    swatch, calc result)    │
│                         │   (right, 300px)           │
└─────────────────────────┴────────────────────────────┘
```
- Total width expands to 680px when preview panel is shown
- Left: results list. Right: preview panel content.
- Hairline vertical divider between them.

---

## Glass Effect

This is the signature of Arc. Every surface is glass.

**Light mode glass:**
```css
background: rgba(255, 255, 255, 0.72);
backdrop-filter: blur(24px) saturate(1.8);
-webkit-backdrop-filter: blur(24px) saturate(1.8);
border: 1px solid rgba(255, 255, 255, 0.5);
```

**Dark mode glass:**
```css
background: rgba(20, 20, 22, 0.78);
backdrop-filter: blur(24px) saturate(1.6);
-webkit-backdrop-filter: blur(24px) saturate(1.6);
border: 1px solid rgba(255, 255, 255, 0.08);
```

**Important:** Arc uses WPF and .NET 9 for Windows. The glass effect is achieved natively using DWM interoperability (`DwmSetWindowAttribute`) to enable Windows 11 Mica or Acrylic system backdrops (`DWMSBT_TRANSIENTWINDOW` or `DWMSBT_MAINWINDOW`).

---

## Color Palette

Arc is monochrome-first. The only color in the UI is:
- Black/white for text
- One blue for selected states and category button active state
- Semantic colors (green/orange/red) for action types only

### Light Mode

| Role | Value | Usage |
|------|-------|-------|
| Glass background | `rgba(255,255,255,0.72)` | Main window surface |
| Surface (opaque areas) | `#ffffff` | Inside preview panel |
| Surface low | `#f5f5f5` | Hover row background |
| Primary text | `#0a0a0a` | App names, result titles |
| Secondary text | `#6b6b6b` | Subtitles, file paths |
| Muted text | `#a0a0a0` | Placeholder, keyboard hints |
| Border | `rgba(0,0,0,0.08)` | Hairline dividers |
| Border strong | `rgba(0,0,0,0.14)` | Section dividers |
| Selected row bg | `#0a0a0a` | Active/selected result |
| Selected row text | `#ffffff` | Text on selected row |
| Accent (blue) | `#3650d4` | Category button active, cursor |
| Accent wash | `#e8edfb` | Category button active bg |

### Dark Mode

| Role | Value | Usage |
|------|-------|-------|
| Glass background | `rgba(20,20,22,0.78)` | Main window surface |
| Surface (opaque areas) | `#1a1a1c` | Inside preview panel |
| Surface low | `#242426` | Hover row background |
| Primary text | `#f0f0f0` | App names, result titles |
| Secondary text | `#8a8a8a` | Subtitles, file paths |
| Muted text | `#555555` | Placeholder, keyboard hints |
| Border | `rgba(255,255,255,0.06)` | Hairline dividers |
| Border strong | `rgba(255,255,255,0.10)` | Section dividers |
| Selected row bg | `#f0f0f0` | Active/selected result |
| Selected row text | `#0a0a0a` | Text on selected row |
| Accent (blue) | `#5b7eff` | Category button active, cursor |
| Accent wash | `#171f42` | Category button active bg |

### Semantic (both modes)

| Token | Light | Dark | Usage |
|-------|-------|------|-------|
| Green | `#1a7a40` | `#5ee577` | Calculator result, success |
| Orange | `#a04f00` | `#ffc779` | Timer, clipboard |
| Red | `#c0180f` | `#ff7b7b` | Error states |
| Blue | `#3650d4` | `#5b7eff` | AI, active states |

---

## Typography

Two fonts only. No exceptions.

- **DM Sans** — all UI text: search input, result names, subtitles, settings labels, section headers
- **DM Mono** — all data text: keyboard shortcut hints, file paths, calculator results, IP addresses, color values, timer countdown

| Usage | Font | Size | Weight |
|-------|------|------|--------|
| Search bar input | DM Sans | 17px | 400 |
| Search bar placeholder | DM Sans | 17px | 400 |
| Result name | DM Sans | 14px | 500 |
| Result subtitle / path | DM Sans | 12px | 400 |
| Keyboard shortcut badge | DM Mono | 11px | 500 |
| Section label | DM Sans | 11px | 700 (uppercase) |
| Calculator output | DM Mono | 32px | 700 |
| Color hex value | DM Mono | 20px | 600 |
| Timer countdown | DM Mono | 48px | 700 |
| IP address | DM Mono | 16px | 500 |
| AI response body | DM Sans | 14px | 400 |
| Settings label | DM Sans | 14px | 500 |
| Settings value/hint | DM Sans | 13px | 400 |

---

## Window & Layout Sizes

| Element | Value |
|---------|-------|
| Window width (idle) | 680px |
| Window width (with preview) | 680px (same — preview is inside) |
| Search bar height | 56px |
| Window border radius | 16px |
| Result row height | 52px |
| Max visible results | 8 rows before scroll |
| Category button size | 36px × 36px circle |
| Category button radius | 50% (circle) |
| Preview panel width | 300px |
| Results list width (with preview) | 380px |
| Section label height | 28px |
| Window shadow | `0 24px 64px rgba(0,0,0,0.35), 0 4px 16px rgba(0,0,0,0.2)` |

---

## Search Bar

The search bar is the entire visual identity of Arc. It must feel premium.

- Full pill shape — `border-radius: 9999px` when idle (just the bar, no results)
- When results expand: top corners stay rounded (16px), bottom corners become square — the window becomes a rounded rectangle, not a pill
- Search icon: 18px, left-aligned, `--muted` color, 16px from left edge
- Input text starts 44px from left (after icon + gap)
- Placeholder: "Search apps, files, clipboard..." in `--muted`
- Right side of bar: the 4 category icon buttons, 8px gap between them, 12px from right edge
- No border on the search bar itself — the window border IS the search bar border when idle
- The text cursor in the input is `--accent` (blue) color

---

## Category Buttons (the 4 icons)

These are displayed as a floating panel on the right side of the window.

- They sit inside a floating panel that smoothly slides out when you hover over the main search bar area.
- This creates an uncluttered look when idle, but provides quick access to filters when engaged.

- Size: 36px × 36px circle
- Default bg: transparent. Default icon color: `--muted`.
- Hover bg: `rgba(0,0,0,0.06)` light / `rgba(255,255,255,0.06)` dark
- Active (selected category): bg `--accent-wash`, icon color `--accent`, border `1px solid --accent` at 30% opacity
- Transition: background and icon color, 120ms ease
- Icons (use Lucide at 16px strokeWidth 1.5):
  - Apps: `layout-grid`
  - Files: `folder`
  - Clipboard: `clipboard`
  - Actions: `zap`

---

## Result Rows

Each result is a row inside the results list.

**Default:**
- Height: 52px. Full width. Horizontal padding: 16px.
- Left: icon (20px). App icons are real extracted .ico files. File icons are Lucide file-type icons at 20px.
- Center: result name in DM Sans 14px 500 `--text-primary`. Subtitle below in DM Sans 12px `--text-secondary`. `flex: 1`.
- Right: keyboard shortcut hint in DM Mono 11px, `--surface-low` bg, `--muted` text, `border-radius: 4px`, px-6 py-2.
- Background: transparent.

**Hover:**
- Background: `--surface-low`. Transition 100ms.

**Selected (keyboard focus):**
- Background: `--selected-bg` (black light / off-white dark).
- Text: `--selected-text` (white light / black dark).
- Subtitle: `--selected-muted`.
- Keyboard hint badge inverts.
- Left accent: 2px solid line on left edge, color matches result type (blue for files, black/white for apps).
- `border-radius: 8px` on selected row (clip the bg, not the row height).

**Section Labels:**
- 11px DM Sans 700 uppercase, `--muted` color.
- Padding: 8px top, 4px bottom, 16px horizontal.
- No background. No border. Pure label.

---

## Preview Panel

Shows on the right when an Action result is selected (AI, color, calculator, timer, IP).

- Background: glass surface (slightly more opaque than the main window — `rgba(255,255,255,0.85)` light / `rgba(18,18,20,0.85)` dark)
- Left edge: `1px solid --border-strong` dividing it from the results list
- Padding: 20px

**Calculator:** Result in DM Mono 32px 700, expression above in DM Mono 13px `--muted`. "↵ copy" hint bottom-right in 11px `--muted`.

**AI response:** Streaming text in DM Sans 14px `--text-primary`, line-height 1.6. Blinking cursor while streaming. "Powered by Groq" in 10px `--muted` bottom-right after done.

**Color:** 80px × 80px swatch with `border-radius: 8px`, `border: 1px solid --border`. Hex in DM Mono 20px 600 right of swatch. RGB + HSL below in DM Mono 12px `--muted`.

**Timer:** Countdown in DM Mono 48px 700, centered. Progress bar below (6px, `border-radius: 3px`). "Cancel" button ghost style bottom-right.

**IP:** "LOCAL" and "PUBLIC" labels (11px DM Sans 700 uppercase `--muted`) with IP values in DM Mono 16px 500 below each. "↵ copy" hint on hover.

---

## Settings Screen

Accessed via `Ctrl+,` or typing `settings`. Replaces the results area.

- Same glass window. Search bar stays at top showing "Settings".
- Settings content below the divider, padding 20px.
- Each row: 48px height, DM Sans 14px 500 label left, control right.
- `border-bottom: 1px solid --border` between rows. No border on last row.
- No save button — changes apply instantly.

Rows in order:
1. **Theme** — segmented control: Light / Dark / Windows
2. **Open shortcut** — text showing current shortcut (`Alt+Space`), click to remap
3. **Groq API Key** — text input, password masked, placeholder "Paste your Groq key..."
4. **Results to show** — segmented: 5 / 8 / 10
5. **File search** — toggle
6. **Clipboard history** — toggle
7. Divider
8. **Clear usage data** — ghost button, destructive red text
9. Version number — `Arc v0.1.0` in 11px `--muted`, right-aligned, bottom

**Segmented control:**
- Container: `border: 1px solid --border`, `border-radius: 6px`, `background: --surface-low`, `padding: 3px`
- Active segment: `background: --surface`, `color: --text-primary`
- Inactive: transparent, `--muted`

**Toggle:**
- Track: 40px × 22px, `border-radius: 99px`, `background: --border-strong` when off
- Active: `background: --accent`
- Knob: 16px white circle

---

## Animation

- **Window open:** opacity 0→1 + scale 0.96→1, 160ms `cubic-bezier(0.22, 1, 0.36, 1)`
- **Window close:** opacity 1→0 + scale 1→0.96, 100ms ease-in
- **Results expand (idle → active):** window height grows from 56px to full. CSS `max-height` transition with `overflow: hidden`, 160ms `cubic-bezier(0.22, 1, 0.36, 1)`
- **Results collapse (active → idle):** shrinks back to 56px, 120ms ease-in
- **Row hover:** background, 100ms ease
- **Row selection:** background + text color, 100ms ease
- **Preview panel appear:** opacity 0→1 + translateX 8→0, 180ms `cubic-bezier(0.22, 1, 0.36, 1)`
- **Category button active:** background + icon color, 120ms ease
- **AI streaming:** text appears token by token — no animation needed
- **Timer bar:** width transition, 1s linear

Nothing else animates. No springs. No bounces. No staggers on rows.

---

## What This Should Feel Like

You press `Alt+Space`. A frosted pill appears in the center of your screen. It feels like it grew out of the wallpaper. You type. Results slide in below instantly. You press Enter. It disappears.

The whole interaction takes 2 seconds and leaves no trace. That's the goal.

When it's open it should feel like a premium piece of glass floating above your desktop — lighter than the apps behind it, not heavier. The glass effect is not decoration. It is the product communicating: "I am not an app. I am a layer above everything."

Never cluttered. Never colorful. Never demanding attention. Just fast, calm, and precise.
