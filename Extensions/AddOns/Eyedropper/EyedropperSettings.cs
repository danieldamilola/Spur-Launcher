using System.Runtime.InteropServices;

namespace Spur.Extensions.AddOns.Eyedropper;

/// <summary>Settings for the Eyedropper add-on.</summary>
public sealed class EyedropperSettings
{
    /// <summary>Whether to copy the color value automatically after picking.</summary>
    public bool AutoCopy { get; set; } = true;

    /// <summary>Default output format: "hex", "rgb", or "hsl".</summary>
    public string DefaultFormat { get; set; } = "hex";
}
