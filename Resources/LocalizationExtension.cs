namespace Spur.Resources;

/// <summary>
/// Provides static access to localized strings for XAML data binding.
/// 
/// Usage in XAML:
///   1. Add namespace:  xmlns:res="clr-namespace:Spur.Resources"
///   2. Bind to string: Text="{x:Static res:Strings.SettingsTitle}"
///
/// The <see cref="Strings"/> class is auto-generated from Strings.resx
/// with Public access modifier, making all resource properties available
/// as static members (e.g. Strings.SearchPlaceholder, Strings.SettingsTitle).
///
/// For dynamic/runtime locale switching, use <see cref="LocalizationProvider"/>
/// which implements INotifyPropertyChanged and can be used as a binding source:
///   Text="{Binding SettingsTitle, Source={x:Static res:LocalizationProvider.Instance}}"
/// </summary>
public sealed class LocalizationProvider : INotifyPropertyChanged
{
    private static readonly Lazy<LocalizationProvider> _instance = new(() => new LocalizationProvider());

    /// <summary>Singleton instance for XAML binding.</summary>
    public static LocalizationProvider Instance => _instance.Value;

    private LocalizationProvider() { }

    /// <summary>
    /// Gets the <see cref="System.Resources.ResourceManager"/> for the Strings resource file.
    /// Useful for programmatic access to localized strings by key name.
    /// </summary>
    public static System.Resources.ResourceManager ResourceManager => Strings.ResourceManager;

    // ═══════════════════════════════════════════════════════════════
    // Expose commonly-used strings as bindable properties.
    // These delegate to the auto-generated Strings class so XAML can
    // use either {x:Static} or {Binding} with this provider.
    // ═══════════════════════════════════════════════════════════════

    public string SearchPlaceholder => Strings.SearchPlaceholder;
    public string ClipboardCategory => Strings.ClipboardCategory;
    public string FilesCategory => Strings.FilesCategory;
    public string ActionsCategory => Strings.ActionsCategory;
    public string SettingsTitle => Strings.SettingsTitle;
    public string AboutTitle => Strings.AboutTitle;
    public string GeneralSettings => Strings.GeneralSettings;
    public string SearchSettings => Strings.SearchSettings;
    public string AiSettings => Strings.AiSettings;
    public string AddOnsSettings => Strings.AddOnsSettings;
    public string CheckForUpdates => Strings.CheckForUpdates;
    public string NoResults => Strings.NoResults;
    public string ClearHistory => Strings.ClearHistory;
    public string Pin => Strings.Pin;
    public string Unpin => Strings.Unpin;
    public string Copy => Strings.Copy;
    public string Delete => Strings.Delete;
    public string Cancel => Strings.Cancel;
    public string Save => Strings.Save;
    public string Close => Strings.Close;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Call this method after changing the current UI culture to refresh
    /// all bound strings. This raises PropertyChanged for every property
    /// so that XAML bindings pick up the new locale values.
    /// </summary>
    public void Refresh()
    {
        var handler = PropertyChanged;
        if (handler is null) return;

        // Raise for all properties so every binding updates
        handler(this, new PropertyChangedEventArgs(null));
    }
}
