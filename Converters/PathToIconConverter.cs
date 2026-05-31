using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Arc.Services;

namespace Arc.Converters;

[ValueConversion(typeof(string), typeof(BitmapSource))]
public sealed class PathToIconConverter : IValueConverter
{
    /// <summary>Set by App.xaml.cs during DI initialization.</summary>
    public static IIconService? IconService { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrEmpty(path)) return null;
        return IconService?.GetIcon(path);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
