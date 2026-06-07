using System;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace Spur.Converters;

public sealed class ExtraSettingsViewConverter : IValueConverter
{
    private static readonly ConditionalWeakTable<Spur.Extensions.IExtra, System.Windows.FrameworkElement> _cache = new();

    public object? Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is Spur.Extensions.IExtra extra)
        {
            if (!_cache.TryGetValue(extra, out var view))
            {
                view = extra.CreateSettingsView();
                if (view != null) _cache.Add(extra, view);
            }

            if (parameter is string param && param == "Visibility")
            {
                return view is null ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            }
            return view;
        }
        return parameter is string p && p == "Visibility" ? System.Windows.Visibility.Collapsed : null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
