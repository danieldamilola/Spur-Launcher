using Spur.Models;

namespace Spur.Actions;

/// <summary>
/// Handler for a specific action type (timer, AI, shell, etc).
/// Each handler encapsulates the logic for one action category.
/// </summary>
public interface IActionHandler
{
    /// <summary>
    /// Determines if this handler can process the given result.
    /// </summary>
    bool CanHandle(SearchResult result);

    /// <summary>
    /// Executes the action and returns the panel state to display.
    /// </summary>
    Task<ActionPanelState> ExecuteAsync(SearchResult result, string actionInput, CancellationToken ct = default);
}
