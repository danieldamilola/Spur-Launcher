namespace Arc.Extensions;

/// <summary>
/// Settings action. Triggered by "settings".
/// Opens the settings panel.
/// </summary>
public sealed class SettingsAction : IAction
{
    public string Id => "settings";

    public bool CanHandle(string query) =>
        !string.IsNullOrWhiteSpace(query) && query.Trim().Equals("settings", StringComparison.OrdinalIgnoreCase);

    public SearchResult BuildResult(string query)
    {
        return new SearchResult
        {
            Id         = "action:settings",
            Type       = ResultType.Action,
            Name       = "Open Settings",
            Subtitle   = "Press ↵ to open the settings panel",
            LucideIcon = "settings",
            ActionId   = Id,
        };
    }
}

