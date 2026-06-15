namespace Spur.Extensions.AddOns.Ocr;

/// <summary>Settings for the OCR add-on.</summary>
public sealed class OcrSettings
{
    /// <summary>Whether to automatically copy extracted text to clipboard.</summary>
    public bool AutoCopy { get; set; } = true;
}
