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

    /// <summary>Window opacity as a fraction (0.0–1.0). Clamped on load.</summary>
    public double WindowOpacity { get; set; } = 0.95;

    /// <summary>Width of the main search bar window.</summary>
    public double BarWidth { get; set; } = 640;

    /// <summary>How many results to display before scrolling (5, 8, or 10).</summary>
    public int ResultsCount { get; set; } = 5;


    /// <summary>True after the user completes the first-launch onboarding.</summary>
    public bool OnboardingComplete { get; set; } = false;

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

    // Per-action toggles — each action can be individually disabled in Settings → Actions
    public bool ActionColor      { get; set; } = true;
    public bool ActionTimer      { get; set; } = false;
    public bool ActionIp         { get; set; } = false;
    public bool ActionAi         { get; set; } = false;
    public bool ActionCurrency   { get; set; } = true;
    public bool ActionPasswordGen { get; set; } = false;
    public bool ActionQuickNote  { get; set; } = false;
    public bool ActionKillProcess { get; set; } = false;
    public bool ActionScreenshot { get; set; } = false;

    // ═══════════════════════════════════════════════════════════════
    // Per-action settings (Flow Launcher-style)
    // Each action gets its own settings object with keyword, enabled,
    // and action-specific options. These are the canonical source;
    // the ActionXxx / KeywordXxx properties above are kept for
    // backward compatibility and sync to/from these objects on load/save.
    // ═══════════════════════════════════════════════════════════════
    public SystemActionSettings       System       { get; set; } = new();
    public TimerActionSettings        Timer        { get; set; } = new();
    public KillProcessActionSettings  KillProcess  { get; set; } = new();
    public PasswordGenActionSettings  PasswordGen  { get; set; } = new();
    public ScreenshotActionSettings   Screenshot   { get; set; } = new();
    public QuickNoteActionSettings    QuickNote    { get; set; } = new();
    public CurrencyActionSettings     Currency     { get; set; } = new();
    public ColorActionSettings        Color        { get; set; } = new();
    public IpActionSettings           Ip           { get; set; } = new();
    public AiActionSettings           Ai           { get; set; } = new();

    // ═══════════════════════════════════════════════════════════════
    // Action Keywords (Flow Launcher-style)
    // Type "keyword " (with space) to enter that action's scope.
    // ═══════════════════════════════════════════════════════════════
    public string KeywordSystem    { get; set; } = "sys";
    public string KeywordColor     { get; set; } = "color";
    public string KeywordTimer     { get; set; } = "timer";
    public string KeywordIp        { get; set; } = "ip";
    public string KeywordAi        { get; set; } = "ai";
    public string KeywordCurrency  { get; set; } = "cur";
    public string KeywordPassword  { get; set; } = "pw";
    public string KeywordNote      { get; set; } = "note";
    public string KeywordKill      { get; set; } = "kill";
    public string KeywordScreenshot { get; set; } = "ss";
    public string KeywordClipboard { get; set; } = "c";
    public string KeywordFiles     { get; set; } = "files";
    public string KeywordApps      { get; set; } = "apps";

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


    // ═══════════════════════════════════════════════════════════════
    // Clone
    // ═══════════════════════════════════════════════════════════════

    public SpurConfig Clone()
    {
        var clone = (SpurConfig)MemberwiseClone();
        clone.IndexedFolders = new List<string>(IndexedFolders);
        clone.ExcludedFolders = new List<string>(ExcludedFolders);
        clone.FileExtensions = new List<string>(FileExtensions);
        clone.PinnedItems = new HashSet<string>(PinnedItems, StringComparer.OrdinalIgnoreCase);
        clone.PinnedClipboard = PinnedClipboard.Select(p => new PinnedClipboardItem { Id = p.Id, Content = p.Content, Preview = p.Preview, Timestamp = p.Timestamp }).ToList();
        return clone;
    }
}

