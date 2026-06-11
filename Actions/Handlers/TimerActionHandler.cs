using Spur.Models;
using Spur.ViewModels;

namespace Spur.Actions.Handlers;

public sealed class TimerActionHandler : IActionHandler
{
    private readonly TimerViewModel _timer;

    public TimerActionHandler(TimerViewModel timer)
    {
        _timer = timer ?? throw new ArgumentNullException(nameof(timer));
    }

    public bool CanHandle(SearchResult result) =>
        result.ActionId == "timer";

    public Task<ActionPanelState> ExecuteAsync(
        SearchResult result,
        string actionInput,
        CancellationToken ct = default)
    {
        var started = _timer.StartTimerPreview(actionInput);
        if (started) _timer.StartCommand.Execute(null);

        var state = new ActionPanelState
        {
            PanelId = "timer",
            Title = result.Name,
            Subtitle = actionInput,
            State = _timer.TimerRunning ? "Running" : "Check input"
        };

        return Task.FromResult(state);
    }
}
