using System.Diagnostics;

namespace Spur.Extensions;

/// <summary>
/// Kill Process action. Triggered by "kill [name]", e.g. "kill notepad".
/// Finds running processes matching the name and force-closes them.
/// </summary>
public sealed class KillProcessAction : IAction
{
    public string Id => "kill";
    public string Name => "Kill";
    public string IconGlyph => "\ue711";
    public bool IsGlobal => false;

    private static readonly Regex _trigger = new(
        @"^kill\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ── Keyword-scoped ────────────────────────────────────────────
    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var name = subQuery.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            // List top memory-consuming processes
            var procs = Process.GetProcesses()
                .OrderByDescending(p =>
                {
                    try { return p.WorkingSet64; }
                    catch { return 0; }
                })
                .Take(20);

            foreach (var p in procs)
            {
                string? pname;
                try { pname = p.ProcessName; } catch { continue; }
                var mb = 0L;
                try { mb = p.WorkingSet64 / 1024 / 1024; } catch { }
                yield return new SearchResult
                {
                    Id         = $"kill:{pname.ToLowerInvariant()}",
                    Type       = ResultType.Action,
                    Name       = pname,
                    Subtitle   = $"{mb} MB  ·  PID {p.Id}",
                    IconGlyph  = "\ue711",
                    ActionId   = Id,
                };
            }
            yield break;
        }

        // Search & kill
        var matches = Process.GetProcesses()
            .Where(p =>
            {
                try { return p.ProcessName.StartsWith(name, StringComparison.OrdinalIgnoreCase); }
                catch { return false; }
            })
            .ToList();

        if (matches.Count == 0)
        {
            yield return new SearchResult
            {
                Id         = $"action:kill:{name.ToLowerInvariant()}",
                Type       = ResultType.Action,
                Name       = $"Kill \"{name}\" · not found",
                Subtitle   = "No matching process running",
                IconGlyph  = "\ue711",
                ActionId   = Id,
            };
            yield break;
        }

        foreach (var p in matches.Take(10))
        {
            var mb = 0L;
            try { mb = p.WorkingSet64 / 1024 / 1024; } catch { }
            yield return new SearchResult
            {
                Id         = $"kill:{p.ProcessName.ToLowerInvariant()}:{p.Id}",
                Type       = ResultType.Action,
                Name       = $"Kill {p.ProcessName}",
                Subtitle   = $"{mb} MB  ·  PID {p.Id}  ·  ↵ to kill",
                IconGlyph  = "\ue711",
                ActionId   = Id,
            };
        }
    }

    // ── Legacy (global) ───────────────────────────────────────────
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
            IconGlyph = "\ue711",
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
