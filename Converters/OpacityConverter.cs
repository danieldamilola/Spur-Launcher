using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Arc.Converters;

public class OpacityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is double opacity && values[1] is string hexColor)
        {
            // Parse hex color (#RRGGBB)
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            // Apply opacity
            var alpha = (byte)(opacity * 255);
            return new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
        }
        return new SolidColorBrush(Color.FromArgb(199, 20, 20, 22)); // Default GlassBg
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

