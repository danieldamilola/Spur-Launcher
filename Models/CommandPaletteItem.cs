using CommunityToolkit.Mvvm.ComponentModel;

namespace Arc.Models;

/// <summary>
/// View model item for a single command palette entry.
/// Displayed in the filtered list with icon, label, description.
/// </summary>
public sealed partial class CommandPaletteItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _lucideIcon = string.Empty;

    /// <summary>
    /// The action to invoke when this command is selected.
    /// </summary>
    public Action? ExecuteAction { get; init; }
}
