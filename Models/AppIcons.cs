namespace Spur.Models;

/// <summary>
/// Canonical paths for Spur launcher brand icons under <c>Icons/</c>.
/// Files are embedded as WPF resources (pack URIs) and used for exe, tray, and installers.
/// </summary>
public static class AppIcons
{
    public const string Folder = "Icons";

    public const string Size16  = "spur-16x16.ico";
    public const string Size24  = "spur-24x24.ico";
    public const string Size32  = "spur-32x32.ico";
    public const string Size48  = "spur-48x48.ico";
    public const string Size64  = "spur-64x64.ico";
    public const string Size96  = "spur-96x96.ico";
    public const string Size128 = "spur-128x128.ico";
    public const string Size256 = "spur-256x256.ico";
    public const string Size512 = "spur-512x512.ico";
    public const string Size1024 = "spur-1024x1024.ico";

    /// <summary>Embedded in the .exe — compile-time <see cref="ApplicationIcon"/>.</summary>
    public const string Application = Size256;

    /// <summary>System tray notification area (16×16 logical).</summary>
    public const string Tray = Size16;

    /// <summary>Velopack / installer packaging.</summary>
    public const string Installer = Size256;

    /// <summary>Legacy path kept for scripts that still reference <c>Assets\Spur.ico</c>.</summary>
    public const string AssetsLegacy = "Assets/spur.ico";

    public static Uri PackUri(string fileName)
        => new($"pack://application:,,,/{Folder}/{fileName}", UriKind.Absolute);

    public static string RelativePath(string fileName)
        => $"{Folder}/{fileName}";
}

