using System.Runtime.InteropServices;

namespace Volt.Services;

/// <summary>
/// Sends a Windows toast notification via shell32 ShellExecute.
/// Used by the timer action when the countdown completes.
/// </summary>
public static class NotificationService
{
    [DllImport("shell32.dll")]
    private static extern IntPtr ShellExecute(IntPtr hwnd, string lpOperation,
        string lpFile, string lpParameters, string lpDirectory, int nShowCmd);

    /// <summary>
    /// Shows a Windows balloon notification via a PowerShell one-liner.
    /// This works on all Windows versions without requiring COM or UWP.
    /// </summary>
    public static void Show(string title, string message)
    {
        try
        {
            var script = $"""
                [System.Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms') | Out-Null;
                $n = New-Object System.Windows.Forms.NotifyIcon;
                $n.Icon = [System.Drawing.SystemIcons]::Information;
                $n.Visible = $true;
                $n.ShowBalloonTip(4000, '{EscapePs(title)}', '{EscapePs(message)}', [System.Windows.Forms.ToolTipIcon]::Info);
                Start-Sleep -Seconds 5;
                $n.Dispose();
                """;

            Process.Start(new ProcessStartInfo
            {
                FileName        = "powershell.exe",
                Arguments       = $"-NoProfile -WindowStyle Hidden -Command \"{script.Replace("\"", "\\\"")}\"",
                CreateNoWindow  = true,
                UseShellExecute = false,
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Volt] Notification failed: {ex.Message}");
        }
    }

    private static string EscapePs(string s) =>
        s.Replace("'", "''").Replace("\r", "").Replace("\n", " ");
}
