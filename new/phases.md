# phases.md — Rebrand Implementation Plan

> **Rule:** Finish each phase’s gate before the next. Update `Themes/` and `Views/` to match `/new` specs—not the reverse.

**Branch suggestion:** `rebrand/Spur-v2`

---

## Overview

```
Phase R0  Brand tokens & motion primitives
Phase R1  Capsule shell (no halo, correct geometry)
Phase R2  Present states (Empty / Hover / Anchors)
Phase R3  Typing + results + selection rail
Phase R4  Shelf (Find / Do / Recall)
Phase R5  Scope chips + inline detection
Phase R6  Settings visual pass
Phase R7  Onboarding + tray + icons audit
Phase R8  Extras quarantine + cleanup
Phase R9  Polish gate (perf, a11y, reduced motion)
```

---

## Phase R0 — Tokens & motion

**Goal:** Single source of truth for color, type, motion in code.

### Tasks

1. Replace `Token.Accent` `#0A84FF` with brand tokens from [design.md §2](design.md#2-color-system)
2. Add `Spur.Motion` helper (Show/Hide/Anchors/Crossfade)
3. Bundle **Inter Variable**; map `UIFont` key
4. Document icon sampling script (optional `tools/sample-brand-colors.ps1`)

### Gate

```
□ Dark + light themes load without duplicate resource keys
□ Caret uses brand.accent, not #0A84FF
□ Spur.Motion.Show/Hide called from MainWindow only
```

---

## Phase R1 — Capsule shell

**Goal:** 640×56 matte bar, shadow plate, zero white fringe.

### Tasks

1. `MainWindow.xaml`: shadow plate + capsule per [design.md §6](design.md#6-shadow--edge-no-halo)
2. Remove `DropShadowEffect` from launcher tree
3. `AllowsTransparency=True`; **no** DWM on launcher
4. `SizeToContent=Height`; fixed width 640

### Gate

```
□ Screenshot: no light ring on dark wallpaper
□ Open/close uses motion.md timings
□ Click-outside dismisses
```

---

## Phase R2 — Empty & Hover

**Goal:** Search row + anchor reveal per [ux.md §3–4](ux.md#3-present--empty).

### Tasks

1. `SearchBar`: icon + placeholder from brand copy
2. `CategoryCircle` → rename to `AnchorButton` (optional) with motion.md stagger
3. Hover: column 0→156px, anchors stagger, labels on hover
4. Typing hides anchors < 60ms

### Gate

```
□ Empty: only search visible
□ Hover: three anchors, no window width change
□ Mouse leave hides anchors
```

---

## Phase R3 — Typing & results

**Goal:** Unified list + monochrome selection.

### Tasks

1. `ResultsList`: 2px rail, `bg.selected`, no blue wash
2. Ranking per [features.md §2.1](features.md#21-app-launch-typing)
3. Footer strip contextual shortcuts
4. Escape ladder per [ux.md §6](ux.md#6-present--typing)

### Gate

```
□ ↑↓ changes selection with rail visible
□ Enter launches and dismisses
□ Footer updates per selection type
```

---

## Phase R4 — Shelf

**Goal:** Anchor click opens shelf; back affordance.

### Tasks

1. Replace `BrowsePanel` styling with shelf layout (header + rows)
2. Wire Find / Do / Recall data sources
3. `←` on active anchor collapses shelf
4. Placeholder strings per anchor

### Gate

```
□ Each anchor opens correct shelf content
□ Escape steps back before dismiss
□ Max height 480px enforced
```

---

## Phase R5 — Chips & inline

**Goal:** Scope chips + calculator/url/web rows.

### Tasks

1. `ScopeBar` → chip style per design.md §5.3
2. Tab cycles chips
3. Inline handlers: calc, color, currency, url, web
4. Remove inline AI panel from bar (move to Extra)

### Gate

```
□ Chips appear only when 2+ domains match
□ Calculator row appears for `2+2` without mode switch
□ No purple/blue chip fills
```

---

## Phase R6 — Settings

**Goal:** Settings feels like a calm system app.

### Tasks

1. Sidebar IA per [features.md §6](features.md#6-settings-structure-maps-to-code)
2. Cards, toggles, spacing from design.md §5.6
3. Group action toggles → Actions + Extras
4. Opacity / theme / hotkey on General

### Gate

```
□ No launcher tokens leak broken colors into Settings
□ All Extras default off on fresh install
```

---

## Phase R7 — Onboarding & brand assets

**Goal:** First run matches brand; shell icons correct.

### Tasks

1. 3-step onboarding per ux.md §9
2. Verify `ApplicationIcon` + tray 16px
3. `Icons/sync-to-assets.ps1` in CI note
4. About page shows mark + version

### Gate

```
□ Fresh install → onboarding → Alt+Space works
□ Task Manager shows new exe icon after rebuild
□ Tray matches 16px ico
```

---

## Phase R8 — Extras quarantine

**Goal:** Remove vibe-coded surfaces from core path.

### Tasks

1. AI chat, timer UI → only via Extra keyword or Settings
2. Delete unused views: hub, side preview, duplicate ViewModels
3. Remove `CategoryCircle` dead code paths
4. Align `SpurConfig` defaults with features.md

### Gate

```
□ Empty/Hover tree has ≤ 1 child panel
□ dotnet build 0 warnings (target)
□ No references to #0A84FF in Views/
```

---

## Phase R9 — Polish gate

### Performance

```
□ Cold open < 200ms to caret on mid-tier PC
□ SearchAsync cancel on dismiss
```

### Accessibility

```
□ AutomationProperties on rows/anchors
□ Reduced motion shortens all durations
```

### Visual QA checklist (from brand.md §8)

```
□ Monochrome selection only
□ One shadow layer
□ 640×56 bar correct
□ Animations match motion.md
```

---

## Migration map (current → v2)

| Current file | Action |
|--------------|--------|
| `MainWindow.xaml` | R1–R4 |
| `MainWindow.xaml.cs` | Motion + states R2–R5 |
| `Views/SearchBar.*` | R2 |
| `Views/CategoryCircle.*` | R2 rename/style |
| `Views/ResultsList.xaml` | R3 |
| `Views/BrowsePanel.*` | R4 shelf |
| `Views/ScopeBar.*` | R5 chips |
| `Themes/DarkTheme.xaml` | R0 tokens |
| `Themes/DesignTokens/*` | R0 |
| `ViewModels/MainViewModel.cs` | R4–R5 ranking, hub off |
| `Extensions/*Action*` | R8 extras |

---

## Docs hygiene

When implementation diverges, **update `/new` first**, then code.

Delete or archive obsolete root docs (`UX.md`, `DESIGN.md`) after v2 ships.

---

*Start: [README.md](README.md) · Brand: [brand.md](brand.md)*

