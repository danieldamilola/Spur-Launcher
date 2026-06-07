namespace Spur.Extensions;

public sealed class ShellAction : IAction
{
    public string Id => "shell";
    public string Name => "Shell";
    public string IconGlyph => "\ue765";
    public bool IsGlobal => false;

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var command = subQuery.Trim();
        yield return new SearchResult
        {
            Id = string.IsNullOrWhiteSpace(command) ? "action:shell" : $"shell:{command}",
            Type = ResultType.Action,
            Name = string.IsNullOrWhiteSpace(command) ? "Run Command" : $"Run {command}",
            Subtitle = string.IsNullOrWhiteSpace(command) ? "Type a command to run" : "Press ↵ to execute",
            IconGlyph = "\ue765",
            ActionId = Id,
            Score = 600,
        };
    }

    public bool CanHandle(string query) => false;

    public SearchResult BuildResult(string query) => new()
    {
        Id = "action:shell",
        Type = ResultType.Action,
        Name = "Run Command",
        Subtitle = "Type a command to run",
        IconGlyph = "\ue765",
        ActionId = Id,
    };
}
