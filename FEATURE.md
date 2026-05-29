# Arc Feature Specification

Arc is a Windows launcher with app search, file search, clipboard history, built-in actions, AI chat, settings, and installer support.

## Launcher Shell

- Global hotkey opens Arc.
- Frameless topmost WPF window.
- Center-screen startup position.
- Search input focused on open.
- Idle pill state.
- Expanded panel state for results, browse modes, previews, and settings.
- Hide/show animation using opacity and scale.
- Does not show in taskbar.
- Optional tray icon support.

## Search

- Live query updates.
- Fuzzy matching.
- Frequency-aware ranking for commonly opened items.
- Category-aware filtering.
- Section labels for grouped results.
- Keyboard selection with Up and Down.
- Enter opens the selected result.
- Ctrl+Enter performs a secondary action.
- Ctrl+Shift+Enter runs supported app targets as administrator.
- Result rows show clean subtitles instead of raw paths.
- Full path/detail is available on hover tooltip.

## Modes

Arc includes four main modes:

- Apps
- Files
- Clipboard
- Actions

Modes are accessible through floating icons in neutral state and keyboard shortcuts. Active modes suppress hover mode behavior so the panel remains stable.

## Apps

- Discovers Start Menu shortcuts.
- Discovers Desktop shortcuts.
- Discovers `%LocalAppData%\Programs`.
- Scans limited nested local app folders for apps without Start Menu shortcuts.
- Includes known app candidates such as Claude, Firefox, and Notepad.
- Reads Windows App Paths registry entries.
- Reads uninstall registry entries to infer installed app folders.
- Supports cached app catalog for fast startup.
- Supports manual app catalog refresh from Settings.
- Shows real app icons where possible.
- Supports app grid view.
- Supports app list view.
- Shows suggested apps based on usage data.
- Can launch selected apps.
- Can open app file location.
- Can run supported apps as administrator.

## Files

- Searches common user folders:
  - Desktop
  - Documents
  - Downloads
  - Pictures
  - Music
  - Videos
  - OneDrive
- Supports file and folder results.
- Supports configurable indexed folders.
- Supports excluded folders.
- Supports file extension allow-list.
- Supports fuzzy file search.
- Supports maximum search depth.
- Skips hidden/system files and common development/system clutter.
- Cancellable search keeps typing responsive.
- Search result count is capped for speed.
- Browse mode includes file-type filter chips.
- Opens files with the default app.
- Opens folders or containing locations.

## Clipboard

- Watches system clipboard.
- Stores recent text entries in memory.
- Stores recent image entries with thumbnails.
- Deduplicates consecutive identical text entries.
- Large text is truncated to a capped stored preview to reduce RAM use.
- Image entries are capped separately to avoid memory growth.
- Clipboard history size is configurable.
- Clipboard history can be cleared manually.
- Clipboard history can be cleared on app exit.
- Clipboard content is not persisted to disk.

## Actions

Actions are built-in utilities that appear through search or the Actions mode.

### Calculator

- Detects math expressions.
- Evaluates arithmetic.
- Shows result in preview.
- Supports copying result.

### Timer

- Supports timer commands such as minutes and seconds.
- Shows countdown.
- Shows progress bar.
- Supports cancel.
- Sends notification when finished.

### Color

- Detects hex color values.
- Shows color swatch.
- Shows hex value.
- Shows RGB value.
- Shows HSL value.
- Supports copying values.

### IP

- Shows local IP.
- Shows public IP.
- Supports copying values.

### AI Assistant

- Supports AI prompt entry.
- Supports follow-up conversation in the same thread.
- Streams assistant responses.
- Full-width AI mode hides the left action/results panel.
- Chat messages use role labels and bubbles.
- Composer supports Enter to send.
- Composer supports Shift+Enter for newline.
- Mouse wheel scrolling works in long answers.
- Shows missing-key error when no API key is configured.
- Supports multiple providers:
  - Groq
  - Gemini
  - OpenRouter
  - DeepSeek
- Supports provider-specific model selection.
- API keys are stored locally with Windows user encryption through DPAPI.

## Settings

Settings are available through `Ctrl+,`, search, or the Settings action.

Appearance:

- Theme selection: dark, light, system.
- Blur effect toggle.
- Window opacity slider.
- Background color field and swatch.
- Accent color mode: Windows accent or custom.
- Launcher width slider.
- Font-related configuration support through config.
- Corner radius and compact mode support through config.

Search:

- Toggle apps indexing.
- Toggle files indexing.
- Toggle folders indexing.
- Toggle clipboard indexing.
- Configure indexed folders.
- Configure file extensions.
- Toggle fuzzy search.

Hotkey:

- View current shortcut.
- Record/edit shortcut.

System:

- Launch on startup.
- Hide to tray.
- Show tray icon.
- App re-index interval: 3h, 6h, 12h, manual.

Privacy:

- Clear clipboard on exit.
- Clear all usage/history data.

AI:

- Select provider.
- Select provider-specific model.
- Add API key.
- Refresh app catalog.

Settings save instantly.

## Themes

- Dark theme.
- Light theme.
- System theme option.
- Resource-based WPF styling.
- Shared common styles.
- Dynamic resources for typography, surfaces, borders, hover states, and semantic colors.

## Performance And Memory

- App catalog is cache-first.
- App discovery refreshes in the background.
- File search is cancellable.
- File search has a max result cap.
- Icon extraction cache is capped.
- Icon size is reduced for lower memory use.
- Clipboard image count is capped.
- Large clipboard text is truncated instead of storing full giant strings.
- Config saves are serialized to avoid save races.

## Installer

- Inno Setup script exists at `installer.iss`.
- Installer name: `Arc-Setup`.
- App name: Arc.
- Default install directory: `{autopf}\Arc`.
- Creates Start Menu shortcut.
- Creates desktop shortcut.
- Can launch Arc after install.

