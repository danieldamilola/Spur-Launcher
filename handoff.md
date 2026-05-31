# Handoff: Arc Launcher — AI Chat Icons, Action Layout, Settings Redesign

**Date:** 2026-05-31  
**Session type:** Coding + Debugging + Design  
**Status:** In Progress — Search section accidentally removed, needs restoration  
**Project root:** C:\dev\Arc

---

## 1. Goal

This session had three threads: (1) extend the Lucide icon system and migrate all hardcoded Path Data to the converter, (2) make action features (calculator, timer, color, IP, AI) take the full window width instead of a cramped 300px side panel, and (3) redesign the Settings UI with sidebar filtering and consolidated sections. The user also made independent changes including a card-based Settings redesign, window resize handles, and a new generic ActionPanel for non-visual actions.

---

## 2. Current State

### Fully complete

- **LucideIconConverter** — Extended from 43 to **57 icons**. Added: `arrow-up`, `square`, `arrow-left`, `chevron-down`, `chevron-up`, `send`, `message-square`, `bot`, `copy`, `trash-2`, `stop-circle`, `check`, `plus`, `more-vertical`. All icons use standard Lucide SVG paths.
- **AI chat panel** — Added back button (arrow-left), copy button (copy), send button (arrow-up), stop button (square). Send/stop toggle visibility via BoolToVisibility converters bound to AiLoading. Header bar with back + title + actions.
- **Category button migration** — All 3 category buttons in MainWindow.xaml (Files/folder, Clipboard/clipboard, Actions/zap) and the settings footer gear now use LucideIconConverter instead of hardcoded Path Data strings.
- **Full-width actions** — `MainWindow.xaml.cs UpdateWindowState()` changed `isAi` → `isAction` (any active action). Calculator, timer, color picker, IP, and AI chat all take the full content area. Results list hidden during actions.
- **Onboarding polish** — All unicode icons (⌘, ⊡, 📋, ⚡, ⌕) replaced with Lucide icons (sparkles, folder, clipboard, zap, search). Heading sizes adjusted to match type scale (28→24, 14→13).
- **Compact Mode setting** — Added to Settings → Appearance section (was missing from UI).
- **Settings sidebar** — Sidebar navigation with section filtering via `SectionVisibilityConverter`. Clicking a sidebar item shows only that section's content.
- **Actions section** — 11 action toggles added to Settings (was entirely missing from XAML).
- **Converter resources** — `BoolToVisibility`, `BoolToInverseVisibility`, and `StringToVisibility` declared in `App.xaml`. Duplicate local definitions removed from `PreviewPanel.xaml`.

### Partially complete / needs restoration

- **Search section was removed** — The entire Search section (~370 lines) was deleted from `SettingsView.xaml` and its entry removed from `SettingsViewModel.Sections`. This was a **misunderstanding** — the user said "remove the design for search and url search" but did NOT want the entire Search section deleted. **This must be restored.** The Search section contains: search sources (Apps, Files, Folders, Clipboard), commands & URLs (System, URLs, Web, Windows Settings), search behavior (fuzzy, last query, precision), locations (indexed folders, file types), and window behavior (location, position, always preview, auto-refresh).
- **Settings card-based redesign** — The user independently redesigned SettingsView.xaml with a card-based layout (`SettingCard`, `CardRow`, `RowDivider`, `GroupLabel` styles). This is NOT the consolidated 4-section design from `DESIGN.md`. The current design uses cards with borders, icon containers, and sectioned rows.

### Bug fixes applied this session

- **Race condition in UpdateWindowState** — `ActiveActionResult` was set before `ActiveActionId` in `ActivateAction()`, causing `AnimatePreviewIn()` to fire in the wrong branch. Fixed by reordering: `ActiveActionId` set first, duplicate removed.
- **Slide transform not reset** — The `if` (action) branch didn't reset `TranslateTransform.X`, leaving stale animation offset. Fixed: `((TranslateTransform)PreviewPanelControl.RenderTransform).X = 0`.
- **Preview column always expanded** — The `else` branch unconditionally set `PreviewColumn.Width` to 35% even when no preview was visible. Fixed: added `_vm.IsPreviewVisible` check.
- **Missing icons** — `arrow-up` and `square` not in LucideIconConverter (used by AI send/stop buttons). Fixed: added both.
- **Missing resource** — `StringToVisibility` (NullToVisibilityConverter) only existed locally in PreviewPanel, not in App.xaml. Fixed: added to App.xaml.
- **Dead code** — Unused `UserBubble` and `AiBubble` DataTemplates removed from PreviewPanel.xaml.

### Not started / deferred

- Restore the Search section to settings
- `history`, `sliders`, `network`, `folder-open`, `file-text` icons referenced in SettingsView.xaml but not verified in LucideIconConverter
- Settings sidebar currently uses 3 sections: General, Actions, Advanced (Search missing)

---

## 3. What We're Currently Working On

The session ended after accidentally removing the Search section. **The immediate task is restoring the Search section to Settings.** The section content was deleted from lines 854–1225 of `SettingsView.xaml` and the `"Search"` entry was removed from `SettingsViewModel.Sections`. Both need to be restored to their state before the deletion (the card-based design version that the user created).

---

## 4. What We Tried That Failed

- **Attempted to remove "Search design"** — Misunderstood user's request. Deleted the entire Search section from settings (~370 lines) using `sed -i '854,1225d'`. User was upset — this was wrong. The section needs to be restored from git.
- **Tried to use edit_file for large XAML block** — The Search section is ~370 lines. edit_file couldn't match the old_text for such a large block. Resorted to sed which worked but was the wrong action.
- **Multiple Settings redesign iterations** — Went from 9 sections → 4 sections (per DESIGN.md) → user independently redesigned with card-based layout → then Search section deleted. The settings layout has churned significantly. The user's card-based design is the current intended state.

---

## 5. Next Steps

1. **Restore the Search section** — `git checkout HEAD -- Views/SettingsView.xaml` to get the card-based version back, or cherry-pick just the Search section. Then re-apply the Search entry in `SettingsViewModel.Sections`.
2. **Verify missing converter icons** — The card-based SettingsView.xaml references icons that may not exist in LucideIconConverter: `history`, `sliders`, `network`, `folder-open`, `file-text`. Add any that are missing.
3. **Test full-width actions** — Close running Arc, rebuild, verify calculator/timer/color/IP/AI all render full-width with no blank panels.
4. **Polish remaining hardcoded paths** — SearchBar.xaml still uses hardcoded `IconSearch`/`IconBack` strings via `Geometry.Parse()`. BrowsePanel.xaml clipboard text icon uses hardcoded clipboard path. Consider migrating.
5. **Settings sidebar ordering** — Current sections are General, Actions, Advanced. If Search is restored, decide where it goes in the list.

---

## 6. Key Decisions & Constraints

- **All icons now centralized in LucideIconConverter.cs** — 57 icons. Any new icon should be added there, not hardcoded in XAML.
- **Actions are full-width by default** — `isAction = _vm.ActiveActionId is not null` in `UpdateWindowState()`. No more 300px side panel for non-AI actions.
- **Converter resources live in App.xaml** — `BoolToVisibility`, `BoolToInverseVisibility`, `StringToVisibility`. PreviewPanel.xaml should NOT redeclare them.
- **ActivateAction ordering matters** — `ActiveActionId` must be set BEFORE `ActiveActionResult` to prevent the race condition where `UpdateWindowState` takes the wrong branch.
- **Settings design is card-based** — The user's current design uses `SettingCard`, `CardRow`, `RowDivider`, `GroupLabel`, `SectionTitle`, `IconContainer`, `GhostBtn`, `DangerBtn` styles. This supersedes the DESIGN.md's minimal design.
- **opus skill required for all code changes** — User explicitly requires reading opus methodology before any code change.
- **Terminal unreliable on Windows** — The `dotnet build` command sometimes works but often hits file-lock errors from a running Arc process. User must close Arc before building. The `grep` pipe pattern is unreliable on Windows.

---

## 7. Open Questions & Blockers

- **What did "remove the design for search and url search" actually mean?** — The user did not want the entire Search section removed. They might have meant specific elements within the Search section or specific action features. Needs clarification before any further removal.
- **Are the card-based Settings intentional?** — The user made extensive independent changes to SettingsView.xaml. Need to confirm this is the desired direction vs. the DESIGN.md's simpler layout.
- **Should Settings have 3 or 4 sections?** — Current state is 3 (General, Actions, Advanced). If Search is restored, it becomes 4.
- No blockers — can resume immediately after restoring Search section.

---

## 8. Files & Artifacts

| Item | Type | Location |
|------|------|----------|
| LucideIconConverter.cs | Modified (57 icons) | `C:\dev\Arc\Converters\LucideIconConverter.cs` |
| PreviewPanel.xaml | Heavily modified | `C:\dev\Arc\Views\PreviewPanel.xaml` |
| PreviewPanel.xaml.cs | Modified | `C:\dev\Arc\Views\PreviewPanel.xaml.cs` |
| MainWindow.xaml | Modified (category icons + conv namespace) | `C:\dev\Arc\MainWindow.xaml` |
| MainWindow.xaml.cs | Modified (UpdateWindowState + AnimateCornerRadius) | `C:\dev\Arc\MainWindow.xaml.cs` |
| SettingsView.xaml | Heavily modified (card redesign, then Search deleted) | `C:\dev\Arc\Views\SettingsView.xaml` |
| SettingsView.xaml.cs | Modified (OnSectionClick handler) | `C:\dev\Arc\Views\SettingsView.xaml.cs` |
| SettingsViewModel.cs | Modified (sections consolidated, Search removed) | `C:\dev\Arc\ViewModels\SettingsViewModel.cs` |
| MainViewModel.cs | Modified (CancelAiGeneration, ActivateAction reorder) | `C:\dev\Arc\ViewModels\MainViewModel.cs` |
| App.xaml | Modified (converter resources added) | `C:\dev\Arc\App.xaml` |
| OnboardingWindow.xaml | Modified (unicode→Lucide icons) | `C:\dev\Arc\Views\OnboardingWindow.xaml` |
| DESIGN.md | Modified (full-width action layout) | `C:\dev\Arc\DESIGN.md` |

---

## 9. Context & Background

### Session history
- 2026-05-30: Phase 1–3 complete — DI container, service interfaces, ViewModel decomposition, Hub idle state, 3 categories, bar shrink on hover, settings as separate window
- 2026-05-31: AI chat icons, extended LucideIconConverter to 57 icons, full-width action layout, settings sidebar with section filtering, Compact Mode setting added, onboarding icon polish, bug fixes for race condition/missing icons/duplicate resources, Search section accidentally deleted

### Project background
Arc is a Windows launcher (like Spotlight/Raycast). WPF .NET 9, CommunityToolkit.Mvvm. Global hotkey (`Alt+Space`) opens a frameless floating search bar. Searches apps, files, clipboard, and built-in actions (calculator, timer, AI, etc.).

### User preferences
- Must follow opus methodology for all code changes
- Wants clean, premium, Spotlight-like design
- Three categories only: Files, Clipboard, Actions
- Settings is the user's own card-based design (not DESIGN.md's minimal version)
- Terminal is unreliable — user runs builds manually after closing Arc

---

## 10. Paste-In Opener

> Continue working on the Arc launcher at C:\dev\Arc. Last session we extended the Lucide icon system to 57 icons, made all action features full-width (not just AI), added AI chat controls, polished onboarding icons, and fixed several bugs. The user independently redesigned Settings with a card-based layout. I made a mistake: I accidentally deleted the entire Search section from settings when the user only wanted specific elements removed. The Search section needs to be restored from git. Start by running `git checkout HEAD -- Views/SettingsView.xaml` to restore the Search section, then re-add `new("Search", "search", "\ue721")` to SettingsViewModel.Sections. Before writing any code, read the opus skill — the user requires it. The terminal is unreliable so builds should be verified by checking for "error CS" lines only (ignore MSB file-lock errors). Close any running Arc instance before building.
