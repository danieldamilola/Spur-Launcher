# Deep Research: UI/UX Design
*General principles + desktop/launcher app design — compiled for AI ingestion*

---

## Part 1 — The Foundation: What Design Actually Is

Design is not decoration. It is not making things pretty. Design is the process of solving a specific user problem while making the solution easy and enjoyable to use. Every visual decision — color, spacing, type, layout — is a communication decision. If a user has to stop and think, the design has failed.

The most expensive design mistake you can make is solving the wrong problem beautifully. Before touching color or layout, you must understand: what is the user trying to accomplish, and what is the fastest path to that outcome?

**The distinction between UI and UX:**
- UX (user experience) determines *what* the product should do and *why*.
- UI (user interface) determines *how* it looks and behaves while doing it.
- A product can have a beautiful UI but poor UX (looks great, confusing to use), or excellent UX with mediocre UI (everything works, but feels dated).
- Both matter. Neither replaces the other.

---

## Part 2 — The 7 Core UX Principles

### 1. User-Centricity
Every single design decision must be anchored to validated user needs, not assumptions or preferences. This is the most fundamental principle — every other principle exists in service of this one. Without it, even perfectly consistent, accessible, visually polished products fail because they solve the wrong problem.

In practice: before adding a feature, ask "does this help the user accomplish their goal faster?" If not, cut it.

### 2. Hierarchy
Users don't read interfaces — they scan them. Visual hierarchy is the intentional arrangement of elements so users encounter the most important content first, without consciously deciding where to look.

The tools for creating hierarchy are: **size, weight, color, contrast, spacing, alignment, and position**. They work in combination, never individually. Nielsen Norman Group eye-tracking research found users spend about 80% of their viewing time on content above the fold — vertical placement is one of your most powerful tools.

The two signals that do most of the heavy lifting: the size difference between levels, and the whitespace separation between sections.

**What hierarchy looks like in practice:**
- Large, bold heading → clear signal this is most important
- Smaller subheading → secondary importance
- Regular body text → details
- Muted/gray text → metadata, hints
- Disabled/invisible → not relevant right now

If everything is emphasized, nothing is. Use no more than 3 contrast variations. If everything is contrasted, nothing stands out.

### 3. Consistency
Repeat patterns. Same component, same behavior, everywhere. Users build a mental model of how your interface works — the moment an element behaves differently than they expect, you've broken trust and introduced cognitive load.

Consistency applies to: typography scale, spacing rhythm, button shapes and states, interaction patterns, icon style, color meanings. Linear is cited as one of the best examples of this — the same type scale, spacing rhythm, and button styles repeat across every surface.

**The danger of inconsistency:** Users will eventually stop trusting the interface. "Will this button do the same thing here as it did there?" is a question your UI should never prompt.

### 4. Feedback and Affordance
Every interactive element must communicate that it is interactive, and every action must have a response. Affordance means an element looks like what it does — a button looks pressable, a text field looks editable.

Feedback means the system tells the user their action was received. Without feedback, users repeat actions, get confused, or lose trust.

Types of feedback: visual state changes (hover, active, loading, success, error), micro-animations, status indicators, error messages, confirmation states.

### 5. Simplicity (Cognitive Load)
The goal is not to make the UI simple — it is to make it feel effortless. Simplicity is achieved by removing everything that doesn't earn its place, not by dumbing things down.

Cognitive load is the mental effort required to use an interface. Every option, every visual element, every label adds to it. The best interfaces are ruthlessly edited — they show only what the user needs at this moment, in this context.

**Hick's Law:** The time it takes to make a decision increases with the number and complexity of choices. Fewer options → faster decisions → better experience.

### 6. Accessibility
Accessible design is good design. It is not an afterthought. WCAG AA standard: minimum 4.5:1 contrast ratio for body text. Never rely solely on color to communicate information — some users are colorblind. Support keyboard navigation. Use sufficient font sizes.

The practical bonus: accessible design almost always produces cleaner, clearer interfaces for all users.

### 7. User-Centered Iteration
Design is not done when it ships. Good design is an ongoing loop of releasing, observing, learning, and improving. The best products are built by teams that treat design as a continuous process, not a phase.

---

## Part 3 — Visual Hierarchy in Depth

Visual hierarchy is the single most impactful design skill. Get it right and everything else becomes easier.

### The Five Levers

**Size** — The most immediate signal of importance. Bigger = more important. But "bigger" is relative; it only works when there is a clear size difference between levels. A 2px difference is invisible. A 4pt difference in type size reads as the same level. Make the jumps meaningful.

**Color and Contrast** — High contrast elements draw the eye first. Limit your palette: 2 primary colors, 2 secondary colors maximum for an uncomplicated design. Do not use too many contrast variations — use no more than 3 in complex designs. If everything is high contrast, nothing stands out.

**Spacing (Whitespace)** — Elements with more breathing room command more attention. An element surrounded by space appears more important than the same element packed in with others. Gestalt psychology confirms that proximity implies relationship: elements close together are perceived as related.

**Weight (Typography)** — Bold text reads as more important. Use only 2 weights: 400 regular, 500 medium. Never use 600 or 700 in UI — they look heavy and break visual rhythm. Save bold for true emphasis; if you use it everywhere, it loses meaning.

**Position** — Top-left is where eyes go first in left-to-right cultures. Elements at the top of a layout are perceived as higher in the hierarchy. Put the most important thing where eyes naturally land.

### Grouping and Proximity
Use space to group related content. The gap between groups should be noticeably larger than the gap within a group. This is Gestalt's law of proximity — you don't need borders or lines if spacing communicates structure. Borders and dividers are often a sign that spacing isn't working.

### The Scan Test
When your design is done, squint at it. If you can identify the most important element while squinting, your hierarchy works. If everything blurs into equal importance, it doesn't.

---

## Part 4 — Typography: The Silent Backbone

Typography is the most impactful single element in making a UI feel premium or cheap. Most UIs look unprofessional because of typography decisions, not color or layout.

### The Rules

**Font choice:** Use sans-serif for UI on screens. Serif fonts are for reading long text or editorial design. In a desktop app, especially on Windows, stick with system fonts (Segoe UI) or a clean sans like Inter, IBM Plex Sans, or DM Sans. Native system fonts guarantee correct rendering on all display configurations.

**Scale:** Use a limited, intentional type scale. Jumping from size to size randomly creates visual noise. A solid scale: 11px (hints/captions), 13px (metadata/secondary), 14–15px (UI labels/results), 16–18px (body), 20–22px (subheadings), 28–36px (headings). Every size should have a clear job. Nothing in between.

**Weights:** Two only — regular (400) and medium (500). Using 600+ weight creates heaviness in UI. The weight difference alone is enough to communicate hierarchy when combined with size.

**Line height (leading):**
- Body text: 1.4–1.6x the font size
- Headings: 1.1–1.3x the font size
- UI labels in tight components: 1.2–1.4x

Too tight feels cramped. Too loose breaks the reading flow. Line height is the difference between text that breathes and text that suffocates.

**Letter spacing (tracking):**
- Body text: 0 (no adjustment)
- Headings at larger sizes (28px+): slight negative (-0.02em to -0.03em) — makes large text feel tighter and more deliberate
- Small caps, category labels, metadata in uppercase: positive (+0.05em to +0.12em) — makes small caps readable

**Contrast:**
The WCAG AA minimum is 4.5:1 for body text. In practice, don't use gray text on gray backgrounds. A common mistake is making secondary text too light — it disappears on low-quality displays, in bright ambient light, and for users with lower visual acuity.

**Text alignment:** Left-align UI text. Center-align only for short headings or callouts. Never justify text in UI — it creates irregular word spacing and disrupts reading flow.

**Paragraph spacing:** Space below a paragraph should be roughly equal to the body font size. At 14px body, use 14px spacing below each paragraph. The gap between a heading and its body text should be tighter than the gap between two separate sections — the heading belongs to the content that follows.

**The premium test:** Large headings at 28px or above should have tight letter spacing (-0.02 to -0.04em). This is one of the smallest changes with the most noticeable premium effect. It makes type feel designed, not default.

---

## Part 5 — Color Theory for UI

### The Fundamentals

Color in UI is not decoration — it is communication. Every color choice should serve a purpose:
- **Primary:** your brand accent, used for the most important interactive elements
- **Secondary:** supporting accent, used sparingly
- **Neutral:** backgrounds, surfaces, borders, disabled states — the majority of your UI
- **Semantic:** green = success, red = error/danger, yellow/amber = warning, blue = information

The most common mistake: using color decoratively (making something colorful because it looks nice) instead of semantically (using color to convey meaning).

### Limits
- 2 primary colors, 2 secondary colors maximum for uncomplicated designs
- If everything is colorful, nothing stands out
- When too many colors of similar value or saturation are used, users' perception of hierarchy is reduced
- Never rely on color alone to communicate meaning — always pair it with text or iconography

### Choosing a Palette
Start neutral. 90–95% of most UI surfaces should be neutral (dark grays, light grays, near-whites, near-blacks). Your accent color appears only where it matters: selected states, primary actions, active indicators.

Warm accents (amber, gold, soft orange) feel crafted and distinctive. Cold accents (blue) feel familiar and safe. The choice shapes the emotional tone of your product.

**Dark mode vs. light mode:** Both are valid. If you support dark mode (you should), define your color system in tokens that switch automatically. Never hardcode hex values — always use semantic references.

### Saturation and Value
The most beautiful UI palettes are desaturated — slightly muted versions of colors, not primary-color-wheel bright. Pure #FF0000 red looks childish in UI; a slightly desaturated #E24B4A looks polished. Desaturation makes colors feel intentional rather than default.

---

## Part 6 — Spacing: The Foundation of Quality

Spacing is how you communicate that your UI is well-made. Cramped spacing feels cheap. Generous spacing feels premium. But too much spacing wastes real estate and breaks visual groups.

### The 8pt Grid
The most widely used spacing system: all spacing values are multiples of 8. This creates rhythm and prevents arbitrary spacing decisions. The scale: 4, 8, 12, 16, 24, 32, 48, 64.

4px for tight internal spacing (between icon and label)
8px for standard internal component padding
12–16px for component padding
24px between sections
32–48px between major layout sections

**Why it works:** Components that align to an 8pt grid line up across your entire UI without manual adjustment. It creates visual harmony without conscious effort.

### The Material Design baseline: minimum 16px padding inside components, 24px between sections.

### Padding vs. Margin
- Padding is space inside a container
- Margin is space outside a container
- Use padding to create breathing room within components
- Use margin/gap to separate components from each other

**The rule about text near edges:** Never let text sit right against a container edge. Minimum 16px padding on desktop, 20–24px on wider surfaces.

### Whitespace is not wasted space
Every pixel of empty space is doing work — it guides the eye, creates groups, suggests relationships, and communicates quality. Attempting to "use up" all available space is one of the most common beginner design mistakes.

An A/B test on an analytics dashboard with the same content but 40% more whitespace consistently resulted in improved comprehension and user satisfaction. The content didn't change. The space did.

---

## Part 7 — Design Systems and Design Tokens

### What a Design System Is
A design system is a collection of reusable components guided by clear standards that can be assembled to build consistent products. It is not a style guide — it is a living system that evolves with the product.

A design system prevents: inconsistent spacing, colors drifting between screens, components looking different on different pages, and the "each new screen is a snowflake" problem.

### Design Tokens: The Atomic Foundation

Design tokens are named entities that store visual design attributes — colors, spacing, typography, corner radii, animation durations. They are the source of truth that bridges design and code.

**The three-level hierarchy:**

1. **Primitive tokens** (raw values):
   - `color-blue-500: #3B82F6`
   - `spacing-4: 16px`
   - `font-size-md: 14px`
   These are the raw material. Never used directly in components.

2. **Semantic tokens** (usage-specific references):
   - `color-primary: var(--color-blue-500)`
   - `color-text-secondary: var(--color-gray-400)`
   - `spacing-component-padding: var(--spacing-4)`
   These have names that describe their purpose, not their value.

3. **Component tokens** (per-component specifics):
   - `button-background: var(--color-primary)`
   - `input-border-color: var(--color-border-default)`
   These are scoped to specific components.

**Why this matters:** When you need to change your primary accent color, you update one token. Every component that references it updates automatically. Without tokens, you hunt through every file looking for instances of a hardcoded hex.

**The biggest mistake:** Starting with components instead of tokens. Teams that build buttons and cards first, then discover inconsistent colors and spacing six months later. Start with tokens. Everything else is built on top.

---

## Part 8 — Desktop and Launcher-Specific Design

### The Unique Rules of Desktop Apps

Desktop apps are fundamentally different from web or mobile:
- Users are in **power mode** — they are trying to work, not explore
- **Keyboard is primary** — mouse is secondary
- **Speed is everything** — every extra click or visual noise is friction
- **Context switching is expensive** — a bad UI interrupts workflow
- **Precision is possible** — desktop users have mice, which means smaller touch targets are acceptable (still not smaller than 24x24px)

### What Makes a Launcher Premium

The best launchers (Spotlight, Raycast, Wox) share these qualities:

**Single surface, no navigation.** There is one view. There are no pages, no menus to navigate, no back button. Every feature is accessible through the single search input. This is the most important principle of launcher design — the moment you add a second screen that requires navigation, you've made it slower.

**The best interface is no interface.** Raycast's design philosophy: you think of a task, you type it, it is done before you reach for the mouse. The goal is to get out of the user's way as fast as possible.

**Results are hierarchical, not equal.** Not all results have the same importance. Apps the user opens most should rank higher. Recent actions should float to the top. Exact matches beat partial matches. A launcher with no frequency-based ranking is a worse launcher.

**Only show what's needed, when it's needed.** Inactive results show minimal information (just the name). The selected result shows full detail. This is progressive disclosure — reveal information at the moment it becomes relevant, not before.

**Keyboard shortcuts are visible.** The user should always know what they can do next. Keyboard hints at the bottom (↑↓ navigate, ↵ open, ⌘+K actions) remove the need to memorize.

**Animations are fast or nonexistent.** Launcher animations should complete in 80–150ms. Anything slower feels laggy. The window appearing should feel instant. Result transitions should feel instant. Animation is only acceptable if it aids perception, never if it adds wait time.

### What Makes a UI Feel Minimal and Premium (Not Just Empty)

Minimal does not mean removing everything. It means removing everything that does not earn its place.

**The editing test:** For every element on a screen, ask "what would be lost if this were removed?" If the answer is "nothing," remove it.

**Restraint in detail:** Premium interfaces are characterized by attention to small things — the exact padding value, the precise font weight, the half-pixel border, the carefully chosen letter spacing. None of these are visible individually. Together they create the feeling of quality.

**Less color, more intention:** The most premium UIs use near-neutral palettes with a single accent. They feel calm and focused. Every time color appears, it means something. When color is used for decoration, it loses its signal value.

**Density and breathing room balance:** "Minimal" does not mean maximum whitespace. It means the right amount. Overly spacious UIs feel empty and waste real estate. The right density depends on context: a text editor needs breathing room; a data table needs density.

**Consistency creates trust:** When every result row behaves the same way, every spacing value comes from the same scale, every color comes from the same palette — the UI feels made rather than assembled. That feeling is what "premium" actually is. It is not a quality added at the end; it is the product of discipline applied throughout.

### The Vocabulary of Quality in Launcher Design

**Typography as structure:** In a text-dense launcher, typography is doing all the work. Name vs. subtitle is a hierarchy communicated by size, weight, and color alone — no icons required. The difference between a professional and an amateur launcher is often just the type scale.

**Subdued color palette:** Dark backgrounds work better for launchers because they create less ambient visual noise and help results stand out. The surface should feel like a stage, not a painting.

**Borders and separators used sparingly:** Group separators should be nearly invisible — a 0.5px line at very low opacity. If you find yourself adding borders everywhere to create structure, it's usually a spacing problem in disguise.

**Iconography:** Icons help users scan faster, but only when they add information. Generic document icons for every file type are worse than no icons. Specific, recognizable icons (VSCode's icon, a browser's icon) are useful. If you can't get high quality, distinctive icons — text-only is cleaner.

**The selected state:** This is the most important state in a launcher. It must be unmistakable. Not subtle. The user's eye must snap to it. But "unmistakable" does not mean "bright" — a strong left border accent combined with a name color shift reads as clearly selected without visual noise.

---

## Part 9 — Building Your Own Design Identity

Copying a product's visual style gets you something that looks like that product, not something that feels like yours. Visual identity comes from constraint, consistency, and decisions made at the foundation level.

### The Steps

**1. Choose one accent color and commit to it.** Not a palette — one color. Everything else is neutral. Your accent appears only where it earns its place: selected states, primary actions, key indicators. This constraint forces you to be intentional about where color appears.

**2. Define your type scale once, at the start.** Pick 5–6 sizes. Give each a name. Use them everywhere. Never add a new size without asking whether an existing one works.

**3. Choose one corner radius and stick to it.** A product that mixes sharp corners, 4px radius, 8px radius, and circular elements feels undesigned. Pick one. (4–8px for most desktop apps; larger feels mobile/soft; sharp feels technical).

**4. Define a spacing unit.** 8pt grid. Everything is a multiple of 4 or 8. If something needs to be 11px, ask whether 8 or 12 works.

**5. Write your design principles in plain language.** "Fast above beautiful." "Text first, icons second." "Show only what's needed now." These principles become the filter through which every decision passes. When you're unsure whether to add a feature, whether to add a visual element, whether to add an animation — you check it against the principles.

### What "Own Style" Actually Means

It doesn't mean unusual or original. The best "own style" is often extremely familiar in structure (users already know how to use it) but distinctive in its details — the specific warmth of the accent color, the exact way the selected state feels, the tightness of the typography. These micro-decisions add up to something that feels like it was made by someone with a point of view.

The most distinctive product UIs in the world (Linear, Notion, Raycast, Arc) don't look unusual — they look considered. Every element is in the right place for an intentional reason, and the product doesn't add anything it doesn't need.

---

## Part 10 — The Design Process for a Solo Builder

If you are building alone, without a designer, here is a repeatable process:

**Before opening any design tool:**
- Write down what the user is trying to do (not what you want to build)
- Write down the 3 most common actions they take in a session
- Ask: what would a perfect 0-click experience look like? (then design toward that even if you can't reach it)

**Start with structure, not style:**
- Get the layout right with no color, no icons, no polish
- If the layout doesn't work in black and white, color won't fix it

**Typography first, color second:**
- Set your type scale and hierarchy before adding any color
- Most of the "this looks professional" feeling comes from typography, not color

**Add color last:**
- Start neutral-only: black, white, three grays
- Add your accent color only when you know exactly where it belongs
- The fewer times color appears, the more it means each time

**Test it at the interaction level:**
- Click through it. Does it feel slow? Do you need to think to navigate it?
- If you hesitate even once, find out why and fix it

**The final edit pass:**
- Remove everything that doesn't earn its place
- Ask "what would be lost if this element were invisible?" for every element
- Tighten spacing. Soften colors. Check every font size against your scale.

---

## Summary: The 20 Rules

1. Design solves problems — beauty is a by-product of good problem solving
2. If the user has to think, something is wrong
3. Hierarchy is more important than decoration
4. Typography sets the tone of quality before anything else
5. Whitespace is structure, not emptiness
6. Limit colors — 2 primary, 2 secondary, everything else neutral
7. One accent color used intentionally beats five colors used decoratively
8. Size, weight, and color must work together to communicate hierarchy
9. Spacing should come from a single scale (8pt grid)
10. Consistency creates trust; inconsistency creates doubt
11. Progressive disclosure: show detail when it's relevant, not before
12. The best launcher interface gets out of the way instantly
13. Keyboard-first design means every action has a shortcut
14. Animations must be fast (< 150ms) or absent
15. Design tokens before components — always
16. Your type scale has 5–6 sizes maximum; define them once, use them everywhere
17. The selected state must be unmistakable, not subtle
18. Borders signal a spacing problem — fix the spacing first
19. Premium is not a quality added at the end — it is discipline applied throughout
20. Your own style comes from constraints consistently applied, not from unusual choices
