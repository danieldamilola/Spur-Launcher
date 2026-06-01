using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using Arc.Models;

namespace Arc.Helpers;

/// <summary>Loads embedded .ico resources for tray and UI.</summary>
public static class IconLoader
{
    public static BitmapSource? LoadBitmap(string iconFileName, bool preferSmallestFrame = false)
    {
        try
        {
            using var stream = OpenIconStream(iconFileName);
            if (stream is null) return null;

            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.None,
                BitmapCacheOption.OnLoad);

            if (decoder.Frames.Count == 0) return null;

            var frame = preferSmallestFrame
                ? decoder.Frames.OrderBy(f => f.PixelWidth).First()
                : decoder.Frames.OrderByDescending(f => f.PixelWidth).First();

            if (frame.CanFreeze) frame.Freeze();
            return frame;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Tray / notification area — use native .ico for crisp rendering.</summary>
    public static System.Drawing.Icon? LoadTrayIcon()
    {
        try
        {
            var stream = OpenIconStream(AppIcons.Tray);
            return stream is null ? null : new System.Drawing.Icon(stream);
        }
        catch
        {
            return null;
        }
    }

    public static BitmapSource? LoadTrayBitmap()
        => LoadBitmap(AppIcons.Tray, preferSmallestFrame: true);

    private static Stream? OpenIconStream(string iconFileName)
    {
        var packUri = AppIcons.PackUri(iconFileName);
        var appStream = Application.GetResourceStream(packUri)?.Stream;
        if (appStream is not null) return appStream;

        // Fallback: read from output directory (dev loose files)
        var path = Path.Combine(AppContext.BaseDirectory, AppIcons.RelativePath(iconFileName));
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public static void ExtractEmbeddedIcon(string iconFileName, string outputPath)
    {
        using var input = OpenIconStream(iconFileName);
        if (input is null) return;
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        using var output = File.Create(outputPath);
        input.CopyTo(output);
    }
}
