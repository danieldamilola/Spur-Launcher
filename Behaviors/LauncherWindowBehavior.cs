using System.Windows;
using Spur.ViewModels;

namespace Spur.Behaviors;

public static class LauncherWindowBehavior
{
    public static void PositionWindow(Window window, MainViewModel vm)
    {
        var monitor = GetTargetMonitor(vm.Config.PreferredMonitor);
        var dpiScale = GetDpiScale(window);
        var workArea = new Rect(
            monitor.WorkArea.Left / dpiScale,
            monitor.WorkArea.Top / dpiScale,
            (monitor.WorkArea.Right - monitor.WorkArea.Left) / dpiScale,
            (monitor.WorkArea.Bottom - monitor.WorkArea.Top) / dpiScale);

        var width = window.Width > 0 && !double.IsNaN(window.Width) ? window.Width : window.ActualWidth;
        var height = window.Height > 0 && !double.IsNaN(window.Height) ? window.Height : window.ActualHeight;

        const double shadowMargin = Models.LauncherLayout.ShadowMargin;
        var barHeight = 56.0;
        var left = workArea.Left + (workArea.Width - width) / 2;
        var top = workArea.Top + (workArea.Height - barHeight) / 2;

        switch (vm.Config.SearchWindowPosition?.ToLowerInvariant())
        {
            case "centertop":
            case "center top":
                top = workArea.Top + workArea.Height * 0.15;
                break;
            case "lefttop":
            case "left top":
                left = workArea.Left + 32;
                top = workArea.Top + 32;
                break;
            case "righttop":
            case "right top":
                left = workArea.Right - width - 32;
                top = workArea.Top + 32;
                break;
            case "custom":
                if (vm.Config.CustomWindowLeft >= 0) left = vm.Config.CustomWindowLeft;
                if (vm.Config.CustomWindowTop >= 0) top = vm.Config.CustomWindowTop;
                break;
        }

        window.Left = left - shadowMargin;
        window.Top = top - shadowMargin;
    }

    private static Helpers.MonitorHelper.MonitorInfo GetTargetMonitor(string preferredMonitor)
    {
        switch (preferredMonitor?.ToLowerInvariant())
        {
            case "mouse":
            case "-1":
                return Helpers.MonitorHelper.GetMonitorFromCursor();
            case null:
            case "":
            case "primary":
            case "0":
                return Helpers.MonitorHelper.GetPrimaryMonitor();
            default:
                if (int.TryParse(preferredMonitor, out int idx))
                {
                    var all = Helpers.MonitorHelper.GetAllMonitors();
                    if (idx >= 1 && idx <= all.Count)
                        return all[idx - 1];
                }
                return Helpers.MonitorHelper.GetPrimaryMonitor();
        }
    }

    private static double GetDpiScale(Window window)
    {
        var source = PresentationSource.FromVisual(window);
        if (source?.CompositionTarget != null)
            return source.CompositionTarget.TransformToDevice.M11;
        return 1.0;
    }
}
