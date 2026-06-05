namespace Spur.Extensions;

/// <summary>
/// Settings action. Triggered by "settings".
/// Opens the settings panel. Remains globally available.
/// </summary>
public sealed class SettingsAction : IAction
{
    public string Id => "settings";
    public string Name => "Settings";
    public string IconGlyph => "\ue713";
    public bool IsGlobal => true;

    // ── Keyword-scoped ────────────────────────────────────────────
    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        yield return BuildResult("settings");
    }

    // ── Legacy (global) ───────────────────────────────────────────
    public bool CanHandle(string query)
        => !string.IsNullOrWhiteSpace(query) && query.Trim().Equals("settings", StringComparison.OrdinalIgnoreCase);

    public SearchResult BuildResult(string query) => new()
    {
        Id         = "action:settings",
        Type       = ResultType.Action,
        Name       = "Open Settings",
        Subtitle   = "Press ↵ to open the settings panel",
        IconGlyph = "\ue713",
        ActionId   = Id,
    };

    public static void Execute()
    {
        // Settings opened via the main window / panel system
    }
}
