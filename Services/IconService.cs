using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Spur.Services;

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

        // If cache is full, evict oldest entry (first key in dictionary)
        if (_cache.Count >= MaxCacheEntries)
        {
            // Evict one entry to keep cache bounded
            var toEvict = _cache.Keys.FirstOrDefault();
            if (toEvict is not null) _cache.TryRemove(toEvict, out _);
        }

        return _cache.GetOrAdd(path, p => TryExtract(p));
    }

    private static string? ResolveUwpIconPath(string shellPath)
    {
        if (!shellPath.StartsWith("shell:AppsFolder\\", StringComparison.OrdinalIgnoreCase))
            return null;

        var appUserModelId = shellPath["shell:AppsFolder\\".Length..];
        var parts = appUserModelId.Split('!');
        if (parts.Length < 2) return null;

        var packageFamilyName = parts[0];
        var windowsAppsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");

        try
        {
            var packageDir = Directory.EnumerateDirectories(windowsAppsPath, $"{packageFamilyName}*").FirstOrDefault();
            if (packageDir == null) return null;

            // 1. Look for explicit unplated icons first
            try
            {
                var unplated = Directory.EnumerateFiles(packageDir, "*unplated*.png", SearchOption.AllDirectories).ToList();
                if (unplated.Count > 0)
                    return unplated.OrderByDescending(f => new FileInfo(f).Length).FirstOrDefault();
            }
            catch { }

            // 2. Look for Square150x150Logo or Square44x44Logo (these are transparent 99% of the time, Windows adds the plate)
            try
            {
                var logos = Directory.EnumerateFiles(packageDir, "*Square150x150Logo*.png", SearchOption.AllDirectories).ToList();
                if (logos.Count == 0)
                    logos = Directory.EnumerateFiles(packageDir, "*Square44x44Logo*.png", SearchOption.AllDirectories).ToList();
                
                if (logos.Count > 0)
                    return logos.OrderByDescending(f => new FileInfo(f).Length).FirstOrDefault();
            }
            catch { }

            // 3. Fallback to StoreLogo
            try
            {
                var logos = Directory.EnumerateFiles(packageDir, "*StoreLogo*.png", SearchOption.AllDirectories).ToList();
                if (logos.Count > 0)
                    return logos.OrderByDescending(f => new FileInfo(f).Length).FirstOrDefault();
            }
            catch { }

            // 4. Fallback to the executable
            return ResolveUwpAppToExe(appUserModelId);
        }
        catch { return null; }
    }

    /// <summary>
    /// Resolves a UWP AppUserModelId to the actual .exe path for icon extraction.
    /// </summary>
    private static string? ResolveUwpAppToExe(string appUserModelId)
    {
        try
        {
            var parts = appUserModelId.Split('!');
            if (parts.Length < 2) return null;
            var packageFamilyName = parts[0];
            var windowsAppsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
            var packageDir = Directory.EnumerateDirectories(windowsAppsPath, $"{packageFamilyName}*").FirstOrDefault();
            if (packageDir == null) return null;

            var exeName = parts[1];
            if (exeName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                exeName = exeName[..^4];

            var exePath = Path.Combine(packageDir, $"{exeName}.exe");
            if (File.Exists(exePath)) return exePath;

            var exes = Directory.EnumerateFiles(packageDir, "*.exe", SearchOption.TopDirectoryOnly)
                .Where(e => !e.Contains("Background") && !e.Contains("Service")).ToList();

            return exes.FirstOrDefault();
        }
        catch { return null; }
    }

    private static BitmapSource? TryExtract(string p)
    {
        try { return ExtractInternal(p); }
        catch { return null; }
    }

    private static BitmapSource? ExtractInternal(string path)
    {
        var realPath = path;
        if (path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
        {
            var resolved = ResolveUwpIconPath(path);
            realPath = resolved ?? path;
        }

        // If it's a PNG (e.g. from a UWP app package), load it directly.
        // IShellItemImageFactory with SIIGBF_ICONONLY on a .png returns the default image viewer icon, not the image.
        if (realPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && File.Exists(realPath))
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.UriSource = new Uri(realPath);
            bmp.DecodePixelWidth = DefaultIconSize * 2;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        if (!File.Exists(realPath) && !Directory.Exists(realPath) && !realPath.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
            return null;

        IShellItemImageFactory? factory = null;
        try
        {
            var riid = _shellItemImageFactoryGuid;
            SHCreateItemFromParsingName(realPath, IntPtr.Zero, ref riid, out factory);

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
        finally
        {
            // Release COM reference to prevent native memory leak
            if (factory is not null)
                System.Runtime.InteropServices.Marshal.ReleaseComObject(factory);
        }
    }
}
