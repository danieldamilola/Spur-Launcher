# design.md — Visual Design System

> **Philosophy:** Clean and simplicity is the new premium.
> Borrow everything Apple mastered. Own it on Windows.

---

## 1. Core Design Principles (from Apple HIG)

These four principles govern every pixel, every margin, every animation decision.

### Clarity
- Every element must communicate exactly what it does — no decoration for decoration's sake
- Text is always legible at every size, in every context
- Icons are precise and carry meaning at a glance
- Adornments are stripped to zero unless they serve a function

### Deference
- The UI steps back. The user's task is the star
- The launcher disappears the moment the user acts — no lingering chrome
- No aggressive UI that competes with the content of the search result
- Background blur exists to *ground* the window, not to show off

### Depth
- Visual layers communicate hierarchy — results list sits "above" the background
- Shadows are used intentionally (one level of shadow, never stacked)
- Motion conveys direction and hierarchy — things slide in from where they belong

### Consistency
- Every interaction follows the same pattern: trigger → type → act → dismiss
- Keyboard shortcuts are predictable and never conflict with system defaults
- Visual weight is consistent — spacing, type scale, and color all speak the same language

---

## 2. Color System

### Base Palette

| Token               | Light Mode          | Dark Mode           | Usage                        |
|---------------------|---------------------|---------------------|------------------------------|
| `--bg-base`         | `#F5F5F7`           | `#141414`           | App window background        |
| `--bg-surface`      | `#FFFFFF`           | `#1C1C1E`           | Card / result item surface   |
| `--bg-elevated`     | `#FFFFFF`           | `#242426`           | Hover state / selected item  |
| `--border`          | `rgba(0,0,0,0.08)`  | `rgba(255,255,255,0.06)` | Dividers, input borders |
| `--text-primary`    | `#1C1C1E`           | `#F5F5F7`           | Main labels, app names       |
| `--text-secondary`  | `#6E6E73`           | `#8E8E93`           | Subtitles, hints, metadata   |
| `--text-tertiary`   | `#AEAEB2`           | `#48484A`           | Placeholder text             |
| `--accent`          | `#0A84FF`           | `#0A84FF`           | Selected state, cursor, CTA  |
| `--accent-subtle`   | `rgba(10,132,255,0.12)` | `rgba(10,132,255,0.15)` | Selected row background |
| `--destructive`     | `#FF3B30`           | `#FF453A`           | Delete actions               |

### Acrylic / Frosted Glass (Windows Mica)
- The launcher window itself uses **Windows Acrylic** (Mica Alt on Win11) for the background
- Blur radius: `20px` behind the window chrome
- Tint: `rgba(20,20,20, 0.72)` in dark mode / `rgba(245,245,247, 0.82)` in light mode
- This is *native* — powered by `DwmExtendFrameIntoClientArea` via Tauri's Rust backend, not CSS faking it

### Accent Color
- Default accent: **Apple Blue** `#0A84FF` — clean, universal, trustworthy
- User can override with one accent color from a preset palette of 8 colors
- Accent is used sparingly: selected row, cursor, active icons only

---

## 3. Typography

### Font Stack
```
Primary:   "Inter Variable", "Segoe UI Variable", system-ui, sans-serif
Monospace: "Geist Mono", "Cascadia Code", "Consolas", monospace
```
Inter Variable is bundled with the app (300kb). It is the closest Windows equivalent to San Francisco — neutral, legible, optical-weight-corrected.

### Type Scale (8pt base grid)

| Role              | Size   | Weight  | Line Height | Letter Spacing | Usage                    |
|-------------------|--------|---------|-------------|----------------|--------------------------|
| `display`         | 17px   | 600     | 22px        | -0.3px         | Category headers         |
| `body-lg`         | 15px   | 500     | 20px        | -0.2px         | App / result names       |
| `body`            | 13px   | 400     | 18px        | -0.1px         | Subtitles, descriptions  |
| `caption`         | 11px   | 400     | 16px        | 0px            | Metadata, shortcuts hint |
| `mono`            | 13px   | 400     | 18px        | 0px            | Calculator output, code  |

### Rules
- **Never more than two font weights in view at the same time**
- Primary text is always `--text-primary`. Never custom color except for accent state
- Truncate with ellipsis (`…`) — never wrap result titles onto two lines
- Search input text: `body-lg`, 500 weight, `--text-primary`

---

## 4. Spacing & Layout (8pt Grid)

Everything is a multiple of 4px. Every layout decision snaps to this grid.

```
4px   — tight internal gaps (icon-to-label)
8px   — small gaps (between metadata items, category label below icon)
12px  — result item vertical padding
16px  — section padding, input padding
20px  — window edge insets
24px  — between result groups
```

### Window Geometry — Single Bar with Hover-Reveal Icons

The window has **one zone**: the search bar. It expands downward only when needed.
No persistent sidebar. No chrome. Just the bar.

```
— EMPTY STATE (default on open) ————————————————————————————
┌──────────────────────────────────────────────────────┐
│  🔍  Search apps, files, commands...                 │
└──────────────────────────────────────────────────────┘
Width: 640px  Height: 56px (bar only)

— ON HOVER (mouse moves while launcher is open) ————————
┌──────────────────────────────────────────────────────┐
│  🔍  Search apps, files, commands...    ⬤   ⬤   ⬤   │
└──────────────────────────────────────────────────────┘
Width: 640px  Height: 56px  (3 circles fade in, right side of bar)
                             📄      ⚙       📋

— CATEGORY SELECTED (bar expands downward) ——————————————
┌──────────────────────────────────────────────────────┐
│  🔍  Search apps, files, commands...  ←  ⬤   ⬤   ⬤  │
├──────────────────────────────────────────────────────┤
│  📄  Files                                           │
│  ─────────────────────────────────────────────────   │
│  📄  project-notes.md       C:/Projects/  2m ago     │
│  📁  nova-launcher/         C:/Dev/       1h ago     │
│  📄  design.md              C:/Dev/       today      │
└──────────────────────────────────────────────────────┘
Width: 640px  Max-height: 480px (bar 56px + results up to 424px)

— TYPING STATE (circles disappear, results drop down) ———
┌──────────────────────────────────────────────────────┐
│  🔍  figma_                                          │
│  [ 📄 Files  1 ]  [ ⚙ Commands  0 ]  [ 🌐 Web ]    │
├──────────────────────────────────────────────────────┤
│  ████  Figma                    ↵ Open               │
│  📄   figma-designs.fig         C:/Projects/         │
└──────────────────────────────────────────────────────┘
Width: 640px  Height: dynamic

Total window width:   640px (fixed, always)
Max window height:    480px
Border radius:        14px
Shadow:               0 20px 60px rgba(0,0,0,0.35), 0 2px 8px rgba(0,0,0,0.2)
```

### Search Input Bar
```
Height:          56px
Horizontal pad:  20px left, 16px right
Icon size:       18px (search icon, left-aligned)
Font:            body-lg / 500
```

### Floating Category Icons (hover state, right side of bar)

These live inside the 56px bar row. They do not add height.
Only 3 circles — one for each thing Windows does NOT already give you a fast path to.

> Apps are not a circle — Windows Start Menu handles app browsing.
> Calculator is not a circle — it auto-activates when you type math, no tap needed.

```
Icon circle size:    34px × 34px
Border-radius:       50% (perfect circle)
Background:          --bg-elevated (glass pill feel)
Icon inside:         18px, --text-secondary
Gap between circles: 8px
Right margin:        16px from bar edge
Animation in:        opacity 0→1, scale 0.85→1.0, 150ms spring, staggered 30ms each
Animation out:       opacity 1→0, scale 1.0→0.85, 100ms ease-out, all at once
```

Category circles (left to right):
```
①  📄  Files        — instant file search (Explorer is slow)
②  ⚙   Commands     — system actions with no fast Windows shortcut
③  📋  Clipboard    — replaces the basic Win+V experience
```

Active/selected circle state:
```
Background:    --accent-subtle
Icon color:    --accent
Scale:         1.05
Back arrow (←) overlays the active circle icon to signal "tap to go back"
```

### Results Panel (appears below bar when expanded)
```
Background:      --bg-surface
Border-top:      1px solid --border
Border-radius:   0 0 14px 14px  (only bottom corners rounded)
Overflow-y:      scroll (custom thin scrollbar: 3px, --text-tertiary)
```

### Result Item
```
Height:          44px (single line) / 52px (with subtitle)
Icon:            32px × 32px, 8px border-radius
Horizontal pad:  16px
Gap icon→text:   10px
Category tag:    caption text, --text-tertiary, right-aligned (only in mixed results)
```

### Dynamic Filter Pills (appear below search bar when typing)

When results span multiple categories, small filter pills appear just below
the search bar — above the results — so the user can narrow without losing
their query.

```
┌─────────────────────────────────────────────────────────────┐
│  🔍  figma_                                                 │
│  [ 🖥 Apps 2 ]  [ 📄 Files 1 ]  [ 🌐 Web ]                │
├─────────────────────────────────────────────────────────────┤
│  ...results...                                              │
└─────────────────────────────────────────────────────────────┘

Pill height:      26px
Pill padding:     0 10px
Border-radius:    13px (fully rounded)
Active pill:      --accent-subtle bg, --accent text
Inactive pill:    --bg-elevated bg, --text-secondary text
Font:             caption / 11px
Animation:        fade+slideY(4px) in when results appear, 120ms
```

---

## 5. Iconography

- All system icons use the **Lucide icon set** (consistent stroke weight: 1.5px)
- App icons are pulled natively from the Windows icon cache — shown as-is, no processing
- Category icons: 16×16, `--text-secondary` color
- Action icons (in result right side): 14×14, `--text-tertiary`, appear only on hover
- No filled icons and stroke icons mixed in the same view

---

## 6. Motion & Animation

Apple's secret: motion communicates state, not decoration.

### Spring Physics (the Apple feel)
All transitions use spring curves, not linear or ease-in-out.

```
Window open:     scale(0.96) → scale(1.0), opacity 0 → 1
                 duration: 180ms, spring: stiffness 300, damping 28

Window close:    scale(1.0) → scale(0.96), opacity 1 → 0
                 duration: 140ms, ease-out

Result list in:  translateY(4px) → translateY(0), opacity 0 → 1
                 duration: 120ms, staggered 16ms per item

Row hover:       background color, duration 80ms, ease-out
Row select:      background to accent-subtle, duration 60ms

Preview panel:   slideX(12px) → 0, opacity 0 → 1, duration 160ms
```

### Rules
- **No animation over 200ms** — if it takes longer than that, cut it
- Animations only play when meaningful — not on every keystroke
- Reduce-motion OS setting is fully respected: all animations disabled, just opacity fades at 80ms

---

## 7. Dark Mode & Theming

- Default: follows Windows system theme (light/dark) automatically
- User can pin to dark or light regardless of system
- Theme changes apply instantly — no restart
- One optional "Midnight" theme: deeper blacks, `#0D0D0D` base, `#1A1A1A` surface
- Custom accent color (8 presets: Blue, Purple, Pink, Red, Orange, Yellow, Green, Teal)

---

## 8. Design DON'Ts

| ❌ Never do this                                   | ✅ Do this instead                           |
|----------------------------------------------------|----------------------------------------------|
| Gradient backgrounds                               | Flat surface with Acrylic blur               |
| Drop shadows on text                               | Use weight and size for hierarchy            |
| Colored icon backgrounds for every result          | Monochrome icon, accent only for active      |
| Bouncy/elastic animations on every interaction     | Spring only on open/close                    |
| More than 2 type weights in one view               | Weight contrast: 500 vs 400                  |
| Showing more than 8 results by default             | 6–8 results, user scrolls for more           |
| Empty state with giant sad-face illustration       | Single line of muted text: "No results"      |
| Tooltips on hover for every element                | Keyboard hint in caption, bottom of window   |
