using System.Diagnostics;

namespace Spur.Extensions;

/// <summary>
/// System action. Triggered by "system [command]", e.g. "system shutdown".
/// Executes system commands: shutdown, restart, sleep, lock, logout.
/// </summary>
public sealed class SystemAction : IAction
{
    public string Id => "system";
    public string Name => "System";
    public string IconGlyph => "power";
    public bool IsGlobal => false;

    private static readonly Dictionary<string, (string display, string command, string args)> _commands = new()
    {
        ["shutdown"] = ("Shut Down",   "shutdown", "/s /t 0"),
        ["restart"]  = ("Restart",     "shutdown", "/r /t 0"),
        ["sleep"]    = ("Sleep",       "rundll32", "powrprof.dll,SetSuspendState 0,1,0"),
        ["lock"]     = ("Lock",        "rundll32", "user32.dll,LockWorkStation"),
        ["logout"]   = ("Sign Out",    "shutdown", "/l"),
    };

    // ── Keyword-scoped ────────────────────────────────────────────
    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var filter = subQuery.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(filter))
        {
            foreach (var (key, (display, _, _)) in _commands)
            {
                yield return new SearchResult
                {
                    Id         = $"system:{key}",
                    Type       = ResultType.Action,
                    Name       = display,
                    Subtitle   = $"system {key}",
                    IconGlyph  = "power",
                    ActionId   = Id,
                };
            }
            yield break;
        }

        foreach (var (key, (display, _, _)) in _commands)
        {
            if (key.StartsWith(filter, StringComparison.OrdinalIgnoreCase)
                || display.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
            {
                yield return new SearchResult
                {
                    Id         = $"system:{key}",
                    Type       = ResultType.Action,
                    Name       = display,
                    Subtitle   = $"system {key}",
                    IconGlyph  = "power",
                    ActionId   = Id,
                };
            }
        }
    }

    // ── Legacy (global) ───────────────────────────────────────────
    public bool CanHandle(string query)
        => !string.IsNullOrWhiteSpace(query) && _commands.ContainsKey(query.Trim().ToLowerInvariant());

    public SearchResult BuildResult(string query)
    {
        var key = query.Trim().ToLowerInvariant();
        if (_commands.TryGetValue(key, out var cmd))
        {
            return new SearchResult
            {
                Id         = $"system:{key}",
                Type       = ResultType.Action,
                Name       = cmd.display,
                Subtitle   = $"Press ↵ to {cmd.display.ToLowerInvariant()}",
                IconGlyph  = "power",
                ActionId   = Id,
            };
        }
        return new() { Id = "action:system", Type = ResultType.Action, Name = "System", ActionId = Id };
    }

    public static void Execute(string command)
    {
        var key = command.Trim().ToLowerInvariant();
        if (_commands.TryGetValue(key, out var cmd))
        {
            var psi = new ProcessStartInfo(cmd.command, cmd.args)
            {
                UseShellExecute = true,
                CreateNoWindow = true,
            };
            Process.Start(psi);
        }
    }
}
