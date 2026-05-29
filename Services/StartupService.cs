using Microsoft.Win32;

namespace Arc.Services;

/// <summary>Interface for startup registry management.</summary>
public interface IStartupService
{
    void Enable();
    void Disable();
    bool IsEnabled();
}

/// <summary>
/// Toggles Arc's "launch when Windows starts" behaviour via the
/// HKCU\Software\Microsoft\Windows\CurrentVersion\Run registry key.
/// </summary>
public sealed class StartupServiceImpl : IStartupService
{
    private const string AppName = "Arc";
    private readonly ILogger _log;
    private readonly string _exePath;

    public StartupServiceImpl(ILogger log)
    {
        _log = log;
        _exePath = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? "Arc.exe";
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

/// <summary>Static facade — delegates to the configured IStartupService instance.</summary>
public static class StartupService
{
    private static IStartupService? _instance;
    private static ILogger _log = NullLogger.Instance;

    public static void Initialize(IStartupService instance, ILogger logger)
    {
        _instance = instance;
        _log = logger;
    }

    public static void Enable() => Instance.Enable();
    public static void Disable() => Instance.Disable();
    public static bool IsEnabled() => Instance.IsEnabled();

    private static IStartupService Instance
    {
        get
        {
            if (_instance is not null) return _instance;
            var impl = new StartupServiceImpl(_log);
            _instance = impl;
            _log.Info("StartupService auto-initialized with defaults.");
            return impl;
        }
    }
}
