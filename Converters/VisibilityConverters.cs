using System.Globalization;
using System.Windows.Data;

namespace Arc.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance  = new();
    public static readonly BoolToVisibilityConverter Collapsed = new() { CollapseWhenFalse = true };

    public bool Invert           { get; set; }
    public bool CollapseWhenFalse { get; set; } = true;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is bool v && v;
        if (Invert) b = !b;
        return b ? Visibility.Visible
                 : (CollapseWhenFalse ? Visibility.Collapsed : Visibility.Hidden);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

[ValueConversion(typeof(object), typeof(Visibility))]
public sealed class NullToVisibilityConverter : IValueConverter
{
    public static readonly NullToVisibilityConverter Instance        = new();
    public static readonly NullToVisibilityConverter WhenNull        = new() { ShowWhenNull = true };

    public bool ShowWhenNull { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool hasValue = value is not null && value is not string s || (value is string str && !string.IsNullOrEmpty(str));
        bool show = ShowWhenNull ? !hasValue : hasValue;
        return show ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

