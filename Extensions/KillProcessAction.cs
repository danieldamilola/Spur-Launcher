using System.Diagnostics;

namespace Arc.Extensions;

/// <summary>
/// Kill Process action. Triggered by "kill [name]", e.g. "kill notepad".
/// Finds running processes matching the name and force-closes them.
/// </summary>
public sealed class KillProcessAction : IAction
{
    public string Id => "kill";

    private static readonly Regex _trigger = new(
        @"^kill\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool CanHandle(string query)
        => !string.IsNullOrWhiteSpace(query) && _trigger.IsMatch(query.Trim());

    public SearchResult BuildResult(string query)
    {
        var name = ExtractName(query);
        if (string.IsNullOrWhiteSpace(name))
            return new() { Id = "action:kill", Type = ResultType.Action, Name = "Kill Process", ActionId = Id };

        var procs = Process.GetProcessesByName(name);
        var count = procs.Length;

        return new SearchResult
        {
            Id         = $"action:kill:{name.ToLowerInvariant()}",
            Type       = ResultType.Action,
            Name       = count > 0
                ? $"Kill \"{name}\" · {count} running"
                : $"Kill \"{name}\" · not found",
            Subtitle   = count > 0 ? "Press ↵ to force-close" : "No matching process running",
            LucideIcon = "x-octagon",
            ActionId   = Id,
        };
    }

    public static string? ExtractName(string query)
    {
        var m = _trigger.Match(query.Trim());
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    public static int Execute(string query)
    {
        var name = ExtractName(query);
        if (string.IsNullOrWhiteSpace(name)) return 0;

        var procs = Process.GetProcessesByName(name);
        var killed = 0;
        foreach (var p in procs)
        {
            try
            {
                p.Kill();
                p.Dispose();
                killed++;
            }
            catch { /* process may have exited or be protected */ }
        }
        return killed;
    }
}
