Rebranding Plan

Current StateName: Arc
Tagline: "A fast, minimal launcher for Windows"
Product: WPF / .NET 9 desktop launcher — global hotkey (Alt+Space), floating search surface, three categories (Files, Commands, Clipboard), built-in calculator, web search, system commands
Visual style: Dark/light themes with system accent, Lucide icons, compact search-bar UI, settings window, onboarding window
Distribution: Velopack updates, optional system tray icon
Brand assets: arc-launcher-256x256.ico (exe/taskbar), arc-launcher-16x16.ico (tray), both in Icons/
Tone: Technical, minimal, fast
Target audience: Power users, developers, Windows enthusiasts

Target IdentityNew name: _TBD — choose during Phase 1 Item 6_
New tagline: _TBD — derived from name personality_
New name: Spur
New tagline: "Spur your workflow."
Personality: Fast, lightweight, invisible when not needed, surgically precise. Feels like a power tool, not an app. Terminal-grade respect for the user — no tracking, no upsells, no clutter.
Target audience: Same (power users, developers, Windows enthusiasts), plus expand to designers and productivity users via usability improvements
Design direction: Micro-interactions and motion that make the tool feel alive; glassmorphism depth; typographic hierarchy; breathing room in the UI

Changes (one at a time)

Phase 1 — Foundation (repo health + naming):check mark: Clean repo. Expand .gitignore, delete dead files (RefactorApp/, .exclude, nul, refactor_appdisc.py, tools/sample-brand-colors.ps1, clean.ps1, clean2.ps1, AddUsings.ps1, AddUsingsToServices.ps1).Eliminate static facades. Kill Services/Facade.cs static singletons, convert to proper DI. Touch App.xaml.cs, ArcViewModel.cs, MainViewModel.cs, KeyboardHook.cs, Theme/ThemeEngine.cs, Services/Facade.cs, ViewModels/.cs.
Archive POC experiments. Archive CLI/, PowerLauncher/, WpfApp/ to _archive/poc/ with a README explaining what they were.
Add CI. GitHub Actions: dotnet build, dotnet test, dotnet format --verify on push/PR.
Add minimal tests. Start with Arc.Tests/MainViewModelTests.cs (search behavior, command dispatch). Target: 5-10 tests to establish the pattern.
Pick the new name. Brainstorm 20+ candidates, shortlist top 5, check availability (domain, NuGet, GitHub, trademark), pick one.
:check mark: Pick the new name. Brainstormed 20+ candidates, shortlisted (Blink, Ray, Snap, Flick, Beam), checked conflicts, all rejected. Second round (Spur, Lance, Kilo) — Spur won: sharp, power-tool, one syllable, no conflicts. Tagline: "Spur your workflow."*

Phase 2 — Design system (the new look)Design tokens. Replace hardcoded hex/RGBA colors with resource dictionary tokens. Standardize spacing (4px grid), typography scale, border radii, shadows. Apply to all .xaml files.Glassmorphism background. Acrylic/blur backdrop for the search window via WindowBlur.cs + SetWindowCompositionAttribute. System-aware — follows light/dark mode.
Typography pass. Choose a monospace font for results (Cascadia Code / JetBrains Mono / Fira Code), set type scale (13px results, 18px input, 11px metadata). Implement in Styles/Typography.xaml.
New icon set. Generate .ico files for the new brand name (256x256 for exe/taskbar, 16x16 for tray). Place in Icons/.
Replace Lucide icons. Evaluate whether Lucide still fits the new personality. If yes, keep but re-key to match token naming. If no, pick a new set. Update all Style="{StaticResource Lucide}" references.
Motion micro-interactions. Add storyboard animations: opening/closing with a subtle scale+fade, result hover states, typing ripple. Implement in Styles/Animations.xaml.

Phase 3 — Product architecture:check mark: Settings redesign. Horizontal tabs + search bar. Replaced sidebar with tab strip (General | Search | Actions | Extras | About), added search TextBox in header with FilteredSections binding, full-width content area. Implemented in Views/SettingsView.xaml + ViewModels/SettingsViewModel.cs.Onboarding experience. Replace the existing onboarding with a 3-step welcome flow: welcome → hotkey → first search. Implement in Views/OnboardingWindow.xaml.
Clipboard Manager extraction. Extract clipboard from launcher into a standalone mode or separate view. Implement Views/ClipboardManager.xaml and ViewModels/ClipboardViewModel.cs.
Command palette. Ctrl+Shift+P opens a VS Code-style command palette for actions like "Toggle theme", "Open settings", "Clear clipboard history". Implement in ViewModels/CommandPaletteViewModel.cs.

Phase 4 — Distribution + polishVelopack branding. Update update URLs, manifest, and installer metadata to the new name.Website/GitHub. Update README with new branding, screenshots, logo. Register domain. Set up landing page.
Accessibility pass. Ensure full keyboard navigation, screen reader labels, contrast ratios, focus indicators, and AutomationProperties on all interactive elements.

NotesEvery phase leaves the project buildable and runnable. No multi-PR waterfall.
Name change touches: assembly name, namespaces, folder structure, .csproj, .sln, installer config, window titles, about text, tray tooltip, README.
Static facade removal (Item 2) is the riskiest change — budget time for DI registration debugging.
POC archiving (Item 3) is safe but touches project structure — keep the archive in-repo so history is preserved.
