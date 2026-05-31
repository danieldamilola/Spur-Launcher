using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Arc.Converters;

/// <summary>Converts a Lucide icon name string to a WPF <see cref="Geometry"/> for Path.Data binding.</summary>
[ValueConversion(typeof(string), typeof(Geometry))]
public sealed class LucideIconConverter : IValueConverter
{
    public static readonly LucideIconConverter Instance = new();

    private static readonly Dictionary<string, string> _paths = new(StringComparer.OrdinalIgnoreCase)
    {
        // Actions
        ["calculator"]  = "M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2zM9 9h6M9 12h6M9 15h4",
        ["timer"]       = "M12 6V12L15 15M3 12a9 9 0 1 0 18 0 9 9 0 1 0-18 0",
        ["palette"]     = "M12 2a10 10 0 0 1 10 10 4 4 0 0 1-4 4h-1.5a2 2 0 0 0-2 2 2 2 0 0 1-2 2A10 10 0 0 1 12 2z M7.5 12a.5.5 0 1 0 1 0 .5.5 0 0 0-1 0 M16.5 10a.5.5 0 1 0 1 0 .5.5 0 0 0-1 0 M14.5 7.5a.5.5 0 1 0 1 0 .5.5 0 0 0-1 0 M9 7a.5.5 0 1 0 1 0 .5.5 0 0 0-1 0",
        ["globe"]       = "M12 2a10 10 0 1 0 0 20A10 10 0 0 0 12 2zM2 12h20M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z",
        ["sparkles"]    = "M12 3 9.5 9.5 3 12l6.5 2.5L12 21l2.5-6.5L21 12l-6.5-2.5zM5 3v4M19 17v4M3 5h4M17 19h4",
        ["zap"]         = "M13 2L3 14h9l-1 8 10-12h-9l1-8z",
        ["bolt"]        = "M13 2L3 14h9l-1 8 10-12h-9l1-8z",
        ["clipboard"]   = "M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2M9 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M9 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2",
        ["folder"]      = "M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V7z",
        ["settings"]    = "M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z",
        ["search"]      = "M21 21l-6-6m2-5a7 7 0 1 1-14 0 7 7 0 0 1 14 0z",
        ["terminal"]    = "M4 17l6-6-6-6M12 19h8",
        // Network / System
        ["wifi"]        = "M5 13a10 10 0 0 1 14 0M8.5 16.5a5 5 0 0 1 7 0M2 8.82a15 15 0 0 1 20 0M12 20h.01",
        ["signal"]      = "M12 20v-3M10 7v10M7 10v7M4 13v4M16 4v16M19 10v10",
        ["bluetooth"]   = "M6.5 6.5l11 11L12 23V1l5.5 5.5-11 11",
        ["volume-2"]    = "M11 5L6 9H2v6h4l5 4V5zM19.07 4.93a10 10 0 0 1 0 14.14M15.54 8.46a5 5 0 0 1 0 7.07",
        ["battery"]     = "M6 7h11a2 2 0 0 1 2 2v.45M17 13v3a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2h1M22 11v2M7 11v4M11 11v4M14 11v4",
        ["cpu"]         = "M6 4h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2zM8 8h8v8H8zM8 2v2M16 2v2M8 20v2M16 20v2M20 8h2M20 16h2M2 8h2M2 16h2",
        ["hard-drive"]  = "M22 12H2M5.45 5.11L2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11zM6 16h.01M10 16h.01",
        ["activity"]    = "M22 12h-4l-3 9L9 3l-3 9H2",
        // UI / Navigation
        ["monitor"]     = "M20 3H4a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2zM8 21h8M12 17v4",
        ["bell"]        = "M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9M10.3 21a1.94 1.94 0 0 0 3.4 0",
        ["user"]        = "M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z",
        ["info"]        = "M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2 2 6.477 2 12s4.477 10 10 10zM12 16v-4M12 8h.01",
        ["package"]     = "M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16zM3.3 7l8.7 5 8.7-5M12 22V12",
        ["layout-grid"] = "M3 3h7v7H3zM14 3h7v7h-7zM14 14h7v7h-7zM3 14h7v7H3z",
        ["eye"]         = "M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8zM12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z",
        ["clock"]       = "M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2 2 6.477 2 12s4.477 10 10 10zM12 6v6l4 2",
        ["plane"]       = "M17.8 19.2L16 11l3.5-3.5C21 6 21.5 4 21 3c-1-.5-3 0-4.5 1.5L13 8 4.8 6.2c-.5-.1-.9.1-1.1.5l-.3.5c-.2.5-.1 1 .3 1.3L9 12l-2 3H4l-1 1 3 2 2 3 1-1v-3l3-2 3.5 5.3c.3.4.8.5 1.3.3l.5-.2c.4-.3.6-.7.5-1.2z",
        ["share-2"]     = "M18 8a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM6 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM18 22a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM8.59 13.51l6.83 3.98M15.41 6.51l-6.82 3.98",
        ["refresh-cw"]  = "M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16M21 3v5h-5M3 21v-5h5",
        // Actions
        ["dollar-sign"] = "M12 2v20M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6",
        ["key"]         = "M21 2l-2 2m-7.61 7.61a5.5 5.5 0 1 1-7.778 7.778 5.5 5.5 0 0 1 7.777-7.777zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4",
        ["file-text"]   = "M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8zM14 2v6h6M16 13H8M16 17H8M10 9H8",
        ["camera"]      = "M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2zM12 17a4 4 0 1 0 0-8 4 4 0 0 0 0 8z",
        ["image"]       = "M19 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2zM8.5 10a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3zM21 15l-5-5L5 21",
        // System / Power
        ["power"]       = "M18.36 6.64a9 9 0 1 1-12.73 0M12 2v10",
        ["moon"]        = "M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z",
        ["shield"]      = "M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z",
        ["lock"]        = "M19 11H5a2 2 0 0 0-2 2v7a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7a2 2 0 0 0-2-2zM7 11V7a5 5 0 0 1 10 0v4",
        ["log-out"]     = "M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9",
        ["archive"]     = "M21 8v13H3V8M1 3h22v5H1zM10 12h4",
        ["x-octagon"]   = "M7.86 2h8.28L22 7.86v8.28L16.14 22H7.86L2 16.14V7.86L7.86 2zM15 9l-6 6M9 9l6 6",
        ["x"]           = "M18 6L6 18M6 6l12 12",
        // Navigation
        ["arrow-left"]   = "M19 12H5M12 19l-7-7 7-7",
        ["chevron-down"] = "M6 9l6 6 6-6",
        ["chevron-up"]   = "M18 15l-6-6-6 6",
        // Communication
        ["send"]          = "M22 2L11 13M22 2l-7 20-4-9-9-4 20-7z",
        ["message-square"]= "M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z",
        ["bot"]           = "M12 8V4H8M12 4h4M12 4v4M3 12h3m12 0h3M12 16v2m-4-4h8M8 12v-1a4 4 0 0 1 8 0v1H8z",
        // Editing
        ["copy"]          = "M8 4h8a2 2 0 0 1 2 2v8M4 16V8a2 2 0 0 1 2-2h2M16 8h2a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2v-2",
        ["trash-2"]       = "M3 6h18M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M10 11v6M14 11v6",
        ["stop-circle"]   = "M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2 2 6.477 2 12s4.477 10 10 10zM9 9h6v6H9z",
        // General
        ["check"]         = "M20 6L9 17l-5-5",
        ["plus"]          = "M12 5v14M5 12h14",
        ["more-vertical"] = "M12 3a1 1 0 1 1 0 2 1 1 0 0 1 0-2zm0 7a1 1 0 1 1 0 2 1 1 0 0 1 0-2zm0 7a1 1 0 1 1 0 2 1 1 0 0 1 0-2z",
    };

    private static readonly Geometry _fallback = Geometry.Parse("M12 2a10 10 0 1 0 0 20A10 10 0 0 0 12 2z");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string name || !_paths.TryGetValue(name, out var data))
            return _fallback;
        try { return Geometry.Parse(data); }
        catch { return _fallback; }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
