# motion.md — Motion & Animation

> Motion explains **state**, not personality. Spur animates like a system utility: fast in, softer out, never bouncy for fun.

---

## 1. Global rules

| Rule | Value |
|------|-------|
| **Open faster than close** | In 180–220ms · Out 120–160ms |
| **Hover faster than panel** | 100–150ms |
| **Stagger sparingly** | Max 3 items (anchors), 30ms delay |
| **No overshoot** on dismiss | Avoid spring on close |
| **GPU-friendly** | Animate `Opacity`, `ScaleTransform`, `TranslateTransform`, `GridLength`—not `Effect` |
| **Reduced motion** | Setting + system flag → opacity-only, duration × 0.5 |

### Easing curves

| Name | Use | WPF |
|------|-----|-----|
| `ease.out.standard` | Open, reveal | `CubicEase` EaseOut |
| `ease.in.standard` | Close, hide | `CubicEase` EaseIn |
| `ease.in.out.subtle` | Width reflow | `QuadraticEase` EaseInOut |

**No** `ElasticEase`, **no** bounce on production paths.

---

## 2. Launcher lifecycle

### 2.1 Show (`Idle` → `Present`)

| Property | From | To | Duration | Easing |
|----------|------|-----|----------|--------|
| Window opacity | 0 | 1 | 200ms | out |
| Scale (transform origin top-center) | 0.97 | 1 | 200ms | out |
| Caret | — | blink on | immediate after 50ms |

Sound (optional): system **Asterisk** at low volume—off by default in rebrand.

### 2.2 Hide (`Present` → `Idle`)

| Property | From | To | Duration |
|----------|------|-----|----------|
| Opacity | 1 | 0 | 140ms in |
| Scale | 1 | 0.98 | optional, same window |

Reset query per config **after** opacity hits 0.

### 2.3 Deactivate

Click outside → same as Hide (no separate animation).

---

## 3. Bar states

### 3.1 Anchor reveal (hover, empty query)

Triggered: pointer enters capsule, query empty.

| Element | Opacity | Scale | Delay | Duration |
|---------|---------|-------|-------|----------|
| Anchor 1 (Find) | 0→1 | 0.92→1 | 0ms | 150ms out |
| Anchor 2 (Do) | 0→1 | 0.92→1 | 30ms | 150ms out |
| Anchor 3 (Recall) | 0→1 | 0.92→1 | 60ms | 150ms out |
| Divider hairline | 0→1 | — | 0ms | 120ms out |
| Column width | 0→156px | — | — | 200ms out |

**Hide (pointer leave):** all anchors together, 100ms in, scale → 0.92.

**Typing starts:** cancel width animation if running; hide anchors in **60ms** (faster than mouse leave).

### 3.2 Shelf expand (anchor click or results)

| Property | Behavior |
|----------|----------|
| Panel height | `0 → auto` with 200ms ease out (max 424px) |
| First row | fade+translate Y: 4px→0, 120ms, delay 40ms |
| Following rows | stagger 20ms, max 8 visible rows animated |

### 3.3 Scope chips (typing)

| Action | Motion |
|--------|--------|
| Chip appears | opacity 0→1, translateY 4→0, 120ms |
| Chip removed | opacity 1→0, 80ms, no height jump (collapse after) |
| Switch active chip | cross-fade 80ms |

---

## 4. Results list

| Interaction | Motion |
|-------------|--------|
| Change selection (↑↓) | Rail snaps instantly; background cross-fade 60ms |
| New result set | Old list fade out 80ms → new fade in 100ms (avoid layout thrash) |
| Scroll | native smooth scroll, no custom physics |

---

## 5. Settings & onboarding

| Screen | Motion |
|--------|--------|
| Settings open | separate window, fade 150ms (or none) |
| Sidebar switch | content cross-fade 100ms |
| Onboarding step | horizontal slide 24px + opacity, 200ms out |
| Toggle | thumb translate 120ms ease out |

Settings **never** use the launcher spring scale.

---

## 6. Micro-interactions

| Control | Feedback |
|---------|----------|
| Anchor hover | background 80ms to elevated |
| Row hover | background 60ms |
| Button press | scale 0.98 for 80ms (Settings only) |
| Copy toast | slide from bottom 12px, 200ms, hold 1.2s, fade 120ms |

---

## 7. Performance budget

| Metric | Target |
|--------|--------|
| Open to focused caret | < 120ms perceived |
| Keystroke to first paint | < 16ms defer, results < 50ms local |
| Animation frame drops | 0 during open on mid-tier GPU |
| Concurrent animations | ≤ 4 timelines |

Cancel all storyboards on `Hide`.

---

## 8. Implementation notes (WPF)

```csharp
// Pseudocode — centralize in Spur.Motion class
SpurMotion.ShowLauncher(window);
SpurMotion.AnimateAnchors(visible: true, stagger: true);
SpurMotion.CrossfadeResults(oldPanel, newPanel);
```

- Prefer `BeginAnimation` with `FillBehavior.Stop` so values don’t stick.
- `GridLengthAnimation` for anchor column only—not window width.
- Re-center window on **height** change only (`SizeToContent=Height`).

---

## 9. Reduced motion matrix

| Full motion | Reduced |
|-------------|---------|
| Scale on open | Opacity only |
| Anchor stagger | Simultaneous fade |
| Shelf slide | Instant show |
| Chip translate | Opacity only |

---

*Flows: [ux.md](ux.md) · Visual tokens: [design.md](design.md)*

