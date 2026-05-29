using System.Runtime.InteropServices;

namespace Arc.Extensions;

/// <summary>
/// Handles system power/session commands: shutdown, restart, sleep, hibernate, lock, sign out.
/// Triggered by an exact keyword match (case-insensitive).
/// </summary>
public sealed class SystemAction : IAction
{
    public string Id => "system";

    private static readonly Dictionary<string, (string Name, string Subtitle, string Icon)> _commands =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["shutdown"]  = ("Shut Down",   "Power off this PC",        "power"),
        ["restart"]   = ("Restart",     "Restart Windows",          "refresh-cw"),
        ["sleep"]     = ("Sleep",       "Put this PC to sleep",     "moon"),
        ["hibernate"] = ("Hibernate",   "Hibernate this PC",        "archive"),
        ["lock"]      = ("Lock",        "Lock this screen",         "lock"),
        ["sign out"]  = ("Sign Out",    "Sign out of Windows",      "log-out"),
        ["logoff"]    = ("Sign Out",    "Sign out of Windows",      "log-out"),
    };

    public bool CanHandle(string query)
        => !string.IsNullOrWhiteSpace(query) && _commands.ContainsKey(query.Trim());

    public SearchResult BuildResult(string query)
    {
        var (name, subtitle, icon) = _commands[query.Trim()];
        return new SearchResult
        {
            Id         = $"system:{query.Trim().ToLowerInvariant()}",
            Type       = ResultType.Action,
            Name       = name,
            Subtitle   = subtitle,
            LucideIcon = icon,
            ActionId   = Id,
        };
    }

    public static void Execute(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;

        switch (query.Trim().ToLowerInvariant())
        {
            case "sleep":
                SetSuspendState(false, true, false);
                break;

            case "hibernate":
                SetSuspendState(true, true, false);
                break;

            case "lock":
                LockWorkStation();
                break;

            case "sign out":
            case "logoff":
                ExitWindowsEx(0x00, 0x00040000);
                break;

            case "restart":
                Process.Start(new ProcessStartInfo("shutdown.exe", "/r /t 5")
                    { UseShellExecute = false, CreateNoWindow = true });
                break;

            case "shutdown":
                Process.Start(new ProcessStartInfo("shutdown.exe", "/s /t 5")
                    { UseShellExecute = false, CreateNoWindow = true });
                break;
        }
    }

    [DllImport("powrprof.dll", ExactSpelling = true)]
    private static extern bool SetSuspendState(bool bHibernate, bool bForce, bool bWakeupEventsDisabled);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool LockWorkStation();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);
}

