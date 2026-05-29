using System.Windows.Media.Imaging;

namespace Arc.Models;

/// <summary>A single clipboard history entry — either text or an image.</summary>
public sealed class ClipboardEntry
{
    public const int MaxStoredTextChars = 20_000;

    /// <summary>Text constructor.</summary>
    public ClipboardEntry(string content)
    {
        IsTruncated = content.Length > MaxStoredTextChars;
        Content   = IsTruncated ? content[..MaxStoredTextChars] : content;
        Timestamp = DateTime.Now;
        IsImage   = false;
        var display = Content.Replace('\n', ' ').Replace('\r', ' ');
        Preview   = display.Length > 80 ? display[..80] + "..." : display;
    }

    /// <summary>Image constructor. The BitmapSource must already be frozen.</summary>
    public ClipboardEntry(BitmapSource image)
    {
        Content   = string.Empty;
        Timestamp = DateTime.Now;
        IsImage   = true;
        Image     = image;
        Preview   = $"Image  {image.PixelWidth} × {image.PixelHeight}";
    }

    public string      Content   { get; }
    public DateTime    Timestamp { get; }
    public bool        IsImage   { get; }
    public bool        IsTruncated { get; }
    public BitmapSource? Image   { get; }

    /// <summary>Truncated single-line preview for display in results list.</summary>
    public string Preview { get; }

    public string TimeAgo
    {
        get
        {
            var elapsed = DateTime.Now - Timestamp;
            if (elapsed.TotalSeconds < 60) return "just now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
            return $"{(int)elapsed.TotalHours}h ago";
        }
    }
}

