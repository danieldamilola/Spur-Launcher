using System.Text.Json.Serialization;

namespace Volt.Models;

/// <summary>All user-configurable settings. Persisted as JSON.</summary>
public sealed class VoltConfig
{
    // ═══════════════════════════════════════════════════════════════
    // Appearance
    // ═══════════════════════════════════════════════════════════════

    /// <summary>"dark" | "light" | "system"</summary>
    public string Theme { get; set; } = "dark";

    /// <summary>Whether acrylic/blur backdrop is enabled on the launcher window.</summary>
    public bool EnableBlur { get; set; } = true;

    /// <summary>Whether Windows Acrylic material is used (more expensive but nicer).</summary>
    public bool EnableAcrylic { get; set; } = true;

    /// <summary>Window opacity as a fraction (0.0–1.0).</summary>
    public double WindowOpacity { get; set; } = 0.95;

    /// <summary>Custom accent color in hex (#RRGGBB). Empty = follow Windows accent.</summary>
    public string AccentColor { get; set; } = string.Empty;

    /// <summary>When true, the Windows accent color overrides AccentColor.</summary>
    public bool UseWindowsAccentColor { get; set; } = true;

    /// <summary>UI font family name.</summary>
    public string FontFamily { get; set; } = "Segoe UI Variable";

    /// <summary>Base font size in px.</summary>
    public double FontSize { get; set; } = 14;

    /// <summary>Launcher window width in px.</summary>
    public double LauncherWidth { get; set; } = 680;

    /// <summary>How many results to display before scrolling (5, 8, or 10).</summary>
    public int ResultsCount { get; set; } = 8;

    /// <summary>Show category labels in the browse panel.</summary>
    public bool ShowCategoryLabels { get; set; } = true;

    /// <summary>Corner radius in px (0 = sharp, 12 = pill).</summary>
    public double CornerRadius { get; set; } = 12;

    /// <summary>Animation speed: "instant", "fast", or "smooth".</summary>
    public string AnimationSpeed { get; set; } = "smooth";

    /// <summary>Show app icons in results.</summary>
    public bool ShowAppIcons { get; set; } = true;

    /// <summary>Compact mode with tighter rows.</summary>
    public bool CompactMode { get; set; } = false;

    // ═══════════════════════════════════════════════════════════════
    // Search
    // ═══════════════════════════════════════════════════════════════

    public bool IndexApps { get; set; } = true;
    public bool IndexFiles { get; set; } = true;
    public bool IndexFolders { get; set; } = true;
    public bool IndexClipboard { get; set; } = true;
    public bool IndexCalculator { get; set; } = true;

    /// <summary>Whether file search is enabled. Convenience alias for IndexFiles.</summary>
    [JsonIgnore]
    public bool FileSearchEnabled
    {
        get => IndexFiles;
        set => IndexFiles = value;
    }

    /// <summary>Whether clipboard monitoring is enabled. Convenience alias for IndexClipboard.</summary>
    [JsonIgnore]
    public bool ClipboardEnabled
    {
        get => IndexClipboard;
        set => IndexClipboard = value;
    }

    /// <summary>User-selected folders to include in file search.</summary>
    public List<string> IndexedFolders { get; set; } = [];

    /// <summary>User-selected folders to exclude from file search.</summary>
    public List<string> ExcludedFolders { get; set; } = [];

    /// <summary>Whitelisted file extensions for file search.</summary>
    public List<string> FileExtensions { get; set; } =
    [
        ".pdf", ".docx", ".doc", ".txt", ".xlsx", ".xls",
        ".pptx", ".ppt", ".csv", ".md", ".rtf",
    ];

    /// <summary>Enable fuzzy matching in search.</summary>
    public bool FuzzySearch { get; set; } = true;

    /// <summary>Maximum directory depth for recursive file search (1–5).</summary>
    public int MaxFileDepth { get; set; } = 3;

    // ═══════════════════════════════════════════════════════════════
    // Hotkey
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Global open shortcut, e.g. "Alt+Space".</summary>
    public string Shortcut { get; set; } = "Alt+Space";

    /// <summary>Whether the global hotkey is enabled.</summary>
    public bool HotkeyEnabled { get; set; } = true;

    // ═══════════════════════════════════════════════════════════════
    // Startup & Performance
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Launch Volt when Windows starts.</summary>
    public bool LaunchOnStartup { get; set; } = true;

    /// <summary>Minimize to system tray instead of closing.</summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>Re-discover apps on every startup.</summary>
    public bool ReIndexOnStartup { get; set; } = false;

    /// <summary>Interval in hours between automatic re-indexing. 0 = never.</summary>
    public int ReIndexIntervalHours { get; set; } = 6;

    /// <summary>Run indexing in the background without blocking the UI.</summary>
    public bool BackgroundIndexing { get; set; } = true;

    /// <summary>Maximum number of items in clipboard history.</summary>
    public int ClipboardHistorySize { get; set; } = 50;

    // ═══════════════════════════════════════════════════════════════
    // Privacy
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Auto-exclude sensitive folders (AppData, .git, etc.) from search.</summary>
    public bool ExcludeSensitiveFolders { get; set; } = true;

    /// <summary>Clear clipboard history when Volt exits.</summary>
    public bool ClearClipboardOnExit { get; set; } = false;

    /// <summary>Log search queries for frequency tracking.</summary>
    public bool LogSearchHistory { get; set; } = false;

    // ═══════════════════════════════════════════════════════════════
    // Result behavior
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Open with Enter key (true) or single click (false).</summary>
    public bool OpenWithEnter { get; set; } = true;

    /// <summary>Hide launcher after opening something.</summary>
    public bool CloseAfterLaunch { get; set; } = true;

    /// <summary>Show recently used items first.</summary>
    public bool ShowRecentFirst { get; set; } = true;

    // ═══════════════════════════════════════════════════════════════
    // AI
    // ═══════════════════════════════════════════════════════════════

    /// <summary>"groq" | "gemini" | "openrouter" | "deepseek"</summary>
    public string AiProvider { get; set; } = "groq";

    /// <summary>Per-provider API keys.</summary>
    public string GroqApiKey       { get; set; } = string.Empty;
    public string GeminiApiKey     { get; set; } = string.Empty;
    public string OpenRouterApiKey { get; set; } = string.Empty;
    public string DeepSeekApiKey   { get; set; } = string.Empty;

    /// <summary>Per-provider model selections.</summary>
    public string GroqModel        { get; set; } = "llama-3.1-8b-instant";
    public string GeminiModel      { get; set; } = "gemini-2.0-flash";
    public string OpenRouterModel  { get; set; } = "google/gemini-2.0-flash-001";
    public string DeepSeekModel    { get; set; } = "deepseek-chat";

    // ═══════════════════════════════════════════════════════════════
    // Pinned items
    // ═══════════════════════════════════════════════════════════════

    /// <summary>List of pinned result IDs.</summary>
    public HashSet<string> PinnedItems { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    public VoltConfig Clone() => new()
    {
        // Appearance
        Theme                = Theme,
        EnableBlur           = EnableBlur,
        EnableAcrylic        = EnableAcrylic,
        WindowOpacity        = WindowOpacity,
        AccentColor          = AccentColor,
        UseWindowsAccentColor = UseWindowsAccentColor,
        FontFamily           = FontFamily,
        FontSize             = FontSize,
        LauncherWidth        = LauncherWidth,
        ResultsCount         = ResultsCount,
        ShowCategoryLabels   = ShowCategoryLabels,
        CornerRadius          = CornerRadius,
        AnimationSpeed        = AnimationSpeed,
        ShowAppIcons          = ShowAppIcons,
        CompactMode           = CompactMode,
        // Search
        IndexApps            = IndexApps,
        IndexFiles           = IndexFiles,
        IndexFolders         = IndexFolders,
        IndexClipboard       = IndexClipboard,
        IndexCalculator      = IndexCalculator,
        IndexedFolders       = [..IndexedFolders],
        ExcludedFolders      = [..ExcludedFolders],
        FileExtensions       = [..FileExtensions],
        FuzzySearch          = FuzzySearch,
        MaxFileDepth         = MaxFileDepth,
        // Hotkey
        Shortcut             = Shortcut,
        HotkeyEnabled        = HotkeyEnabled,
        // Startup & Performance
        LaunchOnStartup      = LaunchOnStartup,
        MinimizeToTray       = MinimizeToTray,
        ReIndexOnStartup     = ReIndexOnStartup,
        ReIndexIntervalHours = ReIndexIntervalHours,
        BackgroundIndexing   = BackgroundIndexing,
        ClipboardHistorySize = ClipboardHistorySize,
        // Privacy
        ExcludeSensitiveFolders = ExcludeSensitiveFolders,
        ClearClipboardOnExit    = ClearClipboardOnExit,
        LogSearchHistory        = LogSearchHistory,
        // Result behavior
        OpenWithEnter          = OpenWithEnter,
        CloseAfterLaunch       = CloseAfterLaunch,
        ShowRecentFirst        = ShowRecentFirst,
        // AI
        AiProvider           = AiProvider,
        GroqApiKey           = GroqApiKey,
        GeminiApiKey         = GeminiApiKey,
        OpenRouterApiKey     = OpenRouterApiKey,
        DeepSeekApiKey       = DeepSeekApiKey,
        GroqModel            = GroqModel,
        GeminiModel          = GeminiModel,
        OpenRouterModel      = OpenRouterModel,
        DeepSeekModel        = DeepSeekModel,
        // Pinned
        PinnedItems          = new HashSet<string>(PinnedItems, StringComparer.OrdinalIgnoreCase),
    };
}
