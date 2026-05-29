using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arc.Models;

/// <summary>All user-configurable settings. Persisted as JSON with encrypted API keys.</summary>
public sealed class ArcConfig
{
    // ═══════════════════════════════════════════════════════════════
    // Appearance
    // ═══════════════════════════════════════════════════════════════

    /// <summary>"dark" | "light" | "system"</summary>
    public string Theme { get; set; } = "dark";

    /// <summary>Whether blur effect is enabled on the search bar.</summary>
    public bool EnableBlur { get; set; } = true;

    /// <summary>Background surface color in hex (#RRGGBB).</summary>
    public string BackgroundColor { get; set; } = "#141416";

    /// <summary>Custom accent color in hex (#RRGGBB). Empty = follow Windows accent.</summary>
    public string AccentColor { get; set; } = string.Empty;

    /// <summary>When true, the Windows accent color overrides AccentColor.</summary>
    public bool UseWindowsAccentColor { get; set; } = true;

    /// <summary>Window opacity as a fraction (0.0–1.0). Clamped on load.</summary>
    public double WindowOpacity { get; set; } = 0.95;

    /// <summary>UI font family name.</summary>
    public string FontFamily { get; set; } = "Segoe UI Variable";

    /// <summary>Base font size in px.</summary>
    public double FontSize { get; set; } = 14;

    /// <summary>Launcher window width in px (300–1200).</summary>
    public double LauncherWidth { get; set; } = 680;

    /// <summary>How many results to display before scrolling (5, 8, or 10).</summary>
    public int ResultsCount { get; set; } = 5;

    /// <summary>Show category labels in the browse panel.</summary>
    public bool ShowCategoryLabels { get; set; } = true;

    /// <summary>Corner radius in px.</summary>
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

    public bool IndexApps      { get; set; } = true;
    public bool IndexFiles     { get; set; } = true;
    public bool IndexFolders   { get; set; } = true;
    public bool IndexClipboard { get; set; } = true;
    public bool IndexCalculator { get; set; } = true;

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
    public bool IndexShell { get; set; } = true;

    /// <summary>Enable system command results such as shutdown, lock, and settings.</summary>
    public bool IndexSystemCommands { get; set; } = true;

    /// <summary>Enable URL opening results.</summary>
    public bool IndexUrls { get; set; } = true;

    /// <summary>Enable web search results.</summary>
    public bool IndexWebSearches { get; set; } = true;

    /// <summary>Enable Windows Settings search results.</summary>
    public bool IndexWindowsSettings { get; set; } = true;

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

    /// <summary>Launch Arc when Windows starts.</summary>
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

    /// <summary>Clear clipboard history when Arc exits.</summary>
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

    /// <summary>Show placeholder text when the query is empty.</summary>
    public bool ShowPlaceholder { get; set; } = true;

    /// <summary>Placeholder text shown when query is empty.</summary>
    public string PlaceholderText { get; set; } = "Search apps, files, clipboard...";

    /// <summary>Use launcher show/hide animations.</summary>
    public bool AnimationEnabled { get; set; } = true;

    /// <summary>Play a small sound when the launcher opens.</summary>
    public bool SoundEffectEnabled { get; set; } = false;

    /// <summary>Monitor used for launcher placement. Currently "primary".</summary>
    public string SearchWindowLocation { get; set; } = "primary";

    /// <summary>Launcher placement on the selected monitor.</summary>
    public string SearchWindowPosition { get; set; } = "center";

    /// <summary>Keep the preview panel open when the launcher activates.</summary>
    public bool AlwaysPreview { get; set; } = false;

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
    // API Key helpers (transparent encryption via DPAPI)
    // ═══════════════════════════════════════════════════════════════

    private static readonly byte[] Entropy = "Arc.Launcher.v1"u8.ToArray();

    [JsonIgnore]
    public string GroqApiKey
    {
        get => Decrypt(EncryptedGroqApiKey);
        set => EncryptedGroqApiKey = Encrypt(value);
    }

    [JsonIgnore]
    public string GeminiApiKey
    {
        get => Decrypt(EncryptedGeminiApiKey);
        set => EncryptedGeminiApiKey = Encrypt(value);
    }

    [JsonIgnore]
    public string OpenRouterApiKey
    {
        get => Decrypt(EncryptedOpenRouterApiKey);
        set => EncryptedOpenRouterApiKey = Encrypt(value);
    }

    [JsonIgnore]
    public string DeepSeekApiKey
    {
        get => Decrypt(EncryptedDeepSeekApiKey);
        set => EncryptedDeepSeekApiKey = Encrypt(value);
    }

    /// <summary>Returns the active provider's API key.</summary>
    [JsonIgnore]
    public string ActiveApiKey => AiProvider switch
    {
        "groq"       => GroqApiKey,
        "gemini"     => GeminiApiKey,
        "openrouter" => OpenRouterApiKey,
        "deepseek"   => DeepSeekApiKey,
        _            => string.Empty,
    };

    /// <summary>Returns the active provider's model.</summary>
    [JsonIgnore]
    public string ActiveModel => AiProvider switch
    {
        "groq"       => GroqModel,
        "gemini"     => GeminiModel,
        "openrouter" => OpenRouterModel,
        "deepseek"   => DeepSeekModel,
        _            => string.Empty,
    };

    // ═══════════════════════════════════════════════════════════════
    // Validation
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Clamps out-of-range values to valid ranges.</summary>
    public void Validate()
    {
        WindowOpacity  = Math.Clamp(WindowOpacity, 0.3, 1.0);
        LauncherWidth  = Math.Clamp(LauncherWidth, 300, 1200);
        ResultsCount   = Math.Clamp(ResultsCount, 3, 20);
        FontSize       = Math.Clamp(FontSize, 11, 20);
        CornerRadius   = Math.Clamp(CornerRadius, 0, 28);
        MaxFileDepth   = Math.Clamp(MaxFileDepth, 1, 5);
        ClipboardHistorySize = Math.Clamp(ClipboardHistorySize, 5, 200);
        PinnedClipboard ??= [];
    }

    // ═══════════════════════════════════════════════════════════════
    // Encryption
    // ═══════════════════════════════════════════════════════════════

    private static string Encrypt(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return string.Empty;
        var bytes = Encoding.UTF8.GetBytes(plain);
        var encrypted = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    private static string Decrypt(string cipher)
    {
        if (string.IsNullOrEmpty(cipher)) return string.Empty;
        try
        {
            var bytes = Convert.FromBase64String(cipher);

            // Legacy plaintext fallback support (avoid writing new ones)
            var test = Encoding.UTF8.GetString(bytes);
            if (test.StartsWith("PLAIN:"))
                return test[6..];

            var decrypted = ProtectedData.Unprotect(bytes, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return string.Empty;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Clone
    // ═══════════════════════════════════════════════════════════════

    public ArcConfig Clone()
    {
        // Fast shallow clone via JSON round-trip — handles all nested collections
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<ArcConfig>(json) ?? new ArcConfig();
    }
}
