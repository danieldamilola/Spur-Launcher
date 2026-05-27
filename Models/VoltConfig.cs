using System.Text.Json.Serialization;

namespace Volt.Models;

/// <summary>All user-configurable settings. Persisted as JSON.</summary>
public sealed class VoltConfig
{
    /// <summary>"dark" | "light" | "system"</summary>
    public string Theme { get; set; } = "dark";

    /// <summary>Global open shortcut, e.g. "Alt+Space".</summary>
    public string Shortcut { get; set; } = "Alt+Space";

    /// <summary>"groq" | "gemini" | "openrouter" | "deepseek"</summary>
    public string AiProvider { get; set; } = "groq";

    /// <summary>Per-provider API keys.</summary>
    public string GroqApiKey      { get; set; } = string.Empty;
    public string GeminiApiKey    { get; set; } = string.Empty;
    public string OpenRouterApiKey { get; set; } = string.Empty;
    public string DeepSeekApiKey  { get; set; } = string.Empty;

    /// <summary>Per-provider model selections.</summary>
    public string GroqModel       { get; set; } = "llama-3.1-8b-instant";
    public string GeminiModel     { get; set; } = "gemini-2.0-flash";
    public string OpenRouterModel { get; set; } = "google/gemini-2.0-flash-001";
    public string DeepSeekModel   { get; set; } = "deepseek-chat";

    /// <summary>How many results to display before scrolling (5, 8, or 10).</summary>
    public int ResultsCount { get; set; } = 8;

    /// <summary>Whether file search is enabled.</summary>
    public bool FileSearchEnabled { get; set; } = true;

    /// <summary>Whether clipboard monitoring is enabled.</summary>
    public bool ClipboardEnabled { get; set; } = true;

    /// <summary>List of pinned result IDs.</summary>
    public HashSet<string> PinnedItems { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public VoltConfig Clone() => new()
    {
        Theme           = Theme,
        Shortcut        = Shortcut,
        AiProvider      = AiProvider,
        GroqApiKey      = GroqApiKey,
        GeminiApiKey    = GeminiApiKey,
        OpenRouterApiKey = OpenRouterApiKey,
        DeepSeekApiKey  = DeepSeekApiKey,
        GroqModel       = GroqModel,
        GeminiModel     = GeminiModel,
        OpenRouterModel = OpenRouterModel,
        DeepSeekModel   = DeepSeekModel,
        ResultsCount    = ResultsCount,
        FileSearchEnabled = FileSearchEnabled,
        ClipboardEnabled  = ClipboardEnabled,
        PinnedItems       = new HashSet<string>(PinnedItems, StringComparer.OrdinalIgnoreCase),
    };
}
