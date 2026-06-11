using Spur.Extensions;
using Spur.Models;

namespace Spur.Actions.Handlers;

public sealed class ShellActionHandler : IActionHandler
{
    private readonly ExtrasRegistry _extras;

    public ShellActionHandler(ExtrasRegistry extras)
    {
        _extras = extras ?? throw new ArgumentNullException(nameof(extras));
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

        var shellExtra = _extras.FindById("shell");
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
