using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using Velopack;

namespace Spur.ViewModels;

/// <summary>
/// Sidebar-based settings view model with instant-save semantics.
/// Every property setter persists the config to disk immediately.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    // AI Provider constants
    private static class AiProviders
    {
        public const string Groq = "groq";
        public const string Gemini = "gemini";
        public const string OpenRouter = "openrouter";
        public const string DeepSeek = "deepseek";
    }

    // Window position constants
    private static class WindowPositions
    {
        public const string Center = "center";
        public const string CenterTop = "centerTop";
        public const string LeftTop = "leftTop";
        public const string RightTop = "rightTop";
        public const string Custom = "custom";
    }

    // Last query style constants
    private static class QueryStyles
    {
        public const string Select = "select";
        public const string Keep = "keep";
        public const string Clear = "clear";
    }
    private readonly IConfigService   _configService;
    private readonly MainViewModel    _main;
    private readonly IThemeManager    _themeManager;
    private readonly IStartupService  _startupService;
    private readonly IFrequencyService _frequencyService;
    private readonly ISecureStorageService _secureStorage;
    private          SpurConfig       _config;

    public Spur.Extensions.AddOnRegistry Registry { get; }
    public System.Collections.Generic.IEnumerable<Spur.Extensions.IAddOn> AllAddOns => Registry.All;
    public Spur.Services.AddOnStoreService StoreService { get; }

    public SettingsViewModel(SpurConfig config, IConfigService configService, MainViewModel main,
                             IThemeManager themeManager, IStartupService startupService, IFrequencyService frequencyService,
                             ISecureStorageService secureStorage, Spur.Extensions.AddOnRegistry registry,
                             Spur.Services.AddOnStoreService storeService)
    {
        _config          = config;
        Registry         = registry;
        StoreService     = storeService;
        _configService   = configService;
        _main            = main;
        _themeManager    = themeManager;
        _startupService  = startupService;
        _frequencyService = frequencyService;
        _secureStorage   = secureStorage;

        // Init sidebar — features.md §6
        Sections = new ObservableCollection<SettingsSection>
        {
            new("General",  "\ue713", "/Assets/Icons/settings.png"),
            new("Search",   "\ue11A", "/Assets/Icons/search.png"),
            new("AI",       "\ue2b1", "/Assets/Icons/find.png"),
            new("Add-ons",  "\ue113", "/Assets/Icons/store.png"),
            new("Store",    "\ue719", "/Assets/Icons/open.png"),
            new("About",    "\ue946", "/Assets/Icons/info.png"),
        };
        SelectedSection = Sections[0];

        LoadStartupState();
        ApplyViewModeResources(_config.ViewMode);

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SearchText))
            {
                _filteredSections = null;
                OnPropertyChanged(nameof(FilteredSections));
            }
        };

        AvailableCategories.Add(new PinnedCategoryOption { Id = "", Name = "None" });
        AvailableCategories.Add(new PinnedCategoryOption { Id = "files", Name = "Files" });
        AvailableCategories.Add(new PinnedCategoryOption { Id = "clipboard", Name = "Clipboard" });
        foreach (var extra in Registry.All)
        {
            AvailableCategories.Add(new PinnedCategoryOption { Id = extra.Id, Name = extra.Name });
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Tabs + Search
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    private ObservableCollection<SettingsSection> _sections;

    [ObservableProperty]
    private SettingsSection _selectedSection;

    [ObservableProperty]
    private string _searchText = string.Empty;

    private ObservableCollection<SettingsSection>? _filteredSections;
    public ObservableCollection<SettingsSection> FilteredSections
    {
        get
        {
            if (_filteredSections == null)
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                    _filteredSections = Sections;
                else
                {
                    var term = SearchText.Trim();
                    _filteredSections = new ObservableCollection<SettingsSection>(
                        Sections.Where(s => s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
                }
            }
            return _filteredSections;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Appearance
    // ═══════════════════════════════════════════════════════════════

    public bool ThemeDark
    {
        get => _config.Theme == "dark";
        set { if (value) SetTheme("dark"); }
    }
    public bool ThemeLight
    {
        get => _config.Theme == "light";
        set { if (value) SetTheme("light"); }
    }
    public bool ThemeSystem
    {
        get => _config.Theme == "system";
        set { if (value) SetTheme("system"); }
    }

    private void SetTheme(string theme)
    {
        _config.Theme = theme;
        _themeManager.Apply(theme);
        Save();
        OnPropertyChanged(nameof(ThemeDark));
        OnPropertyChanged(nameof(ThemeLight));
        OnPropertyChanged(nameof(ThemeSystem));
    }

    public bool AccentModeTheme
    {
        get => _config.AccentColorMode == "theme";
        set { if (value) SetAccentMode("theme"); }
    }
    public bool AccentModeSystem
    {
        get => _config.AccentColorMode == "system";
        set { if (value) SetAccentMode("system"); }
    }
    public bool AccentModeCustom
    {
        get => _config.AccentColorMode == "custom";
        set { if (value) SetAccentMode("custom"); }
    }

    private void SetAccentMode(string mode)
    {
        if (_config.AccentColorMode == mode) return;
        _config.AccentColorMode = mode;
        _themeManager.Apply(_config.Theme); // Reapply theme to trigger accent color update
        Save();
        OnPropertyChanged(nameof(AccentModeTheme));
        OnPropertyChanged(nameof(AccentModeSystem));
        OnPropertyChanged(nameof(AccentModeCustom));
    }

    public string CustomAccentColor
    {
        get => _config.CustomAccentColor;
        set
        {
            if (_config.CustomAccentColor == value) return;
            _config.CustomAccentColor = value;
            if (_config.AccentColorMode == "custom")
            {
                _themeManager.Apply(_config.Theme); // Live update
            }
            Save();
            OnPropertyChanged();
        }
    }


    public double WindowOpacity
    {
        get => _config.WindowOpacity;
        set { _config.WindowOpacity = Math.Clamp(value, 0.5, 1.0); SaveAndApply(); OnPropertyChanged(); }
    }

    public double BarWidth
    {
        get => _config.BarWidth;
        set { _config.BarWidth = Math.Clamp(value, 400, 1000); SaveAndApply(); OnPropertyChanged(); }
    }

    // ── View mode ────────────────────────────────────────────────

    public bool ViewModeCompact
    {
        get => _config.ViewMode == "compact";
        set { if (value) SetViewMode("compact"); }
    }
    public bool ViewModeComfortable
    {
        get => _config.ViewMode == "comfortable";
        set { if (value) SetViewMode("comfortable"); }
    }
    public bool ViewModeSpacious
    {
        get => _config.ViewMode == "spacious";
        set { if (value) SetViewMode("spacious"); }
    }

    private void SetViewMode(string mode)
    {
        _config.ViewMode = mode;
        SaveAndApply();
        OnPropertyChanged(nameof(ViewModeCompact));
        OnPropertyChanged(nameof(ViewModeComfortable));
        OnPropertyChanged(nameof(ViewModeSpacious));
        ApplyViewModeResources(mode);
    }

    /// <summary>Updates the RowHeight WPF resource to match the selected view mode.</summary>
    internal static void ApplyViewModeResources(string mode)
    {
        double rowHeight = mode switch
        {
            "compact"  => 36.0,
            "spacious" => 56.0,
            _          => 44.0, // comfortable (default)
        };
        Application.Current.Resources["RowHeight"] = rowHeight;
    }

    // ── Window Mode (compact / expanded) ─────────────────────────
    public bool WindowModeCompact
    {
        get => _config.WindowMode == "compact";
        set { if (value) SetWindowMode("compact"); }
    }
    public bool WindowModeExpanded
    {
        get => _config.WindowMode == "expanded";
        set { if (value) SetWindowMode("expanded"); }
    }

    private void SetWindowMode(string mode)
    {
        _config.WindowMode = mode;
        SaveAndApply();
        OnPropertyChanged(nameof(WindowModeCompact));
        OnPropertyChanged(nameof(WindowModeExpanded));
    }

    // ═══════════════════════════════════════════════════════════════
    // Search & Indexing
    // ═══════════════════════════════════════════════════════════════

    public bool IndexApps
    {
        get => _config.IndexApps;
        set { _config.IndexApps = value; Save(); OnPropertyChanged(); }
    }

    public bool IndexFiles
    {
        get => _config.IndexFiles;
        set { _config.IndexFiles = value; Save(); OnPropertyChanged(); }
    }

    public bool IndexFolders
    {
        get => _config.IndexFolders;
        set { _config.IndexFolders = value; Save(); OnPropertyChanged(); }
    }

    public bool IndexClipboard
    {
        get => _config.IndexClipboard;
        set { _config.IndexClipboard = value; Save(); OnPropertyChanged(); }
    }

    public bool IndexCalculator
    {
        get => _config.IndexCalculator;
        set { _config.IndexCalculator = value; Save(); OnPropertyChanged(); }
    }

    public bool FuzzySearch
    {
        get => _config.FuzzySearch;
        set { _config.FuzzySearch = value; Save(); OnPropertyChanged(); }
    }

    public bool FilePreviewEnabled
    {
        get => _config.FilePreviewEnabled;
        set { _config.FilePreviewEnabled = value; Save(); OnPropertyChanged(); }
    }

    public string[] QuerySearchPrecisionOptions { get; } = ["Low", "Regular", "Strict"];

    public string QuerySearchPrecision
    {
        get => ToTitle(_config.QuerySearchPrecision);
        set { _config.QuerySearchPrecision = ToKey(value); SaveAndApply(); OnPropertyChanged(); }
    }

    public string[] LastQueryStyleOptions { get; } = ["Clear", "Select last Query", "Keep last Query"];

    public string LastQueryStyle
    {
        get => MapQueryStyleToDisplay(_config.LastQueryStyle);
        set
        {
            _config.LastQueryStyle = MapDisplayToQueryStyle(value);
            SaveAndApply();
            OnPropertyChanged();
        }
    }

    private static string MapQueryStyleToDisplay(string style) => style switch
    {
        QueryStyles.Select => "Select last Query",
        QueryStyles.Keep => "Keep last Query",
        _ => "Clear",
    };

    private static string MapDisplayToQueryStyle(string display) => display switch
    {
        "Select last Query" => QueryStyles.Select,
        "Keep last Query" => QueryStyles.Keep,
        _ => QueryStyles.Clear,
    };

    public int SearchDelay
    {
        get => _config.SearchDelay;
        set { _config.SearchDelay = Math.Clamp(value, 0, 500); SaveAndApply(); OnPropertyChanged(); }
    }

    public string[] WebSearchEngineOptions { get; } = ["Google", "DuckDuckGo", "Bing"];

    public string WebSearchEngine
    {
        get => ToTitle(_config.WebSearchEngine);
        set { _config.WebSearchEngine = ToKey(value); SaveAndApply(); OnPropertyChanged(); }
    }

    public bool IndexShell
    {
        get => _config.IndexShell;
        set { _config.IndexShell = value; SaveAndApply(); OnPropertyChanged(); }
    }

    public bool IndexSystemCommands
    {
        get => _config.IndexSystemCommands;
        set { _config.IndexSystemCommands = value; SaveAndApply(); OnPropertyChanged(); }
    }

    public bool IndexUrls
    {
        get => _config.IndexUrls;
        set { _config.IndexUrls = value; SaveAndApply(); OnPropertyChanged(); }
    }

    public bool IndexWebSearches
    {
        get => _config.IndexWebSearches;
        set { _config.IndexWebSearches = value; SaveAndApply(); OnPropertyChanged(); }
    }

    public bool IndexWindowsSettings
    {
        get => _config.IndexWindowsSettings;
        set { _config.IndexWindowsSettings = value; SaveAndApply(); OnPropertyChanged(); }
    }

    public int MaxFileDepth
    {
        get => _config.MaxFileDepth;
        set { _config.MaxFileDepth = Math.Clamp(value, 1, 5); SaveAndApply(); OnPropertyChanged(); }
    }

    public bool FileSearchEnabled
    {
        get => _config.FileSearchEnabled;
        set
        {
            if (_config.FileSearchEnabled == value) return;
            _config.FileSearchEnabled = value;
            SaveAndApply();
            OnPropertyChanged();
        }
    }

    public bool ClipboardEnabled
    {
        get => _config.ClipboardEnabled;
        set
        {
            if (_config.ClipboardEnabled == value) return;

            // Privacy warning when enabling clipboard monitoring
            if (value)
            {
                var result = System.Windows.MessageBox.Show(
                    "Spur will monitor and store everything you copy, including passwords, " +
                    "2FA codes, and other sensitive data.\n\n" +
                    "Clipboard history is stored locally and never sent anywhere.\n\n" +
                    "Enable clipboard history?",
                    "Clipboard Privacy",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result != System.Windows.MessageBoxResult.Yes)
                {
                    OnPropertyChanged();
                    return;
                }
            }

            _config.ClipboardEnabled = value;
            SaveAndApply();
            OnPropertyChanged();
        }
    }

    // ── Results count ──────────────────────────────────────────
    public bool Results5  { get => _config.ResultsCount == 5;  set { if (value) SetCount(5);  } }
    public bool Results8  { get => _config.ResultsCount == 8;  set { if (value) SetCount(8);  } }
    public bool Results10 { get => _config.ResultsCount == 10; set { if (value) SetCount(10); } }

    private void SetCount(int n)
    {
        _config.ResultsCount = n;
        SaveAndApply();
        OnPropertyChanged(nameof(Results5));
        OnPropertyChanged(nameof(Results8));
        OnPropertyChanged(nameof(Results10));
    }

    // ── Indexed folders (observable list) ────────────────────────

    private ObservableCollection<string>? _indexedFoldersList;
    public ObservableCollection<string> IndexedFoldersList
    {
        get
        {
            if (_indexedFoldersList is null)
            {
                _indexedFoldersList = new ObservableCollection<string>(_config.IndexedFolders);
                _indexedFoldersList.CollectionChanged += (_, _) =>
                { _config.IndexedFolders = [.._indexedFoldersList]; Save(); };
            }
            return _indexedFoldersList;
        }
    }

    [ObservableProperty] private string _newFolderPath = string.Empty;

    [RelayCommand]
    private void AddFolder()
    {
        var path = NewFolderPath.Trim();
        if (!string.IsNullOrEmpty(path) &&
            !IndexedFoldersList.Contains(path, StringComparer.OrdinalIgnoreCase))
            IndexedFoldersList.Add(path);
        NewFolderPath = string.Empty;
    }

    [RelayCommand]
    private void RemoveFolder(string path) => IndexedFoldersList.Remove(path);

    // ── File types (observable list) ──────────────────────────────

    private ObservableCollection<string>? _fileTypesList;
    public ObservableCollection<string> FileTypesList
    {
        get
        {
            if (_fileTypesList is null)
            {
                _fileTypesList = new ObservableCollection<string>(_config.FileExtensions);
                _fileTypesList.CollectionChanged += (_, _) =>
                { _config.FileExtensions = [.._fileTypesList]; Save(); };
            }
            return _fileTypesList;
        }
    }

    [ObservableProperty] private string _newFileType = string.Empty;

    [RelayCommand]
    private void AddFileType()
    {
        var ext = NewFileType.Trim();
        if (!ext.StartsWith('.')) ext = "." + ext;
        if (ext.Length > 1 &&
            !FileTypesList.Contains(ext, StringComparer.OrdinalIgnoreCase))
            FileTypesList.Add(ext);
        NewFileType = string.Empty;
    }

    [RelayCommand]
    private void RemoveFileType(string ext) => FileTypesList.Remove(ext);

    // ═══════════════════════════════════════════════════════════════
    // Hotkey
    // ═══════════════════════════════════════════════════════════════

    public string Shortcut
    {
        get => _config.Shortcut;
        set
        {
            if (_config.Shortcut == value || string.IsNullOrWhiteSpace(value)) return;
            _config.Shortcut = value;
            SaveAndApply();
            OnPropertyChanged();
        }
    }

    public bool HotkeyEnabled
    {
        get => _config.HotkeyEnabled;
        set { _config.HotkeyEnabled = value; Save(); OnPropertyChanged(); }
    }

    public string ClipboardShortcut
    {
        get => _config.ClipboardShortcut;
        set
        {
            if (_config.ClipboardShortcut == value || string.IsNullOrWhiteSpace(value)) return;
            _config.ClipboardShortcut = value;
            SaveAndApply();
            OnPropertyChanged();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Startup & Performance
    // ═══════════════════════════════════════════════════════════════

    private bool _launchOnStartup;

    public bool LaunchOnStartup
    {
        get => _launchOnStartup;
        set
        {
            _launchOnStartup = value;
            _config.LaunchOnStartup = value;
            if (value) _startupService.Enable(); else _startupService.Disable();
            Save();
            OnPropertyChanged();
        }
    }

    public bool MinimizeToTray
    {
        get => _config.MinimizeToTray;
        set { _config.MinimizeToTray = value; Save(); OnPropertyChanged(); }
    }

    public bool ShowTrayIcon
    {
        get => _config.ShowTrayIcon;
        set { _config.ShowTrayIcon = value; SaveAndApply(); OnPropertyChanged(); }
    }

    public bool ReIndexOnStartup
    {
        get => _config.ReIndexOnStartup;
        set { _config.ReIndexOnStartup = value; Save(); OnPropertyChanged(); }
    }

    public int ReIndexIntervalHours
    {
        get => _config.ReIndexIntervalHours;
        set { _config.ReIndexIntervalHours = Math.Max(0, value); Save(); OnPropertyChanged(); }
    }

    public bool ReIndex3h     { get => _config.ReIndexIntervalHours == 3;  set { if (value) SetReIndex(3);  } }
    public bool ReIndex6h     { get => _config.ReIndexIntervalHours == 6;  set { if (value) SetReIndex(6);  } }
    public bool ReIndex12h    { get => _config.ReIndexIntervalHours == 12; set { if (value) SetReIndex(12); } }
    public bool ReIndexManual { get => _config.ReIndexIntervalHours == 0;  set { if (value) SetReIndex(0);  } }

    private void SetReIndex(int h)
    {
        _config.ReIndexIntervalHours = h;
        Save();
        OnPropertyChanged(nameof(ReIndex3h));
        OnPropertyChanged(nameof(ReIndex6h));
        OnPropertyChanged(nameof(ReIndex12h));
        OnPropertyChanged(nameof(ReIndexManual));
    }

    public bool BackgroundIndexing
    {
        get => _config.BackgroundIndexing;
        set { _config.BackgroundIndexing = value; Save(); OnPropertyChanged(); }
    }

    public int ClipboardHistorySize
    {
        get => _config.ClipboardHistorySize;
        set { _config.ClipboardHistorySize = Math.Clamp(value, 10, 200); SaveAndApply(); OnPropertyChanged(); }
    }

    // ═══════════════════════════════════════════════════════════════
    // Privacy
    // ═══════════════════════════════════════════════════════════════

    public bool ExcludeSensitiveFolders
    {
        get => _config.ExcludeSensitiveFolders;
        set { _config.ExcludeSensitiveFolders = value; Save(); OnPropertyChanged(); }
    }

    public bool ClearClipboardOnExit
    {
        get => _config.ClearClipboardOnExit;
        set { _config.ClearClipboardOnExit = value; Save(); OnPropertyChanged(); }
    }

    public bool LogSearchHistory
    {
        get => _config.LogSearchHistory;
        set { _config.LogSearchHistory = value; Save(); OnPropertyChanged(); }
    }

    // ═══════════════════════════════════════════════════════════════
    // Result behavior
    // ═══════════════════════════════════════════════════════════════

    public bool OpenWithEnter
    {
        get => _config.OpenWithEnter;
        set { _config.OpenWithEnter = value; Save(); OnPropertyChanged(); }
    }

    public bool CloseAfterLaunch
    {
        get => _config.CloseAfterLaunch;
        set { _config.CloseAfterLaunch = value; Save(); OnPropertyChanged(); }
    }

    public bool ShowRecentFirst
    {
        get => _config.ShowRecentFirst;
        set { _config.ShowRecentFirst = value; Save(); OnPropertyChanged(); }
    }

    public bool AnimationEnabled
    {
        get => _config.AnimationEnabled;
        set { _config.AnimationEnabled = value; SaveAndApply(); OnPropertyChanged(); }
    }

    public bool SoundEffectEnabled
    {
        get => _config.SoundEffectEnabled;
        set { _config.SoundEffectEnabled = value; SaveAndApply(); OnPropertyChanged(); }
    }

    public string[] SearchWindowPositions { get; } = ["Center", "Center Top", "Left Top", "Right Top", "Custom Position"];

    public string SearchWindowPosition
    {
        get => MapWindowPositionToDisplay(_config.SearchWindowPosition);
        set
        {
            _config.SearchWindowPosition = MapDisplayToWindowPosition(value);
            SaveAndApply();
            NotifyPositionPropertiesChanged();
        }
    }

    private static string MapWindowPositionToDisplay(string position) => position switch
    {
        WindowPositions.CenterTop => "Center Top",
        WindowPositions.LeftTop => "Left Top",
        WindowPositions.RightTop => "Right Top",
        WindowPositions.Custom => "Custom Position",
        _ => "Center",
    };

    private static string MapDisplayToWindowPosition(string display) => display switch
    {
        "Center Top" => WindowPositions.CenterTop,
        "Left Top" => WindowPositions.LeftTop,
        "Right Top" => WindowPositions.RightTop,
        "Custom Position" => WindowPositions.Custom,
        _ => WindowPositions.Center,
    };

    private void NotifyPositionPropertiesChanged()
    {
        OnPropertyChanged(nameof(SearchWindowPosition));
        OnPropertyChanged(nameof(PositionCenter));
        OnPropertyChanged(nameof(PositionTop));
        OnPropertyChanged(nameof(PositionLeft));
        OnPropertyChanged(nameof(PositionRight));
        OnPropertyChanged(nameof(PositionCustom));
    }

    public bool PositionCenter
    {
        get => SearchWindowPosition == "Center";
        set { if (value) SearchWindowPosition = "Center"; }
    }
    public bool PositionTop
    {
        get => SearchWindowPosition == "Center Top";
        set { if (value) SearchWindowPosition = "Center Top"; }
    }
    public bool PositionLeft
    {
        get => SearchWindowPosition == "Left Top";
        set { if (value) SearchWindowPosition = "Left Top"; }
    }
    public bool PositionRight
    {
        get => SearchWindowPosition == "Right Top";
        set { if (value) SearchWindowPosition = "Right Top"; }
    }
    public bool PositionCustom
    {
        get => SearchWindowPosition == "Custom Position";
        set { if (value) SearchWindowPosition = "Custom Position"; }
    }

    // ── Monitor selection ──────────────────────────────────────────
    public List<string> MonitorOptions
    {
        get
        {
            var options = new List<string> { "Primary" };
            var monitors = Helpers.MonitorHelper.GetAllMonitors();
            for (int i = 0; i < monitors.Count; i++)
            {
                if (monitors[i].IsPrimary) continue;
                options.Add($"Monitor {i + 1}");
            }
            options.Add("Follow cursor");
            return options;
        }
    }

    public string PreferredMonitor
    {
        get
        {
            return _config.PreferredMonitor?.ToLowerInvariant() switch
            {
                null or "" or "primary" or "0" => "Primary",
                "mouse" or "-1" => "Follow cursor",
                var s when int.TryParse(s, out int idx) => $"Monitor {idx}",
                _ => "Primary",
            };
        }
        set
        {
            _config.PreferredMonitor = value switch
            {
                "Primary" => "primary",
                "Follow cursor" => "mouse",
                var s when s.StartsWith("Monitor ") && int.TryParse(s["Monitor ".Length..], out int idx) => idx.ToString(),
                _ => "primary",
            };
            Save();
            OnPropertyChanged();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Actions — enable/disable (managed through AddOnRegistry now)
    // ═══════════════════════════════════════════════════════════════

    public bool ActionCalc
    {
        get => _config.IndexCalculator;
        set { _config.IndexCalculator = value; SaveAndApply(); OnPropertyChanged(); }
    }
    public bool ActionSystem
    {
        get => _config.IndexSystemCommands;
        set { _config.IndexSystemCommands = value; SaveAndApply(); OnPropertyChanged(); }
    }

    // ═══════════════════════════════════════════════════════════════
    // Per-action settings (canonical)
    // Each action's settings object is exposed directly for binding.
    // The toggle + keyword sync down into the config on change.
    // ═══════════════════════════════════════════════════════════════

    // ── System ────────────────────────────────────────────────────
    // Removed SystemEnabled

    // ═══════════════════════════════════════════════════════════════
    // AI Assistant
    // ═══════════════════════════════════════════════════════════════

    public bool AiEnabled
    {
        get => _config.AiEnabled;
        set { _config.AiEnabled = value; SaveAndApply(); OnPropertyChanged(); }
    }

    public string[] Providers { get; } = ["groq", "gemini", "openrouter", "deepseek"];

    public string AiProvider
    {
        get => _config.AiProvider;
        set
        {
            var normalized = value.ToLowerInvariant();
            if (string.Equals(_config.AiProvider, normalized, StringComparison.OrdinalIgnoreCase)) return;
            _config.AiProvider = normalized;
            SaveAndApply();
            NotifyAiProviderPropertiesChanged();
        }
    }

    public string ApiKey
    {
        get => GetApiKeyForCurrentProvider();
        set => SetApiKeyForCurrentProvider(value);
    }

    private string GetApiKeyForCurrentProvider() => _config.AiProvider switch
    {
        AiProviders.Gemini => _secureStorage.Decrypt(_config.EncryptedGeminiApiKey),
        AiProviders.OpenRouter => _secureStorage.Decrypt(_config.EncryptedOpenRouterApiKey),
        AiProviders.DeepSeek => _secureStorage.Decrypt(_config.EncryptedDeepSeekApiKey),
        _ => _secureStorage.Decrypt(_config.EncryptedGroqApiKey),
    };

    private void SetApiKeyForCurrentProvider(string value)
    {
        var encrypted = _secureStorage.Encrypt(value);
        switch (_config.AiProvider)
        {
            case AiProviders.Gemini: _config.EncryptedGeminiApiKey = encrypted; break;
            case AiProviders.OpenRouter: _config.EncryptedOpenRouterApiKey = encrypted; break;
            case AiProviders.DeepSeek: _config.EncryptedDeepSeekApiKey = encrypted; break;
            default: _config.EncryptedGroqApiKey = encrypted; break;
        }
        SaveAndApply();
        OnPropertyChanged(nameof(ApiKey));
    }

    public string[] CurrentModels => GetModelsForCurrentProvider();

    private string[] GetModelsForCurrentProvider() => _config.AiProvider switch
    {
        AiProviders.Groq => ["llama-3.1-8b-instant", "llama-3.3-70b-versatile", "qwen/qwen3-32b"],
        AiProviders.Gemini => ["gemini-2.0-flash", "gemini-2.5-pro-exp-03-25", "gemini-1.5-flash"],
        AiProviders.OpenRouter => ["google/gemini-2.0-flash-001", "meta-llama/llama-3.1-8b-instruct", "deepseek/deepseek-chat"],
        AiProviders.DeepSeek => ["deepseek-chat", "deepseek-reasoner"],
        _ => [],
    };

    public string AiModel
    {
        get => GetModelForCurrentProvider();
        set => SetModelForCurrentProvider(value);
    }

    private string GetModelForCurrentProvider() => _config.AiProvider switch
    {
        AiProviders.Gemini => _config.GeminiModel,
        AiProviders.OpenRouter => _config.OpenRouterModel,
        AiProviders.DeepSeek => _config.DeepSeekModel,
        _ => _config.GroqModel,
    };

    private void SetModelForCurrentProvider(string value)
    {
        switch (_config.AiProvider)
        {
            case AiProviders.Gemini: _config.GeminiModel = value; break;
            case AiProviders.OpenRouter: _config.OpenRouterModel = value; break;
            case AiProviders.DeepSeek: _config.DeepSeekModel = value; break;
            default: _config.GroqModel = value; break;
        }
        SaveAndApply();
        OnPropertyChanged(nameof(AiModel));
    }

    private void NotifyAiProviderPropertiesChanged()
    {
        OnPropertyChanged(nameof(AiProvider));
        OnPropertyChanged(nameof(ApiKey));
        OnPropertyChanged(nameof(AiModel));
        OnPropertyChanged(nameof(CurrentModels));
    }

    // ═══════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════

    public void CloseSettings() => _main.IsSettingsOpen = false;

    [RelayCommand]
    private async Task RefreshApps()
    {
        await _main.RefreshAppCatalogAsync();
    }

    [RelayCommand]
    private void ClearAllHistory()
    {
        _frequencyService.ClearAll();
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        var preserveOnboarding = _config.OnboardingComplete;
        _config = new SpurConfig();
        _config.OnboardingComplete = preserveOnboarding; // Never re-show onboarding
        SaveAndApply();
        LoadStartupState();

        _indexedFoldersList = null;
        _fileTypesList = null;

        // Notify all properties changed
        OnPropertyChanged(string.Empty);
    }

    [RelayCommand]
    private void ExportSettings()
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Spur Settings",
                Filter = "JSON files (*.json)|*.json",
                FileName = "spur-settings.json",
                DefaultExt = ".json"
            };

            if (dlg.ShowDialog() != true) return;

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_config, options);
            System.IO.File.WriteAllText(dlg.FileName, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export settings:\n{ex.Message}", "Export Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ImportSettings()
    {
        try
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Spur Settings",
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json"
            };

            if (dlg.ShowDialog() != true) return;

            var json = System.IO.File.ReadAllText(dlg.FileName);
            var imported = JsonSerializer.Deserialize<SpurConfig>(json);

            if (imported is null)
            {
                MessageBox.Show("The selected file does not contain valid settings.",
                    "Import Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Importing settings will replace all current settings.\n\nContinue?",
                "Import Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            // Preserve machine-specific values
            var preserveOnboarding = _config.OnboardingComplete;
            imported.OnboardingComplete = preserveOnboarding;

            // Validate and clamp values
            ConfigValidator.Validate(imported);

            _config = imported;
            SaveAndApply();
            LoadStartupState();

            _indexedFoldersList = null;
            _fileTypesList = null;

            // Notify all properties changed
            OnPropertyChanged(string.Empty);

            MessageBox.Show("Settings imported successfully.", "Import Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (JsonException)
        {
            MessageBox.Show("The selected file is not valid JSON.",
                "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to import settings:\n{ex.Message}", "Import Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // About — Update flow
    // Single state machine drives the entire row: status, text, and button.
    // ═══════════════════════════════════════════════════════════════

    public string Version =>
        $"Spur v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.2.0"}";

    public string Credits => "Built with .NET 9 + WPF · Monochrome v2 design system";

    /// <summary>Drives the update section: text, button, and enabled state.</summary>
    public enum UpdateState
    {
        Idle,
        Checking,
        NoUpdate,
        UpdateAvailable,
        Downloading,
        ReadyToRestart,
        Error,
    }

    [ObservableProperty]
    private UpdateState _updateStatus = UpdateState.Idle;

    [ObservableProperty]
    private int _downloadProgress;

    [ObservableProperty]
    private string _availableVersion = string.Empty;

    private UpdateManager? _updateManager;
    private UpdateInfo? _pendingUpdate;

    /// <summary>Human-readable label for the current state. The UI binds to this directly.</summary>
    public string UpdateStatusText => UpdateStatus switch
    {
        UpdateState.Idle             => "Up to date",
        UpdateState.Checking         => "Checking for updates…",
        UpdateState.NoUpdate         => "You're on the latest version",
        UpdateState.UpdateAvailable  => $"v{AvailableVersion} available",
        UpdateState.Downloading      => $"Downloading… {DownloadProgress}%",
        UpdateState.ReadyToRestart   => "Update ready — restart to install",
        UpdateState.Error            => "Update failed. Try again.",
        _                            => string.Empty,
    };

    /// <summary>True while Spur is working on an update — disables the primary button.</summary>
    public bool IsUpdateBusy =>
        UpdateStatus is UpdateState.Checking or UpdateState.Downloading;

    /// <summary>Whether the user can trigger a check (i.e. nothing is already in flight).</summary>
    public bool CanCheckForUpdates => !IsUpdateBusy;

    /// <summary>True when an update is available and ready to download.</summary>
    public bool CanDownload => UpdateStatus == UpdateState.UpdateAvailable;

    /// <summary>True when the update has been downloaded and is ready to install.</summary>
    public bool CanInstall => UpdateStatus == UpdateState.ReadyToRestart;

    partial void OnUpdateStatusChanged(UpdateState value)
    {
        OnPropertyChanged(nameof(UpdateStatusText));
        OnPropertyChanged(nameof(IsUpdateBusy));
        OnPropertyChanged(nameof(CanCheckForUpdates));
        OnPropertyChanged(nameof(CanDownload));
        OnPropertyChanged(nameof(CanInstall));
    }

    partial void OnDownloadProgressChanged(int value)
    {
        OnPropertyChanged(nameof(UpdateStatusText));
    }

    private UpdateManager GetOrCreateUpdateManager()
    {
        if (_updateManager is null)
        {
            var cfg = _config;
            var updateUrl = cfg.UpdateUrl ?? "https://github.com/danieldamilola/Spur-Launcher/releases/latest/download";
            _updateManager = new UpdateManager(updateUrl);
        }
        return _updateManager;
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        if (IsUpdateBusy) return;

        try
        {
            UpdateStatus = UpdateState.Checking;
            var mgr = GetOrCreateUpdateManager();
            var update = await mgr.CheckForUpdatesAsync();

            if (update is null)
            {
                UpdateStatus = UpdateState.NoUpdate;
                return;
            }

            _pendingUpdate = update;
            AvailableVersion = update.TargetFullRelease.Version.ToString();
            UpdateStatus = UpdateState.UpdateAvailable;
        }
        catch (Exception)
        {
            UpdateStatus = UpdateState.Error;
        }
    }

    [RelayCommand]
    private async Task DownloadUpdate()
    {
        if (_pendingUpdate is null || UpdateStatus != UpdateState.UpdateAvailable) return;

        try
        {
            UpdateStatus = UpdateState.Downloading;
            DownloadProgress = 0;

            var mgr = GetOrCreateUpdateManager();
            await mgr.DownloadUpdatesAsync(_pendingUpdate, progress =>
            {
                DownloadProgress = progress;
            });

            DownloadProgress = 100;
            UpdateStatus = UpdateState.ReadyToRestart;
        }
        catch (Exception)
        {
            UpdateStatus = UpdateState.Error;
        }
    }

    [RelayCommand]
    private void RestartToUpdate()
    {
        if (_pendingUpdate is null) return;

        try
        {
            var mgr = GetOrCreateUpdateManager();
            mgr.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        catch (Exception)
        {
            // Fallback: just shut down so the update applies on next launch
            Application.Current.Shutdown();
        }
    }

    [RelayCommand]
    private void CancelUpdate()
    {
        _pendingUpdate = null;
        DownloadProgress = 0;
        UpdateStatus = UpdateState.Idle;
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    private void LoadStartupState()
    {
        _launchOnStartup = _startupService.IsEnabled();
        OnPropertyChanged(nameof(LaunchOnStartup));
    }

    public void Save()
    {
        Registry.SaveSettings(_config);
        _configService.Save(_config);
    }

    /// <summary>
    /// Saves config to disk and pushes a snapshot to MainViewModel.
    /// Consolidates the repeated Save() + _main.Config = _config.Clone() pattern.
    /// </summary>
    private void SaveAndApply()
    {
        Save();
        _main.Config = _config.Clone();
    }

    private static string ToTitle(string value)
        => string.IsNullOrWhiteSpace(value) ? "Regular" : char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();

    private static string ToKey(string value)
        => string.IsNullOrWhiteSpace(value) ? "regular" : value.Replace(" ", string.Empty).ToLowerInvariant();

    // ═══════════════════════════════════════════════════════════════
    // Categories
    // ═══════════════════════════════════════════════════════════════

    public ObservableCollection<PinnedCategoryOption> AvailableCategories { get; } = new();

    public PinnedCategoryOption? PinnedCategory1
    {
        get => AvailableCategories.FirstOrDefault(c => c.Id == (_config.PinnedCategories.ElementAtOrDefault(0) ?? ""));
        set { SetPinned(0, value?.Id ?? ""); OnPropertyChanged(); }
    }
    public PinnedCategoryOption? PinnedCategory2
    {
        get => AvailableCategories.FirstOrDefault(c => c.Id == (_config.PinnedCategories.ElementAtOrDefault(1) ?? ""));
        set { SetPinned(1, value?.Id ?? ""); OnPropertyChanged(); }
    }
    public PinnedCategoryOption? PinnedCategory3
    {
        get => AvailableCategories.FirstOrDefault(c => c.Id == (_config.PinnedCategories.ElementAtOrDefault(2) ?? ""));
        set { SetPinned(2, value?.Id ?? ""); OnPropertyChanged(); }
    }
    public PinnedCategoryOption? PinnedCategory4
    {
        get => AvailableCategories.FirstOrDefault(c => c.Id == (_config.PinnedCategories.ElementAtOrDefault(3) ?? ""));
        set { SetPinned(3, value?.Id ?? ""); OnPropertyChanged(); }
    }

    private void SetPinned(int index, string id)
    {
        while (_config.PinnedCategories.Count <= index) _config.PinnedCategories.Add("");
        _config.PinnedCategories[index] = id;
        _config.PinnedCategories.RemoveAll(string.IsNullOrWhiteSpace);
        Save();
        _main.UpdatePinnedCategories();
    }
}
