# Spur — Rebrand Proposal V2
## "Clarity"

**Date**: 2026-06-10  
**Approach**: Complete visual identity redesign  
**Premise**: Distinctive *and* beautiful. Not a choice between the two.

---

## Why V1 Was Wrong

The hardware/industrial concept was wrong for one reason: **it confused "distinctive" with "austere"**. A launcher you open 40 times a day has to feel genuinely good to use — that's a baseline requirement, not a bonus. Industrial zero-radius gunmetal describes what a spur *is made of*, not what using one *feels like*. The feeling is the brief.

The feeling of being spurred into action is:

> **The instant of crystalline clarity before movement. Everything else goes quiet. You know exactly what you want. Your hand is already moving.**

That's what the design should feel like. Not a workshop. A moment of perfect focus.

---

## Design Brief (Revised)

**Subject**: Spur — a launcher that collapses the gap between thought and action  
**Audience**: Power users who care about both speed and craft  
**Job**: Be so fast and pleasant that it feels like thinking, not typing  
**The feeling to capture**: Clarity. The moment before. Light through glass.

---

## Core Concept: "Light Through Glass"

The window is a **lens** — not a tool. Not a widget. A surface that focuses your attention and makes everything outside it irrelevant the moment it appears. Results *materialize* through it. The accent color isn't applied — it *refracts*.

Every other launcher competes to be the most minimal. Spur competes to be the most **present** — the one experience on your screen that actually feels like it was made with intention.

**The Aesthetic Risk**: An iridescent, shifting accent — one color that moves through a narrow range of violet → ice → teal depending on context. Not a static hex value. A spectrum. This is unusual in desktop software and takes real execution to not look cheap. When it lands, nothing else on Windows looks like it.

---

## Color Palette — "Northern Light"

Six values. Cool-leaning dark base with one luminous moving accent.

```
Abyss:         #0C0E14   Deep cool navy-black. Not warm, not pure.
Surface:       #13151E   Primary surface. Blue-black undertone.
Lifted:        #1C1F2B   Raised panels, input backgrounds.
Edge:          #2A2E3D   Borders, separators, dividers.
Mist:          #404660   Inactive states, faint structure.

// Text
Snow:          #ECF0F7   Primary text. Cool white, slight blue cast.
Fog:           #8E94A8   Secondary text.
Dust:          #525870   Tertiary, placeholders.

// The Accent (The Risk)
Iris Resting:  #9B8FD6   Soft violet. Default selection, cursor.
Iris Active:   #6BBFD4   Ice blue. Active states, confirmed action.
Iris Success:  #7ECBB5   Soft teal. Success, copied to clipboard.
Iris Wash:     #189B8FD6  6% opacity violet for selection backgrounds.
```

**Rationale**:  
The cool navy base is the *opposite* of the current warm palette — and the opposite of every AI-default dark UI. It reads as deep, precise, nocturnal. The iridescent accent (`Iris`) exists on a spectrum from violet to teal, shifting with context. Idle = lavender calm. Active = ice-blue snap. Complete = mint satisfaction. The shift encodes meaning. Nothing else does this in the launcher space.

**What to avoid**: Don't use Iris Resting and Iris Active at the same time on the same row. The point is that the accent *changes state*, not that it's a multi-color decoration.

---

## Typography — "Considered & Characterful"

**Display face**: **Space Grotesk** (400 / 600 / 700)  
- Geometric, slightly quirky, contemporary — has *character* without being eccentric  
- The letter shapes have subtle idiosyncrasies (the `G`, the `S`, the `a`) that read as handmade at display sizes  
- Used sparingly: app name in onboarding, empty-state headline, action panel titles  
- Free, OFL licensed  
- Alternative if Space Grotesk feels too quirky: **DM Sans** (cleaner, more neutral but still warm)

**Body face**: **Plus Jakarta Sans** (400 / 500 / 600)  
- Humanist proportions, modern weight distribution  
- Exceptional legibility at 12–15px  
- Pairs perfectly with Space Grotesk (both geometric-humanist, same construction era)  
- Free, OFL licensed  
- Replaces Inter — same versatility, more personality

**Mono face**: Keep JetBrains Mono (already excellent, no change needed)

**Type Scale**:
```
Display:   36px  / Space Grotesk Bold 700     / app name, hero moments
Heading:   18px  / Space Grotesk SemiBold 600 / panel titles, onboarding steps
Search:    17px  / Plus Jakarta Sans Medium   / the search input
Result:    13px  / Plus Jakarta Sans SemiBold / result name
Meta:      11px  / Plus Jakarta Sans Regular  / subtitle, path, timestamp
Label:     10px  / Plus Jakarta Sans Medium   / section headers (NOT all-caps)
Mono:      12px  / JetBrains Mono Medium      / file paths, keyboard shortcuts
```

**Letter-spacing**:
- Display: `-0.5px` (tighten slightly, feel precise)
- Heading: `-0.3px`
- Result: `0px`
- Label: `+0.5px` (breathe at small sizes)

**What this pair says**: "Someone made deliberate choices here." Not Inter, not SF Pro. You notice it without being able to name it immediately. That's the goal.

---

## Layout — "The Lens"

**Structural concept**: The window has a distinct, framed quality. Not a floating capsule — a *panel*. The frame is part of the design.

**Window**: 660px wide. Slightly wider than current 640px.  
**Corner Radius**: 14px. Slightly rounder than current 8px — softer, more jewel-like.  
**Border**: 1px, `Edge` color with a subtle top-edge highlight gradient.  
**Shadow**: Two-layer — a wide soft ambient (40px, 20% opacity) and a tight focus shadow (8px, 50% opacity). Creates physical presence.

**The Window Border Signature**:  
The top edge of the window border gets a 1px gradient: `Iris Resting` at 30% opacity fading to transparent. This makes the window look like light is catching the top rim — the "lens edge" effect. Subtle. Only visible in dark contexts. Unmistakable once you see it.

```
┌──────────────────────────────────────────────────┐  ← Iris gradient top rim
│                                                  │
│   ╔══════════════════════════════════════════╗   │
│   ║  ◎  Search or jump to anything...   esc ║   │  ← Search bar
│   ╚══════════════════════════════════════════╝   │
│                                                  │
│   Apps                                           │  ← Section label (lowercase)
│   ───────────────────────────────────────────    │
│   ▓  Visual Studio Code         code editor      │  ← Result row
│   ▓  Chrome                     web browser      │
│   ▓  Figma                      design tool      │
│                                                  │
│   Files                                          │
│   ───────────────────────────────────────────    │
│   ▓  Project brief.pdf          ~/Documents      │
│                                                  │
└──────────────────────────────────────────────────┘
```

**Search Bar**:  
- Background: `Lifted` — slightly lifted from the window surface, not recessed
- Rounded fully on the left where the icon sits: a 10px left radius
- Left icon: The ◎ Spur dot (see Signature Element) in `Iris Resting`
- Right: `esc` in JetBrains Mono, `Edge` background, `Fog` text — understated
- The search text cursor: `Iris Resting` color — the one accent in the input

**Result Rows**:  
- Height: 44px (unchanged — this is well-calibrated)
- Selection: `Iris Wash` background (6% violet) + left 3px bar in `Iris Resting`
- No selection bar on unselected rows — the wash is enough
- Hover: 40ms fade to `Lifted` background — imperceptibly fast
- Icon: 30×30px, 8px radius, no border — clean, app-like

**Section Labels**:  
- 10px Plus Jakarta Sans Medium, `Dust` color
- lowercase, not ALL-CAPS — softer, more premium
- No structural device (no line, no rivet, no count badge) — the space between sections is the separator
- 20px top padding to breathe

---

## Signature Element — "The Iris Dot"

**What it is**: A small circle (◎) that lives to the left of the search input. 12px diameter. An outer ring at `Iris Resting` opacity 40%, an inner dot at 100% opacity.

**What it does**:
1. **Idle (empty search)**: Soft pulse — the inner dot dims and brightens on a 3s cycle. Barely perceptible. Like breathing. Invites interaction.
2. **Typing**: The dot rotates through a subtle arc (30°, 400ms) — one small movement as you type. Confirms the search is live. Not a spinner — just a quiet acknowledgment.
3. **Loading results**: The outer ring traces a short 120° arc on repeat — not a full spinner, just a partial sweep. Smooth, not mechanical.
4. **Action complete (copied/launched)**: The dot flashes from `Iris Resting` → `Iris Success` → back to `Iris Resting` over 600ms. The only moment of vivid color.

**Why this works**:  
It replaces the search icon (generic magnifying glass) with something that *behaves* and belongs only to Spur. It's alive without being noisy. It communicates state without text. It's the one thing in the UI that moves with intention.

**Implementation**: SVG path, animated with XAML storyboard or DispatcherTimer. 30×30px hit area, 12×12px visual.

---

## Motion — "Materialization"

**Core principle**: Things don't fly in from off-screen. They materialize — like gaining focus. The metaphor is a camera lens resolving from blur to sharp, not a drawer opening.

**Window appearance**:
```
Duration:    180ms
Start:       Opacity 0, Scale 0.97, BlurRadius 8px
End:         Opacity 1, Scale 1.0, BlurRadius 0px
Easing:      CubicOut (sharp deceleration)
```
The blur-to-sharp effect is the key differentiator — no other launcher does this. It feels like the window *comes into focus* rather than appearing. Requires WPF Effect animation — possible with `BlurEffect`.

**Result rows appearing**:
```
Duration:    120ms per row
Stagger:     20ms delay between rows
Start:       Opacity 0, TranslateY +8px
End:         Opacity 1, TranslateY 0
Easing:      CubicOut
```
The slight upward drift (+8px → 0) feels like results *settling into place*. Very subtle. Not a dramatic slide — a 8px drift that reads as gravity.

**Selection change**:
```
Duration:    100ms
Animation:   Cross-fade Iris Wash backgrounds
Easing:      Linear
```
No translation. The wash fades from the previous row to the new one. Like a spotlight moving, not a box jumping.

**Iris Dot pulse (idle)**:
```
Duration:    3000ms cycle
Start:       Inner dot opacity 100%, Outer ring opacity 40%
Mid:         Inner dot opacity 50%, Outer ring opacity 20%
End:         Back to start
Easing:      SineInOut (smooth oscillation)
RepeatBehavior: Forever
```

**Window dismiss**:
```
Duration:    120ms
End:         Opacity 0, Scale 0.98, BlurRadius 4px
Easing:      CubicIn (accelerates into disappear)
```

**What to cut**: No rotation, no spring physics, no slide-from-left. Every effect above is soft and serves the "coming into focus" metaphor. Anything that doesn't serve that metaphor is cut.

---

## Onboarding — "Still & Clear"

The onboarding window gets the full Space Grotesk treatment.

```
┌────────────────────────────────────┐
│                                    │
│                                    │
│              ◎                     │  ← Iris Dot, 32px, gentle pulse
│                                    │
│           Spur                     │  ← Space Grotesk Bold, 42px
│                                    │
│    Thought to action.              │  ← Space Grotesk Regular, 17px
│    In one keystroke.               │     Fog color. Two short lines.
│                                    │
│    ┌──────────────────────────┐   │
│    │  Get started             │   │  ← Plus Jakarta, primary button
│    └──────────────────────────┘   │
│                                    │
│                                    │
└────────────────────────────────────┘
```

**Copy** (rewritten):  
- **Headline**: "Thought to action." — three words, full stop, confident
- **Subline**: "In one keystroke." — answers "how"
- **Body** (next slide): "No tracking. No upsells. Runs locally."

**What's removed from current onboarding**:  
- The 64px app icon (redundant — Space Grotesk + Iris Dot is enough)
- "A fast, minimal launcher for Windows" — prose description replaced by two sharp lines
- The three-paragraph description — compress to six words across two lines

---

## Settings — Carried Through

Settings gets lighter treatment — the hardware aesthetic was right about one thing: settings is visited once. But it should still feel like it belongs to the same family.

**Settings window**: Same `Surface` and `Lifted` palette, same `Plus Jakarta Sans` body, Space Grotesk for headings only.

**Panel headers**: Space Grotesk 18px SemiBold. The `◎` Iris Dot appears before the active section name in the sidebar (small, 10px, `Iris Resting`).

**Inputs, toggles, sliders**: Standard WPF custom controls but with:  
- `Iris Resting` for focus rings and active toggle states  
- `Lifted` backgrounds  
- No drop shadows on inputs — flat, clean

---

## Copy & Voice

**Tone**: Warm and direct. Confident without being cold. This is not a tool — it's a product someone built for you.

| Current | New |
|---------|-----|
| "Search" (placeholder) | "Search or jump to anything…" |
| "Welcome to Spur" | "Spur" (just the name — the screen says the rest) |
| "How will you summon Spur?" | "Choose your shortcut" |
| "No results" | "Nothing matched. Try something shorter." |
| "Clear clipboard" | "Clear history" |
| "General" (settings tab) | "General" (this one is fine — don't rename what works) |
| "Extras" (settings tab) | "Extensions" (more self-explanatory) |

**Placeholder text**: "Search or jump to anything…" — the ellipsis is intentional. It trails off. It implies there's more. It invites you to fill it.

**Error messages**:  
- Path not found: "Couldn't open that file."  
- Action failed: "That didn't work. Try again."  
- Not cold, not apologetic. States what happened. Implies a path forward.

---

## Full Token Replacement

### Tokens.xaml (geometry only — themes provide color)

```xml
<!-- Fonts -->
<FontFamily x:Key="Token.Font.Display">
    pack://application:,,,/Themes/Fonts/#Space Grotesk, Segoe UI Variable
</FontFamily>
<FontFamily x:Key="Token.Font.Body">
    pack://application:,,,/Themes/Fonts/#Plus Jakarta Sans, Segoe UI Variable
</FontFamily>
<FontFamily x:Key="Token.Font.Mono">
    Cascadia Code, JetBrains Mono, Consolas
</FontFamily>

<!-- Font Sizes -->
<sys:Double x:Key="Token.FontSize.Display">36</sys:Double>
<sys:Double x:Key="Token.FontSize.Heading">18</sys:Double>
<sys:Double x:Key="Token.FontSize.Search">17</sys:Double>
<sys:Double x:Key="Token.FontSize.Result">13</sys:Double>
<sys:Double x:Key="Token.FontSize.Meta">11</sys:Double>
<sys:Double x:Key="Token.FontSize.Label">10</sys:Double>
<sys:Double x:Key="Token.FontSize.Mono">12</sys:Double>

<!-- Spacing -->
<sys:Double x:Key="Token.Spacing.2">2</sys:Double>
<sys:Double x:Key="Token.Spacing.4">4</sys:Double>
<sys:Double x:Key="Token.Spacing.6">6</sys:Double>
<sys:Double x:Key="Token.Spacing.8">8</sys:Double>
<sys:Double x:Key="Token.Spacing.12">12</sys:Double>
<sys:Double x:Key="Token.Spacing.16">16</sys:Double>
<sys:Double x:Key="Token.Spacing.20">20</sys:Double>
<sys:Double x:Key="Token.Spacing.24">24</sys:Double>
<sys:Double x:Key="Token.Spacing.32">32</sys:Double>

<!-- Layout -->
<sys:Double x:Key="Token.Layout.SearchBarHeight">52</sys:Double>
<sys:Double x:Key="Token.Layout.RowHeight">44</sys:Double>
<sys:Double x:Key="Token.Layout.RowHeightTall">52</sys:Double>
<sys:Double x:Key="Token.Layout.IconSize">30</sys:Double>
<sys:Double x:Key="Token.Layout.WindowWidth">660</sys:Double>
<sys:Double x:Key="Token.Layout.IrisDotSize">12</sys:Double>

<!-- Radii -->
<CornerRadius x:Key="Token.Radius.Window">14</CornerRadius>
<CornerRadius x:Key="Token.Radius.Row">8</CornerRadius>
<CornerRadius x:Key="Token.Radius.SearchBar">10</CornerRadius>
<CornerRadius x:Key="Token.Radius.Button">8</CornerRadius>
<CornerRadius x:Key="Token.Radius.Badge">6</CornerRadius>
<CornerRadius x:Key="Token.Radius.Icon">8</CornerRadius>
```

### DarkTheme.xaml (color only)

```xml
<!-- Surfaces -->
<SolidColorBrush x:Key="Void"           Color="#0C0E14"/>
<SolidColorBrush x:Key="Depth1"         Color="#13151E"/>
<SolidColorBrush x:Key="Depth2"         Color="#1C1F2B"/>
<SolidColorBrush x:Key="Depth3"         Color="#2A2E3D"/>
<SolidColorBrush x:Key="Depth4"         Color="#404660"/>
<SolidColorBrush x:Key="Surface"        Color="#13151E"/>
<SolidColorBrush x:Key="SurfaceAcrylic" Color="#EE13151E"/>
<SolidColorBrush x:Key="SurfaceLow"     Color="#0C0E14"/>
<SolidColorBrush x:Key="SurfaceRaised"  Color="#1C1F2B"/>

<!-- Text -->
<SolidColorBrush x:Key="TextPrimary"    Color="#ECF0F7"/>
<SolidColorBrush x:Key="TextSecondary"  Color="#8E94A8"/>
<SolidColorBrush x:Key="TextMuted"      Color="#5C6178"/>
<SolidColorBrush x:Key="TextTertiary"   Color="#525870"/>
<SolidColorBrush x:Key="TextInverse"    Color="#0C0E14"/>

<!-- Borders -->
<SolidColorBrush x:Key="BorderBrush"    Color="#2A2E3D"/>
<SolidColorBrush x:Key="BorderStrong"   Color="#3A3F52"/>
<SolidColorBrush x:Key="Separator"      Color="#1E2130"/>
<SolidColorBrush x:Key="GlassBorder"    Color="#309B8FD6"/>  <!-- Iris tinted -->

<!-- Accent — The Iris -->
<SolidColorBrush x:Key="Accent"         Color="#9B8FD6"/>   <!-- Violet resting -->
<SolidColorBrush x:Key="AccentActive"   Color="#6BBFD4"/>   <!-- Ice active -->
<SolidColorBrush x:Key="AccentSuccess"  Color="#7ECBB5"/>   <!-- Teal success -->
<SolidColorBrush x:Key="AccentWash"     Color="#189B8FD6"/> <!-- 10% selection bg -->
<SolidColorBrush x:Key="AccentGhost"    Color="#0D9B8FD6"/> <!-- 5% subtle tint -->
<SolidColorBrush x:Key="BrandHighlight" Color="#B8AFEA"/>   <!-- Lighter iris -->

<!-- Selection -->
<SolidColorBrush x:Key="SelectedBg"     Color="#1A1C28"/>
<SolidColorBrush x:Key="HoverBg"        Color="#171926"/>

<!-- Semantic -->
<SolidColorBrush x:Key="Green"          Color="#7ECBB5"/>   <!-- Same as AccentSuccess -->
<SolidColorBrush x:Key="Orange"         Color="#D4956A"/>
<SolidColorBrush x:Key="Red"            Color="#C46E6E"/>
```

---

## Implementation Phases

### Phase 1 — Foundation (6 hours)
Replace the token system. Update colors, fonts (download + bundle Space Grotesk, Plus Jakarta Sans), corner radii, layout constants. Visual output: everything looks noticeably different before a single component is changed.

**Files**: `Tokens.xaml`, `DarkTheme.xaml`, `LightTheme.xaml`

### Phase 2 — Search Bar + Iris Dot (4 hours)
Implement the Iris Dot SVG + animation storyboard. Redesign the search bar: new background, dot on left, updated placeholder copy, updated esc badge.

**Files**: `SearchBar.xaml`, `Animations.xaml`

### Phase 3 — Result Rows + Section Labels (4 hours)
Update result template: new icon size, Iris Wash selection, updated typography, lowercase section labels, new hover/selection animation.

**Files**: `ResultTemplates.xaml`

### Phase 4 — Window Frame + Motion (4 hours)
Implement the blur-to-sharp window appearance. Add top-edge Iris gradient border. Update window dismiss animation.

**Files**: `MainWindow.xaml`, `Animations.xaml`, `MainWindow.xaml.cs`

### Phase 5 — Onboarding + Settings (4 hours)
Rewrite onboarding with new layout, copy, and Space Grotesk display treatment. Carry the system into Settings.

**Files**: `OnboardingWindow.xaml`, `SettingsWindow.xaml`, `SettingsView.xaml`

**Total**: ~22 hours for full implementation

---

## Before / After

### Before
- Warm beige accent on near-black. Generic.
- Inter everywhere. Invisible typography.
- Standard scale-in animation. Launcher template.
- Floating window with soft shadow.
- No signature element.
- Grade: **B+ polished, C distinctive**

### After
- Cool navy base, iridescent violet-to-teal accent. Unmistakably Spur.
- Space Grotesk display + Plus Jakarta Sans body. Character with readability.
- Blur-to-focus materialization. Nobody else does this.
- Framed lens window with Iris-tinted top rim.
- The breathing ◎ Iris Dot. One animated element that encodes state.
- Grade: **A− polished, A distinctive**

---

## Self-Critique

**"Is this beautiful?"**  
Yes. The navy-iris palette is luminous in the same way Linear and Arc are — cool, precise, and alive. Space Grotesk + Plus Jakarta Sans are a pair that reads as intentional from the first second.

**"Does it still feel pleasant to use 40 times a day?"**  
Yes. The animations are gentle (blur-to-focus, 8px drift). The selection is a soft wash, not a harsh block. Nothing shouts. The Iris Dot pulses at 3s — below the threshold of annoyance, at the threshold of aliveness.

**"Does it say Spur?"**  
Yes — the Iris Dot ◎ becomes the visual shorthand for the brand. The color system is named for it. It's on the onboarding screen, the search bar, the active section indicator. It's the one thing that travels.

**"What would I remove?"**  
The blur-to-focus effect on window open is the most complex to implement. If it produces any jank on lower-end machines, cut it — the drift-in animation works fine without the blur. The blur is a delight; the drift is the baseline.

**"Is any part of this an AI default?"**  
- Warm cream? No.
- Near-black with acid green? No.
- Broadsheet hairlines? No.
- The iridescent shifting accent is genuinely unusual in desktop software. The Space Grotesk + Plus Jakarta pairing is specific. The blur-to-focus animation is specific. This passes.

---

**Status**: Ready for review.  
**Recommendation**: Approve the palette and typography first — implement Phase 1 only, screenshot the result, and evaluate before committing to Phases 2–5. The token swap alone will show whether the direction is right.
