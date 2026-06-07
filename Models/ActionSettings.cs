namespace Spur.Models;

/// <summary>
/// Base class for per-action settings. Each action type has a derived class
/// that adds action-specific options — all persisted as nested objects in SpurConfig.
/// </summary>
public class ActionSettings
{
    /// <summary>The keyword that triggers this action's scope (e.g. "sys", "kill").</summary>
    public string Keyword { get; set; } = "";

    /// <summary>Whether this action is active. Mirrors SpurConfig.ActionXxx for binding.</summary>
    public bool Enabled { get; set; } = true;
}

// ═══════════════════════════════════════════════════════════════════
// System — shutdown, restart, sleep, lock, logout
// ═══════════════════════════════════════════════════════════════════
public sealed class SystemActionSettings : ActionSettings
{
    public SystemActionSettings() { Keyword = "sys"; Enabled = true; }
}

// ═══════════════════════════════════════════════════════════════════
// Timer — countdown from the launcher
// ═══════════════════════════════════════════════════════════════════
public sealed class TimerActionSettings : ActionSettings
{
    /// <summary>Comma-separated presets shown in the scope, e.g. "1m,3m,5m,10m,15m,30m".</summary>
    public string DefaultPresets { get; set; } = "1m,3m,5m,10m,15m,30m";

    public TimerActionSettings() { Keyword = "timer"; Enabled = false; }
}

// ═══════════════════════════════════════════════════════════════════
// Kill Process — find and kill a running process
// ═══════════════════════════════════════════════════════════════════
public sealed class KillProcessActionSettings : ActionSettings
{
    /// <summary>Show the window title (e.g. "Document1 — Notepad") next to the process name.</summary>
    public bool ShowWindowTitles { get; set; } = true;

    /// <summary>Sort processes that have visible windows to the top of the list.</summary>
    public bool PrioritizeVisibleWindows { get; set; } = true;

    public KillProcessActionSettings() { Keyword = "kill"; Enabled = false; }
}

// ═══════════════════════════════════════════════════════════════════
// Password Generator
// ═══════════════════════════════════════════════════════════════════
public sealed class PasswordGenActionSettings : ActionSettings
{
    public int  DefaultLength   { get; set; } = 16;
    public bool IncludeSymbols  { get; set; } = true;
    public bool IncludeNumbers  { get; set; } = true;
    public bool IncludeUppercase { get; set; } = true;

    public PasswordGenActionSettings() { Keyword = "pw"; Enabled = false; }
}

// ═══════════════════════════════════════════════════════════════════
// Screenshot
// ═══════════════════════════════════════════════════════════════════
public sealed class ScreenshotActionSettings : ActionSettings
{
    /// <summary>png, jpg, or bmp.</summary>
    public string SaveFormat { get; set; } = "png";

    /// <summary>Custom save folder (empty = default Pictures folder).</summary>
    public string SaveFolder { get; set; } = "";

    public ScreenshotActionSettings() { Keyword = "ss"; Enabled = false; }
}

// ═══════════════════════════════════════════════════════════════════
// Quick Note
// ═══════════════════════════════════════════════════════════════════
public sealed class QuickNoteActionSettings : ActionSettings
{
    /// <summary>Custom save folder for notes (empty = default Documents folder).</summary>
    public string SaveFolder { get; set; } = "";

    public QuickNoteActionSettings() { Keyword = "note"; Enabled = false; }
}

// ═══════════════════════════════════════════════════════════════════
// Currency converter
// ═══════════════════════════════════════════════════════════════════
public sealed class CurrencyActionSettings : ActionSettings
{
    public string DefaultFrom { get; set; } = "USD";
    public string DefaultTo   { get; set; } = "EUR";

    public CurrencyActionSettings() { Keyword = "cur"; Enabled = true; }
}

// ═══════════════════════════════════════════════════════════════════
// Color tools
// ═══════════════════════════════════════════════════════════════════
public sealed class ColorActionSettings : ActionSettings
{
    public ColorActionSettings() { Keyword = "color"; Enabled = true; }
}

// ═══════════════════════════════════════════════════════════════════
// IP tools
// ═══════════════════════════════════════════════════════════════════
public sealed class IpActionSettings : ActionSettings
{
    public IpActionSettings() { Keyword = "ip"; Enabled = false; }
}

// ═══════════════════════════════════════════════════════════════════
// AI assistant
// ═══════════════════════════════════════════════════════════════════
public sealed class AiActionSettings : ActionSettings
{
    public AiActionSettings() { Keyword = "ai"; Enabled = false; }
}

// ═══════════════════════════════════════════════════════════════════
// Shell — execute a command through the user's preferred terminal
// ═══════════════════════════════════════════════════════════════════
public sealed class ShellActionSettings : ActionSettings
{
    public bool CloseAfterExecution { get; set; } = true;
    public bool AlwaysRunAsAdministrator { get; set; } = false;
    public bool UseWindowsTerminal { get; set; } = false;

    /// <summary>cmd, powershell, or pwsh.</summary>
    public string Terminal { get; set; } = "cmd";

    public ShellActionSettings() { Keyword = ">"; Enabled = false; }
}
