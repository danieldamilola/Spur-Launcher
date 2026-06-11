using Spur.Models;
using Spur.Services;

namespace Spur.Actions;

/// <summary>
/// Central dispatcher routing action execution to registered handlers.
/// Replaces the OpenActionResult switch in MainViewModel.
/// </summary>
public sealed class ActionDispatcher
{
    private readonly ILogger _log;
    private readonly List<IActionHandler> _handlers = new();

    public ActionDispatcher(ILogger log)
    {
        _log = log;
    }

    public void Register(IActionHandler handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));
        _handlers.Add(handler);
    }

    public async Task<ActionPanelState> DispatchAsync(
        SearchResult result,
        string actionInput,
        CancellationToken ct = default)
    {
        if (result is null) return ActionPanelState.Empty();

        var handler = FindHandler(result);
        if (handler is null)
        {
            _log.Warning($"No handler for action: {result.ActionId ?? "null"}");
            return ActionPanelState.Empty();
        }

        try
        {
            return await handler.ExecuteAsync(result, actionInput, ct);
        }
        catch (Exception ex)
        {
            _log.Warning($"Action handler failed: {result.ActionId}", ex);
            return new ActionPanelState
            {
                PanelId = "error",
                Title = "Error",
                Subtitle = actionInput,
                State = "Failed",
                ResultText = ex.Message
            };
        }
    }

    private IActionHandler? FindHandler(SearchResult result)
    {
        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(result)) return handler;
        }
        return null;
    }
}
