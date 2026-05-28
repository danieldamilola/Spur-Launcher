using Microsoft.Win32;

namespace Volt.Services;

/// <summary>
/// Toggles Volt's "launch when Windows starts" behaviour via the
/// HKCU\Software\Microsoft\Windows\CurrentVersion\Run registry key.
/// </summary>
public static class StartupService
{
    private const string AppName = "Volt";

    private static readonly string ExePath =
        Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "Volt.exe";

    /// <summary>Adds Volt to the current user's startup registry key.</summary>
    public static void Enable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
            key?.SetValue(AppName, $"\"{ExePath}\" --minimized");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Volt] StartupService.Enable failed: {ex.Message}");
        }
    }

    /// <summary>Removes Volt from the current user's startup registry key.</summary>
    public static void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
            key?.DeleteValue(AppName, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Volt] StartupService.Disable failed: {ex.Message}");
        }
    }

    /// <summary>Returns true if Volt is registered to launch on startup.</summary>
    public static bool IsEnabled()
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
