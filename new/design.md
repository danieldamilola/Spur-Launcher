# design.md — Visual System (Spur v2)

> **Philosophy:** Matte, monochrome, precise. Depth comes from value steps—not blur stacks, not accent floods.

Aligned with [brand.md](brand.md). Implements on Windows with **WPF** + optional **DWM** materials where they don’t fight transparency.

---

## 1. Principles (HIG-adapted for Spur)

| Principle | Spur interpretation |
|-----------|-------------------|
| **Clarity** | One primary focal point per state (caret, then selection rail) |
| **Deference** | Chrome is 56px until content earns height |
| **Depth** | 3 surface levels + one shadow; never more |
| **Consistency** | Same row geometry in every list (apps, files, clipboard) |

---

## 2. Color system

### 2.1 Dark theme (default)

| Token | Value | Usage |
|-------|-------|-------|
| `bg.void` | `#0C0C0E` | Desktop bleed outside capsule (rarely seen) |
| `bg.base` | `#121214` | Settings window, onboarding |
| `bg.surface` | `#1A1A1D` | Capsule fill |
| `bg.elevated` | `#222226` | Hover row, anchor hover |
| `bg.selected` | `#2A2A2E` | Keyboard selection (subtle lift) |
| `border.subtle` | `#FFFFFF` @ 6% | Dividers, capsule edge (optional 1px) |
| `border.strong` | `#FFFFFF` @ 10% | Settings inputs only |
| `text.primary` | `#F4F2EF` | Titles (warm white) |
| `text.secondary` | `#9B9893` | Metadata |
| `text.tertiary` | `#5C5A57` | Placeholder |
| `brand.accent` | `#C9C4BC` | Caret, 2px selection rail, focus |
| `brand.highlight` | `#E8E4DF` | Active anchor stroke |
| `semantic.danger` | `#C45C5C` | Destructive only |
| `semantic.success` | `#6B8F71` | Rare confirmations |

### 2.2 Light theme

| Token | Value |
|-------|-------|
| `bg.base` | `#F2F2F4` |
| `bg.surface` | `#FFFFFF` |
| `bg.elevated` | `#F7F7F8` |
| `bg.selected` | `#EEEEF0` |
| `text.primary` | `#1A1A1D` |
| `text.secondary` | `#6E6E73` |
| `text.tertiary` | `#AEAEB2` |
| `brand.accent` | `#3A3A3D` |

### 2.3 Accent usage budget

In the **launcher bar**, accent may appear only on:

1. Text caret (1px)
2. Selection rail (2px left border)
3. Active anchor ring (1px stroke, not fill)

**Never** on: row backgrounds, icons by default, footer hints, scope chips (chips use elevated fill, not accent fill).

### 2.4 Materials (Windows)

| Surface | Treatment |
|---------|-----------|
| **Launcher capsule** | **Solid** `bg.surface` + ambient shadow plate (see §6)—no `DropShadowEffect` on rounded Border |
| **Settings** | Solid `bg.base`; optional Mica on Win11 **only** when window is opaque (`AllowsTransparency=False`) |
| **Onboarding** | Solid card on dim scrim `#000000` @ 40% |

> Lesson learned: `AllowsTransparency=True` + DWM Mica + `DropShadowEffect` = white halo. Pick **one** depth strategy.

---

## 3. Typography

### 3.1 Families

| Role | Stack | Notes |
|------|-------|-------|
| UI | **Inter Variable** → `Segoe UI Variable` → `Segoe UI` | Bundle Inter (400, 500, 600) |
| Mono | **JetBrains Mono** → `Cascadia Mono` | Calculator, paths, IP |

Retire DM Sans in launcher UI when Inter ships (Settings can migrate last).

### 3.2 Scale (4px grid, optical sizes)

| Token | Size | Weight | Tracking | Use |
|-------|------|--------|----------|-----|
| `type.search` | 17px | 400 | -0.2px | Input + placeholder |
| `type.result` | 15px | 500 | -0.2px | Primary result line |
| `type.meta` | 12px | 400 | 0 | Path, size, time |
| `type.caption` | 11px | 400 | 0 | Footer shortcuts |
| `type.label` | 11px | 500 | 0.2px | Anchor hover labels (uppercase optional: `FILES`) |

**Rules**

- Max **two weights** visible at once.
- Titles **single line**, ellipsis end.
- Tabular nums for sizes (`font-feature-settings: tnum` equivalent in WPF).

---

## 4. Spacing & layout

### 4.1 Grid

Base unit **4px**. All padding/margins ∈ {4, 8, 12, 16, 20, 24}.

### 4.2 Launcher geometry

| Constant | Value |
|----------|-------|
| Bar width | **640px** fixed |
| Bar height | **56px** |
| Corner radius | **14px** |
| Max panel height | **480px** (bar + list) |
| Horizontal inset | **16px** |
| Row height | **44px** (compact), **52px** (with subtitle) |
| Icon box | **20×20** stroke icons |
| Anchor size | **40×40** hit target, **36×36** visual circle |
| Selection rail | **2px** left, inset 4px from row edge |

### 4.3 Structure

```
┌─ Shadow plate (offset, no Effect) ─────────────────────┐
│ ┌─ Capsule (surface, r14) ─────────────────────────────┐ │
│ │ [icon] [ search ........................ [anchors] ] │ │
│ │ ─ optional: scope chips (typing) ─────────────────── │ │
│ │ ─ optional: results / shelf ────────────────────────── │ │
│ │ ─ optional: footer hints (selection) ───────────────── │ │
│ └──────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────┘
```

---

## 5. Components

### 5.1 Search field

- No visible border box; caret = `brand.accent`
- Leading **search** icon: `text.tertiary`, 20px, 12px gap
- Placeholder: `Search apps, files, actions…`

### 5.2 Anchors (hover)

- Default: **hidden** (opacity 0, scale 0.92)
- Reveal: stagger 30ms × index, 150ms spring out
- Hover: `bg.elevated` circle, label below (`type.caption`, `text.secondary`)
- Active: `brand.highlight` stroke 2px, fill unchanged

### 5.3 Scope chips (typing)

Appear **below** search row when multiple domains match.

```
[ Files 2 ]  [ Web ]  [ Actions 1 ]
```

- Height 28px, radius 8px, padding 8×12
- Idle: `bg.elevated`, `text.secondary`
- Active filter: `bg.selected`, `text.primary`, no accent fill
- Count: `text.tertiary` in parentheses

### 5.4 Result row

```
│█│ [icon]  Title                    meta right │
│ │         subtitle optional                    │
```

- Selected: `bg.selected` + 2px rail `brand.accent`
- Hover (mouse): `bg.elevated` only if not selected
- No full-row blue wash

### 5.5 Footer strip

- Height 36px, top hairline `border.subtle`
- Hints: `↵ Open` · `Ctrl+C Copy` — `text.tertiary`, symbols from Segoe UI

### 5.6 Settings (separate window)

- **Sidebar** 220px: icon + label rows, Win11 Settings density
- Content: cards with `bg.surface`, 12px radius, 16px padding
- Toggles: custom pill (not default CheckBox)

### 5.7 Onboarding (3 cards max)

1. **Welcome** — mark + one sentence + hotkey
2. **Privacy** — what’s indexed locally
3. **Ready** — `Alt+Space` try it

---

## 6. Shadow & edge (no halo)

**Do not** use WPF `DropShadowEffect` on the transparent launcher.

**Do** use a sibling `Border`:

| Property | Value |
|----------|-------|
| Offset | `0, 8px` |
| Fill | `#000000` @ 40% |
| Blur | simulated with larger radius plate + low opacity, or Win32 `DwmSetWindowAttribute` shadow on opaque host |
| Capsule edge | optional 1px `border.subtle` **inside** clip |

---

## 7. Iconography

| Context | Style |
|---------|-------|
| UI chrome | Lucide-compatible strokes, 1.5px, round caps |
| Results | OS app icons 16/20px, file type icons |
| Anchors | Custom 3 glyphs matching brand stroke weight |

**Don’t** mix filled Material icons with stroke anchors.

---

## 8. WPF token map (implementation)

Map to `Themes/DesignTokens/`:

```
Spur.Color.Bg.Surface      → bg.surface
Spur.Color.Text.Primary    → text.primary
Spur.Color.Brand.Accent    → brand.accent
Spur.Radius.Window         → 14
Spur.Space.Inset           → 16
Spur.Motion.Duration.Fast  → 120ms (see motion.md)
```

Theme files (`DarkTheme.xaml`, `LightTheme.xaml`) should **only** assign DynamicResource keys—no raw hex in Views.

---

## 9. Accessibility

| Requirement | Approach |
|-------------|----------|
| Contrast | Primary on surface ≥ 7:1 |
| Focus | Visible rail + focus rect on anchors in keyboard mode |
| Motion | Respect `SystemParameters.ClientAreaAnimation` + Spur setting |
| Screen reader | AutomationProperties.Name on rows, anchors |

---

*Motion timing: [motion.md](motion.md) · Flows: [ux.md](ux.md)*

