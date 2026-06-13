using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spur.Models;

/// <summary>All user-configurable settings. Persisted as JSON with encrypted API keys.</summary>
public sealed class SpurConfig
{
    // ═══════════════════════════════════════════════════════════════
    // Appearance
    // ═══════════════════════════════════════════════════════════════

    /// <summary>"dark" | "light" | "system"</summary>
    public string Theme { get; set; } = "dark";

    /// <summary>"theme" | "system" | "custom"</summary>
    public string AccentColorMode { get; set; } = "theme";

    /// <summary>Hex code for custom accent color</summary>
    public string CustomAccentColor { get; set; } = "#D7CFC2";

    /// <summary>Window opacity as a fraction (0.0–1.0). Clamped on load.</summary>
    public double WindowOpacity { get; set; } = 0.95;

    /// <summary>Width of the main search bar window.</summary>
    public double BarWidth { get; set; } = 640;

    /// <summary>How many results to display before scrolling (5, 8, or 10).</summary>
    public int ResultsCount { get; set; } = 5;


    /// <summary>True after the user completes the first-launch onboarding.</summary>
    public bool OnboardingComplete { get; set; } = false;

    /// <summary>UI font scale multiplier (0.8–1.4). 1.0 = default size.</summary>
    public double FontScale { get; set; } = 1.0;

    /// <summary>Which monitor to show the launcher on: "primary", "mouse", or a monitor index.</summary>
    public string PreferredMonitor { get; set; } = "primary";


    // ═══════════════════════════════════════════════════════════════
    // Search
    // ═══════════════════════════════════════════════════════════════

    public bool IndexApps      { get; set; } = true;
    public bool IndexFiles     { get; set; } = true;
    public bool IndexFolders   { get; set; } = true;
    public bool IndexClipboard { get; set; } = true;
    public bool IndexCalculator { get; set; } = true;

    // Per-source app indexing (like Flow Launcher's Program plugin)
    public bool IndexUwpApps     { get; set; } = true;   // UWP/Store apps
    public bool IndexStartMenu   { get; set; } = true;   // Start Menu shortcuts
    public bool IndexRegistry    { get; set; } = true;   // Registry (App Paths + Uninstall)
    public bool IndexPath        { get; set; } = true;   // PATH environment variable

    /// <summary>Whether file search is enabled. Convenience alias for IndexFiles.</summary>
    [JsonIgnore]
    public bool FileSearchEnabled
    {
        get => IndexFiles;
        set => IndexFiles = value;
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

    /// <summary>Minimum fuzzy score required for visible matches: "low", "regular", or "strict".</summary>
    public string QuerySearchPrecision { get; set; } = "regular";

    /// <summary>Debounce delay in milliseconds before search starts (0–500). Lower = faster but more CPU.</summary>
    public int SearchDelay { get; set; } = 30;

    /// <summary>Maximum results per section before truncation (3–20).</summary>
    public int MaxResultsPerSection { get; set; } = 5;

    /// <summary>Glob patterns to exclude from file search (e.g. "node_modules", ".git").</summary>
    public List<string> ExclusionPatterns { get; set; } = ["node_modules", ".git", "__pycache__", "bin", "obj"];

    /// <summary>Default web search engine: "google", "duckduckgo", "bing", or "custom".</summary>
    public string WebSearchEngine { get; set; } = "google";

    /// <summary>Custom search URL template. Use {query} as placeholder.</summary>
    public string CustomWebSearchUrl { get; set; } = "";


    /// <summary>What to do with the previous query when the launcher is shown again.</summary>
    public string LastQueryStyle { get; set; } = "clear";

    /// <summary>Enable built-in shell command results.</summary>
    public bool IndexShell { get; set; } = false;

    /// <summary>Enable system command results such as shutdown, lock, and settings.</summary>
    public bool IndexSystemCommands { get; set; } = true;

    /// <summary>Enable URL opening results.</summary>
    public bool IndexUrls { get; set; } = true;

    /// <summary>Enable web search results.</summary>
    public bool IndexWebSearches { get; set; } = true;

    /// <summary>Enable Windows Settings search results.</summary>
    public bool IndexWindowsSettings { get; set; } = true;

    // Legacy action toggles
    [JsonIgnore] public bool ActionColor      { get; set; } = true;
    [JsonIgnore] public bool ActionTimer      { get; set; } = false;
    [JsonIgnore] public bool ActionIp         { get; set; } = false;
    [JsonIgnore] public bool ActionAi         { get; set; } = false;
    [JsonIgnore] public bool ActionCurrency   { get; set; } = true;
    [JsonIgnore] public bool ActionPasswordGen { get; set; } = false;
    [JsonIgnore] public bool ActionQuickNote  { get; set; } = false;
    [JsonIgnore] public bool ActionKillProcess { get; set; } = false;
    [JsonIgnore] public bool ActionScreenshot { get; set; } = false;

    // Per-action settings have been moved to individual extra folders
    // and are stored in the new Extras dictionary below.

    // ═══════════════════════════════════════════════════════════════
    // Action Keywords (Legacy Flow Launcher-style, kept for compat)
    // ═══════════════════════════════════════════════════════════════
    [JsonIgnore] public string KeywordSystem    { get; set; } = "sys";
    [JsonIgnore] public string KeywordColor     { get; set; } = "color";
    [JsonIgnore] public string KeywordTimer     { get; set; } = "timer";
    [JsonIgnore] public string KeywordIp        { get; set; } = "ip";
    [JsonIgnore] public string KeywordAi        { get; set; } = "ai";
    [JsonIgnore] public string KeywordCurrency  { get; set; } = "cur";
    [JsonIgnore] public string KeywordPassword  { get; set; } = "pw";
    [JsonIgnore] public string KeywordNote      { get; set; } = "note";
    [JsonIgnore] public string KeywordKill      { get; set; } = "kill";
    [JsonIgnore] public string KeywordScreenshot { get; set; } = "ss";
    [JsonIgnore] public string KeywordShell      { get; set; } = ">";

    public string KeywordClipboard { get; set; } = "c";
    public string KeywordFiles     { get; set; } = "files";
    public string KeywordApps      { get; set; } = "apps";

    /// <summary>Up to 4 category or extra IDs to pin to the launcher UI.</summary>
    public List<string> PinnedCategories { get; set; } = ["files", "ai", "clipboard", ""];

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

    /// <summary>Launch Spur when Windows starts.</summary>
    public bool LaunchOnStartup { get; set; } = true;

    /// <summary>Minimize to system tray instead of closing.</summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>Show system tray icon.</summary>
    public bool ShowTrayIcon { get; set; } = true;

    /// <summary>Re-discover apps on every startup.</summary>
    public bool ReIndexOnStartup { get; set; } = false;

    /// <summary>Interval in hours between automatic re-indexing. 0 = never.</summary>
    public int ReIndexIntervalHours { get; set; } = 6;

    /// <summary>Run indexing in the background without blocking the UI.</summary>
    public bool BackgroundIndexing { get; set; } = true;

    /// <summary>Maximum number of items in clipboard history.</summary>
    public int ClipboardHistorySize { get; set; } = 50;

    // ═══════════════════════════════════════════════════════════════
    // Clipboard & Privacy
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Whether clipboard monitoring is active.</summary>
    public bool ClipboardEnabled { get; set; } = true;

    /// <summary>Clear clipboard history when Spur exits.</summary>
    public bool ClearClipboardOnExit { get; set; } = false;

    /// <summary>Auto-exclude sensitive folders (AppData, .git, etc.) from search.</summary>
    public bool ExcludeSensitiveFolders { get; set; } = true;

    /// <summary>Log search queries for frequency tracking.</summary>
    public bool LogSearchHistory { get; set; } = false;

    /// <summary>Open with Enter key (true) or single click (false).</summary>
    public bool OpenWithEnter { get; set; } = true;

    /// <summary>Hide launcher after opening something.</summary>
    public bool CloseAfterLaunch { get; set; } = true;

    /// <summary>Show recently used items first.</summary>
    public bool ShowRecentFirst { get; set; } = true;

    /// <summary>Use launcher show/hide animations.</summary>
    public bool AnimationEnabled { get; set; } = true;
    public string UpdateUrl { get; set; } = "https://github.com/danieldamilola/Spur-Launcher/releases/latest/download";

    /// <summary>Play a small sound when the launcher opens.</summary>
    public bool SoundEffectEnabled { get; set; } = false;

    /// <summary>Launcher placement on the selected monitor.</summary>
    public string SearchWindowPosition { get; set; } = "Center Top";

    public double CustomWindowLeft { get; set; } = -1;
    public double CustomWindowTop { get; set; } = -1;

    // ═══════════════════════════════════════════════════════════════
    // AI (API keys are encrypted at rest via DPAPI)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Whether AI results are shown when searching.</summary>
    public bool AiEnabled { get; set; } = false;

    /// <summary>"groq" | "gemini" | "openrouter" | "deepseek"</summary>
    public string AiProvider { get; set; } = "groq";

    /// <summary>Encrypted Groq API key (base64). Decrypt via ProtectedData.</summary>
    public string EncryptedGroqApiKey { get; set; } = string.Empty;

    /// <summary>Encrypted Gemini API key.</summary>
    public string EncryptedGeminiApiKey { get; set; } = string.Empty;

    /// <summary>Encrypted OpenRouter API key.</summary>
    public string EncryptedOpenRouterApiKey { get; set; } = string.Empty;

    /// <summary>Encrypted DeepSeek API key.</summary>
    public string EncryptedDeepSeekApiKey { get; set; } = string.Empty;

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

    /// <summary>Persisted pinned clipboard entries (text-only).</summary>
    public List<PinnedClipboardItem> PinnedClipboard { get; set; } = [];

    // ═══════════════════════════════════════════════════════════════
    // Add-ons Store
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Canonical storage for all add-ons (built-in and community).</summary>
    [System.Text.Json.Serialization.JsonPropertyName("Extras")] public Dictionary<string, AddOnEntryConfig> AddOns { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // ═══════════════════════════════════════════════════════════════
    // Clone
    // ═══════════════════════════════════════════════════════════════

    public SpurConfig Clone()
    {
        var clone = (SpurConfig)MemberwiseClone();
        clone.IndexedFolders = new List<string>(IndexedFolders);
        clone.ExcludedFolders = new List<string>(ExcludedFolders);
        clone.FileExtensions = new List<string>(FileExtensions);
        clone.PinnedCategories = new List<string>(PinnedCategories);
        clone.PinnedItems = new HashSet<string>(PinnedItems, StringComparer.OrdinalIgnoreCase);
        clone.PinnedClipboard = PinnedClipboard.Select(p => new PinnedClipboardItem { Id = p.Id, Content = p.Content, Preview = p.Preview, Timestamp = p.Timestamp }).ToList();
        clone.AddOns = new Dictionary<string, AddOnEntryConfig>(AddOns, StringComparer.OrdinalIgnoreCase);
        clone.ExclusionPatterns = new List<string>(ExclusionPatterns);
        return clone;
    }
}
