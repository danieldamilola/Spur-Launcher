namespace Spur.Services;

using iNKORE.UI.WPF.Modern;

/// <summary>Interface for theme switching.</summary>
public interface IThemeManager : IDisposable
{
    void Apply(string theme);
}

/// <summary>
/// Swaps the application's active theme resource dictionary at runtime.
/// Applies dark, light, or system-detected theme.
/// Listens for Windows theme changes and re-applies when config is "system".
/// </summary>
public sealed class ThemeManagerImpl : IThemeManager
{
    private const string DarkUri  = "Themes/DarkTheme.xaml";
    private const string LightUri = "Themes/LightTheme.xaml";
    private readonly ILogger _log;
    private readonly Spur.Models.SpurConfig _config;
    private bool _disposed;

    public ThemeManagerImpl(ILogger log, Spur.Models.SpurConfig config)
    {
        _log = log;
        _config = config;

        Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public void Apply(string theme)
    {
        var resolved = theme == "system" ? GetSystemTheme() : theme;
        var uri = resolved == "light" ? LightUri : DarkUri;

        var dicts = Application.Current.Resources.MergedDictionaries;
        var existing = dicts.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains("Theme.xaml") == true);

        var newDict = new ResourceDictionary
        {
            Source = new Uri(uri, UriKind.Relative)
        };

        var accentColor = ResolveAccentColor(newDict);
        var accentBrush = new System.Windows.Media.SolidColorBrush(accentColor);
        accentBrush.Freeze();
        newDict["Accent"] = accentBrush;

        if (existing is not null)
        {
            var idx = dicts.IndexOf(existing);
            dicts[idx] = newDict;
        }
        else
        {
            dicts.Add(newDict);
        }

        try
        {
            ThemeManager.Current.ApplicationTheme = resolved == "light" ? ApplicationTheme.Light : ApplicationTheme.Dark;
            ThemeManager.Current.AccentColor = accentColor;
        }
        catch (Exception ex) { _log.Warning("Failed to set iNKORE theme", ex); }

        _log.Info($"Theme applied: {resolved}");
    }

    /// <summary>
    /// Handles Windows theme/preference changes. When the config theme is "system",
    /// re-applies the theme so the app follows the new system setting.
    /// </summary>
    private void OnUserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
    {
        if (e.Category != Microsoft.Win32.UserPreferenceCategory.General)
            return;

        if (!string.Equals(_config.Theme, "system", StringComparison.OrdinalIgnoreCase))
            return;

        _log.Info("System theme change detected, re-applying theme");

        // The event may fire on a background thread; dispatch to the UI thread.
        Application.Current?.Dispatcher?.Invoke(() => Apply("system"));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }

    private System.Windows.Media.Color ResolveAccentColor(ResourceDictionary newDict)
    {
        if (_config.AccentColorMode == "system")
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                if (key?.GetValue("ColorizationColor") is int colorVal)
                {
                    byte r = (byte)((colorVal >> 16) & 0xFF);
                    byte g = (byte)((colorVal >> 8) & 0xFF);
                    byte b = (byte)(colorVal & 0xFF);
                    return System.Windows.Media.Color.FromRgb(r, g, b);
                }
            }
            catch { }
        }
        else if (_config.AccentColorMode == "custom")
        {
            try
            {
                return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_config.CustomAccentColor);
            }
            catch { }
        }

        if (newDict["Accent"] is System.Windows.Media.SolidColorBrush brush)
            return brush.Color;

        return System.Windows.Media.Color.FromRgb(215, 207, 194); // #D7CFC2 fallback
    }

    private static string GetSystemTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 1 ? "light" : "dark";
        }
        catch { return "dark"; }
    }
}
