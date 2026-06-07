## Analysis Complete — Recommending: Design System Consolidation + Visual Cleanup

I found significant inconsistencies across the theme system. Here's a summary before I present the plan:

### What's Broken

| Issue | Detail |
|---|---|
| **Duplicate token systems** | `Themes/DesignTokens/Colors.xaml` defines `Token.Bg.*`, `Token.Text.*` etc. — but `DarkTheme.xaml` and `LightTheme.xaml` define their own parallel set of keys (`TextPrimary`, `TextSecondary`, `SelectedBg`, etc.) that the views actually reference. The design tokens exist but are mostly unused. Two sources of truth. |
| **Missing LightTheme keys** | `LightTheme.xaml` is missing ~14 keys that `DarkTheme.xaml` defines: `FontNano`, `FontMicro`, `FontCaption`, `FontBody`, `FontBase`, `FontMd`, `FontHeading`, `FontTitle`, `FontHero`, `AccentTime`, `AccentAi`, `AccentMath`, `AccentNetwork`, `AccentFiles`, `AccentClipboard`, `RadiusLg`, `RadiusXl`. Light mode will silently fall back to defaults on these. |
| **Dark/Light radii don't match** | `DarkTheme.xaml`: `RadiusRow: 10`, `RadiusPill: 16`. `LightTheme.xaml`: `RadiusRow: 8`, `RadiusPill: 14`. The themes should differ in color only, not layout geometry. |
| **Font weight rule violated** | DESIGN_RESEARCH.md Rule 5 says "only 2 weights: 400 regular, 500 medium, never 600/700." But `ResultTemplates.xaml` uses `FontWeight="SemiBold"` (600) in 4 places, and `MainWindow.xaml` uses `FontWeight="Bold"` (700) on the timer display. |
| **Type scale is bloated** | DarkTheme defines 23 font size keys; DESIGN_RESEARCH says 5–6. LightTheme defines only 16 — meaning 7 keys silently fail in Light mode. |
| **Hardcoded values sprinkled everywhere** | `MainWindow.xaml` has hardcoded hex colors (`#0AFFFFFF`, `#00FFFFFF`), arbitrary margins (`6`, `10`, `2`) that don't align to the 4px spacing scale, and padding values (10, 7) that aren't on the grid. |
| **Three result templates, three behaviors** | `ResultRowTemplate`, `ResultTemplate`, and `ClipResultTemplate` each have different hover animation speeds (150ms vs 80ms), different icon sizes (24px vs 26px vs 30px), and different padding schemes. |
| **No design principles documented** | The codebase has no written principles filtering every decision. |

### Recommendation

A **design system consolidation** rather than a full visual redesign. The bones are good — MVVM, token system, theme switching. The problem is inconsistency accumulation. Fixing this will make the UI feel "premium and calm" without throwing anything away. This aligns with the research document's core message: premium comes from discipline, not decoration.

Does this direction look right before I build the detailed implementation plan? I'd consolidate tokens, enforce the type scale, normalize spacing to the 4px grid, unify result templates, and add design principle documentation.