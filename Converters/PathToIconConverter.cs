using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Spur.Services;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace Spur.Converters;

[ValueConversion(typeof(string), typeof(BitmapSource))]
public sealed class PathToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrEmpty(path)) return null;
        return Ioc.Default.GetService<IIconService>()?.GetIcon(path);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
