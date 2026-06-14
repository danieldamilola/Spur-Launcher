using System.Text;
using Spur.Extensions;
using Spur.Services;
using Spur.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Spur.ViewModels;

public sealed partial class AiChatViewModel : ObservableObject, IDisposable
{
    private readonly IAiService _aiService;
    private readonly ISecureStorageService _secureStorage;
    private readonly SpurConfig _config;
    private CancellationTokenSource? _aiCts;

    // Contains only "user" and "assistant" turns. The system prompt is owned by
    // AiService and must not be added here — doing so caused a duplicate system
    // prompt to be sent with conflicting instructions.
    private readonly List<(string Role, string Content)> _aiConversation = [];

    // Performance: use StringBuilder to avoid O(n²) string concatenation during streaming
    private readonly StringBuilder _aiTextBuilder = new();
    private readonly StringBuilder _responseBuilder = new();

    // Debounce UI updates — max ~60fps instead of per-token
    private DateTime _lastUiUpdate = DateTime.MinValue;
    private static readonly TimeSpan UiUpdateInterval = TimeSpan.FromMilliseconds(16);

    // Cap conversation to prevent unbounded memory growth
    private const int MaxConversationTurns = 50;

    public AiChatViewModel(IAiService aiService, ISecureStorageService secureStorage, SpurConfig config)
    {
        _aiService      = aiService;
        _secureStorage  = secureStorage;
        _config         = config;

        // AsyncRelayCommand surfaces exceptions via its error path rather than
        // crashing the process (previously was async void).
        AiFollowUpCommand = new AsyncRelayCommand<string>(OnAiFollowUpAsync);
        RetryLastCommand  = new AsyncRelayCommand(RetryLastAsync, () => !string.IsNullOrEmpty(AiError));
    }

    [ObservableProperty] private string _aiText    = string.Empty;
    [ObservableProperty] private bool   _aiLoading = false;
    [ObservableProperty] private string _aiError   = string.Empty;

    /// <summary>Tracks the last user query so it can be retried on error.</summary>
    private string? _lastQuery;

    public IAsyncRelayCommand<string> AiFollowUpCommand { get; }
    public IAsyncRelayCommand RetryLastCommand { get; }

    public event EventHandler? ConversationChanged;

    public IReadOnlyList<(string Role, string Content)> AiConversation => _aiConversation;

    public void CancelPending()
    {
        _aiCts?.Cancel();
        _aiCts?.Dispose();
        _aiCts = null;
    }

    public void ClearConversation()
    {
        CancelPending();
        _aiConversation.Clear();
        _aiTextBuilder.Clear();
        _responseBuilder.Clear();
        AiText    = string.Empty;
        AiLoading = false;
        AiError   = string.Empty;
        ConversationChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Returns the conversation as plain text. Only user/assistant turns are
    /// included; the service-level system prompt is not stored in this list.
    /// </summary>
    public string GetConversationText()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var (role, content) in _aiConversation)
        {
            sb.AppendLine(role == "user" ? "You:" : "AI:");
            sb.AppendLine(content);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    public async Task StartAiAsync(string query)
    {
        CancelPending();
        _aiCts = new CancellationTokenSource();
        var ct = _aiCts.Token;

        var trimmed  = query.Trim();
        var question = trimmed.StartsWith("ai ", StringComparison.OrdinalIgnoreCase)
            ? trimmed[3..].Trim()
            : trimmed;

        _lastQuery = question;
        AiText    = string.Empty;
        AiError   = string.Empty;
        AiLoading = true;

        _aiConversation.Clear();
        _aiConversation.Add(("user", question));
        _aiTextBuilder.Clear();
        _responseBuilder.Clear();
        ConversationChanged?.Invoke(this, EventArgs.Empty);

        try
        {
            var (key, model) = GetAiConfig();
            if (string.IsNullOrWhiteSpace(key))
            {
                AiError   = $"Add your {_config.AiProvider} API key in Settings (Ctrl+,) to use AI.";
                AiLoading = false;
                ConversationChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            await _aiService.StreamAsync(_config.AiProvider, model, key, _aiConversation, token =>
            {
                Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    _aiTextBuilder.Append(token);

                    // Debounce UI updates — skip if less than 16ms since last update
                    var now = DateTime.UtcNow;
                    if (now - _lastUiUpdate < UiUpdateInterval) return;
                    _lastUiUpdate = now;

                    AiText    = _aiTextBuilder.ToString();
                    AiLoading = AiText.Length == 0;
                    if (_aiConversation.Count == 1)
                        _aiConversation.Add(("assistant", AiText));
                    else if (_aiConversation.Count > 0)
                        _aiConversation[^1] = ("assistant", AiText);
                    ConversationChanged?.Invoke(this, EventArgs.Empty);
                });
            }, ct);

            // Final flush — ensure last tokens are displayed
            AiText = _aiTextBuilder.ToString();
            if (_aiConversation.Count > 0)
                _aiConversation[^1] = ("assistant", AiText);
            ConversationChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (TaskCanceledException)   { AiError = "Request was canceled. Please try again."; }
        catch (HttpRequestException ex) { AiError = ex.Message; }
        catch (Exception ex)            { AiError = $"Error: {ex.Message}"; }
        finally
        {
            AiLoading = false;
            ConversationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Handles follow-up questions. Returns Task so that exceptions propagate
    /// through AsyncRelayCommand rather than being swallowed (previously async void).
    /// </summary>
    private async Task OnAiFollowUpAsync(string? followUp)
    {
        if (string.IsNullOrWhiteSpace(followUp)) return;

        CancelPending();
        _aiCts = new CancellationTokenSource();
        var ct = _aiCts.Token;

        // Cap conversation to prevent unbounded memory growth
        TrimConversationIfNeeded();

        _lastQuery = followUp;
        _aiConversation.Add(("user", followUp));
        AiError   = string.Empty;
        AiLoading = true;
        _responseBuilder.Clear();
        ConversationChanged?.Invoke(this, EventArgs.Empty);

        try
        {
            var (key, model) = GetAiConfig();
            if (string.IsNullOrWhiteSpace(key))
            {
                AiError   = $"Add your {_config.AiProvider} API key in Settings (Ctrl+,) to use AI.";
                AiLoading = false;
                ConversationChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            bool isFirstToken = true;
            await _aiService.StreamAsync(_config.AiProvider, model, key, _aiConversation, token =>
            {
                Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    if (isFirstToken)
                    {
                        isFirstToken = false;
                        _aiTextBuilder.Append("\n\n");
                    }
                    _responseBuilder.Append(token);
                    _aiTextBuilder.Append(token);

                    // Debounce UI updates
                    var now = DateTime.UtcNow;
                    if (now - _lastUiUpdate < UiUpdateInterval) return;
                    _lastUiUpdate = now;

                    var response = _responseBuilder.ToString();
                    AiText = _aiTextBuilder.ToString();
                    if (isFirstToken || _aiConversation[^1].Role != "assistant")
                        _aiConversation.Add(("assistant", response));
                    else
                        _aiConversation[^1] = ("assistant", response);
                    AiLoading = false;
                    ConversationChanged?.Invoke(this, EventArgs.Empty);
                });
            }, ct);

            // Final flush
            var finalResponse = _responseBuilder.ToString();
            AiText = _aiTextBuilder.ToString();
            if (_aiConversation.Count > 0 && _aiConversation[^1].Role == "assistant")
                _aiConversation[^1] = ("assistant", finalResponse);
            else
                _aiConversation.Add(("assistant", finalResponse));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (TaskCanceledException)   { AiError = "Request was canceled. Please try again."; }
        catch (HttpRequestException ex) { AiError = ex.Message; }
        catch (Exception ex)            { AiError = $"Error: {ex.Message}"; }
        finally
        {
            AiLoading = false;
            ConversationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Retry the last failed query.</summary>
    private async Task RetryLastAsync()
    {
        if (string.IsNullOrEmpty(_lastQuery)) return;

        // If there's an existing conversation, retry as follow-up; otherwise start fresh
        if (_aiConversation.Count > 1)
        {
            // Remove the last user message that failed (if it's still there without an assistant reply)
            if (_aiConversation.Count > 0 && _aiConversation[^1].Role == "user")
                _aiConversation.RemoveAt(_aiConversation.Count - 1);
            await OnAiFollowUpAsync(_lastQuery);
        }
        else
        {
            await StartAiAsync(_lastQuery);
        }
    }

    /// <summary>Trims conversation history to MaxConversationTurns, keeping the most recent turns.</summary>
    private void TrimConversationIfNeeded()
    {
        if (_aiConversation.Count <= MaxConversationTurns) return;
        var excess = _aiConversation.Count - MaxConversationTurns;
        _aiConversation.RemoveRange(0, excess);
    }

    private (string Key, string Model) GetAiConfig() => _config.AiProvider.ToLowerInvariant() switch
    {
        "groq"       => (_secureStorage.Decrypt(_config.EncryptedGroqApiKey),       _config.GroqModel),
        "gemini"     => (_secureStorage.Decrypt(_config.EncryptedGeminiApiKey),     _config.GeminiModel),
        "openrouter" => (_secureStorage.Decrypt(_config.EncryptedOpenRouterApiKey), _config.OpenRouterModel),
        "deepseek"   => (_secureStorage.Decrypt(_config.EncryptedDeepSeekApiKey),   _config.DeepSeekModel),
        var p        => throw new InvalidOperationException($"Unknown AI provider: {p}"),
    };

    public void Dispose()
    {
        CancelPending();
    }
}
