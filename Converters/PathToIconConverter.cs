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

        // Resource URIs (e.g. "/Assets/Icons/calculator.png") → load as WPF pack resource
        if (path.StartsWith("/Assets/", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri($"pack://application:,,,{path}", UriKind.Absolute);
                bmp.DecodePixelWidth = 64;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; /* Intentional: resource image may be missing or corrupt */ }
        }

        // Filesystem paths → shell icon extraction
        return Ioc.Default.GetService<IIconService>()?.GetIcon(path);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
