using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Arc.Services;

/// <summary>Interface for app icon extraction and caching.</summary>
public interface IIconService
{
    BitmapSource? GetIcon(string path);
}

/// <summary>
/// Extracts high-quality app icons via IShellItemImageFactory — the same
/// API File Explorer uses. Returns crisp icons at 2x for HiDPI.
/// Results are cached and frozen for thread-safe cross-thread access.
/// </summary>
public sealed class IconServiceImpl : IIconService
{
    private readonly ConcurrentDictionary<string, BitmapSource?> _cache
        = new(StringComparer.OrdinalIgnoreCase);
    private const int MaxCacheEntries = 96;
    private readonly ILogger _log;

    private static readonly Guid _shellItemImageFactoryGuid =
        new("bcc18b79-ba16-442f-80c4-8a59c30c463b");
    private const int DefaultIconSize = 32;
    private const uint SIIGBF_BIGGERSIZEOK = 0x00000001;
    private const uint SIIGBF_ICONONLY     = 0x00000004;

    public IconServiceImpl(ILogger log) => _log = log;

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
    private struct SIZE { public int cx; public int cy; }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        string pszPath, IntPtr pbc, ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    public BitmapSource? GetIcon(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (_cache.TryGetValue(path, out var cached)) return cached;
        if (_cache.Count >= MaxCacheEntries) return TryExtract(path);
        return _cache.GetOrAdd(path, p => TryExtract(p));
    }

    private static BitmapSource? TryExtract(string p)
    {
        try { return ExtractInternal(p); }
        catch { return null; }
    }

    private static BitmapSource? ExtractInternal(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
            return null;

        try
        {
            var riid = _shellItemImageFactoryGuid;
            SHCreateItemFromParsingName(path, IntPtr.Zero, ref riid, out var factory);

            var size = new SIZE { cx = DefaultIconSize * 2, cy = DefaultIconSize * 2 };
            int hr = factory.GetImage(size, SIIGBF_ICONONLY | SIIGBF_BIGGERSIZEOK, out var hBitmap);

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

/// <summary>Static facade — delegates to the configured IIconService instance.</summary>
public static class IconService
{
    private static IIconService? _instance;
    private static ILogger _log = NullLogger.Instance;

    public static void Initialize(IIconService instance, ILogger logger)
    {
        _instance = instance;
        _log = logger;
    }

    public static BitmapSource? GetIcon(string path) => Instance.GetIcon(path);

    private static IIconService Instance
    {
        get
        {
            if (_instance is not null) return _instance;
            var impl = new IconServiceImpl(_log);
            _instance = impl;
            _log.Info("IconService auto-initialized with defaults.");
            return impl;
        }
    }
}
