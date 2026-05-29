using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Arc.Services;

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
    private string? _lastText;
    private System.Windows.Media.Imaging.BitmapSource? _lastImage;
    private readonly ILogger _log;

    public ClipboardWatcher(ILogger? log = null)
    {
        _log = log ?? NullLogger.Instance;
    }

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
            // Images take priority (checking text on an image clipboard gives a null)
            var img = ClipboardService.ReadImageFromSystem();
            if (img is not null)
            {
                // Deduplicate by comparing dimensions (simple but effective)
                if (_lastImage is null || _lastImage.PixelWidth != img.PixelWidth || _lastImage.PixelHeight != img.PixelHeight)
                {
                    _lastImage = img;
                    _lastText = null;
                    ClipboardService.AddImage(img);
                }
                return IntPtr.Zero;
            }

            var text = ClipboardService.ReadFromSystem();
            if (text is not null && text != _lastText)
            {
                _lastText = text;
                _lastImage = null;
                ClipboardService.Add(text);
            }
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
