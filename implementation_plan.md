# Implementation Plan

[Overview]
Consolidate Spur's fragmented design token system into a single source of truth, fix all visual inconsistencies, and apply subtle polish across the entire UI — producing a "premium & calm" launcher that follows every principle in DESIGN_RESEARCH.md.

Spur currently has two parallel token systems: `Themes/DesignTokens/` (primitive) and `Themes/DarkTheme.xaml`/`LightTheme.xaml` (semantic). The design tokens are mostly unused. Views reference semantic keys, but those keys differ between dark and light themes — missing keys, mismatched radii, duplicate font sizes, and hardcoded hex values throughout. The result is a UI that works but feels assembled rather than designed.

This implementation merges both systems: primitives live in a single `Tokens.xaml`; DarkTheme and LightTheme define only semantic aliases that are guaranteed mirror images of each other (differing only in color values, never in layout geometry). Every view is then audited to remove hardcoded values, enforce the 4px spacing grid, fix font weight violations (no SemiBold/Bold in UI), unify result row templates, and apply subtle animation/color/spacing polish.

The goal is "discipline applied throughout" — the product of DESIGN_RESEARCH.md Rule 19.

[Types]
No C# type changes. This is a pure XAML/resource dictionary refactor. All work is in `.xaml` and `.md` files.

The only new artifact is a design principles markdown file:
- `Spur.DesignPrinciples.md` — 5 written principles that govern all future design decisions

Existing model types (`SearchResult.cs`, `ScopeFilterItem.cs`, `SectionLabel.cs`, etc.) are reused unchanged. The `ResultTemplateSelector` class in `Views/` continues to work with the unified templates.

[Files]

### New Files

| Path | Purpose |
|---|---|
| `Themes/Tokens.xaml` | Single primitive token source. Contains all raw values organized into four sections: Colors, Typography, Spacing, Radii. No semantic aliases. |
| `Spur.DesignPrinciples.md` | Written design principles document. 5 principles: Fast above beautiful, Text first icons second, One surface no navigation, Show only what's needed now, Neutral canvas accent for signal. |

### Files to Rewrite

| Path | Changes |
|---|---|
| `Themes/DarkTheme.xaml` | Strip all primitive tokens. Keep only semantic aliases pointing to `Tokens.xaml` primitives via `DynamicResource`. Must be an exact mirror of `LightTheme.xaml` — same keys, same count, same structure, differing only in color values. |
| `Themes/LightTheme.xaml` | Same treatment as DarkTheme. Add all missing keys (`FontNano`, `FontMicro`, `FontCaption`, `FontBody`, `FontMd`, `FontHeading`, `FontTitle`, `FontHero`, `AccentTime`, `AccentAi`, `AccentMath`, `AccentNetwork`, `AccentFiles`, `AccentClipboard`, `RadiusLg`, `RadiusXl`). Match all geometry tokens to DarkTheme exactly. |
| `Themes/ResultTemplates.xaml` | Consolidate 3 result templates (`ResultRowTemplate`, `ResultTemplate`, `ClipResultTemplate`) into 1 base template. Unify animation speeds to 100ms hover / 50ms selection. Unify icon sizes to 32×32. Enforce 4px grid on all margins/padding. Replace all `FontWeight="SemiBold"` with `FontWeight="Medium"`. |
| `Themes/CommonStyles.xaml` | Audit all styles. `SettingsKicker` and `SettingsTitle`: `FontWeight="SemiBold"` → `FontWeight="Medium"`. All hardcoded font sizes → `DynamicResource` references. `ScopeChip`: normalize to 4px grid. CheckBox: replace `SurfaceHigh` (undefined) with `Surface-Raised`. |

### Files to Modify

| Path | Changes |
|---|---|
| `MainWindow.xaml` | Replace hardcoded gradient hex (`#0AFFFFFF`, `#00FFFFFF`) with token references. Fix all margin/padding to 4px multiples. Replace `FontWeight="Bold"` on Timer with `FontWeight="Medium"` + size bump. Audit `ActionPreviewPanel` — remove hardcoded padding `12,10`, align to grid. |
| `Views/SearchBar.xaml` | Tokenize esc pill. Remove hardcoded background/border from esc chip → use `Surface-Raised` / `Border-Subtle`. Fix `Margin="12,0,8,0"` → `16,0,16,0` (Space-16). Remove hardcoded `CornerRadius="16"` → `Radius-Window`. |
| `Views/UnifiedResultsView.xaml` | Update template references to new single unified template name. Fix `Padding="0,2,0,4"` → `0,0,0,0`. |
| `Views/ScopeBar.xaml` | Replace `SelectedBg` with `Surface-Selected`. Replace hardcoded `Margin="16,2,16,7"` → `16,4,16,4`. Replace `FontWeight="Medium"` with `FontWeight="Medium"` (already correct, just verify). |
| `Views/CommandPalette.xaml` | Audit for hardcoded values. Normalize spacing to 4px grid. Ensure overlay background uses token. |
| `Themes/Animations.xaml` | Ensure all storyboard durations ≤ 150ms. Any hover animation at 200ms+ → reduce to 100–150ms. |

### Files to Delete

| Path | Reason |
|---|---|
| `Themes/DesignTokens/Colors.xaml` | Merged into `Tokens.xaml` |
| `Themes/DesignTokens/Typography.xaml` | Merged into `Tokens.xaml` |
| `Themes/DesignTokens/Spacing.xaml` | Merged into `Tokens.xaml` |
| `Themes/DesignTokens/Radii.xaml` | Merged into `Tokens.xaml` |

The `DesignTokens/` directory itself is removed (now empty).

### Configuration File Updates

- `Spur.csproj` — verify no explicit file includes for deleted DesignTokens files
- `App.xaml` — update merged dictionary references: remove individual DesignTokens includes, add `Tokens.xaml`

[Functions]

No C# code changes. This is a pure XAML refactor. All existing functions, ViewModels, services, and converters remain untouched.

The only "function-level" change is in code-behind files that may reference named elements being renamed — but we are not renaming any named elements in this plan. `x:Name` values are preserved as-is.

[Classes]

No class changes. The `ResultTemplateSelector` class in `Views/` continues to function — its `SectionTemplate` and `ResultTemplate` properties will be pointed at the new unified templates.

No ViewModel, Model, Service, Extension, Converter, or Behavior classes are modified.

[Dependencies]

No new NuGet packages. No version changes. The project already depends on `inkore.ui.wpf.modern` for `ui:FontIcon` — this continues unchanged.

The only dependency consideration: Inter font (`Token.Font.Primary`) must be available as a system font or bundled. Currently referenced as `pack://application:,,,/Themes/Fonts/#Inter` — if the Inter font file is not in `Themes/Fonts/`, it must be added or the fallback chain (`Segoe UI Variable, Segoe UI`) will serve as the actual rendered font. This is acceptable per DESIGN_RESEARCH.md: "Native system fonts guarantee correct rendering."

[Testing]

No new test files. The existing test suite (`Spur.Tests/`) covers ViewModel logic which is unchanged.

Visual validation strategy:
1. Build and run in Debug after completing the DarkTheme → verify all resource references resolve (no XAML parse errors)
2. Toggle to Light theme via settings → verify all resource references resolve (no missing key errors)
3. Squint test: blur the launcher window and verify the search bar is the most prominent element
4. Scan test: verify only 3 contrast levels are perceivable (Primary, Secondary, Tertiary)
5. Grid test: verify no element sits at an off-grid pixel position
6. Animation test: all transitions complete in ≤ 150ms

[Implementation Order]

1. Create `Tokens.xaml` — all primitives in one file
2. Create `Spur.DesignPrinciples.md` — written principles
3. Rewrite `DarkTheme.xaml` — semantic aliases only, mirror-ready
4. Rewrite `LightTheme.xaml` — exact mirror of DarkTheme, differing only in color values
5. Update `App.xaml` — swap merged dictionary references
6. Delete `DesignTokens/Colors.xaml`, `Typography.xaml`, `Spacing.xaml`, `Radii.xaml`
7. Rewrite `ResultTemplates.xaml` — single unified template, 4px grid, Medium weight only, 100ms/50ms animations
8. Modify `CommonStyles.xaml` — fix SemiBold→Medium, fix undefined SurfaceHigh, normalize spacing
9. Modify `MainWindow.xaml` — remove hardcoded hex, fix spacing, fix Bold→Medium
10. Modify `Views/SearchBar.xaml` — tokenize esc pill, 4px grid margins
11. Modify `Views/UnifiedResultsView.xaml` — update template references, fix padding
12. Modify `Views/ScopeBar.xaml` — tokenize, 4px grid
13. Modify `Views/CommandPalette.xaml` — audit and normalize
14. Modify `Themes/Animations.xaml` — enforce ≤ 150ms durations
15. Build, test, verify — ensure all resources resolve in both themes