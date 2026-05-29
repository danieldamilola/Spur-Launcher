namespace Arc.Services;

/// <summary>Interface for theme switching.</summary>
public interface IThemeManager
{
    void Apply(string theme);
}

/// <summary>
/// Swaps the application's active theme resource dictionary at runtime.
/// Applies dark, light, or system-detected theme.
/// </summary>
public sealed class ThemeManagerImpl : IThemeManager
{
    private const string DarkUri  = "Themes/DarkTheme.xaml";
    private const string LightUri = "Themes/LightTheme.xaml";
    private readonly ILogger _log;

    public ThemeManagerImpl(ILogger log) => _log = log;

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

        if (existing is not null)
        {
            var idx = dicts.IndexOf(existing);
            dicts[idx] = newDict;
        }
        else
        {
            dicts.Add(newDict);
        }

        _log.Info($"Theme applied: {resolved}");
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

/// <summary>Static facade — delegates to the configured IThemeManager instance.</summary>
public static class ThemeManager
{
    private static IThemeManager? _instance;
    private static ILogger _log = NullLogger.Instance;

    public static void Initialize(IThemeManager instance, ILogger logger)
    {
        _instance = instance;
        _log = logger;
    }

    public static void Apply(string theme) => Instance.Apply(theme);

    private static IThemeManager Instance
    {
        get
        {
            if (_instance is not null) return _instance;
            var impl = new ThemeManagerImpl(_log);
            _instance = impl;
            _log.Info("ThemeManager auto-initialized with defaults.");
            return impl;
        }
    }
}
