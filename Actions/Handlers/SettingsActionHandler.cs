using Spur.Models;

namespace Spur.Actions.Handlers;

public sealed class SettingsActionHandler : IActionHandler
{
    public event Action? OpenSettingsRequested;

    public bool CanHandle(SearchResult result) =>
        result.ActionId == "settings";

    public Task<ActionPanelState> ExecuteAsync(
        SearchResult result,
        string actionInput,
        CancellationToken ct = default)
    {
        OpenSettingsRequested?.Invoke();
        return Task.FromResult(ActionPanelState.Hidden());
    }
}
