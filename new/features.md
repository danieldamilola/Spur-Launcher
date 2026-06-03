# features.md — Product Scope (Spur v2)

> **Rule:** The bar shows what helps *this week*. Everything else is an **Extra** in Settings.

Design drives packaging—see [ux.md](ux.md).

---

## 1. Tiers

```
CORE      Always in the bar loop; cannot disable without breaking Spur
BUILT-IN  Ships on; user toggles in Settings → Actions / Search
EXTRA     Off by default or separate surface; never clutters Empty state
```

---

## 2. CORE (the loop)

### 2.1 App launch (typing)

- Index Win32, UWP, common stores
- Fuzzy + frecency ranking
- **No** browse-all-apps UI
- `Ctrl+Enter` → elevated launch

### 2.2 Unified search (typing)

- Files via Windows Search + optional Everything path
- Windows Settings entries
- Single result list geometry

### 2.3 Anchors (hover)

| Anchor | CORE behavior |
|--------|----------------|
| **Find** | Shelf: recent files, pinned folders |
| **Do** | Shelf: verbs (sleep, lock, theme, volume…) |
| **Recall** | Shelf: clipboard history |

### 2.4 Clipboard recall

- History list in Recall shelf
- Pin / delete item
- Paste on Enter
- Global shortcut optional (`Win+Shift+V`) — conflicts called out in Settings

### 2.5 Chrome & shell

- Global hotkey, tray, auto-start
- Light / dark / system
- Hide on deactivate
- Onboarding (3 steps)

---

## 3. BUILT-IN (detected or toggled)

### Inline (typing, no chip)

| Intent | Detection | Result row |
|--------|-----------|------------|
| Calculator | numeric expression | Evaluated answer, Enter copies |
| Unit / currency | `100 usd in eur` | Converted value |
| Color | `#hex` or `rgb()` | Swatch + copy |
| URL | looks like URL | Open in browser |
| Web | low local matches | “Search the web for …” row |

### Actions (Do shelf + typing)

| Verb group | Examples |
|------------|----------|
| Power | Sleep, restart, shutdown, lock |
| Display | Dark/light toggle |
| Audio | Mute, volume steps |
| Capture | Screenshot (region) |
| System | Empty recycle bin, open Task Manager |

### Search sources (Settings → Search)

| Source | Default |
|--------|---------|
| Apps | On |
| Files | On |
| Settings | On |
| Shell (`>`) | Off |
| Web fallback | On |

---

## 4. EXTRAS (Settings → Extras)

Not in Empty/Hover. Invoked by:

- Explicit keyword (`ai`, `timer`, `ip`)
- Or enabled “show in search” flag per extra

| Extra | Purpose | v2 default |
|-------|---------|------------|
| AI assist | External API chat | Off |
| Timer / Pomodoro | Focus timer | Off |
| Kill process | End task by name | Off |
| Quick note | Scratch pad | Off |
| Password gen | One-shot | Off |
| Plugins (future) | Packaged extensions | — |

**Product rule:** Extras never add icons to the hover anchors row.

---

## 5. Explicitly cut or demoted from bar

| Was | v2 |
|-----|-----|
| Hub grid on open | **Removed** |
| Side preview panel | **Removed** |
| Category pills floating outside bar | **Removed** |
| Blue full-row selection | **Removed** → rail + elevated fill |
| AI chat inline panel | **Extra** only |
| Browse “apps” category | **Removed** |
| 12+ action toggles visible | Grouped in Settings |

---

## 6. Settings structure (maps to code)

```
General/
  Hotkey, startup, theme, opacity, sound
Search/
  Apps, files, folders, precision, frecency
Actions/
  Built-in verbs (grouped)
Extras/
  AI, timer, …
About/
  Version, update channel, diagnostics
```

---

## 7. Success metrics (qualitative)

| Signal | Healthy |
|--------|---------|
| Time to first keystroke | < 150ms after hotkey |
| Actions per session | 1 (then dismiss) |
| Settings visit rate | Low after week 1 |
| Anchor use | < 30% of sessions (typing dominates) |

---

## 8. Future (not v2 rebrand)

- Plugin SDK + signed packages
- Shared theme import (optional accent packs)
- Cloud sync for clipboard (opt-in, encrypted)
- Multi-monitor position memory

---

*Implementation phases: [phases.md](phases.md)*

