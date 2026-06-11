namespace Spur.Actions;

/// <summary>
/// Immutable state describing what to display in the action panel.
/// Returned by action handlers to decouple them from MainViewModel.
/// </summary>
public sealed record ActionPanelState
{
    public string PanelId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ResultText { get; init; } = string.Empty;
    public string ResultSubText { get; init; } = string.Empty;
    public bool ShouldHide { get; init; }

    public static ActionPanelState Hidden() => new() { ShouldHide = true };

    public static ActionPanelState Empty() => new();
}
