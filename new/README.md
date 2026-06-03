# Spur — Rebrand & Product Spec (`/new`)

This folder is the **source of truth** for Spur’s premium minimal rebrand. Implementation in the WPF app should follow these documents in order—not the older root-level `UX.md` / `DESIGN.md` copies or the `,md/` drafts.

## Vision (one line)

**Spur is a quiet command surface for Windows:** one beautiful bar, instant results, gone before you notice it was there.

## What changed

| Before | After |
|--------|--------|
| Apple-blue accent (`#0A84FF`) everywhere | **Monochrome-first** brand; accent sampled from icon, used sparingly |
| Categories as a product surface | **Modes** revealed by context (type, hover, intent)—not a mini app store |
| Many toggles & action plugins visible in search | **Core loop** + **Extensions** drawer in Settings |
| WPF `DropShadowEffect` + glass borders | **Layered matte surfaces** + single ambient shadow (no white halo) |
| Feature list drives UI | **UX states** drive what ships in the bar |

## Document map

| File | Read when you need… |
|------|---------------------|
| [**brand.md**](brand.md) | Name, personality, icon rules, voice, what Spur is *not* |
| [**design.md**](design.md) | Color, type, spacing, components, materials, tokens |
| [**motion.md**](motion.md) | Durations, springs, state transitions, reduced motion |
| [**ux.md**](ux.md) | Screen-by-screen flows, keyboard model, empty/hover/search |
| [**features.md**](features.md) | What ships in v2, tiers, how features map to UI |
| [**phases.md**](phases.md) | Build order, gates, migration from current codebase |

## Brand ↔ icon

Windows shell icons live in [`Icons/`](../Icons/README.md). In-app UI **never** pastes the `.ico` into the bar—it **rhymes** with it: same corner language, same contrast discipline, same single luminous stroke idea.

## Implementation rule

> If a control needs a label to explain itself, the design failed.  
> If it needs color to look “premium,” the palette failed.

When in doubt: fewer pixels, slower fade-out, faster fade-in.

## Status

| Spec | Status |
|------|--------|
| brand.md | ✅ Draft for rebrand |
| design.md | ✅ Draft for rebrand |
| motion.md | ✅ Draft for rebrand |
| ux.md | ✅ Draft for rebrand |
| features.md | ✅ Draft for rebrand |
| phases.md | ✅ Draft for rebrand |

Start with **brand.md** + **ux.md**, then **design.md** + **motion.md**, then implement via **phases.md**.

