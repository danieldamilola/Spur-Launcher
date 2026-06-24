using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace Spur.Models;

/// <summary>A single clipboard history entry — either text or an image.</summary>
public sealed class ClipboardEntry : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public const int MaxStoredTextChars = 20_000;

    /// <summary>Text constructor.</summary>
    public ClipboardEntry(string content)
    {
        Id            = Guid.NewGuid();
        FullTextHash  = content.GetHashCode(StringComparison.Ordinal);
        IsTruncated   = content.Length > MaxStoredTextChars;
        Content       = IsTruncated ? content[..MaxStoredTextChars] : content;
        Timestamp     = DateTime.UtcNow;
        IsImage       = false;
        var display   = Content.Replace('\n', ' ').Replace('\r', ' ');
        Preview      = display.Length > 80 ? display[..80] + "..." : display;
    }

    /// <summary>Deserialization constructor — restores the original timestamp.</summary>
    internal ClipboardEntry(string content, DateTime timestamp)
    {
        Id            = Guid.NewGuid();
        FullTextHash  = content.GetHashCode(StringComparison.Ordinal);
        IsTruncated   = content.Length > MaxStoredTextChars;
        Content       = IsTruncated ? content[..MaxStoredTextChars] : content;
        Timestamp     = timestamp;
        IsImage       = false;
        var display   = Content.Replace('\n', ' ').Replace('\r', ' ');
        Preview       = display.Length > 80 ? display[..80] + "..." : display;
    }

    /// <summary>Image constructor. The BitmapSource must already be frozen.</summary>
    public ClipboardEntry(BitmapSource image)
    {
        Id        = Guid.NewGuid();
        Content   = string.Empty;
        Timestamp = DateTime.UtcNow;
        IsImage   = true;
        Image     = image;
        Preview   = $"Image {image.PixelWidth} × {image.PixelHeight}";
    }

    public Guid        Id        { get; }
    public string      Content   { get; }
    public DateTime    Timestamp { get; }
    public bool        IsImage   { get; }
    public bool        IsTruncated { get; }
    /// <summary>Hash of the full original text (before truncation). Used for reliable dedup.</summary>
    public int         FullTextHash { get; }
    public BitmapSource? Image   { get; }
    /// <summary>Fingerprint for image dedup (dimensions + first row sample hash).</summary>
    public int ImageFingerprint { get; set; }

    /// <summary>Truncated single-line preview for display in results list.</summary>
    public string Preview { get; }

    /// <summary>Whether this entry is pinned. Set by the ViewModel during refresh.</summary>
    private bool _isPinned;
    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (_isPinned == value) return;
            _isPinned = value;
            OnPropertyChanged(nameof(IsPinned));
        }
    }

    public string TimeAgo
    {
        get
        {
            var elapsed = DateTime.UtcNow - Timestamp;
            if (elapsed.TotalSeconds < 60) return "just now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
            return $"{(int)elapsed.TotalHours}h ago";
        }
    }
}
