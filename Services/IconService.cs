using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Volt.Services;

/// <summary>
/// Extracts 32×32 icons from .exe / .lnk files via SHGetFileInfo.
/// Results are cached and frozen for thread-safe cross-thread access.
/// </summary>
public static class IconService
{
    private const uint SHGFI_ICON      = 0x100;
    private const uint SHGFI_LARGEICON = 0x000;

    private static readonly ConcurrentDictionary<string, BitmapSource?> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int    iIcon;
        public uint   dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttr,
        ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>Returns a cached icon for <paramref name="path"/>, or null on failure.</summary>
    public static BitmapSource? GetIcon(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        return _cache.GetOrAdd(path, static p =>
        {
            try { return Extract(p); }
            catch { return null; }
        });
    }

    public static Task PreloadAsync(IEnumerable<string> paths)
        => Task.Run(() => { foreach (var p in paths) GetIcon(p); });

    private static BitmapSource? Extract(string path)
    {
        var info = new SHFILEINFO();
        var result = SHGetFileInfo(path, 0, ref info,
            (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_LARGEICON);

        if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero) return null;

        try
        {
            var bmp = Imaging.CreateBitmapSourceFromHIcon(
                info.hIcon, Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(32, 32));
            bmp.Freeze();
            return bmp;
        }
        finally { DestroyIcon(info.hIcon); }
    }
}
