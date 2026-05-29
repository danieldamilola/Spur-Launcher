using System.Runtime.InteropServices;

namespace Arc.Services;

/// <summary>Interface for showing notifications.</summary>
public interface INotificationService
{
    void Show(string title, string message);
}

/// <summary>
/// Sends a Windows toast notification via Shell_NotifyIcon P/Invoke.
/// No PowerShell dependency.
/// </summary>
public sealed class NotificationServiceImpl : INotificationService
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    private const uint NIM_ADD     = 0x00000000;
    private const uint NIM_DELETE  = 0x00000002;
    private const uint NIF_INFO    = 0x00000010;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIIF_INFO   = 0x00000001;
    private const uint WM_USER     = 0x0400;

    private readonly ILogger _log;

    public NotificationServiceImpl(ILogger log) => _log = log;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct NOTIFYICONDATA
    {
        public int    cbSize;
        public IntPtr hWnd;
        public uint   uID;
        public uint   uFlags;
        public uint   uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint   dwState;
        public uint   dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint   uVersionOrTimeout;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint   dwInfoFlags;
        public Guid   guidItem;
        public IntPtr hBalloonIcon;
    }

    void INotificationService.Show(string title, string message)
        => ShowInternal(title, message);

    private void ShowInternal(string title, string message)
    {
        var hwnd = CreateWindowEx(0, "STATIC", $"ArcNotify_{Environment.ProcessId}",
            0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero,
            GetModuleHandle(null), IntPtr.Zero);

        if (hwnd == IntPtr.Zero) return;

        try
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize            = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd              = hwnd,
                uID               = 1,
                uFlags            = NIF_INFO | NIF_MESSAGE,
                uCallbackMessage  = WM_USER + 100,
                szInfoTitle       = Truncate(title, 64),
                szInfo            = Truncate(message, 256),
                dwInfoFlags       = NIIF_INFO,
                uVersionOrTimeout = 5000,
            };

            Shell_NotifyIcon(NIM_ADD, ref nid);

            _ = Task.Delay(5500).ContinueWith(_ =>
            {
                Shell_NotifyIcon(NIM_DELETE, ref nid);
                DestroyWindow(hwnd);
            }, TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            _log.Warning("Notification failed", ex);
            try { DestroyWindow(hwnd); } catch { /* best effort */ }
        }
    }

    private static string Truncate(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= maxLen ? s : s[..(maxLen - 1)] + "\u2026";
    }
}

/// <summary>Static facade for backward compatibility.</summary>
public static class NotificationService
{
    private static INotificationService? _instance;
    private static ILogger _log = NullLogger.Instance;

    public static void Initialize(INotificationService instance, ILogger logger)
    {
        _instance = instance;
        _log = logger;
    }

    public static void Show(string title, string message) => Instance.Show(title, message);

    private static INotificationService Instance
    {
        get
        {
            if (_instance is not null) return _instance;
            var impl = new NotificationServiceImpl(_log);
            _instance = impl;
            _log.Info("NotificationService auto-initialized.");
            return impl;
        }
    }
}
