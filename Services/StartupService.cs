using Microsoft.Win32;

namespace Spur.Services;

/// <summary>Interface for startup registry management.</summary>
public interface IStartupService
{
    void Enable();
    void Disable();
    bool IsEnabled();
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
            ?? "Spur.exe";
    }

    public void Enable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
            key?.SetValue(AppName, $"\"{_exePath}\" --minimized");
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
            key?.DeleteValue(AppName, throwOnMissingValue: false);
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
            return key?.GetValue(AppName) is not null;
        }
        catch
        {
            return false;
        }
    }
}
