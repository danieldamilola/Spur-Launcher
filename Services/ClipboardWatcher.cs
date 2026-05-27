using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Volt.Services;

/// <summary>
/// Listens for system clipboard changes using Win32 AddClipboardFormatListener.
/// Hooks into the MainWindow HWND via HwndSource and calls ClipboardService.Add
/// whenever new text lands on the clipboard.
/// </summary>
public sealed class ClipboardWatcher : IDisposable
{
    private const int WM_CLIPBOARDUPDATE = 0x031D;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    private IntPtr _hwnd;
    private HwndSource? _source;
    private bool _disposed;

    public void Attach(IntPtr hwnd)
    {
        _hwnd   = hwnd;
        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(Hook);
        AddClipboardFormatListener(hwnd);
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            var text = ClipboardService.ReadFromSystem();
            if (text is not null)
                ClipboardService.Add(text);
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _source?.RemoveHook(Hook);
        if (_hwnd != IntPtr.Zero)
            RemoveClipboardFormatListener(_hwnd);
        _disposed = true;
    }
}
