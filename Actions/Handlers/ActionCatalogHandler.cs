using Spur.Models;

namespace Spur.Actions.Handlers;

public sealed class ActionCatalogHandler : IActionHandler
{
    public event Action<string, string?>? ScopeChangeRequested;

    public bool CanHandle(SearchResult result) =>
        result.Id?.StartsWith("action-catalog:", StringComparison.OrdinalIgnoreCase) == true
        && !string.IsNullOrWhiteSpace(result.ActionId);

    public Task<ActionPanelState> ExecuteAsync(
        SearchResult result,
        string actionInput,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(result.ActionId))
        {
            ScopeChangeRequested?.Invoke(result.ActionId, result.IconGlyph);
        }

        return Task.FromResult(ActionPanelState.Hidden());
    }
}
