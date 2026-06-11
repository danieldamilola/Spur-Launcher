using Spur.Models;
using Spur.Services;
using Spur.ViewModels;

namespace Spur.Actions.Handlers;

public sealed class AiActionHandler : IActionHandler
{
    private readonly AiChatViewModel _ai;
    private readonly ILogger _log;

    public AiActionHandler(AiChatViewModel ai, ILogger log)
    {
        _ai = ai ?? throw new ArgumentNullException(nameof(ai));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public bool CanHandle(SearchResult result) =>
        result.ActionId == "ai";

    public async Task<ActionPanelState> ExecuteAsync(
        SearchResult result,
        string actionInput,
        CancellationToken ct = default)
    {
        try
        {
            await _ai.StartAiAsync(actionInput);

            return new ActionPanelState
            {
                PanelId = "ai",
                Title = result.Name,
                Subtitle = actionInput,
                State = string.IsNullOrWhiteSpace(_ai.AiError) ? "Answered" : "Needs setup"
            };
        }
        catch (Exception ex)
        {
            _log.Warning("AI action failed", ex);
            return new ActionPanelState
            {
                PanelId = "ai",
                Title = result.Name,
                Subtitle = actionInput,
                State = "Error",
                ResultText = ex.Message
            };
        }
    }
}
