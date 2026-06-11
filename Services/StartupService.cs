using Microsoft.Win32;

namespace Spur.Services;

/// <summary>Interface for startup registry management.</summary>
public interface IStartupService
{
    void Enable();
    void Disable();
    bool IsEnabled();
    /// <summary>Toggles startup registration. Returns the new state.</summary>
    bool Toggle();
}

/// <summary>
/// Toggles Spur's "launch when Windows starts" behaviour via the
/// HKCU\Software\Microsoft\Windows\CurrentVersion\Run registry key.
/// </summary>
public sealed class StartupServiceImpl : IStartupService
{
    private const string AppName = "Spur";
    private readonly ILogger _log;
    private readonly string _exePath;

    public StartupServiceImpl(ILogger log)
    {
        _log = log;
        _exePath = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Spur", "Spur.exe");

        // Sanity check: if we somehow ended up with a bare filename, log it.
        if (!Path.IsPathRooted(_exePath))
            _log.Warning($"Startup exe path is not rooted: {_exePath}");
    }

    public void Enable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);

            if (key is null)
            {
                _log.Warning("Startup registry key not found or access denied — cannot enable");
                return;
            }

            key.SetValue(AppName, $"\"{_exePath}\" --minimized");
        }
        catch (Exception ex)
        {
            _log.Warning("StartupService.Enable failed", ex);
        }
    }

    public void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);

            if (key is null)
            {
                _log.Warning("Startup registry key not found — cannot disable");
                return;
            }

            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            _log.Warning("StartupService.Disable failed", ex);
        }
    }

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            var value = key?.GetValue(AppName) as string;

            if (value is null) return false;

            // Verify the stored path actually points to this exe.
            // If the user moved the install, the stale entry would look "enabled"
            // but silently fail on actual login.
            return value.Contains(_exePath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _log.Warning("StartupService.IsEnabled check failed", ex);
            return false;
        }
    }

    /// <inheritdoc />
    public bool Toggle()
    {
        if (IsEnabled())
        {
            Disable();
            return false;
        }
        Enable();
        return IsEnabled();
    }
}
