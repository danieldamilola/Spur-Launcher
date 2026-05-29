using System.Globalization;
using System.Windows.Data;

namespace Arc.Converters;

/// <summary>
/// Shortens file-system paths for display by replacing the user profile root with ~
/// and converting backslashes to forward slashes. Non-path strings are returned as-is.
/// </summary>
[ValueConversion(typeof(string), typeof(string))]
public sealed class PathTrimConverter : IValueConverter
{
    public static readonly PathTrimConverter Instance = new();

    private static readonly string _home =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string raw || string.IsNullOrEmpty(raw)) return value;

        if (raw.StartsWith("ms-settings:", StringComparison.OrdinalIgnoreCase)) return "Windows Settings";

        // Only transform actual file-system paths
        if (!raw.Contains('\\') && !raw.Contains('/')) return raw;

        string s = raw.StartsWith(_home, StringComparison.OrdinalIgnoreCase)
            ? "~" + raw[_home.Length..]
            : raw;

        return s.Replace('\\', '/');
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

