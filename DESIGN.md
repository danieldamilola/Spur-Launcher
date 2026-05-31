# Arc Design System

## Product Identity

Arc is a precision productivity launcher for Windows — fast, minimal, and visually cohesive. It bridges the gap between native Windows utility and macOS-grade polish, combining Spotlight's translucency, Raycast's density, and Apple's attention to tactile feedback.

**Core Values:**
- **Speed:** Every interaction completes in <100ms perceived time
- **Clarity:** One primary action per view; no visual competition
- **Precision:** Mathematical spacing, consistent rhythm, predictable motion
- **Depth:** Layered glass surfaces create hierarchy without heavy chrome

---

## Color System

### Canvas & Surfaces

| Token | Hex | Usage |
|-------|-----|-------|
| `--void` | `#0A0A0B` | Deepest background, window canvas |
| `--depth-1` | `#111113` | Primary panels, search bar resting |
| `--depth-2` | `#18181B` | Elevated surfaces, hover states |
| `--depth-3` | `#1F1F23` | Active selections, input fields |
| `--depth-4` | `#27272A` | Borders, dividers, subtle boundaries |

**Principle:** The surface stack stays in the neutral void. Never use warm or cool tints on container backgrounds — only on accent applications.

### Accent: Mineral Collection

Premium minimalism favors restraint over vibrancy. The Mineral Collection uses desaturated, warm-cool neutrals that feel expensive without demanding attention. Each context receives a subtle tonal shift — more like variations of shadow than splashes of color.

| Context | Gradient | Hex Values |
|---------|----------|------------|
| **Primary (Default)** | Warm Grey → Soft Pearl | `#9CA3AF` → `#D1D5DB` |
| **Calculations/Math** | Dusty Sage → Pale Mint | `#84A98C` → `#B7C9BC` |
| **Time/Scheduler** | Muted Rose → Blush | `#C4A4A8` → `#DBC4C7` |
| **Network/System** | Slate Blue → Mist | `#7B8FA8` → `#A9B8C9` |
| **AI/Chat** | Warm Sand → Cream | `#C9B99A` → `#E3DCCF` |
| **Files/Documents** | Cool Pewter → Silver | `#8B939C` → `#B8BFC7` |
| **Clipboard** | Soft Wheat → Ivory | `#D4C4A8` → `#EBE4D6` |

**Gradient Application:**
- Use 180° angle (top-to-bottom) for subtle depth, not showy diagonals
- 0.6 opacity at core, fading to 0 at 150% spread — restrained glow
- Text on gradients: `--text-inverse` (#18181B) for soft contrasts, never pure white
- No drop-shadows on gradient text — let the muted color speak quietly

**Principle:** The accents are barely there. A user should feel the difference between calculator (sage) and timer (rose) without being able to name the colors immediately. Premium is felt, not seen.

### Semantic Text Colors

| Token | Hex | Usage |
|-------|-----|-------|
| `--text-primary` | `#FAFAFA` | Primary labels, search input |
| `--text-secondary` | `#A1A1AA` | Subtitles, metadata, hints |
| `--text-tertiary` | `#71717A` | Disabled, placeholder, section labels |
| `--text-muted` | `#52525B` | Structural elements, inactive |
| `--text-inverse` | `#18181B` | Text on accent backgrounds |

### Functional Colors

Minimal states use desaturated tones — not alerts, but whispers.

| Token | Hex | Usage |
|-------|-----|-------|
| `--success` | `#6B8E6B` | Success states, muted sage |
| `--warning` | `#A8947A` | Warnings, warm taupe |
| `--error` | `#A67C7C` | Errors, dusty rose |
| `--info` | `#7A8BA0` | Informational, slate grey |

### Glass & Blur

Less is more. Subtle blur creates depth without announcing itself.

| Token | Value | Usage |
|-------|-------|-------|
| `--backdrop` | `blur(16px) saturate(120%)` | Main window backdrop, restrained |
| `--glass-border` | `rgba(255,255,255,0.06)` | Barely-there edge definition |
| `--glass-highlight` | `rgba(255,255,255,0.03)` | Sheen almost imperceptible |
| `--glass-shadow` | `0 4px 24px rgba(0,0,0,0.35)` | Elevation without drama |

---

## Typography

### Font Families

**Primary:** `Inter` (weights 400, 500, 600)
- UI labels, search results, body text
- Feature settings: `ss03` for alternate 'a' (distinguishes from default)

**Monospace:** `JetBrains Mono` or `Geist Mono` (weights 400, 500)
- Code snippets, file paths, keyboard shortcuts
- Version strings, timer displays

**Display:** `Inter` with tight tracking
- Large calculator results, empty states

### Type Scale

| Level | Size | Weight | Tracking | Line | Usage |
|-------|------|--------|----------|------|-------|
| **Hero** | 48px | 600 | -0.04em | 1.1 | Reserved for essential displays only |
| **Title** | 24px | 600 | -0.03em | 1.2 | Panel headers, AI chat header |
| **Heading** | 16px | 600 | -0.01em | 1.3 | Section labels (APPS, FILES) |
| **Body** | 13px | 500 | 0 | 1.4 | Primary row labels |
| **Caption** | 12px | 500 | +0.01em | 1.4 | Subtitles, metadata |
| **Micro** | 11px | 500 | +0.03em | 1.3 | Keyboard hints, badges |
| **Nano** | 10px | 400 | +0.04em | 1.2 | Section labels, timestamps |

**Principles:**
- Restrained hierarchy: 48px reserved for only the most essential displays
- Negative tracking on headlines creates quiet density, not loud presence
- Small metadata uses minimal positive tracking — barely noticeable
- 11px minimum for all readable content — 10px only for structural labels

---

## Spacing System

### Base Grid: 4px

| Token | Value | Usage |
|-------|-------|-------|
| `--space-1` | 4px | Tight internal padding |
| `--space-2` | 8px | Icon containers, compact gaps |
| `--space-3` | 12px | Row padding, list spacing |
| `--space-4` | 16px | Section gaps, panel padding |
| `--space-5` | 20px | Card padding, major sections |
| `--space-6` | 24px | Window padding, comfortable gaps |
| `--space-8` | 32px | Large separations |
| `--space-10` | 40px | Hero spacing, onboarding |

### Layout Dimensions

| Element | Width | Height | Notes |
|---------|-------|--------|-------|
| Main window | 680px | auto (max 600px) | Centered, max-height constraint |
| Search bar | 100% | 56px | Pill shape, floating appearance |
| Category icons | 44px | 44px | Floating glass circles |
| Result row | 100% | 44px | Compact, 12px vertical padding |
| Preview panel | 300px | auto | Side panel or full-width |
| Settings window | 640px | 520px | Smaller, tighter, centered |

### Border Radius Scale

| Token | Value | Usage |
|-------|-------|-------|
| `--radius-sm` | 6px | Small buttons, badges |
| `--radius-md` | 8px | Rows, input fields |
| `--radius-lg` | 12px | Cards, panels |
| `--radius-xl` | 16px | Main window, large containers |
| `--radius-full` | 9999px | Pills, circles, search bar |

**Principle:** Never exceed 20px on cards — precision instrument aesthetic requires controlled, deliberate corners.

---

## Components

### Search Bar (The Anchor)

**Structure:**
- 56px height, `--radius-full` pill
- `--depth-1` background, `--glass-border` 1px border
- Glass highlight: inset 0 1px 0 `--glass-highlight`

**States:**
- **Idle:** `--depth-1` fill, minimal shadow
- **Active:** `--depth-2` fill, 1px `--accent-primary` border — no glow, no ring
- **Typing:** Cursor blink, placeholder fades immediately

**Contents:**
- Left: 16px padding, 20px magnifying glass icon (`--text-tertiary`)
- Center: Input text (`--text-primary`, 16px, weight 500)
- Right: 16px padding, clear button (appears on input, 16px ×)

### Result Row

**Structure:**
- 44px height, `--radius-md` corners
- 12px horizontal padding, 8px gap between elements

**Layout:**
```
[icon:32px] [8px gap] [name + subtitle stack]
```

**States:**
- **Default:** Transparent background
- **Hover:** `--depth-2` background
- **Selected:** `--depth-3` background only — no borders, no hints. Selection is its own signal

**Elements:**
- **Icon container:** 32px × 32px, `--radius-sm`, icon color matches section accent
- **Name:** 13px, `--text-primary`, weight 500
- **Subtitle:** 11px, `--text-secondary`, weight 400

### Category Icons (Floating)

**Structure:**
- 44px × 44px circles
- `--depth-2` background with glass effect
- Icons: 20px stroke width 1.5

**States:**
- **Default:** `--text-secondary` icon, minimal shadow
- **Hover:** `--depth-3` background, `--text-primary` icon
- **Active:** `--depth-4` background, contextual accent icon color, no scale change

**Motion:**
- Enter: Fade in + translate 8px (200ms, ease-smooth)
- Exit: Fade out (150ms, ease-in)
- Stagger: 30ms between each icon — barely noticeable sequencing

### Preview Panels

**Shared Structure:**
- 300px width, `--radius-lg` left corners when side-docked
- `--depth-1` background, glass border left (1px)
- 20px padding internal

**Calculator Panel:**
- Expression: 13px, `--text-secondary`, top — barely there
- Result: 36px, `--text-primary`, weight 500 — clear but not loud
- Copy hint: 11px, `--text-tertiary`, bottom with subtle ↵ icon

**Color Panel:**
- Swatch: 80px × 80px, `--radius-md`, shadow
- Hex: 16px, `--text-primary`, weight 500, copyable
- RGB/HSL: 12px, `--text-secondary`, stacked

**Timer Panel:**
- Display: 36px, tabular nums, `--text-primary` — prominent but not oversized
- Progress bar: 2px height, `--radius-full`, accent fill — thin line of intent
- Status: 12px, `--text-secondary`
- Cancel: text button, `--text-secondary` on hover — never aggressive red

**AI Chat Panel:**
- Header: 44px height, subtle divider below (`--depth-4` 1px), no background
- Messages: flat text rows, no bubbles, no backgrounds
  - User: right-aligned, `--accent-ai` text color, 12px vertical padding
  - AI: left-aligned, `--text-primary`, 12px vertical padding
- Input bar: 40px height, pill, `--depth-2` background, single shadow only

---

## Settings Window — Minimal Redesign

**Problem:** 38 settings across 9 sections is too fragmented. Mental overhead is high.

**Solution:** Consolidate to 4 sections — General, Search, Actions, Advanced. Remove decorative chrome.

### Window Structure

| Property | Value |
|----------|-------|
| Size | 640px × 480px (smaller, tighter) |
| Background | `--void` solid — no glass distraction |
| Layout | [Sidebar: 140px] [Content: 1px divider + flex] |

### Sidebar Navigation

Only 4 items. No icons, no uppercase shouting, no section headers.

```
General
Search
Actions
Advanced
```

**Visual:**
- 13px Inter, `--text-secondary`
- 32px height rows, `--radius-md`
- Active: `--depth-3` pill background, `--text-primary`
- Spacing: 4px gap between items

---

## Settings Sections

### General (13 settings)

Appearance, behavior, and system integration.

| Setting | Control | Default | Bind |
|---------|---------|---------|------|
| **Theme** | Radio pills: Dark / Light / System | System | `ThemeMode` |
| **Glass effect** | Toggle | On | `EnableBlur` |
| **Window width** | Slider 500–900px | 680px | `LauncherWidth` |
| **Compact mode** | Toggle | Off | `CompactMode` |
| **Launch at sign in** | Toggle | On | `LaunchOnStartup` |
| **Show tray icon** | Toggle | On | `ShowTrayIcon` |
| **Global shortcut** | Key capture field | Alt+Space | `GlobalShortcut` |
| **Close after launch** | Toggle | On | `CloseAfterLaunch` |
| **Show recent first** | Toggle | On | `ShowRecentFirst` |
| **Clear clipboard on exit** | Toggle | Off | `ClearClipboardOnExit` |
| **Clear all history** | Text button (destructive) | — | `ClearHistoryCommand` |
| **Reset to defaults** | Text button (secondary) | — | `ResetDefaultsCommand` |

**Layout:** Single column, 24px vertical spacing between groups. No cards, no borders between settings.

---

### Search (7 settings)

What gets indexed and where to look.

| Setting | Control | Default | Bind |
|---------|---------|---------|------|
| **Applications** | Toggle | On | `IndexApps` |
| **Files** | Toggle | On | `IndexFiles` |
| **Folders** | Toggle | On | `IndexFolders` |
| **Clipboard history** | Toggle | On | `IndexClipboard` |
| **Indexed folders** | List + Add/Remove | User folders | `IndexedFolders` |
| **Background indexing** | Toggle | On | `BackgroundIndexing` |
| **Auto-refresh interval** | Radio: 3h / 6h / 12h / Manual | 6h | `ReIndexInterval` |

**Notes:** 
- "Indexed folders" shows as subtle list below with × remove buttons
- "Manual" selection reveals a "Refresh now" text button

---

### Actions (11 toggles)

Enable/disable instant actions. Simple grid of toggles.

| Setting | Bind |
|---------|------|
| Calculator | `ActionCalc` |
| Currency converter | `ActionCurrency` |
| Timer | `ActionTimer` |
| Color picker | `ActionColor` |
| IP address | `ActionIp` |
| AI assistant | `ActionAi` |
| Password generator | `ActionPasswordGen` |
| Quick note | `ActionQuickNote` |
| Kill process | `ActionKillProcess` |
| Screenshot | `ActionScreenshot` |
| System commands | `ActionSystem` |

**Layout:** 2-column grid, 12px gaps. Each row:
```
[Toggle: 36×20px] [12px gap] [Name: 13px]
```

Default: All enabled.

---

### Advanced (4 settings)

Power user controls and app info.

| Setting | Control | Bind |
|---------|---------|------|
| **AI Provider** | ComboBox: groq / gemini / openrouter / deepseek | `AiProvider` |
| **Model** | ComboBox (dependent) | `AiModel` |
| **API key** | PasswordBox | `ApiKey` |
| **Version** | Static text | `Version` |

**Notes:**
- Provider selection populates Model dropdown
- API key shows •••••••• with reveal toggle
- Version sits at bottom with subtle `--text-muted` styling

---

### Settings Controls

All controls follow minimal principles:

**Toggle:**
- 36px × 20px pill
- `--depth-4` background off
- `--accent-primary` solid on (no gradient, no animation)
- Knob: 16px circle, white, 2px padding from edge

**Radio Pills:**
- Segmented container: `--depth-2` background, `--radius-md`
- Each pill: 28px height, 12px horizontal padding
- Selected: `--depth-3` background, `--text-primary`
- Unselected: transparent, `--text-secondary`

**Slider:**
- Track: 4px height, `--depth-3`, `--radius-full`
- Fill: `--accent-primary` (left of thumb)
- Thumb: 16px circle, `--depth-1`, 1px `--depth-4` border

**Text Input:**
- 32px height, `--depth-2` background, `--radius-md`
- 10px horizontal padding
- Focus: 1px `--accent-primary` border

**Buttons:**
- **Primary:** `--depth-3` background, `--text-primary`, no border
- **Destructive:** `--error` text color only, transparent background
- **Secondary:** `--text-secondary`, transparent, underline on hover

---

## Motion & Animation

### Timing Principles

| Duration | Usage |
|----------|-------|
| 100ms | Micro-feedback (button presses, icon state) |
| 200ms | Standard transitions (hover, selection) |
| 300ms | Panel transitions, mode switches |
| 400ms | Window open/close, major state changes |

### Easing Curves

Motion should be felt, not watched. No bounces, no playfulness.

| Name | Curve | Usage |
|------|-------|-------|
| `ease-out` | `cubic-bezier(0, 0, 0.2, 1)` | Elements entering, opening |
| `ease-in` | `cubic-bezier(0.4, 0, 1, 1)` | Elements leaving, closing |
| `ease-in-out` | `cubic-bezier(0.4, 0, 0.2, 1)` | Reversible actions, hovers |
| `ease-smooth` | `cubic-bezier(0.25, 0.1, 0.25, 1)` | Subtle transitions, barely perceptible |

### Choreography

**Window Open:**
1. Backdrop fades in (0 → 1, 150ms, ease-smooth)
2. Window fades in only — no scale, no bounce (200ms)
3. Search bar cursor appears, no ring, no pulse

**Search Results Appear:**
1. Section labels fade in together (no stagger, 150ms)
2. Rows fade in only (150ms, 20ms stagger — barely perceptible)
3. First row selected with no hint — selection itself is the signal

**Category Hover:**
1. Icons fade in (150ms, ease-smooth, 30ms stagger)
2. No blur change — glass stays constant

**Preview Panel Open:**
1. Panel fades in only (200ms, ease-smooth)
2. Results list width adjusts without animation
3. Panel content fades in (100ms delay)

**Keyboard Navigation:**
- Selection moves: instant background change only — no scale, no pulse
- No animation delay for rapid key presses

---

## Layout Patterns

### Hub State (Idle)

```
┌─────────────────────────────────────────┐
│  [Search Bar: 56px pill, centered]      │
│                                         │
│         [Recent apps row]               │
│         (or empty state text)           │
│                                         │
│  [Settings link: footer]                │
└─────────────────────────────────────────┘
```

### Search Results

```
┌─────────────────────────────────────────┐
│  [Search Bar: with typed query]         │
├─────────────────────────────────────────┤
│  APPS                                   │
│  [▢ Chrome      ] ← selected            │
│  [▢ VS Code     ]                       │
│  FILES                                  │
│  [▢ report.pdf  ]                       │
│  ACTIONS                                │
│  [▢ Calculate   ]                       │
└─────────────────────────────────────────┘
```

### Results + Preview Split

```
┌───────────────────────────────────────────────────┐
│  [Search Bar]                                     │
├─────────────────────────┬─────────────────────────┤
│  APPS                   │                         │
│  [▢ Calculator  ] ← sel │   [Preview Panel]       │
│  [▢ Calendar    ]       │   ┌─────────────────┐   │
│  [▢ Clock       ]       │   │ 42 + 58         │   │
│                         │   │ ─────────────   │   │
│                         │   │ = 100           │   │
│                         │   │ [↵ to copy]     │   │
│                         │   └─────────────────┘   │
└─────────────────────────┴─────────────────────────┘
```

### Settings Window

```
┌───────────────────────────────────────────────────┐
│  Settings                              [×]          │
├──────────────────┬──────────────────────────────────┤
│                  │                                  │
│  General         │  Theme              [Dark ▼]     │
│  Search          │  Glass effect       [●──]      │
│  Actions         │  Window width       [====●===]   │
│  Advanced        │                                  │
│                  │  Launch at sign in  [●──]        │
│                  │  Tray icon          [●──]        │
│                  │                                  │
│                  │  Global shortcut    [Alt+Space]│
│                  │                                  │
│                  │  ─────────────────────────────   │
│                  │                                  │
│                  │  Close after launch [●──]        │
│                  │  Show recent first  [●──]        │
│                  │                                  │
└──────────────────┴──────────────────────────────────┘
```

---

## Accessibility

### Contrast Requirements

- `--text-primary` on `--depth-1`: 15.3:1 ✓
- `--text-secondary` on `--depth-1`: 7.8:1 ✓
- Accent gradients maintain 4.5:1 minimum when used for text

### Motion Preferences

- Respect `prefers-reduced-motion`
- Instant state changes when reduced motion is preferred
- Disable backdrop blur for `prefers-reduced-transparency`

### Focus Indicators

- All interactive elements have 2px accent outline on focus
- Focus ring offset: 2px from element edge
- Search bar: inner glow + outline for dual visibility

---

## Do & Don't

### Do

- Use `--void` (#0A0A0B) as the absolute canvas — never pure black
- Apply glass borders (1px, 6% white) for subtle edge definition
- Use negative tracking on large display text for quiet density
- Apply the mineral accent system contextually — subtle tonal shifts only
- Keep border radius ≤20px for precision aesthetic
- Use `cubic-bezier(0, 0, 0.2, 1)` for entering elements
- Maintain 44px minimum touch targets
- Use tabular nums for timer displays

### Don't

- Don't use colored backgrounds for section containers — stay in the neutral depth stack
- Don't exceed 20px border radius on cards
- Don't use generic #007AFF blue as the only accent
- Don't animate layout properties (width/height) — use transforms
- Don't show section labels when no results exist in that category
- Never use gradients for large area backgrounds — reserve only for subtle active state indicators
- Don't go below 10px font size for any readable content
- Don't add icons to settings sidebar — text-only navigation
- Don't use uppercase section headers — they're loud
- Don't group settings into cards with borders — space alone creates hierarchy

---

## Implementation Notes

### CSS Variables

```css
:root {
  /* Depth */
  --void: #0A0A0B;
  --depth-1: #111113;
  --depth-2: #18181B;
  --depth-3: #1F1F23;
  --depth-4: #27272A;
  
  /* Accents - Mineral Collection */
  --accent-primary: #9CA3AF;
  --accent-primary-glow: rgba(156, 163, 175, 0.25);
  --accent-math: #84A98C;
  --accent-math-glow: rgba(132, 169, 140, 0.25);
  --accent-time: #C4A4A8;
  --accent-time-glow: rgba(196, 164, 168, 0.25);
  --accent-network: #7B8FA8;
  --accent-network-glow: rgba(123, 143, 168, 0.25);
  --accent-ai: #C9B99A;
  --accent-ai-glow: rgba(201, 185, 154, 0.25);
  --accent-files: #8B939C;
  --accent-files-glow: rgba(139, 147, 156, 0.25);
  --accent-clipboard: #D4C4A8;
  --accent-clipboard-glow: rgba(212, 196, 168, 0.25);
  
  /* Text */
  --text-primary: #FAFAFA;
  --text-secondary: #A1A1AA;
  --text-tertiary: #71717A;
  --text-muted: #52525B;
  --text-inverse: #18181B;
  
  /* Functional - Muted States */
  --success: #6B8E6B;
  --warning: #A8947A;
  --error: #A67C7C;
  --info: #7A8BA0;
  
  /* Glass - Subtle & Restrained */
  --glass-border: rgba(255, 255, 255, 0.06);
  --glass-highlight: rgba(255, 255, 255, 0.03);
  --glass-shadow: 0 4px 24px rgba(0, 0, 0, 0.35);
  --backdrop: blur(16px) saturate(120%);
  
  /* Motion - Quiet & Smooth */
  --ease-out: cubic-bezier(0, 0, 0.2, 1);
  --ease-in: cubic-bezier(0.4, 0, 1, 1);
  --ease-in-out: cubic-bezier(0.4, 0, 0.2, 1);
  --ease-smooth: cubic-bezier(0.25, 0.1, 0.25, 1);
}
```

### Glass Effect Recipe

```css
.glass-panel {
  background: var(--depth-1);
  border: 1px solid var(--glass-border);
  box-shadow: var(--glass-shadow);
  backdrop-filter: var(--backdrop);
}
```

**Principle:** The glass effect should be felt, not seen. If someone notices the blur, it's too much.

---

## Settings Migration Summary

| Old | New |
|-----|-----|
| 9 sections | 4 sections |
| 38 settings | 35 settings (consolidated) |
| 720×520px window | 640×480px window |
| 180px sidebar | 140px sidebar |
| Icons + uppercase headers | Text-only, sentence case |
| Cards with borders | Spacing-only hierarchy |
| Gradient toggles | Solid color, no animation |

**Consolidations:**
- Appearance + Startup + Result Behavior + Privacy → **General**
- Search folders + indexing options → **Search**
- 11 individual action toggles → **Actions** grid
- AI settings → **Advanced** (power users only)

---

## Screens Checklist

| Screen | Visual Status | To Apply |
|--------|--------------|----------|
| Hub (Idle) | Needs refresh | Glass pill bar, floating category icons |
| Search Results | Needs consolidation | Unified row design, contextual accent hints |
| Browse Panels | Needs cohesion | Consistent chip design, unified list spacing |
| Calculator Preview | Needs color integration | Sage accent, muted result display |
| Color Preview | Needs polish | Swatch shadow, value display |
| Timer Preview | Needs refinement | Tabular nums, progress styling |
| AI Chat | Needs glass treatment | Message bubbles, input bar styling |
| **Settings** | **Needs full redesign** | **4-section minimal structure, 640×480px** |
| Onboarding | Needs completion | Slide transitions, button styling |

---

*Design system version: 1.1*
*Last updated: May 2026*
