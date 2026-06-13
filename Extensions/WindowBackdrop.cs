using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Spur.Extensions;

/// <summary>
/// Applies Windows 11 DWM system backdrops (Mica / Acrylic) to a WPF window.
/// This bypasses WPF's own compositing layer and renders the effect at the DWM level,
/// which is the only way to get true blur-behind in WPF without AllowsTransparency.
/// </summary>
public static class WindowBackdrop
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    // DWM attribute constants
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_MICA_EFFECT = 1029; // Undocumented, for older Win11 builds

    // Backdrop types for DWMWA_SYSTEMBACKDROP_TYPE
    private const int DWMSBT_AUTO = 0;
    private const int DWMSBT_NONE = 1;
    private const int DWMSBT_MAINWINDOW = 2;      // Mica
    private const int DWMSBT_TRANSIENTWINDOW = 3;  // Acrylic
    private const int DWMSBT_TABBEDWINDOW = 4;     // Mica Alt (Tabbed)

    // Corner preference
    private const int DWMWCP_ROUND = 2;

    /// <summary>
    /// Enable a system-level backdrop on the given window.
    /// Must be called AFTER the window handle is created (e.g., OnSourceInitialized).
    /// </summary>
    /// <param name="window">The WPF window.</param>
    /// <param name="useMica">True for Mica, false for Acrylic.</param>
    /// <param name="isDarkTheme">Match the window's dark/light mode for correct DWM tinting.</param>
    public static void EnableBackdrop(Window window, bool useMica = false, bool isDarkTheme = true)
    {
        var helper = new WindowInteropHelper(window);
        var hwnd = helper.Handle;

        if (hwnd == IntPtr.Zero)
            return;

        try
        {
            // 1) Tell DWM whether this window uses dark or light mode
            //    This affects Mica/Acrylic tint colors rendered by the system
            int darkMode = isDarkTheme ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            // 2) Set the system backdrop type
            int backdropType = useMica ? DWMSBT_MAINWINDOW : DWMSBT_TRANSIENTWINDOW;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

            // 3) Request rounded corners from DWM
            int cornerPref = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

            // 4) Extend the DWM frame into the entire client area
            //    -1 means "the entire window is glass"
            var margins = new MARGINS
            {
                cxLeftWidth = -1,
                cxRightWidth = -1,
                cyTopHeight = -1,
                cyBottomHeight = -1
            };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);

            // 5) Make WPF's own background fully transparent so DWM renders behind it
            if (HwndSource.FromHwnd(hwnd) is HwndSource source && source.CompositionTarget != null)
            {
                source.CompositionTarget.BackgroundColor = Colors.Transparent;
            }
        }
        catch
        {
            // Best-effort — don't crash on unsupported Windows versions
        }
    }

    /// <summary>
    /// Update only the dark/light mode flag without re-applying the full backdrop.
    /// Call this when the user switches themes at runtime.
    /// </summary>
    public static void SetDarkMode(Window window, bool isDark)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        try
        {
            int darkMode = isDark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
        }
        catch { /* intentional: dark mode attribute not supported on older Windows */ }
    }
}
