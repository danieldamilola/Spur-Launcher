# Arc Product Requirements Document

## Product Summary

Arc is a keyboard-first launcher for Windows. It gives users a fast, premium way to open apps, find files, reuse clipboard history, run small utilities, and ask AI questions without opening a full application window.

Arc should feel like a system layer: lightweight, calm, and available everywhere.

## Goals

- Provide a faster, cleaner alternative to Windows Start search.
- Keep the interface minimal and premium.
- Make app and file launching feel instant.
- Keep floating mode icons as a signature interaction.
- Provide useful built-in actions without turning the app into a dashboard.
- Provide AI chat in a full-width, usable panel.
- Keep memory usage low even with large clipboard text.
- Keep startup and typing responsiveness fast.
- Support installer packaging through Inno Setup.

## Non-Goals

- Browser history search.
- Cloud sync.
- Telemetry.
- Plugin marketplace.
- Full file manager replacement.
- Full chat app replacement.
- Complex window management.
- Auto-update system.
- Cross-platform support.

## Target Users

- Windows power users.
- Developers.
- Designers.
- Students.
- Users who prefer keyboard-first workflows.
- Users who want macOS Spotlight/Raycast-like speed on Windows.

## Core User Stories

- As a user, I can press `Alt+Space`, type an app name, and press Enter to launch it.
- As a user, I can browse apps without typing by using the floating Apps mode.
- As a user, I can find recent files without opening File Explorer.
- As a user, I can reuse recent clipboard text or images.
- As a user, I can run quick calculations, timers, color conversions, and IP checks.
- As a user, I can ask AI questions and continue the thread inside Arc.
- As a user, I can configure my AI provider, API key, and model.
- As a user, I can refresh the app catalog when a newly installed app does not show up.
- As a user, I can change theme, opacity, search folders, file types, and startup behavior.

## Functional Requirements

### Launcher

- The app must open from a global hotkey.
- The search input must focus automatically.
- The window must stay topmost while open.
- The window must hide after launching results when configured.
- The window must support Escape-based back/close behavior.

### Search

- Search must update as the user types.
- Search must support fuzzy matching.
- Search must combine results from apps, files, clipboard, and actions when no category is active.
- Search must respect category filters.
- Results must be keyboard navigable.
- Results must avoid showing full paths in normal row subtitles.

### App Discovery

- The app must discover Start Menu and Desktop shortcuts.
- The app must discover local user-installed apps.
- The app must discover common missing apps such as Claude, Firefox, and Notepad when installed.
- The app must use registry App Paths and uninstall entries as additional discovery sources.
- The app must cache discovered apps.
- The app must support manual catalog refresh.

### File Search

- The app must search common user folders.
- The app must support folders and files.
- The app must skip system/hidden/clutter paths.
- The app must support configurable file extensions.
- The app must be cancellable during typing.

### Clipboard

- The app must watch clipboard text and images when enabled.
- Clipboard history must be in memory only.
- Large text must be truncated to avoid high memory usage.
- Image history must be capped.
- Users must be able to clear clipboard history.

### Actions

- The app must support calculator, timer, color, IP, AI, and settings actions.
- Action results must show in the preview panel.
- AI must use full-width preview mode.
- AI must support provider, model, and API key settings.

### Settings

- Settings must be reachable from keyboard and search.
- Settings must save changes immediately.
- Settings must expose appearance, search, hotkey, system, privacy, and AI controls.

## Non-Functional Requirements

Performance:

- Opening Arc should feel instant.
- Typing should remain responsive while file search and app discovery run.
- Cached app catalog should be used before expensive discovery.
- Large clipboard entries should not cause RAM spikes.

Reliability:

- Config writes should not race.
- App discovery failures should not crash the launcher.
- Clipboard read failures should be ignored safely.
- AI network/API errors should be shown inside the UI.

Privacy:

- Clipboard history stays local and in memory.
- API keys are encrypted locally with Windows DPAPI.
- No telemetry.
- No cloud sync.

Accessibility:

- Keyboard navigation is required.
- Tooltips expose full paths/details.
- Text should be readable in dark and light themes.
- Focus/selection states must be visible.

## Technology Stack

Application:

- .NET 9
- WPF
- C#
- XAML
- CommunityToolkit.Mvvm

Windows integration:

- Win32 global hotkey APIs
- WPF clipboard APIs
- Windows registry APIs for startup and app discovery
- Shell shortcut COM interop for `.lnk` resolution
- Shell icon extraction through `IShellItemImageFactory`
- Windows notification/tray support through `Hardcodet.NotifyIcon.Wpf`
- DPAPI through `System.Security.Cryptography.ProtectedData`

AI/network:

- `HttpClient`
- Provider adapters in `AiService`
- Supported providers: Groq, Gemini, OpenRouter, DeepSeek
- Provider-specific model configuration

Persistence:

- JSON config file in `%LocalAppData%\Arc`
- JSON app catalog cache in `%LocalAppData%\Arc`
- In-memory clipboard history
- Frequency/usage data service for ranking

Build and packaging:

- MSBuild / `dotnet build`
- `Arc.csproj`
- `Flow.csproj` compatibility project file
- Inno Setup through `installer.iss`
- Output installer name: `Arc-Setup.exe`

Testing/verification:

- `dotnet build Arc.csproj -o obj\verify-feature-port`
- `dotnet build Flow.csproj -o obj\verify-flow-port`
- Existing test project: `Arc.Tests`

## Key Screens And Panels

- Neutral search pill
- Search results panel
- Apps browse panel
- Files browse panel
- Clipboard browse panel
- Actions browse panel
- Preview panel
- Full-width AI chat panel
- Settings panel
- Tray/menu integration

## Success Metrics

- User can launch common apps faster than Windows Search.
- Newly installed apps can be found after refresh.
- AI chat is usable for multi-message answers without layout crowding.
- Clipboard remains useful without high RAM use.
- User can configure provider/model/API key without editing files.
- No visible Volt branding remains.
- Installer can package the app as Arc.

## Risks

- Windows app discovery is fragmented; some apps may still require special handling.
- Registry scanning can add noise if filters are too loose.
- WPF glass/blur behavior may vary by Windows version.
- AI provider APIs can change.
- Unsigned installers may trigger Windows trust warnings.

## Future Improvements

- Add app discovery diagnostics.
- Add clearer loading/progress state for catalog refresh.
- Add signed installer support.
- Add optional update channel.
- Add richer keyboard shortcut customization.
- Add provider-specific model validation.
- Add lightweight onboarding only if users need help discovering modes.

