# Arc UX Flow

Arc is designed around one promise: press the launcher shortcut, type or choose a mode, act, and disappear. Every panel is a continuation of that loop.

## Primary Mental Model

Arc has five major states:

1. Neutral launcher
2. Search results
3. Browse mode
4. Action preview
5. Settings

The search bar is always the anchor. Panels do not feel like separate screens; they are expansions of the same floating surface.

## Entry Flow

The user opens Arc with the global hotkey, usually `Alt+Space`.

Flow:

1. User presses hotkey.
2. Arc appears centered with a fade and slight scale animation.
3. Input is focused immediately.
4. If the query is empty, only the search pill is visible.
5. Hovering near the pill reveals the floating mode icons.

The first screen should feel empty in a good way: no onboarding, no cards, no dashboard, no explanatory copy.

## Neutral To Search

When the user types:

1. Query updates instantly.
2. Floating mode icons close.
3. Search card expands downward.
4. Results appear under a thin divider.
5. First result is selected when available.

The result list combines enabled categories unless the user selected a category. Results are fuzzy-ranked and frequency-aware.

Keyboard flow:

- `Down` and `Up` move selection.
- `Enter` opens the selected result.
- `Ctrl+Enter` performs a secondary action, usually open folder/location or copy.
- `Ctrl+Shift+Enter` runs supported app results as administrator.
- `Esc` clears the query first, then exits active state or closes the launcher.

## Floating Mode Flow

The floating mode icons are visible only when the launcher is neutral.

Flow:

1. User opens Arc.
2. User hovers over the main grid.
3. The mode strip slides out from the right.
4. User clicks Apps, Files, Clipboard, or Actions.
5. The launcher enters that browse mode.
6. The floating strip hides and stays hidden.

The important UX detail is that hover is exploratory only. Once a mode is active, the UI stops shifting and becomes stable. This preserves the premium feel and prevents accidental panel changes while the user is working.

## Browse Mode Flow

Browse mode is for users who want to explore without typing.

### Apps Flow

1. User clicks Apps mode or presses the apps shortcut.
2. Arc expands into the apps panel.
3. Suggested apps may appear first.
4. All apps appear in grid view by default.
5. User can switch to list view from the overflow menu.
6. Clicking or pressing Enter launches an app.

If an app is missing, the user goes to Settings and presses Refresh in the AI/App catalog section. Refresh clears the cached catalog and reruns discovery.

### Files Flow

1. User clicks Files mode.
2. Arc shows filter chips across the top.
3. Recent/relevant files appear below.
4. User filters by type or searches from the main query.
5. Enter opens the file or folder.
6. Ctrl+Enter opens the containing location.

Files mode should feel like a quick shelf of user-facing files, not a full file manager.

### Clipboard Flow

1. User clicks Clipboard mode or searches clipboard text.
2. Clipboard entries appear newest first.
3. Text entries show a short preview.
4. Image entries show a thumbnail.
5. Enter reuses the selected clipboard entry.
6. Clear removes clipboard history.

Large text is intentionally truncated for memory safety. The user still sees useful copied content, but Arc does not keep huge strings in RAM.

### Actions Flow

1. User clicks Actions mode or types an action trigger.
2. Arc shows action rows.
3. Selecting an action can open a preview.
4. Action-specific output appears in the preview panel.

Actions should feel like utilities, not separate apps.

## Action Preview Flow

Action previews appear when a command needs output or follow-up interaction.

### Calculator

1. User types a math expression.
2. Calculator result appears.
3. Enter can copy or use the result depending on selection context.

The flow is instant and should not require an explicit calculator mode.

### Color

1. User types a hex color.
2. Arc shows a swatch and values.
3. User can copy the useful value.

The color panel is a compact inspector.

### Timer

1. User types a timer command.
2. Timer preview appears.
3. User starts or cancels the countdown.
4. Arc sends a Windows notification when complete.

Timer animation is limited to the progress bar.

### IP

1. User types `ip`.
2. Arc shows local and public IP values.
3. User can copy the values.

The panel should remain simple and data-first.

### AI

1. User types an `ai` prompt or selects AI Assistant.
2. Arc enters full-width AI mode.
3. Left action/results panel disappears.
4. Chat header, message list, and composer appear.
5. User sends with Enter.
6. AI response streams into the current assistant bubble.
7. User can ask follow-ups in the same thread.
8. `Esc` backs out of the mode.

The AI experience is intentionally full-width because long answers and follow-ups need space. The prior split layout was too cramped.

## Settings Flow

Settings can be opened by typing `settings`, choosing the settings action, or pressing `Ctrl+,`.

Flow:

1. Arc opens the settings panel under the search bar.
2. Search context is suspended.
3. User scrolls through grouped settings.
4. Changes save immediately.
5. Close button or `Esc` returns to the launcher.

Settings are grouped by task:

- Appearance controls how Arc looks.
- Search controls what Arc indexes.
- Search folders and file types tune file discovery.
- Hotkey controls launch access.
- System controls startup/tray behavior.
- Privacy controls clipboard/history cleanup.
- AI controls provider, model, API key, and app catalog refresh.

Settings should never feel like a wizard. It is a compact control surface for power users.

## Back And Escape Behavior

`Esc` should follow a predictable unwind order:

1. Close settings if open.
2. Clear query if text exists.
3. Exit active action/mode if one is active.
4. Close the launcher if already neutral.

This lets users tap Escape without thinking and always move one level back.

## Animation Feel

Arc animations should feel like the app is calmly becoming available.

Open:

- Fade in
- Slight scale from smaller to full size
- Fast enough to feel instant

Close:

- Fade out
- Slight scale down
- Faster than opening

Panel expand:

- Content appears under the search bar
- Window shifts from pill to rounded rectangle
- No bounce

Floating icons:

- Slide horizontally
- Soft easing
- Hidden when active

Rows:

- Hover background appears quickly
- Selection changes feel immediate
- No layout shift

AI:

- The only motion is text streaming and scrolling
- Composer remains anchored at bottom

## Error And Empty States

Empty states should be short and quiet.

Examples:

- No app results: show an empty browse/list state, not a modal.
- Missing AI key: show a direct settings-oriented error.
- App discovery loading: show the small loading bar and `Loading apps...`.
- Network/API errors: show readable error text inside the AI panel.

Arc should not interrupt the user with modal dialogs unless Windows itself requires it.

