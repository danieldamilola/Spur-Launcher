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

    // Track the last user action so we can retry on failure.
    private enum MessageKind { None, Initial, FollowUp }
    private MessageKind _lastMessageKind = MessageKind.None;
    private string _lastMessageText = string.Empty;

    // Contains only "user" and "assistant" turns. The system prompt is owned by
    // AiService and must not be added here — doing so caused a duplicate system
    // prompt to be sent with conflicting instructions.
    private readonly List<(string Role, string Content)> _aiConversation = [];

    // Thread-safe token accumulation — written from HTTP thread, read from UI thread.
    // Using lock(_tokenLock) instead of StringBuilder because SB is not thread-safe.
    private readonly object _tokenLock = new();
    private readonly StringBuilder _fullResponseBuilder = new();   // Entire conversation display text
    private readonly StringBuilder _currentResponseBuilder = new(); // Current assistant response only

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
        lock (_tokenLock)
        {
            _fullResponseBuilder.Clear();
            _currentResponseBuilder.Clear();
        }
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

    // ═══════════════════════════════════════════════════════════════
    // Core streaming — handles both initial and follow-up queries
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Accumulates a streaming token. Called from HTTP thread pool — must be thread-safe.
    /// Appends to builders under lock, then schedules a debounced UI update.
    /// </summary>
    private void OnTokenReceived(string token, bool isFollowUp)
    {
        // 1. Thread-safe accumulation (no dispatcher needed)
        lock (_tokenLock)
        {
            _fullResponseBuilder.Append(token);
            _currentResponseBuilder.Append(token);
        }

        // 2. Debounced UI update via dispatcher
        var now = DateTime.UtcNow;
        if (now - _lastUiUpdate < UiUpdateInterval) return;
        _lastUiUpdate = now;

        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            FlushToUi();
        });
    }

    /// <summary>
    /// Reads accumulated tokens and pushes them to UI-bound properties.
    /// Must run on UI thread.
    /// </summary>
    private void FlushToUi()
    {
        string fullText;
        string currentResponse;
        lock (_tokenLock)
        {
            fullText = _fullResponseBuilder.ToString();
            currentResponse = _currentResponseBuilder.ToString();
        }

        AiText = fullText;
        AiLoading = fullText.Length == 0;

        // Update conversation history with latest response
        if (_aiConversation.Count > 0 && _aiConversation[^1].Role == "assistant")
            _aiConversation[^1] = ("assistant", currentResponse);
        else
            _aiConversation.Add(("assistant", currentResponse));

        ConversationChanged?.Invoke(this, EventArgs.Empty);
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

        _lastMessageKind = MessageKind.Initial;
        _lastMessageText = question;
        _lastQuery = question;
        AiText    = string.Empty;
        AiError   = string.Empty;
        AiLoading = true;

        _aiConversation.Clear();
        _aiConversation.Add(("user", question));
        lock (_tokenLock)
        {
            _fullResponseBuilder.Clear();
            _currentResponseBuilder.Clear();
        }
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

            // Stream tokens — OnTokenReceived accumulates them thread-safely
            await _aiService.StreamAsync(
                _config.AiProvider, model, key, _aiConversation,
                token => OnTokenReceived(token, isFollowUp: false), ct);

            // Final flush on UI thread — all tokens are already accumulated
            await Application.Current.Dispatcher.InvokeAsync(FlushToUi);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (TaskCanceledException)        { AiError = ToFriendlyError(null, null, ct); }
        catch (HttpRequestException ex)        { AiError = ToFriendlyError(typeof(HttpRequestException), ex.Message, ct); }
        catch (Exception ex)                   { AiError = ToFriendlyError(typeof(Exception), ex.Message, ct); }
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

        _lastMessageKind = MessageKind.FollowUp;
        _lastMessageText = followUp;
        _lastQuery = followUp;
        _aiConversation.Add(("user", followUp));
        AiError   = string.Empty;
        AiLoading = true;

        // Add separator and prepare for new response
        lock (_tokenLock)
        {
            _fullResponseBuilder.Append("\n\n");
            _currentResponseBuilder.Clear();
        }
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

            // Stream tokens — OnTokenReceived accumulates them thread-safely
            await _aiService.StreamAsync(
                _config.AiProvider, model, key, _aiConversation,
                token => OnTokenReceived(token, isFollowUp: true), ct);

            // Final flush on UI thread — all tokens are already accumulated
            await Application.Current.Dispatcher.InvokeAsync(FlushToUi);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (TaskCanceledException)        { AiError = ToFriendlyError(null, null, ct); }
        catch (HttpRequestException ex)        { AiError = ToFriendlyError(typeof(HttpRequestException), ex.Message, ct); }
        catch (Exception ex)                   { AiError = ToFriendlyError(typeof(Exception), ex.Message, ct); }
        finally
        {
            AiLoading = false;
            ConversationChanged?.Invoke(this, EventArgs.Empty);
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

    /// <summary>Replays the last user question or follow-up that failed.</summary>
    private async Task RetryLastAsync()
    {
        if (_lastMessageKind == MessageKind.Initial)
            await StartAiAsync(_lastMessageText);
        else if (_lastMessageKind == MessageKind.FollowUp)
            await OnAiFollowUpAsync(_lastMessageText);
    }

    /// <summary>Maps exception types to user-friendly messages.</summary>
    private static string ToFriendlyError(Type? exType, string? message, CancellationToken ct)
    {
        if (exType == typeof(HttpRequestException))
        {
            if (!string.IsNullOrEmpty(message))
                return message.Length > 120 ? message[..120] + "…" : message;
            return "Connection failed. Check your internet and API key.";
        }

        // TaskCanceledException that isn't from a user-initiated cancel
        if (exType is null && !ct.IsCancellationRequested)
            return "Request timed out. Try again.";

        return "Something went wrong. Please try again.";
    }

    /// <summary>Notify RetryLastCommand when AiError changes so the button can react.</summary>
    partial void OnAiErrorChanged(string value)
    {
        RetryLastCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        CancelPending();
    }
}
