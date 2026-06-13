using Spur.Extensions;
using Spur.Models;

namespace Spur.Actions.Handlers;

public sealed class ShellActionHandler : IActionHandler
{
    private readonly AddOnRegistry _addOns;

    public ShellActionHandler(AddOnRegistry addOns)
    {
        _addOns = addOns ?? throw new ArgumentNullException(nameof(addOns));
    }

    public bool CanHandle(SearchResult result) =>
        result.ActionId == "shell";

    public async Task<ActionPanelState> ExecuteAsync(
        SearchResult result,
        string actionInput,
        CancellationToken ct = default)
    {
        var command = actionInput.Trim();
        if (string.IsNullOrWhiteSpace(command))
            return ActionPanelState.Empty();

        var shellExtra = _addOns.FindById("shell");
        if (shellExtra is not null)
            await shellExtra.ExecuteAsync(command);

        return new ActionPanelState
        {
            PanelId = "shell",
            Title = "Shell command",
            Subtitle = command,
            State = "Started",
            ResultText = "Command started",
            ResultSubText = command
        };
    }
}
