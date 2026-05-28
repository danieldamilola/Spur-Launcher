using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Volt.Services;

/// <summary>
/// Extracts high-quality app icons via IShellItemImageFactory — the same
/// API File Explorer uses.  Returns crisp 64×64 icons (requested at 2x for
/// HiDPI).  When pointed at a .exe target (not a .lnk shortcut), shortcut
/// overlay arrows are eliminated entirely.
///
/// Results are cached and frozen for thread-safe cross-thread access.
/// </summary>
public static class IconService
{
    private static readonly ConcurrentDictionary<string, BitmapSource?> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    private const int MaxCacheEntries = 200;

    private static readonly Guid _shellItemImageFactoryGuid =
        new("bcc18b79-ba16-442f-80c4-8a59c30c463b");

    private const int DefaultIconSize = 64;

    // ── SIIGBF flags ──────────────────────────────────────────────
    private const uint SIIGBF_BIGGERSIZEOK = 0x00000001;
    private const uint SIIGBF_ICONONLY     = 0x00000004;

    // ═══════════════════════════════════════════════════════════════
    // COM interop
    // ═══════════════════════════════════════════════════════════════

    [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage([In] SIZE size, [In] uint flags, [Out] out IntPtr phbm);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE
    {
        public int cx;
        public int cy;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        string pszPath, IntPtr pbc, ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    // ═══════════════════════════════════════════════════════════════
    // Public API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Returns a cached icon for <paramref name="path"/>, or null on failure.</summary>
    public static BitmapSource? GetIcon(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (_cache.TryGetValue(path, out var cached)) return cached;
        if (_cache.Count >= MaxCacheEntries) return TryExtract(path);
        return _cache.GetOrAdd(path, static p => TryExtract(p));
    }

    private static BitmapSource? TryExtract(string p)
    {
        try { return Extract(p); }
        catch { return null; }
    }

    // ═══════════════════════════════════════════════════════════════
    // Extraction
    // ═══════════════════════════════════════════════════════════════

    private static BitmapSource? Extract(string path)
    {
        // IShellItemImageFactory requires a real filesystem path
        if (!File.Exists(path) && !Directory.Exists(path))
            return null;

        try
        {
            var riid = _shellItemImageFactoryGuid;
            SHCreateItemFromParsingName(path, IntPtr.Zero, ref riid, out var factory);

            // Request at 2x for crisp HiDPI rendering
            var size = new SIZE
            {
                cx = DefaultIconSize * 2,
                cy = DefaultIconSize * 2,
            };

            int hr = factory.GetImage(
                size, SIIGBF_ICONONLY | SIIGBF_BIGGERSIZEOK, out var hBitmap);

            if (hr < 0 || hBitmap == IntPtr.Zero)
                return null;

            try
            {
                var bmp = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                bmp.Freeze();
                return bmp;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
        catch
        {
            return null;
        }
    }
}
