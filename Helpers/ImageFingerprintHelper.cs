using System.Windows.Media.Imaging;

namespace Spur.Helpers;

/// <summary>Shared image fingerprint computation for clipboard dedup.</summary>
internal static class ImageFingerprintHelper
{
    /// <summary>Fast image fingerprint: dimensions + first row pixel sample.</summary>
    public static int Compute(BitmapSource img)
    {
        int w   = img.PixelWidth;
        int h   = img.PixelHeight;
        int bpp = Math.Max(1, img.Format.BitsPerPixel / 8);
        int sampleWidth = Math.Min(w, 128 / bpp);
        var buf = new byte[sampleWidth * bpp];
        img.CopyPixels(new System.Windows.Int32Rect(0, 0, sampleWidth, 1), buf, buf.Length, 0);

        var hash = new HashCode();
        hash.Add(w);
        hash.Add(h);
        foreach (var b in buf) hash.Add(b);
        return hash.ToHashCode();
    }
}
