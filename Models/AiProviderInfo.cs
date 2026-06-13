namespace Spur.Models;

/// <summary>
/// Canonical registry of AI provider metadata.
/// Single source of truth for provider keys, API endpoints, and available model lists.
/// Previously duplicated between AiService (endpoints) and SettingsViewModel (model arrays).
/// </summary>
public static class AiProviderInfo
{
    // ── Provider key constants ─────────────────────────────────────
    public const string Groq       = "groq";
    public const string Gemini     = "gemini";
    public const string OpenRouter = "openrouter";
    public const string DeepSeek   = "deepseek";

    /// <summary>All supported provider keys in display order.</summary>
    public static readonly string[] AllProviders = [Groq, Gemini, OpenRouter, DeepSeek];

    // ── API endpoints ──────────────────────────────────────────────
    public static readonly IReadOnlyDictionary<string, string> Endpoints =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Groq]       = "https://api.groq.com/openai/v1/chat/completions",
            [Gemini]     = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
            [OpenRouter] = "https://openrouter.ai/api/v1/chat/completions",
            [DeepSeek]   = "https://api.deepseek.com/v1/chat/completions",
        };

    // ── Default models per provider ────────────────────────────────
    public static readonly IReadOnlyDictionary<string, string> DefaultModels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Groq]       = "llama-3.1-8b-instant",
            [Gemini]     = "gemini-2.0-flash",
            [OpenRouter] = "google/gemini-2.0-flash-001",
            [DeepSeek]   = "deepseek-chat",
        };

    // ── Available models per provider ──────────────────────────────
    public static readonly IReadOnlyDictionary<string, string[]> AvailableModels =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [Groq]       = ["llama-3.1-8b-instant", "llama-3.3-70b-versatile", "qwen/qwen3-32b"],
            [Gemini]     = ["gemini-2.0-flash", "gemini-2.5-pro-exp-03-25", "gemini-1.5-flash"],
            [OpenRouter] = ["google/gemini-2.0-flash-001", "meta-llama/llama-3.1-8b-instruct", "deepseek/deepseek-chat"],
            [DeepSeek]   = ["deepseek-chat", "deepseek-reasoner"],
        };

    /// <summary>Returns the endpoint for a provider, or throws for an unknown key.</summary>
    public static string GetEndpoint(string provider)
    {
        if (Endpoints.TryGetValue(provider, out var url)) return url;
        throw new ArgumentException($"Unknown AI provider: '{provider}'", nameof(provider));
    }

    /// <summary>Returns available models for a provider, or an empty array.</summary>
    public static string[] GetModels(string provider)
        => AvailableModels.TryGetValue(provider, out var models) ? models : [];

    /// <summary>
    /// Returns (encryptedKey, selectedModel) for the given provider and config,
    /// or (empty, empty) if the provider is unknown.
    /// Centralises the provider→config-field mapping that was previously
    /// duplicated in AiChatViewModel and SettingsViewModel.
    /// </summary>
    public static (string EncryptedKey, string Model) GetApiKeyConfig(string provider, SpurConfig config)
        => provider.ToLowerInvariant() switch
        {
            Groq       => (config.EncryptedGroqApiKey,       config.GroqModel),
            Gemini     => (config.EncryptedGeminiApiKey,     config.GeminiModel),
            OpenRouter => (config.EncryptedOpenRouterApiKey, config.OpenRouterModel),
            DeepSeek   => (config.EncryptedDeepSeekApiKey,   config.DeepSeekModel),
            _          => (string.Empty, string.Empty),  // unknown — caller will show friendly error
        };
}
