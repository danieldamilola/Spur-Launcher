using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Arc.Converters;

public class HexToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hexColor && hexColor.StartsWith("#"))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexColor);

                // If parameter is "Light", lighten the color significantly
                if (parameter is string param && param == "Light")
                {
                    var r = (byte)Math.Min(255, color.R + 80);
                    var g = (byte)Math.Min(255, color.G + 80);
                    var b = (byte)Math.Min(255, color.B + 80);
                    color = Color.FromRgb(r, g, b);
                }

                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(Color.FromRgb(20, 20, 22)); // Default
            }
        }
        return new SolidColorBrush(Color.FromRgb(20, 20, 22)); // Default
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

