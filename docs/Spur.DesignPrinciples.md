# Spur Design Principles

These five principles govern all design decisions for Spur. Every component, layout, colour choice, and animation is measured against them. If a change violates a principle, the change is wrong — regardless of how good it looks in isolation.

---

## 1. Fast above beautiful

Speed is the defining feature of a launcher. Every millisecond of animation, every layout calculation, every resource load must be justified by user benefit.

- All animations ≤ 150ms. Hover feedback: 100ms. Selection feedback: 50ms.
- No decorative animations, parallax, particle effects, or transitions that serve only aesthetic goals.
- Opacity and transform transitions only — avoid properties that trigger layout reflow.
- When in doubt between a prettier animation and a faster one, choose faster.

## 2. Text first, icons second

Spur launches things. The user's goal is to identify the target and press Enter. Text is the fastest way to scan a list; icons are visual anchors that speed recognition once learned.

- Result rows are 44px tall — tall enough for two lines of text, not tall enough for hero icons.
- Icons are 32×32 in a rounded box, secondary to the primary text label.
- Icon colour is secondary (TextSecondary), never primary (TextPrimary) — the text is the signal.
- Lucide glyphs replace file icons only when no file icon exists; they are not decorative.
- Category icons in the float zone are 36px circles, not labels-with-icons.

## 3. One surface, no navigation

Spur is a single-surface launcher. There are no tabs, no pages, no breadcrumbs, no back buttons in the main flow. The user types, scans results, presses Enter, and the window closes.

- The search capsule is the only persistent surface. Category anchors float outside it.
- The command palette is an overlay, not a page — it appears on top of the search results.
- Settings is a separate window. It is not part of the launcher surface.
- No "back" navigation inside results — typing refines the query.

## 4. Show only what's needed now

A launcher with 100 results on screen is not faster than one with 10 — it's slower, because the user has to scan more. Spur shows the minimum number of elements required for the current action.

- Maximum visible results: ~8 rows (352px at 44px each). More requires scrolling — a signal the query is too broad.
- No result metadata by default — show subtitle, not description. The user can arrow-key to inspect.
- Category buttons appear on hover only, not persistently.
- The "esc" hint on a result row disappears when clipboard actions appear, and vice versa — never show both.
- No splash screens, loading spinners, or empty-state illustrations.

## 5. Neutral canvas, accent for signal

The UI is a tool, not a gallery. Colour is used to communicate state and hierarchy, not to decorate.

- The colour palette is warm-neutral: off-whites, warm greys, soft beige accents.
- Only three text contrast levels: Primary (high), Secondary (medium), Tertiary (low). No fourth level.
- Accent colour (warm beige `#D7CFC2`) is reserved for: selection rail, caret, focus ring, toggle checked state. It is not used for backgrounds, borders, or decorative elements.
- Category accent colours (Time, AI, Math, Network, Files, Clipboard) are all the same muted neutral — because category identity is communicated by label text and icon, not by colour.
- Red/Orange/Green are reserved for semantic meaning: errors, warnings, success states. They are never used for category accents or decorative elements.