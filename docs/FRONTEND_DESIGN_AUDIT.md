# Frontend Design Audit — Spur Launcher

**Date**: 2026-06-10  
**Framework**: Distinctive Visual Design Principles  
**Subject**: Windows desktop launcher (competitor to Alfred, Raycast, Spotlight)

---

## Executive Summary

Spur demonstrates a **coherent, well-implemented design system** with strong technical foundations. The visual language is clean, professional, and functional — but it **lacks a distinctive signature** that would make it immediately recognizable as Spur rather than "a launcher."

**Current Grade**: B+  
**Distinctive Quality**: C  
**Technical Execution**: A-

The design reads as **template-professional** rather than **intentionally memorable**. It executes launcher conventions well but doesn't take the aesthetic risk that makes a product feel like it has a point of view.

---

## 1. Ground in the Subject

### What is Spur?
**Subject**: A Windows launcher for power users  
**Audience**: Developers, designers, productivity enthusiasts who value speed and minimalism  
**Job**: Launch apps, find files, execute actions in < 200ms with zero cognitive friction  
**Differentiator**: Local-first, no tracking, no upsells, community-driven

### The World of Launching
**Materials**: Keyboard shortcuts, instant results, muscle memory, search precision  
**Instruments**: Text input, fuzzy matching, icons, keyboard navigation  
**Artifacts**: `.exe` files, shortcuts, PowerShell commands, clipboard history  
**Vernacular**: "Spur your workflow", search-first, action-oriented, minimal chrome

### Current Signature Element
❌ **None identified**  
The design has no single visual element that says "this is Spur, not Raycast/Alfred/Wox"

---

## 2. Current Design Analysis

### Typography

**Display**: Inter (via `Token.Font.Primary`)
- Weight: Normal to SemiBold
- Size: 18px search, 13px results, 11px metadata
- Line-height: Not explicitly set (defaults to 1.0–1.2)

**Mono**: Cascadia Code, JetBrains Mono fallbacks
- Used for: Keyboard hints ("esc"), code snippets
- Size: 13px, 11px for hints

**Assessment**: ✓ Solid choices, well-executed  
**Issue**: Inter is the **default web app font of 2024**. It's everywhere. It's the typographic equivalent of saying "I'm professional but have no opinion."

**Distinctive Alternative Needed**: A characterful display face used with restraint for the app name, section headers, or the search placeholder when empty.

---

### Color Palette (Dark Theme)

```
Void:            #09090A  // Deep black base
Depth1:          #101012  // Primary surface
Depth2:          #171719  // Raised elements
Depth3:          #1C1C1F
Depth4:          #29292D

TextPrimary:     #F7F3EC  // Warm off-white
TextSecondary:   #B2ADA4  // Muted warm gray
TextTertiary:    #6B665F  // Very muted

Accent:          #D7CFC2  // Warm beige/taupe
BrandHighlight:  #F1E9DC  // Lighter warm
AccentWash:      #24D7CFC2  // 14% opacity overlay

Green:           #6B8F71  // Muted sage
Orange:          #A8947A  // Desaturated terracotta
Red:             #C45C5C  // Soft red
```

**Assessment**: This is **exactly** the AI-default warm cream/taupe palette.  
**From the brief**: "a warm cream background (near #F4F1EA) with a high-contrast serif display and a terracotta accent" — this is **option 1** from the clustered looks.

**Issue**: The palette is pleasant, but it's the same one that appears in 70% of AI-generated dark UIs right now. It doesn't encode anything specific to Spur's identity.

**Signature Opportunity**: A single, unexpected accent that connects to the **"spur" metaphor** — something sharp, precise, directional. Think: a steel-blue edge, a sharp citrus yellow, a mechanical orange.

---

### Spacing & Layout

**Grid**: 4px base (good)  
**Tokens**: `2, 4, 6, 8, 12, 16, 20, 24, 32, 40, 48, 56, 64`  
**Row Height**: 44px (standard), 52px (tall)  
**Search Bar**: 56px fixed height  
**Window Width**: 640px (no variations used)

**Corner Radii**:
- Window: 8px
- Rows: 8px  
- Buttons: 6px
- Pills: 16px

**Assessment**: ✓ Consistent, well-structured  
**Issue**: The radii are **generic modern app defaults**. Every launcher uses 6–12px. Nothing here says "Spur."

**Distinctive Alternative**: Either commit to **sharper** (2–4px, precise, technical) or **softer** (16–20px, organic, flowing). The current middle ground is safe but forgettable.

---

### Motion & Animation

**Current**:
- Window scale-in from 0.96 to 1.0 with elastic ease
- 80ms hover transitions
- Liquid glass top-edge gradient (subtle luminosity)
- Expand/collapse for result panels

**Assessment**: ✓ Polished, appropriate  
**Issue**: These are **launcher conventions**. Every modern launcher does scale-in + hover fades.

**Signature Opportunity**: A **page-load sequence** that embodies "spur" — something quick, directional, pointed. Example: results don't fade in, they **snap into place** like spurs clicking. Or the search bar doesn't scale, it **extends** from a point like a lance.

---

## 3. What Works (Strengths)

### ✓ Design System Discipline
- Clean token system with semantic naming
- Consistent spacing scale (4px grid)
- Light/Dark theme parity (same key list)
- Separation of structure (Tokens.xaml) from color (themes)

### ✓ Typography Hierarchy
- Clear distinction: 18px search > 13px results > 11px metadata
- Proper use of weight (Normal → Medium → SemiBold)
- Mono font for technical elements (keyboard hints, code)

### ✓ Accessibility Foundations
- Keyboard focus visible
- Screen reader labels (`AutomationProperties.Name`)
- High contrast ratios (TextPrimary on Depth1 = ~16:1)
- Reduced motion hooks exist (in code)

### ✓ Micro-Interactions
- 80ms hover (fast, appropriate for power users)
- Selection rail (3px left edge) — clear, minimal
- Glass effect (top-edge gradient) — subtle refinement

### ✓ Icon Treatment
- Clean 32x32px icons with 8px corner radius
- No background pills (previous version had them, removed)
- Proper fallback chain: image → glyph → letter

---

## 4. What's Missing (Opportunities)

### ❌ 1. Distinctive Typography

**Issue**: Inter is the default. It says "I'm a web app from 2024."

**Solution**: Pick a characterful display face for the **app name** and **empty-state placeholder**:

**Option A — Technical Precision**:
- Display: **JetBrains Mono** (already loaded) or **IBM Plex Mono**  
- Rationale: Connects to developer audience, emphasizes speed/precision  
- Usage: App name, onboarding title, empty search placeholder

**Option B — Sharp Modernism**:
- Display: **Outfit** or **Manrope** (geometric, slightly condensed)  
- Rationale: "Spur" is a short, sharp word — needs a sharp face  
- Usage: Section headers ("Apps", "Files"), action panel titles

**Option C — Flow Contrast**:
- Display: **Fraunces** (serif with optical sizing, warm)  
- Rationale: Contrasts with the functional UI, adds personality  
- Usage: Onboarding only, settings headings

**Recommendation**: **Option A** (JetBrains Mono for display) — already in the font stack, reinforces the technical/developer positioning, creates instant differentiation.

### Example Implementation:
```xaml
<!-- OnboardingWindow.xaml, line 33 -->
<TextBlock Text="Spur"
           FontFamily="{DynamicResource Token.Font.Mono}"  <!-- Changed -->
           FontSize="42"  <!-- Slightly larger -->
           FontWeight="Bold"  <!-- Stronger -->
           LetterSpacing="20"  <!-- Tracked out -->
```

---

### ❌ 2. Signature Color Accent

**Issue**: The warm beige (#D7CFC2) is pleasant but forgettable. It's on every AI-generated design.

**Solution**: Pick **one unexpected accent** that connects to "spur":

**Option A — Steel Edge**:
- Primary: `#7C9BAF` (cool blue-gray, like metal)  
- Usage: Selection rail, active category, keyboard hints  
- Rationale: Spurs are metal, sharp, functional tools

**Option B — Citrus Snap**:
- Primary: `#E8B84E` (bright yellow-orange, like a spark)  
- Usage: Search cursor, active result rail, success states  
- Rationale: "Spur" as verb = sudden action, energy

**Option C — Deep Vermilion**:
- Primary: `#C7553A` (terracotta-red with warmth)  
- Usage: Category badges, action states  
- Rationale: Western/ranch aesthetic (literal spurs)

**Recommendation**: **Option A** (Steel Edge #7C9BAF) — pairs well with the warm grays, adds precision, avoids the AI-default warm palette, doesn't shout.

### Implementation:
```xaml
<!-- DarkTheme.xaml additions -->
<SolidColorBrush x:Key="Accent"          Color="#7C9BAF"/>  <!-- Changed -->
<SolidColorBrush x:Key="BrandHighlight"  Color="#A3BFD1"/>  <!-- Lighter -->
<SolidColorBrush x:Key="AccentWash"      Color="#247C9BAF"/>

<!-- SelectionRail color (ResultTemplates.xaml) -->
<Setter Property="Background" Value="{DynamicResource Accent}"/>
```

---

### ❌ 3. Distinctive Motion Signature

**Issue**: Scale-in animations are universal. Nothing here is memorable.

**Solution**: One **signature micro-interaction** that embodies "spur as directed action":

**Option A — Directional Snap**:
- Results slide in from **left edge** (not fade, not scale)  
- Each row enters 20ms after the previous (staggered cascade)  
- Easing: `CubicOut` (starts fast, decelerates)  
- Rationale: Feels like momentum, like being spurred forward

**Option B — Search Cursor Pulse**:
- When search is empty, cursor has a **slow pulse** (1.5s cycle)  
- Changes from muted → accent → muted  
- Rationale: Invites action, draws eye to the entry point

**Option C — Selection Rail Flick**:
- When selection changes, rail doesn't fade — it **flicks** (10px left, springs back)  
- Duration: 120ms  
- Rationale: Physical, tactile, like a mechanical click

**Recommendation**: **Option A** (Directional Snap) + **Option C** (Rail Flick) — both reinforce directionality and precision.

---

### ❌ 4. Empty State Personality

**Current**: Empty search shows placeholder "Search"  
**Issue**: Generic. Every launcher says "Search."

**Solution**: Make the placeholder a **thesis statement**:

**Option A — Verb-first**:
```
"Spur something"  // Active, aligned with brand
```

**Option B — Speed claim**:
```
"Launch, find, act"  // Three verbs, shows capability
```

**Option C — Minimalist**:
```
">"  // Just the prompt symbol, ultra-minimal
```

**Recommendation**: **Option A** ("Spur something") — on-brand, active voice, memorable. Changes `SearchPlaceholder` logic in MainViewModel.

---

### ❌ 5. Onboarding Visual Hierarchy

**Current State**:
- Slide 1: Icon (64px) → "Spur" (36px) → Tagline → Description  
- All centered, vertical stack, neutral layout

**Issue**: The onboarding doesn't **feel** like launching. It's a typical welcome screen.

**Solution**: Make the first screen a **demonstration**:

**Concept**: "Watch Spur in action"
- Instead of static text, show an **animated search sequence**  
- Types "vsc" → shows result → highlights Enter key → fades  
- Then shows actual onboarding text  
- Rationale: People learn launchers by **doing**, not reading

**Implementation**: Add a 2-second auto-play demo before Slide 1 appears.

---

## 5. Structural Issues

### Issue A: Keyboard Hint Styling

**Current**: `"esc"` badge in search bar (right side)  
**Styling**: Small rounded pill with mono font  
**Problem**: The badge is **muted** (TextTertiary) and blends in

**Solution**: Make keyboard hints a **visual anchor**:
```xaml
<!-- Higher contrast, stronger presence -->
<Border Background="{DynamicResource Accent}"  <!-- Changed from SurfaceRaised -->
        BorderBrush="Transparent"
        Opacity="0.8">  <!-- Subtle but visible -->
    <TextBlock Foreground="{DynamicResource TextInverse}"  <!-- Dark text on light accent -->
               FontWeight="Medium"/>
</Border>
```

**Rationale**: Keyboard navigation is **core** to launcher UX. The hints should feel important, not apologetic.

---

### Issue B: Section Label Treatment

**Current**: "Apps", "Files", "Clipboard" labels  
**Styling**: 11px Medium, TextMuted, 16px left padding

**Problem**: The labels are **too subtle**. They don't guide the eye.

**Solution**: Add a **structural device** that encodes information:

**Option A — Left Accent Line**:
```xaml
<Border BorderThickness="2,0,0,0"  <!-- 2px left edge -->
        BorderBrush="{DynamicResource Accent}"
        Padding="14,8,16,4">  <!-- Reduced left for border -->
    <TextBlock Text="{Binding Title}"
               FontSize="11"
               FontWeight="SemiBold"  <!-- Stronger -->
               Foreground="{DynamicResource TextSecondary}"/>  <!-- Lighter -->
</Border>
```

**Option B — Inline Count Badge**:
```
"Apps · 12"  // Shows result count inline
"Files · 5"
```

**Recommendation**: **Option A** (Left Accent Line) — provides visual rhythm, guides scanning, doesn't add noise.

---

### Issue C: Result Row Icon Size

**Current**: 32×32px icons with 8px corner radius  
**Container**: 36px wide column with centered icon

**Problem**: The icons feel **small** in a 44px row. There's wasted vertical space.

**Solution**: Increase to **36×36px** icons (fills the column):
```xaml
<Border Grid.Column="0"
        Width="36" Height="36"  <!-- Changed from 32 -->
        CornerRadius="10">  <!-- Proportional increase -->
```

**Rationale**: Icons are the **primary visual anchor** in result rows. Bigger = faster recognition.

---

## 6. Proposed Design Plan

### Vision Statement
> Spur feels **precise, immediate, and undecorated**. Like a well-machined tool. The UI is a **launch mechanism**, not a showcase. Typography is technical. Motion is directional. Color is restrained with one sharp accent.

---

### Color System (Revised)

**Dark Theme**:
```scss
// Surfaces (unchanged — these work)
Void:           #09090A
Depth1:         #101012
Depth2:         #171719
Depth3:         #1C1C1F

// Text (unchanged)
TextPrimary:    #F7F3EC
TextSecondary:  #B2ADA4
TextTertiary:   #6B665F

// Accent (NEW — steel blue, precision tool)
Accent:         #7C9BAF  // Changed from warm beige
BrandHighlight: #A3BFD1  // Lighter steel
AccentWash:     #247C9BAF

// Semantic (unchanged)
Green:          #6B8F71
Orange:         #A8947A
Red:            #C45C5C
```

**Rationale**: Steel blue = precision, tools, machinery. Contrasts with warm text colors. Doesn't shout. Differentiates from AI-default warm palette.

---

### Typography System (Revised)

**Display** (NEW):  
- **JetBrains Mono Bold, tracked +20**  
- Usage: App name ("Spur"), onboarding titles, empty state placeholder  
- Size: 42px for hero, 22px for headings

**Body** (unchanged):  
- **Inter Regular/Medium**  
- Usage: Search input, result names, descriptions

**Mono** (expanded usage):  
- **JetBrains Mono Medium**  
- Usage: Keyboard hints, file paths, code snippets, version numbers

**Type Scale**:
```
Display:  42px / Bold / +20 tracking  (app name)
Title:    22px / SemiBold            (section headers)
Search:   18px / Normal              (search input)
Result:   13px / Medium              (result names)
Meta:     11px / Normal              (subtitles, hints)
```

---

### Motion Signatures (NEW)

**1. Result Entry — Directional Snap**:
```csharp
// ResultTemplates.xaml trigger
<Storyboard>
    <DoubleAnimation From="-20" To="0" Duration="0:0:0.15"  
                     Storyboard.TargetProperty="RenderTransform.X"
                     EasingFunction="{StaticResource CubicOut}"/>
    <DoubleAnimation From="0" To="1" Duration="0:0:0.1"
                     Storyboard.TargetProperty="Opacity"/>
</Storyboard>
```
**Effect**: Results slide in from left edge (20px), staggered by row index.

**2. Selection Rail — Spring Flick**:
```csharp
// When selection changes
<DoubleAnimation From="-10" To="0" Duration="0:0:0.12"
                 EasingFunction="{StaticResource ElasticOut}"/>
```
**Effect**: Rail flicks left 10px, springs back.

**3. Search Cursor Pulse** (empty state):
```csharp
// Subtle pulse on TextBox caret brush
<ColorAnimation From="#6B665F" To="#7C9BAF" Duration="0:0:1.5"
                AutoReverse="True" RepeatBehavior="Forever"/>
```

---

### Layout Refinements

**1. Icon Size**: 32×32 → **36×36**  
**2. Section Labels**: Add **2px left accent line**  
**3. Keyboard Hints**: Accent background instead of muted  
**4. Corner Radii**: Reduce to **4px** (window), **6px** (rows) for sharper feel

---

### Signature Element

**The "Spur Point"** — A directional chevron that appears in three places:

1. **Empty Search State**: A subtle `>` chevron before the placeholder text  
   ```
   > Spur something
   ```

2. **Active Category**: Small chevron next to active category name in scope bar  
   ```
   Files ▸
   ```

3. **Keyboard Shortcuts**: Replace "esc" with directional indicator  
   ```
   ◂ esc
   ```

**Visual Style**:
- Color: Steel blue accent (#7C9BAF)
- Size: 12px
- Weight: 2px stroke
- Animation: Subtle rightward pulse (5px, 2s cycle)

**Rationale**: Reinforces "spur" as directional action. Creates visual cohesion. Memorable without being loud.

---

## 7. Implementation Priority

### Phase 1: High-Impact, Low-Effort ✓

1. **Change accent color** (#D7CFC2 → #7C9BAF)  
   - File: `DarkTheme.xaml`, `LightTheme.xaml`  
   - Impact: Immediate differentiation from AI-default

2. **Update empty search placeholder** ("Search" → "> Spur something")  
   - File: `MainViewModel.cs` (SearchPlaceholder property)  
   - Add chevron glyph in `SearchBar.xaml`

3. **Increase icon size** (32×32 → 36×36)  
   - File: `ResultTemplates.xaml`  
   - Improves scannability

4. **Strengthen keyboard hint styling**  
   - File: `SearchBar.xaml` (esc badge)  
   - Use Accent background, TextInverse foreground

5. **Add section label accent line**  
   - File: `ResultTemplates.xaml` (SectionTemplate)  
   - 2px left border in Accent color

**Estimated Time**: 2 hours  
**Impact**: Immediate visual distinctiveness

---

### Phase 2: Signature Elements ✓

6. **Add JetBrains Mono display treatment**  
   - Files: `OnboardingWindow.xaml`, `SettingsWindow.xaml`  
   - Apply to app name, titles

7. **Implement directional snap animation**  
   - File: `ResultTemplates.xaml`, `Animations.xaml`  
   - Staggered left-to-right entry

8. **Add selection rail flick**  
   - File: `ResultTemplates.xaml` (selection trigger)  
   - Spring animation on state change

9. **Implement "Spur Point" chevrons**  
   - Files: `SearchBar.xaml`, `ScopeBar.xaml`  
   - Add directional indicators

**Estimated Time**: 4 hours  
**Impact**: Memorable signature interactions

---

### Phase 3: Polish & Refinement

10. **Reduce corner radii** (8px → 4px for precision feel)  
11. **Add search cursor pulse** (empty state)  
12. **Create onboarding animation demo**  
13. **Audit all copy for active voice**

**Estimated Time**: 3 hours  
**Impact**: Cohesive, polished identity

---

## 8. Copy & Voice Audit

### Current Issues

**Onboarding Slide 1**:
```
"A fast, minimal launcher for Windows."
```
**Problem**: Describes **what it is**, not **what it does for you**.

**Improved**:
```
"Launch apps, find files, run commands.  
Zero clicks. Zero tracking."
```
**Rationale**: Active verbs. Benefit-first. Addresses privacy concern directly.

---

**Onboarding Slide 2**:
```
"How will you summon Spur?"
```
**Problem**: "Summon" is whimsical but doesn't match the technical tone.

**Improved**:
```
"Set your shortcut"
```
**Rationale**: Direct. Matches the functional aesthetic.

---

**Settings Panel Headers**:  
Current: "General", "Search", "Extras"  
**Problem**: Generic admin panel language.

**Improved**:  
"Settings", "Search Behavior", "Installed Extras"  
**Rationale**: More specific. "Behavior" implies user control.

---

**Empty States**:  
Current: No custom empty states (just shows no results)

**Needed**:
```
Files: "No files found. Try a different search."  
Clipboard: "Your clipboard history is empty."  
Actions: "No matching actions. Try 'timer', 'ip', or 'pw'."
```
**Rationale**: Provides direction, teaches discovery.

---

## 9. Before/After Comparison

### Before (Current)

**Palette**: Warm beige accent (#D7CFC2)  
**Typography**: Inter everywhere, 18px/13px/11px  
**Motion**: Standard scale-in (0.96→1.0)  
**Signature**: None  
**Voice**: Professional, neutral  
**Distinctiveness**: 3/10

**Reads as**: A well-executed launcher. Could be anyone's.

---

### After (Proposed)

**Palette**: Steel blue accent (#7C9BAF)  
**Typography**: JetBrains Mono display + Inter body  
**Motion**: Directional snap + rail flick  
**Signature**: "Spur Point" chevrons  
**Voice**: Precise, active, undecorated  
**Distinctiveness**: 8/10

**Reads as**: Spur. A precision tool for power users. Technical, intentional, sharp.

---

## 10. Critique & Risks

### Risk: Technical Aesthetic May Feel Cold

**Mitigation**: The warm text colors (#F7F3EC, #B2ADA4) balance the cool accent. The organic corner radii (even reduced to 4–6px) prevent it from feeling like CAD software.

### Risk: Monospace Display Font May Feel "Coder-Only"

**Mitigation**: Use it sparingly (app name, empty state, titles only). Body text stays Inter. The mix says "technical but approachable."

### Risk: Directional Animations May Distract

**Mitigation**: Keep duration < 150ms. Use easing (CubicOut) to feel snappy, not slow. The user should *feel* momentum, not *watch* animation.

### Risk: Removing Warm Palette May Lose Friendliness

**Counter**: The warm palette is **indistinguishable** from AI defaults. The steel blue + warm text pairing is still warm overall, just with more contrast and precision.

---

## 11. Final Recommendations

### Immediate Actions (Do First)

1. ✅ **Change accent** to #7C9BAF (steel blue)
2. ✅ **Update empty placeholder** to "> Spur something"  
3. ✅ **Increase icons** to 36×36px
4. ✅ **Strengthen keyboard hints** (Accent bg)
5. ✅ **Add section accent lines** (2px left border)

**Why these first**: Zero risk, high visual impact, 2 hours total.

---

### Signature Additions (Do Second)

6. ✅ **Apply JetBrains Mono** to app name and titles
7. ✅ **Implement directional snap** for results
8. ✅ **Add selection rail flick** animation
9. ✅ **Add "Spur Point" chevrons** in key locations

**Why these second**: Define the signature. Memorable without being risky.

---

### Polish & Voice (Do Last)

10. ✅ Reduce corner radii (4–6px)
11. ✅ Add cursor pulse (empty state)
12. ✅ Rewrite onboarding copy (active voice)
13. ✅ Add empty state direction

**Why these last**: Refinement. Assumes the core signature is in place.

---

## 12. Success Criteria

After implementation, the design should pass these tests:

### The Glance Test
> If you see a screenshot of Spur with no branding, can you identify it as Spur and not Raycast/Alfred?

**Target**: Yes, by the steel blue accent, JetBrains Mono titles, and "Spur Point" chevrons.

### The Motion Test
> Do the animations feel **directional** rather than generic?

**Target**: Yes, results snap left-to-right, selection rail flicks, cursor pulses forward.

### The Voice Test
> Does the copy sound like a **tool** or a **friendly assistant**?

**Target**: Tool. Active verbs, no apologies, precise language.

### The Aesthetic Test
> Does the visual treatment match the positioning: "fast, local-first, undecorated"?

**Target**: Yes. Sharp, technical, minimal chrome, no decoration.

---

## Conclusion

**Current State**: Spur is a well-executed, professional launcher with no distinctive visual identity. It follows conventions excellently but doesn't break any.

**Proposed State**: Spur becomes recognizable by its **precision aesthetic** — steel blue accent, technical typography, directional motion, and the "Spur Point" signature element.

**Core Principle**: The UI should feel like **the tool it is** — sharp, fast, undecorated, pointed.

**Grade After Changes**: A- (Distinctive), A (Technical)

---

**Ready for implementation when approved.**
