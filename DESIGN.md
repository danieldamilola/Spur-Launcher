# Arc Design System

Arc is a minimal, premium Windows launcher. The visual target is closer to macOS Spotlight than to a traditional Windows utility: one quiet glass surface, restrained contrast, almost no decoration, and details that feel deliberate when hovered, focused, or selected.

The app is built in WPF, so the design language is expressed through XAML resources, WPF brushes, system backdrop behavior, extracted Windows icons, and lightweight storyboards rather than CSS.

## Design Principles

1. **One surface first.** Arc begins as a single floating search bar. Panels appear only when the user has asked for them.
2. **Premium through restraint.** The interface avoids heavy color, thick borders, large cards, and noisy labels. Most elements are glass, muted text, soft hover surfaces, and precise spacing.
3. **Keyboard confidence.** Every visible panel must be usable from the keyboard. Hover polish is welcome, but keyboard flow is the core experience.
4. **Paths are details, not primary content.** App and file rows show clean names and simple type labels. Full paths appear on hover/tooltips where they help without crowding the layout.
5. **Modes feel intentional.** The floating mode icons are a discovery affordance in neutral state. Once a mode is active, the launcher becomes a full focused panel.

## App Shell

The main window is a frameless, topmost WPF window centered on screen. It does not appear in the taskbar. The root has a transparent background and a large outer margin so the glass card and floating icon strip can breathe.

The primary card is `SearchCardBorder` in `MainWindow.xaml`. It starts as a pill and becomes a rounded panel when content appears.

- Idle width: `680px`
- Search row height: `56px`
- Content max height: `464px`
- Idle radius: pill radius from `RadiusPill`
- Expanded radius: window radius from `RadiusWindow`
- Main shadow: large soft black shadow, currently `BlurRadius=64`, `ShadowDepth=16`, `Opacity=0.35`
- Border: single glass border using `GlassBorder`
- Background: `Config.BackgroundColor` mixed with `Config.WindowOpacity`

The app should feel like a floating pane above the desktop, not a separate app window. Transparency and shadow are used to create depth, while the content itself stays flat and quiet.

## Search Bar

The search bar is the anchor of every state. It remains at the top whether the launcher is idle, browsing, searching, previewing an action, or showing settings.

Expected feel:

- Calm, centered, immediately focused
- No extra helper text inside the shell
- Search icon on the left
- Query text in the center lane
- Mode/back affordance integrated into the launcher behavior

When empty and neutral, the search card stays compact and pill-like. When the user types or activates a mode, the card expands downward and becomes a panel.

## Floating Mode Icons

The floating mode strip sits to the right of the search card and slides open on hover. It contains four circular buttons:

- Apps
- Files
- Clipboard
- Actions

Each button is a `44x44` circular glass control with a `18px` line icon. The default state is muted. Hover adds a soft surface. Active state uses stronger foreground and selected surface treatment.

Important behavior:

- The strip appears only when the launcher is neutral: no active category, no active action, and no query.
- Clicking a mode activates it and converts the launcher into the full panel state.
- While a mode is active, hover should not reopen the floating strip.
- The user backs out with `Esc`, by clearing the mode, or by returning to neutral state.

This creates the intended feeling: the icons float as a playful premium affordance, but active work gets a stable, focused panel.

## Results Panel

The results panel is used for typed search across apps, files, clipboard, and actions. It lives under the search bar and uses compact rows.

Result row structure:

- Left icon area: `30px`
- Gap: `11px`
- Main text stack
- Optional enter hint on the right
- Row height: about `46px`
- Row margin: small vertical spacing
- Row radius: `9px`

Visual rules:

- Primary text is the app/file/action name.
- Secondary text uses `DisplaySubtitle`, not raw path.
- Full path/detail is available through `DetailText` tooltip.
- Selected rows invert or strengthen contrast using selected resources.
- Hover is subtle and should never shift layout.

For app and file icons, Arc extracts real Windows icons where possible. Action and fallback icons use line icons through `LucideIconConverter`.

## Browse Panel

The browse panel appears when the user activates a category directly. It replaces normal results with a mode-specific surface.

### Apps

Apps support grid and list views.

Grid view:

- Compact app tiles
- `96x72` tile footprint
- `36x36` icon container
- App name centered under icon
- Suggested apps section can appear above all apps

List view:

- Row layout matches the main result list
- Icon left, name and subtitle right
- Used when scanning many apps is easier than tile browsing

Apps panel includes an overflow button for switching between grid and list. It also shows app count and suggested apps when frequency data exists.

### Files

Files browse mode uses horizontal filter chips at the top:

- All
- Documents
- Images
- PDFs
- Videos
- Music
- Code
- Folders

The file list appears below. Rows keep names prominent and paths secondary. The panel is meant for browsing recent or relevant user files, not system internals.

### Clipboard

Clipboard browse mode has a header with count and a clear action. Text entries use a clipboard line icon. Image entries use a thumbnail. Large copied text is stored as a capped preview so the UI remains fast and memory-conscious.

### Actions

Actions browse mode is a simple list of command rows. It intentionally avoids a busy two-column layout. Each action has a neutral icon block, name, and short instruction. Selecting an action can open a preview panel.

## Preview Panel

The preview panel is used for action outputs: calculator, color, timer, IP, and AI.

For most actions, it appears as a right-hand panel with a fixed preview width. For AI, it becomes full-width and hides the left results/actions panel so the chat has enough space.

Visual rules:

- Background uses `PreviewBg`
- Border is minimal; AI mode removes the left divider entirely
- Padding is `20px`
- Text stays compact and readable
- No nested card-heavy presentation

### AI Chat

AI mode is the most important preview surface. It should feel like a focused chat pane, not a cramped sidebar.

Structure:

- Header: `Ask AI`
- Subtext: `Concise answers, follow-ups stay in this thread`
- Scrollable message area
- Message role labels: `You`, `AI` or `Arc`
- Message bubble with soft surface background
- Composer at bottom with send icon

Behavior:

- AI preview spans the full content area.
- Left actions/results panel is hidden.
- Message list scrolls independently.
- Mouse wheel works inside the AI panel.
- Enter sends; Shift+Enter inserts a newline.
- Streaming updates the current assistant message instead of waiting until completion.

## Settings Panel

Settings replaces the results area and keeps the search shell above it. It is a scrollable panel with grouped cards.

Sections:

- Appearance
- Search
- Search folders
- File types
- Hotkey
- System
- Privacy
- AI
- Version stamp

Settings rows use:

- Section labels in uppercase muted text
- Cards with one-pixel borders
- `48px` rows for simple controls
- `52-58px` rows where descriptions are needed
- Segmented controls for mutually exclusive choices
- Toggle switches for boolean settings
- Sliders for numeric appearance controls
- Password input for API keys

The settings design is utilitarian but still premium: dense enough to work, quiet enough to not feel like a control panel from another app.

## Color System

Arc is monochrome-first. Color is used for focus, semantic meaning, and small identity cues.

Core resources:

- `GlassBg`
- `GlassBorder`
- `PreviewBg`
- `Surface`
- `SurfaceLow`
- `HoverBg`
- `TextPrimary`
- `TextSecondary`
- `TextMuted`
- `BorderBrush`
- `BorderStrong`
- `Accent`
- `Red`
- `Orange`

Dark mode should feel deep but not blue-black. Light mode should feel frosted and clean, not beige or flat white.

## Typography

Arc uses bundled fonts and WPF font resources:

- UI font: DM Sans or the configured UI font family
- Mono font: DM Mono

Use UI font for labels, names, rows, settings, and chat prose. Use mono font for shortcuts, file extensions, IP addresses, calculator values, color values, and technical tokens.

Typography should remain compact. Avoid oversized headings inside panels.

## Motion

Motion should be short and calm.

- Window open: fade and slight scale up
- Window close: fade and slight scale down
- Floating mode strip: horizontal slide, about `180ms`
- Hover states: quick background change
- Selection changes: immediate or near-immediate color update
- Loading apps: small looping bar
- AI: token streaming, no decorative animation

Avoid bounce, spring, staggered list animation, and decorative movement.

