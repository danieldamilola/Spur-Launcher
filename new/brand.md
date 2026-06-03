# brand.md — Spur Identity

> Spur is not “Spotlight for Windows.” It is a **native-feeling command surface** with its own calm voice—borrowed discipline from Apple, owned visual character from the Spur mark.

---

## 1. Brand essence

| Pillar | Meaning for Spur |
|--------|------------------|
| **Quiet** | UI speaks in whispers: low contrast borders, no shouting badges |
| **Immediate** | Open → type → act in one breath; motion is quick, never playful |
| **Considered** | Every margin is intentional; nothing “default WPF” shows through |
| **Honest** | Windows app, Windows shortcuts, Windows materials—no fake macOS chrome |

**Tagline (internal):** *Find it. Do it. Leave.*

---

## 2. The mark (`Icons/`)

The launcher icon set (`spur-launcher-*.ico`) is the **only** full-color brand artifact in the product.

### What the mark communicates (design intent)

Read the 256px master as:

- **Field:** deep, neutral graphite (not pure black—soft depth)
- **Stroke:** a single confident **Spur**—bright, slightly warm highlight (think moonlit edge, not neon)
- **Shape language:** continuous curve, no sharp logo clutter; reads at 16px in the tray

### Rules

| Rule | Detail |
|------|--------|
| **Shell vs UI** | `.ico` → taskbar, tray, installer, About. Never scale the mark inside the search bar. |
| **In-app glyph** | Use a **1.5px stroke** Lucide-style icon set, rounded caps—same weight as the Spur stroke *feeling* |
| **No duplicates** | Don’t place the app icon next to every result; app icons come from the OS. |
| **Clear space** | Minimum padding around the mark = ½ Spur radius in marketing; in UI, 8px. |

### Sampling accent from the icon (implementation)

1. Open `Icons/spur-launcher-256x256.ico` in a color picker.
2. Sample the **brightest stroke** along the Spur → `Brand.Highlight`
3. Sample the **deepest field** → `Brand.Field`
4. Derive `Brand.Accent` = Highlight at **72% luminance** (slightly subdued for UI, never full neon)

**Fallback tokens** (if sampling not done yet)—neutral brand, not generic blue:

| Token | Hex | Role |
|-------|-----|------|
| `Brand.Field` | `#121214` | Icon background family |
| `Brand.Highlight` | `#E8E4DF` | Spur stroke family (warm pearl) |
| `Brand.Accent` | `#C9C4BC` | Focus caret, selection rail, active scope |

> **Banned as default:** `#0A84FF`, `#6366F1`, purple gradients, cyan glow, “AI assistant” lavender.

User-selectable accent (Settings) is **Phase 2**—ship monochrome first.

---

## 3. Personality & voice

### Sounds like

- Calm, direct, short sentences.
- “Search” not “What would you like to find?”
- Error: “Couldn’t reach the index.” not “Oops! Something went wrong.”

### Doesn’t sound like

- Startup hype, chatbot friendliness, gamified copy.
- Feature dumps in the empty state.

### Microcopy patterns

| Context | Pattern | Example |
|---------|---------|---------|
| Placeholder | verb + light scope | `Search apps, files, actions…` |
| Empty results | fact, no apology | `No matches for “figma”` |
| Loading | progressive | `Indexing apps…` → `Ready` |
| Destructive | verb first | `Delete from history` |

---

## 4. Product metaphor

**The bar is a lens**, not a dashboard.

```
         ┌───────────────────────────────┐
  You ──►│  lens (search + context)      │──► Result world (apps, files, …)
         └───────────────────────────────┘
                    │
                    └── dismisses instantly after action
```

- **No hub** on open (no grid of modules).
- **No persistent side nav** in the launcher.
- **Settings** is a separate, slower room—visited intentionally.

---

## 5. Competitive position

| Tool | Spur difference |
|------|----------------|
| Windows Search | Narrow, fast, keyboard-native, clipboard + verbs built in |
| PowerToys Run | Fewer modes visible; stricter visual system |
| Raycast (Mac) | Windows-native materials; no plugin marketplace in the bar |
| Alfred | Simpler surface; learning happens in results order, not workflows |

---

## 6. Naming inside the app

| Old / generic | Spur name | Why |
|---------------|----------|-----|
| Categories | **Scopes** (when typing) / **Anchors** (hover) | Avoid “category picker” mental model |
| Commands | **Actions** | Plain language |
| Browse panel | **Shelf** | Short content strip, not a “panel” |
| Extensions / plugins | **Extras** | Settings area, not the bar |

**Anchors** (hover, right side)—three affordances, icon-only until hover:

| Anchor | Icon idea | Job |
|--------|-----------|-----|
| **Find** | Folder / doc stroke | Recent & indexed files |
| **Do** | Sliders / bolt stroke | System & app actions |
| **Recall** | Clipboard stroke | History |

Apps are **never** an anchor—typing is the app launcher.

---

## 7. What Spur refuses to be

- A **widget dashboard** (weather, stocks, news cards).
- An **AI chat window** disguised as search (AI is an Extra, invoked by intent).
- A **theme demo** (glass, neon, gradients).
- A **settings app** that happens to search (Settings is secondary).

---

## 8. Brand checklist (review every PR)

- [ ] No default blue focus/caret unless user chose accent
- [ ] No double shadows or glow stacks
- [ ] No icon + text + badge + shortcut in one row unless selected
- [ ] Tray icon matches `Icons/spur-launcher-16x16.ico`
- [ ] Motion under 250ms for open; under 120ms for hover feedback
- [ ] Empty state fits in **one line** of chrome

---

*Next: [design.md](design.md) for tokens and components · [ux.md](ux.md) for flows*

