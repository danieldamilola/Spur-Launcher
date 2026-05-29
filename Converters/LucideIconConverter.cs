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
        ["calculator"] = "M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2zM9 9h6M9 12h6M9 15h4",
        ["timer"]      = "M12 6V12L15 15M3 12a9 9 0 1 0 18 0 9 9 0 1 0-18 0",
        ["palette"]    = "M12 2a10 10 0 0 1 10 10 4 4 0 0 1-4 4h-1.5a2 2 0 0 0-2 2 2 2 0 0 1-2 2A10 10 0 0 1 12 2z M7.5 12a.5.5 0 1 0 1 0 .5.5 0 0 0-1 0 M16.5 10a.5.5 0 1 0 1 0 .5.5 0 0 0-1 0 M14.5 7.5a.5.5 0 1 0 1 0 .5.5 0 0 0-1 0 M9 7a.5.5 0 1 0 1 0 .5.5 0 0 0-1 0",
        ["globe"]      = "M12 2a10 10 0 1 0 0 20A10 10 0 0 0 12 2zM2 12h20M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z",
        ["sparkles"]   = "M12 3 9.5 9.5 3 12l6.5 2.5L12 21l2.5-6.5L21 12l-6.5-2.5zM5 3v4M19 17v4M3 5h4M17 19h4",
        ["zap"]        = "M13 2L3 14h9l-1 8 10-12h-9l1-8z",
        ["clipboard"]  = "M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2M9 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M9 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2",
        ["folder"]     = "M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V7z",
        ["settings"]   = "M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z",
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

