using System.Net.Http.Headers;
using System.Text.Json;

namespace Spur.Services;

/// <summary>Interface for AI streaming service.</summary>
public interface IAiService
{
    string[] SupportedProviders { get; }
    Task StreamAsync(string provider, string model, string apiKey, string question, Action<string> onToken, CancellationToken ct = default);
    Task StreamAsync(string provider, string model, string apiKey, IEnumerable<(string Role, string Content)> messages, Action<string> onToken, CancellationToken ct = default);
}

/// <summary>
/// Streams completions from Groq, Gemini, OpenRouter, or DeepSeek.
/// All providers use OpenAI-compatible chat completions endpoints.
/// </summary>
public sealed class AiService : IAiService
{
    // SocketsHttpHandler with PooledConnectionLifetime prevents stale DNS entries
    // that accumulate when a plain static HttpClient holds connections indefinitely.
    // ConnectTimeout bounds connection establishment; no overall Timeout is set
    // because streaming responses can legitimately exceed any fixed duration.
    private static readonly HttpClient _http = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        ConnectTimeout = TimeSpan.FromSeconds(30),
    })
    {
        Timeout = TimeSpan.FromMinutes(5),
        DefaultRequestHeaders =
        {
            UserAgent = { new("Spur", "1.0") },
        },
    };

    private readonly ILogger _log;

    public AiService(ILogger? log = null) => _log = log ?? NullLogger.Instance;

    private static readonly Dictionary<string, (string Endpoint, string? DefaultModel)> Providers = new()
    {
        ["groq"]       = ("https://api.groq.com/openai/v1/chat/completions",                           "llama-3.1-8b-instant"),
        ["gemini"]     = ("https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",   "gemini-2.0-flash"),
        ["openrouter"] = ("https://openrouter.ai/api/v1/chat/completions",                              null),
        ["deepseek"]   = ("https://api.deepseek.com/v1/chat/completions",                               "deepseek-chat"),
    };

    // Owned here so callers never need to supply or duplicate it.
    private const string SystemPrompt =
        "You are a helpful assistant inside a desktop launcher app called Spur. Give clear, complete answers. You may use markdown formatting like **bold**, *italic*, `code`, code blocks, and lists when helpful.";

    public string[] SupportedProviders => [.. Providers.Keys];

    Task IAiService.StreamAsync(string provider, string model, string apiKey, string question, Action<string> onToken, CancellationToken ct)
        => StreamAsyncInternal(provider, model, apiKey, [("user", question)], onToken, ct);

    Task IAiService.StreamAsync(string provider, string model, string apiKey, IEnumerable<(string Role, string Content)> messages, Action<string> onToken, CancellationToken ct)
        => StreamAsyncInternal(provider, model, apiKey, messages, onToken, ct);

    /// <summary>
    /// Streams an AI response. The service unconditionally prepends its own system
    /// prompt, so callers should pass only "user" and "assistant" turns. Any
    /// "system" entries in <paramref name="messages"/> are silently dropped to
    /// prevent the dual-system-prompt bug that previously occurred when the
    /// ViewModel seeded the conversation with its own system message.
    /// </summary>
    private async Task StreamAsyncInternal(
        string provider,
        string model,
        string apiKey,
        IEnumerable<(string Role, string Content)> messages,
        Action<string> onToken,
        CancellationToken ct = default)
    {
        // Normalize provider name for case-insensitive lookup.
        var normalizedProvider = provider.ToLowerInvariant();
        if (!Providers.TryGetValue(normalizedProvider, out var p))
            throw new ArgumentException($"Unknown provider: {provider}", nameof(provider));

        var (endpoint, _) = p;

        var msgList = new List<object>
        {
            new { role = "system", content = SystemPrompt },
        };

        foreach (var (role, content) in messages)
        {
            if (role == "system") continue; // guard against caller-side duplication
            msgList.Add(new { role, content });
        }

        var body = new { model, stream = true, max_tokens = 2048, messages = msgList };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body),
                System.Text.Encoding.UTF8,
                "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException(ct);
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            var msg = err.Trim();
            if (msg.Length > 200) msg = msg[..200] + "…";
            _log.Warning($"{provider} API returned {(int)response.StatusCode}: {msg}");
            throw new HttpRequestException(
                $"{provider} API error {(int)response.StatusCode}: {msg}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            // Guard: line must have content after "data: " prefix (at least 7 chars).
            if (line.Length <= 6) continue;
            var data = line[6..];
            if (data == "[DONE]") break;

            string? token = null;
            try
            {
                using var doc = JsonDocument.Parse(data);
                var delta = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta");

                if (delta.TryGetProperty("content", out var content))
                    token = content.GetString();
            }
            catch (Exception ex)
            {
                _log.Debug($"Malformed SSE chunk from {provider}: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(token))
                onToken(token);
        }
    }
}
