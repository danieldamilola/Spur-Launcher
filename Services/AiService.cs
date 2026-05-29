using System.Net.Http.Headers;
using System.Text.Json;

namespace Arc.Services;

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
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromMinutes(5),
    };

    private static readonly Dictionary<string, (string Endpoint, string? DefaultModel)> Providers = new()
    {
        ["groq"]       = ("https://api.groq.com/openai/v1/chat/completions",                  "llama-3.1-8b-instant"),
        ["gemini"]     = ("https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", "gemini-2.0-flash"),
        ["openrouter"] = ("https://openrouter.ai/api/v1/chat/completions",                     null),
        ["deepseek"]   = ("https://api.deepseek.com/v1/chat/completions",                      "deepseek-chat"),
    };

    public string[] SupportedProviders => [.. Providers.Keys];

    private static readonly AiService _default = new();

    /// <summary>
    /// Static helper for backward compatibility.
    /// </summary>
    public static Task StreamAsync(
        string provider, string model, string apiKey, string question,
        Action<string> onToken, CancellationToken ct = default)
        => _default.StreamAsyncInternal(provider, model, apiKey, question, onToken, ct);

    /// <summary>
    /// Static helper for backward compatibility.
    /// </summary>
    public static Task StreamAsync(
        string provider, string model, string apiKey,
        IEnumerable<(string Role, string Content)> messages,
        Action<string> onToken, CancellationToken ct = default)
        => _default.StreamAsyncInternal(provider, model, apiKey, messages, onToken, ct);

    /// <summary>
    /// Instance method for single question (convenience wrapper).
    /// </summary>
    Task IAiService.StreamAsync(string provider, string model, string apiKey, string question, Action<string> onToken, CancellationToken ct)
        => StreamAsyncInternal(provider, model, apiKey, question, onToken, ct);

    /// <summary>
    /// Instance method for full conversation.
    /// </summary>
    Task IAiService.StreamAsync(string provider, string model, string apiKey, IEnumerable<(string Role, string Content)> messages, Action<string> onToken, CancellationToken ct)
        => StreamAsyncInternal(provider, model, apiKey, messages, onToken, ct);

    private Task StreamAsyncInternal(
        string provider, string model, string apiKey, string question,
        Action<string> onToken, CancellationToken ct)
        => StreamAsyncInternal(provider, model, apiKey,
            new[] { ("user", question) }, onToken, ct);

    /// <summary>
    /// Streams an AI response using the given provider, model, and API key.
    /// <paramref name="messages"/> is the full conversation: each item is (role, content)
    /// where role is "user" or "assistant". A system prompt is prepended automatically.
    /// </summary>
    private async Task StreamAsyncInternal(
        string provider,
        string model,
        string apiKey,
        IEnumerable<(string Role, string Content)> messages,
        Action<string> onToken,
        CancellationToken ct = default)
    {
        if (!Providers.TryGetValue(provider, out var p))
            throw new ArgumentException($"Unknown provider: {provider}", nameof(provider));

        var (endpoint, _) = p;

        var msgList = new List<object>
        {
            new { role = "system", content = "You are a helpful assistant. Be concise and use plain text — no markdown symbols." },
        };
        foreach (var (role, content) in messages)
            msgList.Add(new { role, content });

        var body = new
        {
            model,
            stream = true,
            messages = msgList,
        };

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
            response = await _http.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException(ct);
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            var msg = err.Trim();
            if (msg.Length > 200) msg = msg[..200] + "\u2026";
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
            catch { /* malformed chunk */ }

            if (!string.IsNullOrEmpty(token))
                onToken(token);
        }
    }
}

