using System.Globalization;
using System.Windows.Data;

namespace Arc.Converters;

/// <summary>Converts a string to bool for RadioButton comparison (value == parameter).</summary>
[ValueConversion(typeof(string), typeof(bool))]
public sealed class StringEqConverter : IValueConverter
{
    public static readonly StringEqConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && parameter is string param)
            return string.Equals(str, param, StringComparison.OrdinalIgnoreCase);
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is string param)
            return param;
        return Binding.DoNothing;
    }
}

