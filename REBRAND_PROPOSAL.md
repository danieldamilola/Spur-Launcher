# Spur — Complete Rebrand Proposal
## "The Hardware Aesthetic"

**Date**: 2026-06-10  
**Approach**: Ground-up visual identity redesign  
**Risk Level**: High (intentionally bold)

---

## Design Brief (Self-Defined)

**Subject**: Spur is a physical tool made digital  
**Audience**: Power users who value craftsmanship, precision, and control  
**Job**: Provide instant access with the tactile satisfaction of a well-machined mechanism  
**Thesis**: Software doesn't have to feel soft

---

## Core Concept: "Machined Precision"

**Vision Statement**:
> Spur looks and feels like a **precision-machined tool** sitting on your desktop. Not a floating ethereal window — a solid, physical object with weight, material, and mechanism. Every interaction has the satisfying **click** of metal engaging metal. The UI doesn't glow and fade — it **moves**, **locks**, **slides** like hardware.

**Why This Works**:
- Name "Spur" = physical metal tool → literal interpretation
- Launcher = mechanism for triggering action → mechanical metaphor fits
- Differentiation = Every launcher is soft/ethereal → we go physical/industrial
- User feeling = Not "using software" but "operating a device"

**The Aesthetic Risk**:
This will feel **heavier** than typical UI. It trades ethereal polish for industrial character. Some will find it too bold. That's the point — it has a strong opinion.

---

## Design System

### 1. Color Palette — "Gunmetal Workshop"

**Primary Colors**:
```
Void Black:       #0A0A0B  // Deep black, not pure
Charcoal:         #1C1D1F  // Primary surface
Steel Dark:       #2A2B2E  // Raised elements
Steel Mid:        #3E4045  // Borders, dividers
Brushed Metal:    #525559  // Inactive elements
```

**Accent Colors**:
```
Forge Orange:     #E8590C  // Primary accent (hot metal)
Spark Yellow:     #F4A261  // Warning/highlight states
Rivet Silver:     #B8BCC2  // Metallic details
Copper Patina:    #6B8E7F  // Success states
```

**Text Colors**:
```
Engraved Text:    #E8E9EB  // Primary text (looks etched)
Stamped Text:     #A8ACB2  // Secondary (lighter etch)
Ghost Text:       #6B6E73  // Tertiary (faint stamp)
Inverse:          #0A0A0B  // Text on accent
```

**Material Effects**:
```
Brushed Gradient: Linear gradient simulating brushed aluminum
Bevel Highlight:  #FFFFFF at 8% opacity on top edge
Bevel Shadow:     #000000 at 40% opacity on bottom edge
Rivet Glow:       Radial gradient for dimensional rivets
```

**Rationale**: This palette says "workshop", "forge", "machine shop". The orange accent is **hot metal** — the moment of impact, the spur driving forward. Not a safe choice. Memorable.

---

### 2. Typography — "Cut & Stamped"

**Display Face**:
- **Bebas Neue** (condensed, all-caps, industrial)
- Usage: App name only, section headers
- Weight: Bold (700)
- Letter-spacing: +40 (extra tracking, like stamped letters)
- Transform: Uppercase
- Size: 32px (app name), 11px (section headers)

**Alternative if Bebas feels too aggressive**:
- **Rajdhani SemiBold** (geometric, technical, less shouty)

**Body Face**:
- **IBM Plex Sans** (technical but readable)
- Usage: Search input, result names, descriptions
- Weight: Regular (400), Medium (500), SemiBold (600)
- Size: 16px (search), 13px (results), 11px (metadata)

**Mono Face**:
- **JetBrains Mono** (already in stack)
- Usage: File paths, keyboard shortcuts, version numbers, timestamps
- Weight: Medium (500)
- Size: 12px (body), 10px (hints)

**Type Scale**:
```
Display:   32px / Bold / +40 tracking / UPPERCASE  (app name)
Header:    11px / Bold / +20 tracking / UPPERCASE  (section labels)
Search:    16px / Medium                           (input field)
Result:    13px / SemiBold                         (result names)
Meta:      11px / Regular                          (subtitles)
Mono:      12px / Medium                           (technical data)
```

**Text Rendering**:
- Drop shadow on all text: 0 1px 2px rgba(0,0,0,0.4)
- Gives "engraved into metal" appearance
- Light text on dark = etched, dark on light = stamped

**Rationale**: Bebas Neue (or Rajdhani) creates instant differentiation. It says "industrial, mechanical, purpose-built". IBM Plex Sans pairs well — both have geometric construction. The uppercase headers mimic stamped metal labels. The text shadow makes everything feel **carved** not painted.

---

### 3. Layout — "Device Enclosure"

**Window Structure**:

```
┌─────────────────────────────────────────┐
│  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓  │  ← Bezel (4px, beveled)
│  │                                    │  │
│  │  ┌────────────────────────────┐  │  │
│  │  │  Search Slot (machined)    │  │  │  ← Inset search bar
│  │  └────────────────────────────┘  │  │
│  │                                    │  │
│  │  ╔═══════════════════════════╗  │  │
│  │  ║ RESULTS                   ║  │  │  ← Drawer section
│  │  ║ ────────────────────────  ║  │  │
│  │  ║ ▸ Visual Studio Code      ║  │  │  ← Tabs/cards
│  │  ║ ────────────────────────  ║  │  │
│  │  ║ ▸ Chrome                  ║  │  │
│  │  ╚═══════════════════════════╝  │  │
│  │                                    │  │
│  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓  │
└─────────────────────────────────────────┘
      │                         │
    Rivet                     Rivet  ← Visual detail
```

**Key Layout Principles**:

1. **Bezel Frame** (NEW)
   - 4px solid border on all sides
   - Color: Steel Mid (#3E4045)
   - Beveled appearance via gradient
   - Creates "enclosure" feeling

2. **Inset Search Bar** (NEW)
   - Appears carved INTO the surface
   - Inner shadow: 0 2px 4px rgba(0,0,0,0.6)
   - Background: Slightly darker than surface (depth)
   - No rounded corners — industrial slot

3. **Rivet Details** (NEW)
   - Small circular indicators at corners
   - 8px diameter
   - Radial gradient (highlight + shadow)
   - Purely decorative but adds character

4. **Result Drawer** (NEW)
   - Results don't float — they're in a **recessed panel**
   - Thin separator lines (1px, Steel Mid)
   - Each result = drawer tab (pull-out metaphor)
   - Hover: Tab slides right 4px (mechanical)

5. **No Window Shadow**
   - Current: Soft drop shadow
   - Rebrand: No shadow — object sits ON desktop, not above it
   - Could add very subtle contact shadow (tight, under bottom edge)

**Spacing System**:
- Grid: 4px base (unchanged)
- Padding: Tighter — 12px instead of 16px (more compact, tool-like)
- Row height: 40px instead of 44px (denser)
- Icon size: 28px instead of 32px (proportional to new row height)

**Corner Radii**:
- Window: **0px** (sharp corners, like metal plate)
- Buttons: **0px** (no soft edges)
- Search slot: **2px** (very subtle, just enough to avoid 1px artifacts)
- Everything else: **0px**

Sharp corners = industrial. This is the anti-rounded aesthetic.

---

### 4. Motion — "Mechanical Action"

**Core Principle**: Motion should feel **mechanical** not organic. No eases, no bounces. Things move with the precision of a machine.

**Window Appearance**:
```
Current: Scale from 0.96 → 1.0 with elastic ease
Rebrand: Slide DOWN from -40px with hard stop
        Duration: 120ms
        Easing: Cubic bezier(0.4, 0, 1, 1) — sharp deceleration
        Effect: Drops like a metal plate, locks into place
```

**Result Entry**:
```
Current: Fade in with scale
Rebrand: Slide RIGHT from -20px (drawer tab extending)
        Duration: 100ms per row
        Stagger: 30ms delay between rows
        Easing: Linear (mechanical precision)
        Effect: Tabs slide out from left edge in sequence
```

**Selection Change**:
```
Current: Fade background color
Rebrand: Instant snap + metallic "clink" sound effect (optional)
        Selected tab slides RIGHT 6px
        Unselected tab snaps back LEFT
        Duration: 80ms
        Easing: Step(2) — only two positions, no in-between
        Effect: Physical detent, like a mechanical selector
```

**Keyboard Shortcut Badge**:
```
Current: Static pill
Rebrand: Subtle rotation on hover (±2 degrees)
        Duration: 100ms
        Effect: Badge is a metal tag that can rotate slightly
```

**Hover States**:
```
Current: Soft fade to HoverBg
Rebrand: Instant border highlight (left edge becomes Forge Orange)
        No background change
        Duration: 0ms (instant)
        Effect: Indicator light turning on, not soft glow
```

**Rationale**: Organic motion (eases, bounces) feels natural. Mechanical motion (linear, stepped, instant) feels **built**. Every motion reinforces "this is a device."

---

### 5. Signature Element — "The Spur Indicator"

**What It Is**:
A small **rotating spur icon** (actual spur wheel) that appears in three contexts:

1. **Loading State**:
   - Replaces generic spinner
   - Spur wheel rotates (6 teeth, clicks through 60° steps)
   - Not smooth rotation — discrete clicks
   - Located: Center of result area when loading

2. **Active Search**:
   - Mini spur (16px) appears before search input cursor
   - Rotates slightly when typing (tactile feedback)
   - Color: Forge Orange

3. **Success Action**:
   - When action completes, spur does one full rotation
   - Visual "click" confirming engagement
   - Appears inline next to completed action name

**Visual Design**:
```
   ╱─╲
  │ ● │  ← 6-tooth spur wheel
   ╲─╱
    │    ← shaft
```

- 6 triangular teeth
- Central hub with rivet
- Monochrome (Steel Mid) when idle
- Forge Orange when active
- CSS: Can be SVG with transform: rotate()

**Rationale**: This is the ONE thing unique to Spur. Not a generic icon — a literal interpretation of the brand. Functional (loading indicator) and memorable (brand signature).

---

## Detailed Component Redesign

### Search Bar (The Machined Slot)

**Current**:
- Floating transparent background
- 18px Inter Regular
- Soft focus state

**Rebrand**:
```xaml
<Border Background="#19191B"  <!-- Darker than surface -->
        BorderThickness="0,1,0,1"  <!-- Top/bottom only -->
        BorderBrush="#2A2B2E"
        Height="48"  <!-- Reduced from 56 -->
        Margin="8,8,8,0">  <!-- Tight margins -->
    
    <!-- Inner shadow effect -->
    <Border.Effect>
        <DropShadowEffect Direction="90" ShadowDepth="2" 
                         BlurRadius="4" Opacity="0.6" Color="#000"/>
    </Border.Effect>
    
    <Grid Margin="16,0">
        <!-- Spur icon -->
        <Path Fill="#E8590C" Width="16" Height="16" 
              HorizontalAlignment="Left"
              Data="M8,2 L10,6 L14,6 L10,10 L12,14 L8,11 L4,14 L6,10 L2,6 L6,6 Z"/>
        
        <!-- Input -->
        <TextBox FontFamily="IBM Plex Sans"
                 FontSize="16"
                 FontWeight="Medium"
                 Foreground="#E8E9EB"
                 Margin="28,0,0,0"
                 Background="Transparent"
                 BorderThickness="0"
                 Text="{Binding Query}"/>
        
        <!-- Placeholder (when empty) -->
        <TextBlock Text="LAUNCH"
                   FontFamily="Bebas Neue"
                   FontSize="14"
                   LetterSpacing="20"
                   Foreground="#6B6E73"
                   IsHitTestVisible="False"
                   Margin="28,0,0,0"/>
        
        <!-- Keyboard hint (metal tag) -->
        <Border Background="#3E4045"
                BorderThickness="1"
                BorderBrush="#525559"
                Padding="8,4"
                HorizontalAlignment="Right"
                CornerRadius="0">
            <TextBlock Text="ESC"
                       FontFamily="JetBrains Mono"
                       FontSize="10"
                       FontWeight="Bold"
                       Foreground="#A8ACB2"/>
        </Border>
    </Grid>
</Border>
```

**Key Changes**:
- Darker inset background (looks carved in)
- Spur icon on left (brand reinforcement)
- UPPERCASE placeholder in Bebas Neue
- Metal tag for keyboard hint (not soft pill)
- Sharp corners, no radius

---

### Section Headers (Stamped Labels)

**Current**:
- 11px Inter Medium
- Subtle gray
- Simple padding

**Rebrand**:
```xaml
<Border BorderThickness="0,1,0,0"  <!-- Top rule only -->
        BorderBrush="#2A2B2E"
        Background="#1C1D1F"
        Padding="16,6,16,4">
    
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>
        
        <!-- Left rivet -->
        <Ellipse Width="6" Height="6" 
                 Fill="#525559"
                 Margin="0,0,8,0"/>
        
        <!-- Label text -->
        <TextBlock Grid.Column="1"
                   Text="APPLICATIONS"
                   FontFamily="Bebas Neue"
                   FontSize="11"
                   LetterSpacing="20"
                   Foreground="#6B6E73">
            <TextBlock.Effect>
                <DropShadowEffect Direction="90" ShadowDepth="1" 
                                 BlurRadius="1" Opacity="0.4"/>
            </TextBlock.Effect>
        </TextBlock>
        
        <!-- Count badge -->
        <Border Grid.Column="2"
                Background="#E8590C"
                Padding="6,2"
                MinWidth="24">
            <TextBlock Text="12"
                       FontFamily="JetBrains Mono"
                       FontSize="10"
                       FontWeight="Bold"
                       Foreground="#0A0A0B"
                       TextAlignment="Center"/>
        </Border>
    </Grid>
</Border>
```

**Key Changes**:
- Small rivet on left (industrial detail)
- ALL CAPS with Bebas Neue (stamped metal label)
- Drop shadow (engraved appearance)
- Count badge in Forge Orange (functional + accent)
- Top border rule (separates sections)

---

### Result Row (Drawer Tab)

**Current**:
- Soft hover fade
- 44px height
- 3px selection rail
- Rounded corners

**Rebrand**:
```xaml
<Border x:Name="ResultTab"
        Height="40"  <!-- Reduced -->
        Background="#1C1D1F"
        BorderThickness="0,0,0,1"  <!-- Bottom separator -->
        BorderBrush="#2A2B2E"
        Margin="8,0,8,0">
    
    <!-- Transform for slide animation -->
    <Border.RenderTransform>
        <TranslateTransform x:Name="TabSlide" X="0"/>
    </Border.RenderTransform>
    
    <Grid Margin="12,0,12,0">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="4"/>    <!-- Selection indicator -->
            <ColumnDefinition Width="8"/>    <!-- Gap -->
            <ColumnDefinition Width="28"/>   <!-- Icon -->
            <ColumnDefinition Width="12"/>   <!-- Gap -->
            <ColumnDefinition Width="*"/>    <!-- Text -->
        </Grid.ColumnDefinitions>
        
        <!-- Selection indicator (vertical bar) -->
        <Rectangle x:Name="SelectionBar"
                   Grid.Column="0"
                   Fill="#E8590C"
                   Opacity="0"/>
        
        <!-- Icon -->
        <Border Grid.Column="2"
                Width="28" Height="28"
                Background="#2A2B2E"
                BorderThickness="1"
                BorderBrush="#3E4045">
            <Image Source="{Binding IconPath}"
                   Stretch="Uniform"/>
        </Border>
        
        <!-- Text stack -->
        <StackPanel Grid.Column="4" VerticalAlignment="Center">
            <TextBlock Text="{Binding Name}"
                       FontFamily="IBM Plex Sans"
                       FontSize="13"
                       FontWeight="SemiBold"
                       Foreground="#E8E9EB"/>
            <TextBlock Text="{Binding Subtitle}"
                       FontFamily="IBM Plex Sans"
                       FontSize="11"
                       Foreground="#6B6E73"
                       Margin="0,2,0,0"/>
        </StackPanel>
    </Grid>
</Border>

<!-- Triggers -->
<DataTrigger Binding="{Binding IsSelected}" Value="True">
    <!-- Instant snap to right -->
    <Setter TargetName="TabSlide" Property="X" Value="6"/>
    <Setter TargetName="SelectionBar" Property="Opacity" Value="1"/>
</DataTrigger>

<EventTrigger RoutedEvent="MouseEnter">
    <BeginStoryboard>
        <Storyboard>
            <!-- Instant highlight -->
            <ColorAnimation Storyboard.TargetName="SelectionBar"
                           Storyboard.TargetProperty="Fill.Color"
                           To="#F4A261" Duration="0:0:0"/>
        </Storyboard>
    </BeginStoryboard>
</EventTrigger>
```

**Key Changes**:
- Slides right when selected (drawer tab)
- Sharp icon border (not soft shadow)
- Vertical selection bar (not 3px rail)
- Instant state changes (no fades)
- Tighter spacing (40px row, 28px icon)

---

## Typography in Context

### Example: Onboarding Screen

**Current**:
```
Spur
Spur your workflow.
A fast, minimal launcher for Windows...
```

**Rebrand**:
```
┌─────────────────────────────────────────┐
│  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓  │
│                                          │
│                                          │
│              ⚙ SPUR ⚙                   │  ← Bebas Neue, 48px
│                                          │
│         PRECISION  LAUNCHER              │  ← Bebas Neue, 14px, +40
│                                          │
│                                          │
│      Launch · Find · Execute             │  ← IBM Plex, 16px
│      Zero friction.                      │
│      Zero tracking.                      │
│                                          │
│                                          │
│      ┌──────────────────────────┐       │
│      │  SET ACTIVATION KEY      │       │  ← Metal button
│      └──────────────────────────┘       │
│                                          │
│  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓  │
└─────────────────────────────────────────┘
```

**Key Changes**:
- Spur icon (⚙ gear/spur) + name in Bebas Neue
- Tagline in all-caps Bebas with extreme tracking
- Body copy: active verbs, period-separated, minimal
- Button looks like stamped metal, not soft pill

---

## Motion Choreography

### Launch Sequence (First Open)

**Frame-by-frame**:

1. **0ms**: Screen is empty
2. **0-120ms**: Window slides DOWN from -40px to 0, hard deceleration
3. **120ms**: Window locks into place (visible "thunk")
4. **150ms**: Search slot inner shadow fades in (depth reveal)
5. **180ms**: Spur icon rotates one click (60°)
6. **200ms**: Placeholder text appears (no fade, instant)
7. **Done**: Ready for input

**Total**: 200ms from trigger to ready

Compare to current: 300ms scale-in with elastic = slower, softer

---

### Search Results Appear

**Stagger Pattern**:

1. **0ms**: First result slides RIGHT from -20px
2. **30ms**: Second result starts (first still moving)
3. **60ms**: Third result starts
4. **90ms**: Fourth result starts
5. **120ms**: All motion complete

**Effect**: Cascading drawer tabs extending from left edge

**Sound Design** (optional enhancement):
- Each result makes subtle "click" (mechanical)
- Pitched up slightly for each subsequent row
- Creates ascending tone sequence
- Reinforces physicality

---

## Color Usage Map

| Element | Background | Border | Text | Accent |
|---------|------------|--------|------|--------|
| Window Frame | Charcoal | Steel Mid (beveled) | — | Rivet Silver (corners) |
| Search Bar | Void Black | Steel Mid | Engraved Text | Forge Orange (icon) |
| Section Header | Charcoal | Steel Mid (top) | Ghost Text (shadow) | Forge Orange (count) |
| Result Row | Charcoal | Steel Dark (bottom) | Engraved Text | Forge Orange (selection) |
| Result (hover) | Charcoal | Spark Yellow (left) | Engraved Text | — |
| Result (selected) | Charcoal | Forge Orange (left) | Engraved Text | — |
| Keyboard Hint | Steel Mid | Brushed Metal | Stamped Text | — |
| Button | Steel Dark | Brushed Metal | Engraved Text | Forge Orange (hover) |

**Accent Usage Rules**:
- Forge Orange: Active states, primary actions, brand moments
- Spark Yellow: Warnings, hover previews
- Copper Patina: Success confirmations
- Rivet Silver: Decorative details only

**Rationale**: Restrained accent usage. Orange only appears when meaningful (selection, action, brand). Not scattered everywhere.

---

## Copy Voice

### Principles

1. **Imperative mood**: Commands, not suggestions
   - ✓ "Launch" not "You can launch"
   - ✓ "Execute" not "Run your command"

2. **Technical precision**: Exact terms
   - ✓ "Set activation key" not "Choose your shortcut"
   - ✓ "Index depth" not "How deep to search"

3. **No apologies**: Errors are statements
   - ✓ "Path not found." not "Sorry, we couldn't find that"
   - ✓ "Invalid input." not "Hmm, that doesn't look right"

4. **Tool language**: It's a device, not a friend
   - ✓ "Ready" not "What can I help you with?"
   - ✓ "Standby" not "Waiting for you..."

### Example Copy Updates

| Current | Rebrand |
|---------|---------|
| "Welcome to Spur" | "SPUR — PRECISION LAUNCHER" |
| "How will you summon Spur?" | "SET ACTIVATION KEY" |
| "Search" (placeholder) | "LAUNCH" |
| "No results found" | "NO MATCH." |
| "Settings" | "CONFIGURATION" |
| "Clear clipboard" | "PURGE HISTORY" |

**Tone**: Terse. Exact. No personality, just function.

---

## Implementation Phases

### Phase 1: Foundation (8 hours)

1. **Color System**
   - Replace all theme colors
   - Add material effect gradients
   - Update semantic mappings

2. **Typography**
   - Load Bebas Neue, IBM Plex Sans
   - Create new type scale tokens
   - Add text shadow effects globally

3. **Window Frame**
   - Add 4px bezel border
   - Remove window shadow
   - Add corner rivets (decorative)

4. **Remove All Radii**
   - Set all CornerRadius to 0
   - Search slot only: 2px

**Deliverable**: Shell looks different but non-functional changes

---

### Phase 2: Components (12 hours)

5. **Search Bar Redesign**
   - Inset appearance (inner shadow)
   - Add spur icon
   - New placeholder treatment
   - Metal tag keyboard hint

6. **Section Headers**
   - All-caps Bebas treatment
   - Add rivet detail
   - Count badges

7. **Result Rows**
   - Reduce height to 40px
   - Sharp icon borders
   - Slide-right selection
   - Remove hover fade

8. **Spur Icon/Indicator**
   - Design SVG spur wheel
   - Add to loading states
   - Active search indicator

**Deliverable**: All components match new aesthetic

---

### Phase 3: Motion (6 hours)

9. **Window Animation**
   - Slide-down entry
   - Hard deceleration

10. **Result Animations**
    - Drawer slide-right
    - Staggered cascade
    - Step-based selection

11. **Micro-interactions**
    - Instant state changes
    - Badge rotation on hover
    - Spur wheel clicks

**Deliverable**: Motion feels mechanical

---

### Phase 4: Polish (4 hours)

12. **Copy Updates**
    - All-caps labels
    - Terse error messages
    - Tool-focused voice

13. **Material Details**
    - Brushed metal gradients
    - Bevel highlights/shadows
    - Rivet glow effects

14. **Sound Design** (optional)
    - Mechanical click sounds
    - Selection "clunk"
    - Result cascade tones

**Deliverable**: Complete rebrand shipped

**Total**: ~30 hours (aggressive estimate)

---

## Before/After Summary

### Before (Current)

**Aesthetic**: Soft, ethereal, polished  
**Palette**: Warm beige accent, gentle grays  
**Typography**: Inter (neutral, invisible)  
**Motion**: Organic eases, smooth fades  
**Feeling**: Floating window, typical launcher  
**Personality**: Professional, safe  
**Distinctiveness**: 3/10

"A well-executed launcher. Could be any launcher."

---

### After (Rebrand)

**Aesthetic**: Hard, industrial, machined  
**Palette**: Gunmetal + forge orange  
**Typography**: Bebas Neue (bold, stamped) + IBM Plex  
**Motion**: Mechanical snaps, linear slides  
**Feeling**: Physical device sitting on the desktop
**Personality**: Precise, terse, uncompromising
**Distinctiveness**: 9/10

"This is Spur. Nothing else looks like this."

---

## Self-Critique

### Does any part of this read as AI-default?

**Checklist**:
- Warm cream background? ❌ No. Deep gunmetal.
- Acid green on near-black? ❌ No. Forge orange on charcoal.
- Broadsheet newspaper layout? ❌ No. Device enclosure.
- Rounded corners everywhere? ❌ No. Zero radius.
- Inter/SF Pro as default? ❌ No. Bebas Neue + IBM Plex.
- Smooth elastic animations? ❌ No. Mechanical linear snaps.
- Generic placeholder text? ❌ No. "LAUNCH" in all-caps.

**Verdict**: None of the three AI defaults appear. Every choice was made for this brief specifically.

### What I'd Remove (Chanel Edit)

Before shipping, cut one accessory:

**Remove**: The sound design (optional mechanical clicks). It adds novelty the first time and becomes noise by the tenth. Keep the motion — lose the sound. The visual snap is enough.

**Remove**: The `⚙` spur icon flanking the app name in onboarding. It reads kitsch. The Bebas Neue all-caps alone is strong enough. Let the typography carry the moment.

---

## Open Questions for the Client

1. **Forge Orange vs. something cooler?**
   - Orange is warm, energetic, aggressive — is that the right energy?
   - Alternative: Electric blue `#3A7BD5` (cooler, more "precision instrument")
   - Alternative: Phosphor green `#39D353` (terminal heritage)
   - Recommendation: Forge Orange — it's the unexpected choice for a dark tool UI, which is why it works

2. **Zero radius on window corners?**
   - Sharp corners look great in screenshots; on a transparent WPF window they can produce 1px artifacts at some DPI levels
   - Safe compromise: 2px instead of 0px — visually indistinguishable from sharp but DPI-safe

3. **Bebas Neue licensing?**
   - Free, OFL licensed — safe to bundle
   - IBM Plex Sans — also free, OFL licensed — safe to bundle
   - Both load from packed resources, no web request needed

4. **Rebrand scope: Launcher only, or also settings/onboarding?**
   - Recommendation: Launcher first, onboarding second, settings last
   - The launcher is the daily surface — highest ROI
   - Settings is visited once; the hardware aesthetic is less critical there

5. **Light theme?**
   - The hardware aesthetic works best dark. Light version would need a separate direction.
   - Option: Brushed aluminum — light gray `#E8E9EB` surface, darker gray text, orange accent
   - Not the priority; dark first.

---

## Light Theme Direction (if needed)

> Not the primary direction — note for later.

**Concept**: "Machined Aluminum"
- Surface: `#E8E9EB` (brushed aluminum, not white)
- Text: `#1C1D1F` (dark charcoal)
- Border: `#C4C6CA` (metal edge)
- Accent: `#E8590C` (same forge orange — consistent across themes)
- Separator: `#D4D6DA`

**Key difference from dark**: Looks like unpainted aluminum plate rather than gunmetal. The forge orange pops more on the lighter surface. Same zero-radius, same Bebas Neue, same IBM Plex.

---

## Visual Reference Board

What to look at for aesthetic calibration:

**Earn it from these references, don't copy them**:

- **Teenage Engineering OP-1** — minimal hardware UI, industrial texture, color pops on dark
- **Dieter Rams Braun radios** — zero decoration, everything functional, the grid IS the design
- **Focusrite Scarlett interface** — red + charcoal, metal knobs, precision labels
- **1970s Swiss airport signage** — Helvetica or Univers, all-caps, high contrast, total legibility
- **Leica M rangefinder** — brass fittings on matte black, no ostentation, pure tool

**What to borrow**:
- The commitment to surface texture (Braun, Focusrite)
- The color discipline — one accent, used sparingly (Leica, Braun)
- The typography confidence — type that owns its size (airport signage)
- The zero-decoration principle — if it doesn't inform, remove it (all of the above)

---

## Token System — Full Replacement

Replace `Tokens.xaml` and both theme files with:

### Tokens.xaml (new — geometry only)

```xml
<!-- Typography -->
<FontFamily x:Key="Token.Font.Display">pack://application:,,,/Themes/Fonts/#Bebas Neue</FontFamily>
<FontFamily x:Key="Token.Font.Body">pack://application:,,,/Themes/Fonts/#IBM Plex Sans, Segoe UI Variable</FontFamily>
<FontFamily x:Key="Token.Font.Mono">Cascadia Code, JetBrains Mono, Consolas</FontFamily>

<!-- Font sizes -->
<sys:Double x:Key="Token.FontSize.Display">32</sys:Double>    <!-- App name -->
<sys:Double x:Key="Token.FontSize.Header">11</sys:Double>    <!-- Section labels (all-caps) -->
<sys:Double x:Key="Token.FontSize.Search">16</sys:Double>    <!-- Input -->
<sys:Double x:Key="Token.FontSize.Result">13</sys:Double>    <!-- Result name -->
<sys:Double x:Key="Token.FontSize.Meta">11</sys:Double>      <!-- Subtitle -->
<sys:Double x:Key="Token.FontSize.Badge">10</sys:Double>     <!-- Count, hints -->

<!-- Spacing -->
<sys:Double x:Key="Token.Spacing.2">2</sys:Double>
<sys:Double x:Key="Token.Spacing.4">4</sys:Double>
<sys:Double x:Key="Token.Spacing.6">6</sys:Double>
<sys:Double x:Key="Token.Spacing.8">8</sys:Double>
<sys:Double x:Key="Token.Spacing.12">12</sys:Double>
<sys:Double x:Key="Token.Spacing.16">16</sys:Double>
<sys:Double x:Key="Token.Spacing.24">24</sys:Double>
<sys:Double x:Key="Token.Spacing.32">32</sys:Double>

<!-- Layout -->
<sys:Double x:Key="Token.Layout.SearchBarHeight">48</sys:Double>   <!-- Down from 56 -->
<sys:Double x:Key="Token.Layout.RowHeight">40</sys:Double>          <!-- Down from 44 -->
<sys:Double x:Key="Token.Layout.RowHeightTall">48</sys:Double>
<sys:Double x:Key="Token.Layout.IconSize">28</sys:Double>           <!-- Down from 32 -->
<sys:Double x:Key="Token.Layout.BezelThickness">4</sys:Double>      <!-- NEW -->
<sys:Double x:Key="Token.Layout.WindowWidth">600</sys:Double>       <!-- Slightly narrower -->

<!-- Radii — almost none -->
<CornerRadius x:Key="Token.Radius.Window">0</CornerRadius>      <!-- Sharp -->
<CornerRadius x:Key="Token.Radius.Row">0</CornerRadius>         <!-- Sharp -->
<CornerRadius x:Key="Token.Radius.Button">0</CornerRadius>      <!-- Sharp -->
<CornerRadius x:Key="Token.Radius.Slot">2</CornerRadius>        <!-- Search inset -->
<CornerRadius x:Key="Token.Radius.Badge">2</CornerRadius>       <!-- Count badge -->
```

### DarkTheme.xaml (new — color only)

```xml
<!-- Surfaces -->
<SolidColorBrush x:Key="Void"           Color="#0A0A0B"/>
<SolidColorBrush x:Key="Depth1"         Color="#1C1D1F"/>   <!-- Primary surface -->
<SolidColorBrush x:Key="Depth2"         Color="#2A2B2E"/>   <!-- Raised -->
<SolidColorBrush x:Key="Depth3"         Color="#3E4045"/>   <!-- Borders -->
<SolidColorBrush x:Key="Depth4"         Color="#525559"/>   <!-- Inactive -->
<SolidColorBrush x:Key="SearchSlot"     Color="#14141A"/>   <!-- Inset search -->

<!-- Text -->
<SolidColorBrush x:Key="TextPrimary"    Color="#E8E9EB"/>
<SolidColorBrush x:Key="TextSecondary"  Color="#A8ACB2"/>
<SolidColorBrush x:Key="TextMuted"      Color="#6B6E73"/>
<SolidColorBrush x:Key="TextInverse"    Color="#0A0A0B"/>

<!-- Borders -->
<SolidColorBrush x:Key="BorderBrush"    Color="#2A2B2E"/>
<SolidColorBrush x:Key="BorderStrong"   Color="#3E4045"/>
<SolidColorBrush x:Key="Separator"      Color="#232427"/>

<!-- Accent -->
<SolidColorBrush x:Key="Accent"         Color="#E8590C"/>   <!-- Forge orange -->
<SolidColorBrush x:Key="AccentHover"    Color="#F4A261"/>   <!-- Spark yellow -->
<SolidColorBrush x:Key="AccentWash"     Color="#1AE8590C"/>
<SolidColorBrush x:Key="AccentGhost"    Color="#0DE8590C"/>

<!-- Selection -->
<SolidColorBrush x:Key="SelectedBg"     Color="#1C1D1F"/>   <!-- No fill change -->
<SolidColorBrush x:Key="HoverBg"        Color="#1C1D1F"/>   <!-- No fill change -->

<!-- Semantic -->
<SolidColorBrush x:Key="Green"          Color="#6B8E7F"/>   <!-- Copper patina -->
<SolidColorBrush x:Key="Orange"         Color="#F4A261"/>   <!-- Spark yellow -->
<SolidColorBrush x:Key="Red"            Color="#C45C5C"/>

<!-- Material -->
<SolidColorBrush x:Key="BevelHighlight" Color="#14FFFFFF"/>  <!-- Top edge -->
<SolidColorBrush x:Key="BevelShadow"    Color="#66000000"/>  <!-- Bottom edge -->
<SolidColorBrush x:Key="RivetColor"     Color="#525559"/>
```

---

## Summary

This rebrand has one job: make Spur look like nothing else in the launcher space.

Every decision traces back to the same source — **a spur is a machined metal tool**. The UI should feel like one:

- **Color**: Gunmetal workshop, one hot forge accent
- **Typography**: Stamped metal labels (Bebas Neue) over technical body (IBM Plex)
- **Shape**: Zero radius, sharp edges, inset slots
- **Motion**: Mechanical snaps, drawer slides, no organic easing
- **Voice**: Imperative, terse, tool-language
- **Signature**: Rotating spur wheel at loading and active states

The aesthetic risk is going zero-radius and industrial in a space where every product is soft and glassy. The risk pays off because the brand name itself is the brief — and the brief demands metal, precision, and force.

---

**Status**: Ready for review and sign-off.
**Next**: On approval, implement Phase 1 (Foundation — 8 hours) first to validate the aesthetic before committing to full component work.