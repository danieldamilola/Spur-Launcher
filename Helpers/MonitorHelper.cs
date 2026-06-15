using System.Runtime.InteropServices;

namespace Spur.Helpers;

/// <summary>
/// Enumerates connected monitors using Win32 API (EnumDisplayMonitors / GetMonitorInfo).
/// Avoids System.Windows.Forms dependency which causes type ambiguity with WPF.
/// </summary>
internal static class MonitorHelper
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X, Y;
    }

    private const uint MONITORINFOF_PRIMARY = 1;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    private const uint MONITOR_DEFAULTTOPRIMARY = 1;

    public record MonitorInfo(IntPtr Handle, RECT WorkArea, RECT MonitorArea, bool IsPrimary, string DeviceName);

    /// <summary>Returns all connected monitors.</summary>
    public static List<MonitorInfo> GetAllMonitors()
    {
        var monitors = new List<MonitorInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref RECT _, IntPtr _) =>
        {
            var mi = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
            if (GetMonitorInfo(hMonitor, ref mi))
            {
                monitors.Add(new MonitorInfo(
                    hMonitor,
                    mi.rcWork,
                    mi.rcMonitor,
                    (mi.dwFlags & MONITORINFOF_PRIMARY) != 0,
                    mi.szDevice));
            }
            return true;
        }, IntPtr.Zero);

        return monitors;
    }

    /// <summary>Gets the monitor that the mouse cursor is currently on.</summary>
    public static MonitorInfo GetMonitorFromCursor()
    {
        GetCursorPos(out POINT pt);
        var hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTOPRIMARY);

        var mi = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
        GetMonitorInfo(hMonitor, ref mi);

        return new MonitorInfo(
            hMonitor,
            mi.rcWork,
            mi.rcMonitor,
            (mi.dwFlags & MONITORINFOF_PRIMARY) != 0,
            mi.szDevice);
    }

    /// <summary>Gets the primary monitor.</summary>
    public static MonitorInfo GetPrimaryMonitor()
    {
        var all = GetAllMonitors();
        return all.FirstOrDefault(m => m.IsPrimary) ?? all[0];
    }
}
