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

    public AiChatViewModel(IAiService aiService, ISecureStorageService secureStorage, SpurConfig config)
    {
        _aiService      = aiService;
        _secureStorage  = secureStorage;
        _config         = config;

        // AsyncRelayCommand surfaces exceptions via its error path rather than
        // crashing the process (previously was async void).
        AiFollowUpCommand = new AsyncRelayCommand<string>(OnAiFollowUpAsync);
    }

    [ObservableProperty] private string _aiText    = string.Empty;
    [ObservableProperty] private bool   _aiLoading = false;
    [ObservableProperty] private string _aiError   = string.Empty;

    public IAsyncRelayCommand<string> AiFollowUpCommand { get; }

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

        AiText    = string.Empty;
        AiError   = string.Empty;
        AiLoading = true;

        _aiConversation.Clear();
        _aiConversation.Add(("user", question));
        ConversationChanged?.Invoke(this, EventArgs.Empty);

        var (key, model) = GetAiConfig();
        if (string.IsNullOrWhiteSpace(key))
        {
            AiError   = $"Add your {_config.AiProvider} API key in Settings (Ctrl+,) to use AI.";
            AiLoading = false;
            return;
        }

        try
        {
            await _aiService.StreamAsync(_config.AiProvider, model, key, _aiConversation, token =>
            {
                Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    AiText    += token;
                    AiLoading  = AiText.Length == 0;
                    if (_aiConversation.Count == 1)
                        _aiConversation.Add(("assistant", AiText));
                    else if (_aiConversation.Count > 0)
                        _aiConversation[^1] = ("assistant", AiText);
                    ConversationChanged?.Invoke(this, EventArgs.Empty);
                });
            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (TaskCanceledException)   { AiError = "Request was canceled. Please try again."; }
        catch (HttpRequestException ex) { AiError = ex.Message; }
        catch (Exception ex)            { AiError = $"Unexpected error: {ex.Message}"; }
        finally { AiLoading = false; }
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

        _aiConversation.Add(("user", followUp));
        AiError   = string.Empty;
        AiLoading = true;
        ConversationChanged?.Invoke(this, EventArgs.Empty);

        var (key, model) = GetAiConfig();
        if (string.IsNullOrWhiteSpace(key))
        {
            AiError   = $"Add your {_config.AiProvider} API key in Settings (Ctrl+,) to use AI.";
            AiLoading = false;
            return;
        }

        try
        {
            string? newResponse = null;
            await _aiService.StreamAsync(_config.AiProvider, model, key, _aiConversation, token =>
            {
                Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    if (newResponse is null)
                    {
                        newResponse = token;
                        AiText += "\n\n" + token;
                        _aiConversation.Add(("assistant", newResponse));
                    }
                    else
                    {
                        newResponse += token;
                        AiText += token;
                        if (_aiConversation.Count > 0)
                            _aiConversation[^1] = ("assistant", newResponse);
                    }
                    AiLoading = false;
                    ConversationChanged?.Invoke(this, EventArgs.Empty);
                });
            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (TaskCanceledException)   { AiError = "Request was canceled. Please try again."; }
        catch (HttpRequestException ex) { AiError = ex.Message; }
        catch (Exception ex)            { AiError = $"Unexpected error: {ex.Message}"; }
        finally { AiLoading = false; }
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
