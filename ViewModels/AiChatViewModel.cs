using System.Collections.ObjectModel;
using Arc.Extensions;
using Arc.Services;
using Arc.Models;

namespace Arc.ViewModels;

public sealed partial class AiChatViewModel : ObservableObject
{
    private readonly IAiService _aiService;
    private readonly ArcConfig  _config;
    private CancellationTokenSource? _aiCts;
    private readonly List<(string Role, string Content)> _aiConversation = [];

    public AiChatViewModel(IAiService aiService, ArcConfig config)
    {
        _aiService = aiService;
        _config = config;
        AiFollowUpCommand = new RelayCommand<string>(OnAiFollowUp);
    }

    [ObservableProperty] private string _aiText    = string.Empty;
    [ObservableProperty] private bool   _aiLoading = false;
    [ObservableProperty] private string _aiError   = string.Empty;

    public IRelayCommand AiFollowUpCommand { get; }

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
        AiText = string.Empty;
        AiLoading = false;
        AiError = string.Empty;
        ConversationChanged?.Invoke(this, EventArgs.Empty);
    }

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

        var question = AiAction.ExtractQuestion(query);
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
                    else
                        _aiConversation[^1] = ("assistant", AiText);
                    ConversationChanged?.Invoke(this, EventArgs.Empty);
                });
            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (TaskCanceledException)
        {
            AiError = "Request timed out. Please try again.";
        }
        catch (HttpRequestException ex)
        {
            AiError = ex.Message;
        }
        catch (Exception ex)
        {
            AiError = $"Unexpected error: {ex.Message}";
        }
        finally { AiLoading = false; }
    }

    private async void OnAiFollowUp(string? followUp)
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
                        _aiConversation[^1] = ("assistant", newResponse);
                    }
                    AiLoading = false;
                    ConversationChanged?.Invoke(this, EventArgs.Empty);
                });
            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (TaskCanceledException)
        {
            AiError = "Request timed out. Please try again.";
        }
        catch (HttpRequestException ex)
        {
            AiError = ex.Message;
        }
        catch (Exception ex)
        {
            AiError = $"Unexpected error: {ex.Message}";
        }
        finally { AiLoading = false; }
    }

    private (string Key, string Model) GetAiConfig() => _config.AiProvider switch
    {
        "gemini"     => (_config.GeminiApiKey,     _config.GeminiModel),
        "openrouter" => (_config.OpenRouterApiKey, _config.OpenRouterModel),
        "deepseek"   => (_config.DeepSeekApiKey,   _config.DeepSeekModel),
        _            => (_config.GroqApiKey,        _config.GroqModel),
    };
}
