using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Spur.Helpers;

namespace Spur.Services;

/// <summary>
/// Listens for system clipboard changes using Win32 AddClipboardFormatListener.
/// Hooks into the MainWindow HWND via HwndSource and calls IClipboardService.Add
/// whenever new text or an image lands on the clipboard.
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
    private string? _lastText;
    private int? _lastImageFingerprint;
    private readonly IClipboardService _clipboard;
    private readonly ILogger _log;

    public ClipboardWatcher(IClipboardService clipboard, ILogger? log = null)
    {
        _clipboard = clipboard;
        _log = log ?? NullLogger.Instance;
    }

    /// <summary>
    /// Registers the watcher against the given HWND. Throws
    /// <see cref="InvalidOperationException"/> if already attached.
    /// </summary>
    public void Attach(IntPtr hwnd)
    {
        if (_hwnd != IntPtr.Zero)
            throw new InvalidOperationException("ClipboardWatcher is already attached to a window.");

        _hwnd   = hwnd;
        _source = HwndSource.FromHwnd(hwnd)
            ?? throw new InvalidOperationException("HwndSource not found for the given HWND. Window may not be fully initialized.");
        _source.AddHook(Hook);

        if (!AddClipboardFormatListener(hwnd))
            _log.Warning($"AddClipboardFormatListener failed (Win32 error {Marshal.GetLastWin32Error()})");
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_CLIPBOARDUPDATE) return IntPtr.Zero;

        // Images take priority — reading text while an image is on the clipboard returns null.
        var img = _clipboard.ReadImageFromSystem();
        if (img is not null)
        {
            var fp = ImageFingerprintHelper.Compute(img);
            if (fp != _lastImageFingerprint)
            {
                _lastImageFingerprint = fp;
                _lastText = null;
                _clipboard.AddImage(img);
            }
            return IntPtr.Zero;
        }

        var text = _clipboard.ReadFromSystem();
        if (text is not null && text != _lastText)
        {
            _lastText = text;
            _lastImageFingerprint = null;
            _clipboard.Add(text);
        }

        return IntPtr.Zero;
    }



    public void Dispose()
    {
        if (_disposed) return;
        // Only remove the hook from WPF's HwndSource — do NOT dispose it (it is owned
        // by the main window and destroying it would tear down the entire HWND).
        _source?.RemoveHook(Hook);
        _source = null;
        if (_hwnd != IntPtr.Zero)
            RemoveClipboardFormatListener(_hwnd);
        _disposed = true;
    }
}
