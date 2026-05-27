using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Volt.Services;

/// <summary>
/// Registers a system-wide hotkey via Win32 RegisterHotKey and dispatches
/// it to a callback. Cleans up automatically via IDisposable.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt  = 0x0001;
    private const uint ModCtrl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin  = 0x0008;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private IntPtr _hwnd;
    private int _id;
    private Action? _callback;
    private HwndSource? _source;
    private bool _disposed;

    /// <summary>Registers the hotkey described by <paramref name="shortcutString"/> (e.g. "Alt+Space").</summary>
    public bool Register(IntPtr hwnd, string shortcutString, Action callback)
    {
        _hwnd = hwnd;
        _callback = callback;
        _id = GetHashCode();

        ParseShortcut(shortcutString, out var mod, out var vk);

        var ok = RegisterHotKey(hwnd, _id, mod, vk);
        if (!ok)
            Debug.WriteLine($"[Volt] RegisterHotKey failed for '{shortcutString}' (may already be registered).");

        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(Hook);
        return ok;
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == _id)
        {
            _callback?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private static void ParseShortcut(string s, out uint mod, out uint vk)
    {
        mod = 0;
        vk  = 0;

        foreach (var part in s.Split('+'))
        {
            var token = part.Trim().ToLowerInvariant();
            switch (token)
            {
                case "alt":   mod |= ModAlt;   break;
                case "ctrl":  mod |= ModCtrl;  break;
                case "shift": mod |= ModShift; break;
                case "win":   mod |= ModWin;   break;
                default:
                    var key = Enum.TryParse<Key>(part.Trim(), true, out var k) ? k : Key.None;
                    if (key != Key.None)
                        vk = (uint)KeyInterop.VirtualKeyFromKey(key);
                    break;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _source?.RemoveHook(Hook);
        if (_hwnd != IntPtr.Zero)
            UnregisterHotKey(_hwnd, _id);
        _disposed = true;
    }
}
