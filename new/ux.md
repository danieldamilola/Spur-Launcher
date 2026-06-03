# ux.md — Experience & Flows (Spur v2)

> One surface. Three ways to intent: **type**, **hover**, **anchor**. Everything else is a consequence.

Companion: [brand.md](brand.md) · [design.md](design.md) · [motion.md](motion.md) · [features.md](features.md)

---

## 1. Mental model

```
                    ┌─────────────┐
         Alt+Space  │   PRESENT   │  Escape / activate / click-away
        ──────────► │  (the bar)  │ ──────────► IDLE (tray)
                    └──────┬──────┘
                           │
         ┌─────────────────┼─────────────────┐
         ▼                 ▼                 ▼
      EMPTY            HOVER              TYPING
   (just search)   (anchors show)    (results + chips)
         │                 │                 │
         └──────── anchor ──┴──── chip filter ┘
                           ▼
                        SHELF
                  (expanded list)
```

| State | User goal | Chrome |
|-------|-----------|--------|
| **Idle** | — | Tray only |
| **Present/Empty** | Decide what to do | 56px bar |
| **Present/Hover** | Discover anchors | bar + 3 circles |
| **Present/Typing** | Complete a query | bar + chips? + results |
| **Present/Shelf** | Browse without query | bar + list |
| **Settings** | Configure | Separate window |

**No state** shows more than one primary list.

---

## 2. Idle

- Tray: `spur-launcher-16x16.ico`
- Menu: **Open Spur** · Settings · About · Quit
- No badges, no update nags in tray

**Entry:** login / launch  
**Exit:** `Alt+Space` (default) → Present/Empty

---

## 3. Present / Empty

### Layout

```
┌────────────────────────────────────────────────────────┐
│  ⌕   Search apps, files, actions…                      │
└────────────────────────────────────────────────────────┘
        640 × 56 · r14 · matte surface
```

### Behavior

| Input | Result |
|-------|--------|
| Any key | → Typing (character in field) |
| Mouse enter bar | → Hover |
| `Escape` | → Idle |
| Click outside | → Idle |
| `,` or `Ctrl+,` | Settings (optional) |

### Motion

Open: opacity + scale per [motion.md §2.1](motion.md#21-show-idle--present).

Focus: caret in field **immediately** (≤50ms after paint).

---

## 4. Present / Hover

### Layout

```
┌────────────────────────────────────────────────────────┐
│  ⌕   Search apps, files, actions…          ○  ○  ○     │
└────────────────────────────────────────────────────────┘
                                              Find Do Recall
                                         (labels on anchor hover)
```

### Anchors

| Anchor | Opens shelf | Placeholder becomes |
|--------|-------------|---------------------|
| **Find** | Recent files + indexed folders | `Search files…` |
| **Do** | System & app actions | `Search actions…` |
| **Recall** | Clipboard history | `Filter clipboard…` |

### Behavior

| Input | Result |
|-------|--------|
| Mouse leave (no shelf) | → Empty |
| Start typing | Anchors hide **instantly** → Typing |
| Click anchor | → Shelf (anchor stays active, ← back on anchor) |
| `Escape` | If shelf: → Hover; else → Idle |

### Motion

[motion.md §3.1](motion.md#31-anchor-reveal-hover-empty-query)

---

## 5. Present / Shelf

### Layout (example: Find)

```
┌────────────────────────────────────────────────────────┐
│  ⌕   Search files…              ←   ○  ○  ○           │
├────────────────────────────────────────────────────────┤
│  Recent                                                │
│  ─────────────────────────────────────────────────── │
│  project-notes.md          ~/Projects      2m         │
│  design-spec.pdf           ~/Downloads     1d         │
│  …                                                     │
├────────────────────────────────────────────────────────┤
│  ↵ Open   ·   Ctrl+Shift+E Reveal   ·   Ctrl+C Path   │
└────────────────────────────────────────────────────────┘
```

### Rules

- **One** section header per shelf (`Recent`, `Actions`, `Clipboard`)
- Max **8** rows visible without scroll; scroll is minimal strip
- Footer shows only when row selected
- `←` on active anchor collapses shelf → Hover

### Keyboard

| Key | Action |
|-----|--------|
| `↑` `↓` | Move selection |
| `Enter` | Execute |
| `Escape` | Back one level |
| Type | Filter within shelf domain first |

---

## 6. Present / Typing

### Layout

```
┌────────────────────────────────────────────────────────┐
│  ⌕   figma                                             │
│       [ Apps 2 ]  [ Files 1 ]  [ Web ]                 │
├────────────────────────────────────────────────────────┤
│ █ Figma                              Enter to open    │
│   Figma.lnk                          App              │
│   figma-export.pdf                   ~/Downloads      │
│   Search the web for “figma”                          │
├────────────────────────────────────────────────────────┤
│  ↵ Open   ·   Ctrl+↵ Admin   ·   Ctrl+I Reveal        │
└────────────────────────────────────────────────────────┘
```

### Ranking (default)

1. **Apps** (frecency-weighted)
2. **Inline answers** (calculator, unit conversion—detected, not a mode)
3. **Files**
4. **Actions** (verbs)
5. **Settings** entries
6. **Web** (last resort suggestion row)

### Scope chips

- Auto-generated when ≥2 domains have matches
- Click chip → filter to that domain; chip becomes active
- `Tab` cycles active chip when chip row focused
- Chips **never** appear in Empty/Hover

### Inline detection (no chip)

| Pattern | Behavior |
|---------|----------|
| `2+2`, `100 USD in EUR` | Inline result row, Enter to copy/open |
| `?` prefix (optional) | Web search query |
| `>` prefix | Shell (if enabled in Extras) |

### Escape ladder

```
Typing (clear query) → Typing (empty) → Empty → Idle
                      ↑ if shelf open, close shelf first
```

---

## 7. Action executed

1. Execute target (app, file, verb, clipboard paste)
2. Hide launcher (140ms fade)
3. Reset UI to Empty defaults for next open
4. Toast only if action needs confirmation (“Copied”, “Pinned”)

**No** “success” screen inside launcher.

---

## 8. Settings (separate window)

### IA

```
General          Hotkey, startup, theme
Search           Indexing, folders, precision
Actions          Verbs on/off
Extras           AI, timer, advanced tools
About            Version, links, icon credit
```

- Launcher **never** embeds settings in the results list (type `settings` can still find it as a row)
- Sidebar + content pane; matches [design.md §5.6](design.md#56-settings-separate-window)

### Entry

- Tray → Settings
- `Ctrl+,` from launcher
- First-run onboarding → optional “Open Settings”

---

## 9. Onboarding

| Step | Content | Primary |
|------|---------|---------|
| 1 | Spur mark + “Instant search for everything on your PC.” | Continue |
| 2 | What’s indexed locally (apps, optional folders) | Continue |
| 3 | Press `Alt+Space` to try | Finish |

Skip allowed. Never more than 3 steps.

---

## 10. Keyboard reference

| Key | Context | Action |
|-----|---------|--------|
| `Alt+Space` | Global | Toggle Present |
| `Escape` | Present | Back / dismiss |
| `↑` `↓` | Shelf / Typing | Selection |
| `Enter` | Selection | Open / run |
| `Ctrl+Enter` | App row | Run as admin |
| `Ctrl+Shift+E` | File row | Reveal in Explorer |
| `Ctrl+C` | File row | Copy path |
| `Ctrl+1..3` | Present | Focus Find / Recall / Do anchor |
| `Tab` | Typing + chips | Cycle scope |

---

## 11. Error & empty

| Case | UI |
|------|-----|
| No matches | Single row: `No results for “query”` (tertiary) |
| Index loading | Subtle meta under bar: `Indexing apps…` |
| Index failed | Settings link in meta |
| Permission | Explain + Open Settings |

No modals in the bar.

---

## 12. UX decisions log (v2)

| Decision | Rationale |
|----------|-----------|
| Fixed 640px width | Stable muscle memory; anchors animate inside |
| No app anchor | Start Menu owns browse; typing owns launch |
| Monochrome selection | Brand owns highlight, not system blue |
| Shelf vs hub | Shelf is temporal; hub felt like second app |
| AI not in bar | Extras only—keeps trust and focus |
| Rename categories → anchors/scopes | Language matches mental model |

---

*Feature mapping: [features.md](features.md) · Build order: [phases.md](phases.md)*

