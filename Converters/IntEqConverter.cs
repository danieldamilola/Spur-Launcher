using System.Globalization;
using System.Windows.Data;

namespace Arc.Converters;

/// <summary>
/// Returns true when the bound int value equals the converter parameter.
/// Used by segmented radio buttons for numeric settings (depth, history size).
/// </summary>
[ValueConversion(typeof(int), typeof(bool))]
public sealed class IntEqConverter : IValueConverter
{
    public static readonly IntEqConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intVal && parameter is string paramStr && int.TryParse(paramStr, out int target))
            return intVal == target;
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is string paramStr && int.TryParse(paramStr, out int target))
            return target;
        return Binding.DoNothing;
    }
}

