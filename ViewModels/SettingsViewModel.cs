using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;

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
        set { _config.WindowOpacity = Math.Clamp(value, 0.5, 1.0); Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    public double BarWidth
    {
        get => _config.BarWidth;
        set { _config.BarWidth = Math.Clamp(value, 400, 1000); Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
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

    public string[] QuerySearchPrecisionOptions { get; } = ["Low", "Regular", "Strict"];

    public string QuerySearchPrecision
    {
        get => ToTitle(_config.QuerySearchPrecision);
        set { _config.QuerySearchPrecision = ToKey(value); _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
    }

    public string[] LastQueryStyleOptions { get; } = ["Clear", "Select last Query", "Keep last Query"];

    public string LastQueryStyle
    {
        get => MapQueryStyleToDisplay(_config.LastQueryStyle);
        set
        {
            _config.LastQueryStyle = MapDisplayToQueryStyle(value);
            _main.Config = _config.Clone();
            Save();
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
        set { _config.SearchDelay = Math.Clamp(value, 0, 500); _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
    }

    public string[] WebSearchEngineOptions { get; } = ["Google", "DuckDuckGo", "Bing"];

    public string WebSearchEngine
    {
        get => ToTitle(_config.WebSearchEngine);
        set { _config.WebSearchEngine = ToKey(value); _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
    }

    public bool IndexShell
    {
        get => _config.IndexShell;
        set { _config.IndexShell = value; _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
    }

    public bool IndexSystemCommands
    {
        get => _config.IndexSystemCommands;
        set { _config.IndexSystemCommands = value; _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
    }

    public bool IndexUrls
    {
        get => _config.IndexUrls;
        set { _config.IndexUrls = value; _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
    }

    public bool IndexWebSearches
    {
        get => _config.IndexWebSearches;
        set { _config.IndexWebSearches = value; _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
    }

    public bool IndexWindowsSettings
    {
        get => _config.IndexWindowsSettings;
        set { _config.IndexWindowsSettings = value; _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
    }

    public int MaxFileDepth
    {
        get => _config.MaxFileDepth;
        set { _config.MaxFileDepth = Math.Clamp(value, 1, 5); _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
    }

    public bool FileSearchEnabled
    {
        get => _config.FileSearchEnabled;
        set
        {
            if (_config.FileSearchEnabled == value) return;
            _config.FileSearchEnabled = value;
            _main.Config = _config.Clone();
            Save();
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
            _main.Config = _config.Clone();
            Save();
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
        _main.Config = _config.Clone();
        Save();
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
            _main.Config = _config.Clone();
            Save();
            OnPropertyChanged();
        }
    }

    public bool HotkeyEnabled
    {
        get => _config.HotkeyEnabled;
        set { _config.HotkeyEnabled = value; Save(); OnPropertyChanged(); }
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
        set { _config.ShowTrayIcon = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
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
        set { _config.ClipboardHistorySize = Math.Clamp(value, 10, 200); _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
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
        set { _config.AnimationEnabled = value; _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
    }

    public bool SoundEffectEnabled
    {
        get => _config.SoundEffectEnabled;
        set { _config.SoundEffectEnabled = value; _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
    }

    public string[] SearchWindowPositions { get; } = ["Center", "Center Top", "Left Top", "Right Top", "Custom Position"];

    public string SearchWindowPosition
    {
        get => MapWindowPositionToDisplay(_config.SearchWindowPosition);
        set
        {
            _config.SearchWindowPosition = MapDisplayToWindowPosition(value);
            _main.Config = _config.Clone();
            Save();
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

    // ═══════════════════════════════════════════════════════════════
    // Actions — enable/disable each action independently
    // ═══════════════════════════════════════════════════════════════

    public bool ActionCalc
    {
        get => _config.IndexCalculator;
        set { _config.IndexCalculator = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool ActionColor
    {
        get => _config.ActionColor;
        set { _config.ActionColor = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool ActionCurrency
    {
        get => _config.ActionCurrency;
        set { _config.ActionCurrency = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool ActionTimer
    {
        get => _config.ActionTimer;
        set { _config.ActionTimer = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool ActionIp
    {
        get => _config.ActionIp;
        set { _config.ActionIp = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool ActionAi
    {
        get => _config.ActionAi;
        set { _config.ActionAi = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool ActionPasswordGen
    {
        get => _config.ActionPasswordGen;
        set { _config.ActionPasswordGen = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool ActionQuickNote
    {
        get => _config.ActionQuickNote;
        set { _config.ActionQuickNote = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool ActionKillProcess
    {
        get => _config.ActionKillProcess;
        set { _config.ActionKillProcess = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool ActionScreenshot
    {
        get => _config.ActionScreenshot;
        set { _config.ActionScreenshot = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool ActionSystem
    {
        get => _config.IndexSystemCommands;
        set { _config.IndexSystemCommands = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
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
        set { _config.AiEnabled = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
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
            _main.Config = _config.Clone();
            Save();
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
        _main.Config = _config.Clone();
        Save();
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
        _main.Config = _config.Clone();
        Save();
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
        _config = new SpurConfig();
        _configService.Save(_config);
        _main.Config = _config.Clone();
        LoadStartupState();

        _indexedFoldersList = null;
        _fileTypesList = null;

        // Notify all properties changed
        OnPropertyChanged(string.Empty);
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
        Downloading,
        Installing,
        ReadyToRestart,
        Error,
    }

    [ObservableProperty]
    private UpdateState _updateStatus = UpdateState.Idle;

    /// <summary>Human-readable label for the current state. The UI binds to this directly.</summary>
    public string UpdateStatusText => UpdateStatus switch
    {
        UpdateState.Idle            => "Up to date",
        UpdateState.Checking        => "Checking for updates…",
        UpdateState.NoUpdate        => "You're on the latest version",
        UpdateState.Downloading     => "Downloading update…",
        UpdateState.Installing      => "Installing update…",
        UpdateState.ReadyToRestart  => "Update ready — restart Spur to install",
        UpdateState.Error           => "Update failed. Tap to retry.",
        _                            => string.Empty,
    };

    /// <summary>True while Spur is working on an update — disables the primary button.</summary>
    public bool IsUpdateBusy =>
        UpdateStatus is UpdateState.Checking or UpdateState.Downloading or UpdateState.Installing;

    /// <summary>Whether the user can trigger a check (i.e. nothing is already in flight).</summary>
    public bool CanCheckForUpdates => !IsUpdateBusy;

    partial void OnUpdateStatusChanged(UpdateState value)
    {
        OnPropertyChanged(nameof(UpdateStatusText));
        OnPropertyChanged(nameof(IsUpdateBusy));
        OnPropertyChanged(nameof(CanCheckForUpdates));
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        if (IsUpdateBusy) return;

        UpdateStatus = UpdateState.Checking;
        // Mock remote check — replace with real release lookup.
        await Task.Delay(1500);
        UpdateStatus = UpdateState.NoUpdate;
    }

    [RelayCommand]
    private async Task DownloadUpdate()
    {
        if (IsUpdateBusy) return;

        UpdateStatus = UpdateState.Downloading;
        // Mock download with a short, visible delay.
        await Task.Delay(1500);
        await InstallUpdateAsync();
    }

    [RelayCommand]
    private async Task InstallUpdateAsync()
    {
        if (IsUpdateBusy) return;

        UpdateStatus = UpdateState.Installing;
        // Mock install.
        await Task.Delay(800);
        UpdateStatus = UpdateState.ReadyToRestart;
    }

    [RelayCommand]
    private void RestartToUpdate()
    {
        // Real flow would spawn the installed update + exit cleanly.
        System.Windows.Application.Current.Shutdown();
    }

    [RelayCommand]
    private void CancelUpdate()
    {
        // User chose to keep the current version for now.
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
