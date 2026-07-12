# Spur Reusable Component Review

Date: 2026-07-10

## Goal

This review looks for places where Spur can replace repeated layout, hardcoded strings, hardcoded dimensions, and scattered result-building logic with reusable modules. The goal is not abstraction for its own sake. The goal is better readability, locality, and leverage: one clear interface hiding repeated implementation detail.

## Highest-value reusable components

### 1. Settings section + settings row components

**Recommendation strength:** Strong

**Create:**

- `Views/Controls/SettingsSection.xaml`
- `Views/Controls/SettingsRow.xaml`
- Optional small variants: `SettingsIcon.xaml`, `SettingsInlineControlHost.xaml`

**Where it should be used:**

- `Views/Settings/GeneralSettingsPanel.xaml`
- `Views/Settings/SearchSettingsPanel.xaml`
- `Views/Settings/AiSettingsPanel.xaml`
- `Views/Settings/ShortcutSettingsPanel.xaml`
- `Views/Settings/AddOnsSettingsPanel.xaml`
- `Views/Settings/AboutSettingsPanel.xaml`
- Add-on settings views under `Extensions/AddOns/*/*SettingsView.xaml`

**Current repeated pattern:**

Most settings panels repeat this structure manually:

- Uppercase section heading
- Surface border with corner radius, border brush, and margin
- Repeated row grid with `MinHeight="56"`
- Icon column, spacer column, text column, control column
- Title text at `FontSize="13"` and `FontWeight="Medium"`
- Hint text at `FontSize="11"` and `Margin="0,2,0,0"`
- Divider border between rows

Examples:

- `Views/Settings/GeneralSettingsPanel.xaml`: Appearance, Behavior, Startup, Pinned Categories
- `Views/Settings/SearchSettingsPanel.xaml`: Search feel rows and folder list panels
- `Views/Settings/AiSettingsPanel.xaml`: Provider, API Key, Behavior

**Why this matters:**

The current XAML is readable in small sections, but the repeated layout makes every setting look more complicated than it is. Adding a new setting means copying 15-30 lines of layout and hoping all spacing, icon sizing, font sizes, and divider rules match.

**Suggested interface:**

```xml
<controls:SettingsSection Header="APPEARANCE">
    <controls:SettingsRow
        Icon="/Assets/Icons/theme.png"
        Title="Theme"
        Description="Choose a quiet surface or follow Windows.">
        <controls:SegmentedOptions ... />
    </controls:SettingsRow>
</controls:SettingsSection>
```

**Hardcoded things it replaces:**

- `MinHeight="56"`
- `Margin="18,0"`
- `ColumnDefinition Width="36"`
- `ColumnDefinition Width="14"`
- `Width="32" Height="32"`
- `CornerRadius="8"`
- repeated `FontSize="13"`
- repeated `FontSize="11"`
- repeated section panel `CornerRadius="10"`
- repeated section heading style

**Result:**

Settings panel files become a list of settings instead of a wall of layout mechanics. The reusable row becomes the one place to tune settings density, icon size, typography, and divider behavior.

## 2. Segmented option group component

**Recommendation strength:** Strong

**Create:**

- `Views/Controls/SegmentedOptionGroup.xaml`
- `Models/OptionItem.cs` or reuse an existing simple option model if one exists

**Where it should be used:**

- Theme selector in `GeneralSettingsPanel.xaml`
- Accent color mode selector in `GeneralSettingsPanel.xaml`
- View mode selector in `GeneralSettingsPanel.xaml`
- Window mode selector in `GeneralSettingsPanel.xaml`
- Position selector in `GeneralSettingsPanel.xaml`
- Result count selector in `SearchSettingsPanel.xaml`

**Current repeated pattern:**

The panels repeatedly create a bordered container with a horizontal stack of `RadioButton` elements using `SegmentedRadio`.

**Why this matters:**

The style `SegmentedRadio` is reusable, but the option group itself is still copied everywhere. Each group hardcodes its own wrapper border, padding, group name, and option labels.

**Suggested interface:**

```xml
<controls:SegmentedOptionGroup
    ItemsSource="{Binding ThemeOptions}"
    SelectedValue="{Binding Theme, Mode=TwoWay}" />
```

**Hardcoded things it replaces:**

- `Background="{DynamicResource SelectedBg}"`
- `BorderBrush="{DynamicResource BorderBrush}"`
- `CornerRadius="7"`
- `Padding="2"`
- many repeated `RadioButton Content="..." GroupName="..."`

**Result:**

Option lists move into ViewModel data. The XAML no longer needs one boolean property per option like `ThemeDark`, `ThemeLight`, `ThemeSystem`, `Results5`, `Results8`, `Results10`.

## 3. Settings collection editor component

**Recommendation strength:** Strong

**Create:**

- `Views/Controls/SettingsCollectionEditor.xaml`

**Where it should be used:**

- Indexed folders in `SearchSettingsPanel.xaml`
- Excluded folders in `SearchSettingsPanel.xaml`
- Exclusion patterns in `SearchSettingsPanel.xaml`
- File types list if exposed in settings
- Any future keyword, folder, or custom list settings

**Current repeated pattern:**

`SearchSettingsPanel.xaml` repeats add-row + item-list + remove-button blocks for indexed folders, excluded folders, and exclusion patterns.

**Suggested interface:**

```xml
<controls:SettingsCollectionEditor
    Header="INDEXED FOLDERS"
    HelpText="Folders listed here will be indexed."
    ItemsSource="{Binding IndexedFoldersList}"
    NewItem="{Binding NewFolderPath, Mode=TwoWay}"
    AddCommand="{Binding AddFolderCommand}"
    RemoveCommand="{Binding RemoveFolderCommand}"
    BrowseCommand="{Binding BrowseFolderCommand}" />
```

**Hardcoded things it replaces:**

- repeated add row grid
- repeated input border
- repeated item template
- repeated remove button
- repeated `FontSize="12"` and `Margin="0,0,0,6"`
- repeated folder/pattern panel chrome

**Result:**

Folder and pattern settings become data-driven. The only per-list difference is labels, commands, and whether a browse button is shown.

## 4. Preview panel component

**Recommendation strength:** Strong

**Create:**

- `Views/Controls/PreviewPanel.xaml`
- `Models/PreviewContent.cs` or a small ViewModel-owned preview DTO

**Where it should be used:**

- Floating clipboard preview in `MainWindow.xaml`
- Inline file preview in `MainWindow.xaml`
- `Views/FilePreviewPanel.xaml`
- Details pane in `Views/ClipboardManager.xaml`

**Current repeated pattern:**

Preview UI exists in several places with slightly different chrome:

- `FilePreviewPanel.xaml`
- `MainWindow.xaml` floating clipboard preview
- `MainWindow.xaml` inline file preview
- Clipboard manager detail panel

All of them need the same ideas: title, metadata, optional actions, scrollable text, optional image, max height, and monospaced preview text.

**Suggested interface:**

```xml
<controls:PreviewPanel
    Title="{Binding PreviewTitle}"
    Metadata="{Binding PreviewMetadata}"
    Text="{Binding PreviewText}"
    Image="{Binding PreviewImage}"
    Actions="{Binding PreviewActions}" />
```

**Hardcoded things it replaces:**

- preview widths like `Width="280"` and `Width="260"`
- preview max heights like `MaxHeight="280"` and `MaxHeight="424"`
- repeated header typography
- repeated scroll viewer settings
- repeated monospaced text preview blocks
- repeated copy/pin/delete button layout

**Result:**

Preview behavior becomes consistent and future preview types can be added without touching `MainWindow.xaml`.

## 5. Icon button component/style

**Recommendation strength:** Strong

**Create:**

- `IconButton` style in `Themes/CommonStyles.xaml`, or
- `Views/Controls/IconButton.xaml` if command, tooltip, selected state, and icon source need to be formalized

**Where it should be used:**

- Floating clipboard preview buttons in `MainWindow.xaml`
- Clipboard result row actions in `Themes/ResultTemplates.xaml`
- Clipboard manager row actions in `Views/ClipboardManager.xaml`
- Settings edit/browse/add/remove buttons when the button is icon-first
- Back/escape category control in `MainWindow.xaml`

**Current repeated pattern:**

Small square buttons repeat `Width`, `Height`, `Padding`, `ToolTip`, background, hover border, and glyph details. Some use `ui:FontIcon`, one uses a hand-coded `Path`, and sizes differ between `26`, `28`, and `30`.

**Suggested interface:**

```xml
<controls:IconButton
    Icon="Pin"
    ToolTip="Pin"
    Command="{Binding TogglePinCommand}" />
```

**Hardcoded things it replaces:**

- `Width="26" Height="26"`
- `Width="28" Height="28"`
- `Width="30" Height="30"`
- repeated icon font sizes `11`, `12`, `13`, `14`
- repeated hover background triggers
- inline delete path geometry in result templates

**Result:**

Buttons become visually consistent and accessible names/tooltips are less likely to be forgotten.

## 6. Add-on result builder helper

**Recommendation strength:** Worth exploring

**Create:**

- `Extensions/AddOnSearchResultFactory.cs`

**Where it should be used:**

- `Extensions/AddOns/Timer/TimerAddOn.cs`
- `Extensions/AddOns/PasswordGen/PasswordGenAddOn.cs`
- `Extensions/AddOns/Currency/CurrencyAddOn.cs`
- `Extensions/AddOns/Color/ColorAddOn.cs`
- `Extensions/AddOns/KillProcess/KillProcessAddOn.cs`
- `Extensions/AddOns/System/SystemAddOn.cs`
- `Extensions/AddOns/Shell/ShellAddOn.cs`
- `Services/SearchEngineService.cs` for action catalog, URL, and web action rows

**Current repeated pattern:**

Many add-ons manually construct `SearchResult` with the same fields:

- `Type = ResultType.Action`
- `IconGlyph = IconGlyph`
- `IconPath = IconPath`
- `ActionId = Id`
- IDs like `action:pw:{length}`, `timer:{input}`, `action:cur:{...}`

**Suggested interface:**

```csharp
return AddOnSearchResultFactory.Action(
    addOn: this,
    id: $"timer:{input}",
    name: label,
    subtitle: $"timer {input}");
```

**Hardcoded things it replaces:**

- repeated `ResultType.Action`
- repeated icon assignment
- repeated add-on action ID assignment
- ad hoc invalid-result rows
- duplicated action catalog row construction

**Result:**

Each add-on reads like intent: parse input, choose result text, return action row. The boilerplate disappears.

## 7. Category identity module

**Recommendation strength:** Strong

**Create or deepen:**

- Deepen existing `Models/Category.cs`
- Replace raw category strings with `CategoryNames` constants or a `SearchScope` value object

**Where it should be used:**

- `MainWindow.xaml.cs`
- `ViewModels/MainViewModel.cs`
- `Services/SearchEngineService.cs`
- `App.xaml.cs`
- Tests in `Spur.Tests/MainViewModelTests.cs`

**Current repeated hardcoded strings:**

- `"clipboard"`
- `"files"`
- `"actions"`
- `"apps"`
- `"web"`
- `"ai"`
- `"timer"`
- `"system"`
- `"shell"`

Examples found:

- `MainWindow.xaml.cs` checks active category against `"clipboard"` in several places.
- `MainViewModel.cs` maps categories and placeholders with raw strings.
- `SearchEngineService.cs` controls filtering with raw string checks.
- `CategoryNames` exists, but it is not used consistently.

**Suggested interface:**

```csharp
if (CategoryNames.IsClipboard(ActiveCategory)) { ... }
```

or:

```csharp
SearchScope? ActiveScope { get; set; }
```

**Hardcoded things it replaces:**

- scattered string literals for category IDs
- switch expressions that repeat the same category names
- fragile tests that depend on manually typed strings

**Result:**

Renaming or adding a scope becomes local. Misspellings become compile-time issues if this becomes a value object or enum-backed module.

## 8. Launcher layout constants/tokens

**Recommendation strength:** Strong

**Create:**

- Add missing XAML resources to `Themes/Tokens.xaml` or `Themes/CommonStyles.xaml`
- Use these resources in `MainWindow.xaml`, result templates, clipboard manager, and settings panels

**Where it should be used:**

- `MainWindow.xaml`
- `Themes/ResultTemplates.xaml`
- `Views/ClipboardManager.xaml`
- `Views/FilePreviewPanel.xaml`
- `Views/HomePanel.xaml`
- all settings panels

**Current hardcoded values:**

- Row heights: `56`, `52`, `44`, `34`, `32`, `30`, `28`, `26`
- Icon sizes: `18`, `16`, `15`, `14`, `13`, `12`, `11`
- Preview widths: `260`, `280`, `320`
- Max heights: `280`, `352`, `424`
- Margins: `18,0`, `16,14`, `14,12,14,8`, `10,8,10,4`
- Radius values: `4`, `6`, `7`, `8`, `10`, `999`

**Suggested token names:**

- `Launcher.SearchRowHeight`
- `Launcher.ResultMaxHeight`
- `Launcher.PreviewWidth`
- `Launcher.PreviewMaxHeight`
- `Settings.RowHeight`
- `Settings.IconBoxSize`
- `Settings.IconSize`
- `Settings.PanelSpacing`
- `Button.IconSmallSize`
- `Button.IconMediumSize`

**Result:**

Layout decisions move into a small token surface. XAML becomes easier to scan and the design principles become enforceable.

## 9. App/add-on metadata source of truth

**Recommendation strength:** Worth exploring

**Create or deepen:**

- `AddOnDescriptor` record
- possibly load built-in add-on metadata from a single strongly typed list

**Where it should be used:**

- `Extensions/AddOnRegistry.cs`
- individual `IAddOn` implementations
- `Models/SpurConstants.cs` action catalog rows
- README/docs generated tables later if desired

**Current pattern:**

Add-on identity appears in multiple places:

- `AddOnRegistry.RegisterLazy(...)`
- each add-on class properties
- `SpurConstants.DefaultActions`
- README built-in add-ons table
- settings/add-on UI

**Why this matters:**

This is not urgent because the registry already centralizes much of it, but duplicate names, icons, keywords, and descriptions are likely to drift over time.

**Suggested interface:**

```csharp
public sealed record AddOnDescriptor(
    string Id,
    string Name,
    string Description,
    string IconGlyph,
    string? IconPath,
    string DefaultKeyword,
    bool IsGlobal);
```

**Result:**

The app has one authoritative description of built-in add-ons. The registry, settings, action catalog, and docs can all depend on it.

## 10. External HTTP/cache client for network add-ons

**Recommendation strength:** Worth exploring

**Create:**

- `Services/IExchangeRateService.cs`
- `Services/IPublicIpService.cs` if IP lookup has similar behavior
- or a small `AddOnHttpCache<T>` helper if only simple caching is needed

**Where it should be used:**

- `Extensions/AddOns/Currency/CurrencyAddOn.cs`
- `Extensions/AddOns/Ip/IpAddOn.cs`
- future network add-ons

**Current pattern:**

`CurrencyAddOn` owns a static `HttpClient`, static cache fields, fetch timing, JSON parsing, and user-facing action behavior in one class.

**Why this matters:**

The add-on is now both UI action and network data source. Pulling exchange-rate fetching into a service would make tests easier and keep add-on execution focused.

**Result:**

Network behavior becomes testable without executing the add-on UI path, and retry/cache policy can be shared by future add-ons.

## Hardcoded values to replace first

These should be replaced before or during component extraction:

- Category strings: use `CategoryNames` everywhere for `apps`, `files`, `clipboard`, and `actions`.
- Add-on IDs used as modes: centralize `"ai"`, `"timer"`, `"system"`, `"shell"`, `"pw"`, `"kill"`, `"note"`.
- Settings row dimensions: move `56`, `36`, `14`, `32`, `18`, `13`, and `11` into styles/components.
- Preview dimensions: move `260`, `280`, `320`, `424` into preview tokens.
- Icon paths: consider centralizing common paths like `/Assets/Icons/copy.png`, `/Assets/Icons/search.png`, `/Assets/Icons/url.png`, `/Assets/Icons/settings.png`.
- User-facing strings in settings panels: consider `.resx` usage for all settings labels, not just a few settings titles.

## Suggested implementation order

1. **Use `CategoryNames` consistently.** This is small, safe, and reduces string drift immediately.
2. **Create `SettingsSection` and `SettingsRow`.** Apply first to `AiSettingsPanel.xaml` because it is compact and proves the pattern without touching the largest file.
3. **Apply the settings components to `GeneralSettingsPanel.xaml` and `SearchSettingsPanel.xaml`.** These files have the largest duplication payoff.
4. **Create `SegmentedOptionGroup`.** Replace repeated radio groups and gradually simplify ViewModel boolean option properties.
5. **Create `SettingsCollectionEditor`.** Replace indexed folders, excluded folders, and exclusion patterns.
6. **Create `IconButton` style/component.** Replace small action buttons in result rows, clipboard previews, and clipboard manager.
7. **Extract `PreviewPanel`.** Use it to remove preview markup from `MainWindow.xaml`.
8. **Add `AddOnSearchResultFactory`.** Convert add-ons one by one; start with Timer, Password, and Currency because they clearly repeat action-result construction.
9. **Extract network data services for Currency/IP.** Do this after UI readability work, because it changes test seams.

## Top recommendation

Start with the settings UI reusable components.

The settings panels have the clearest duplication and the least domain risk. A good `SettingsSection` + `SettingsRow` pair would immediately improve readability across several files, replace many hardcoded measurements, and give the project a stronger design-system foundation. After that, category string cleanup is the best backend readability win.

