namespace Spur.Extensions.AddOns.Shell;

public sealed class ShellSettings
{
    public string Terminal { get; set; } = "cmd";
    public bool CloseAfterExecution { get; set; } = true;
    public bool AlwaysRunAsAdministrator { get; set; } = false;
    public bool UseWindowsTerminal { get; set; } = false;
}
