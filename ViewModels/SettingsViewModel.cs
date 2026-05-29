using System.Collections.ObjectModel;
using System.Text.Json;

namespace Arc.ViewModels;

/// <summary>
/// Sidebar-based settings view model with instant-save semantics.
/// Every property setter persists the config to disk immediately.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ConfigService  _configService;
    private readonly MainViewModel  _main;
    private          ArcConfig     _config;

    public SettingsViewModel(ArcConfig config, ConfigService configService, MainViewModel main)
    {
        _config        = config;
        _configService = configService;
        _main          = main;

        // Init sidebar sections
        Sections = new ObservableCollection<SettingsSection>
        {
            new("Appearance",      "palette",  "\ue3ae"),
            new("Search & Index",  "search",   "\ue721"),
            new("Hotkey",          "command",  "\ue72d"),
            new("Startup & Perf",  "zap",      "\ue945"),
            new("Result Behavior", "cursor",   "\ue753"),
            new("Privacy",         "shield",   "\ue72e"),
            new("AI Assistant",    "sparkles", "\ue7f3"),
            new("About Arc",      "info",     "\ue946"),
        };
        SelectedSection = Sections[0];

        LoadStartupState();
    }

    // ═══════════════════════════════════════════════════════════════
    // Sidebar
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    private ObservableCollection<SettingsSection> _sections;

    [ObservableProperty]
    private SettingsSection _selectedSection;

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
        ThemeManager.Apply(theme);
        Save();
        OnPropertyChanged(nameof(ThemeDark));
        OnPropertyChanged(nameof(ThemeLight));
        OnPropertyChanged(nameof(ThemeSystem));
    }


    public double WindowOpacity
    {
        get => _config.WindowOpacity;
        set { _config.WindowOpacity = Math.Clamp(value, 0.5, 1.0); Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    public string BackgroundColor
    {
        get => _config.BackgroundColor;
        set { _config.BackgroundColor = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    public bool EnableBlur
    {
        get => _config.EnableBlur;
        set { _config.EnableBlur = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    public bool UseWindowsAccentColor
    {
        get => _config.UseWindowsAccentColor;
        set { _config.UseWindowsAccentColor = value; Save(); OnPropertyChanged(); OnPropertyChanged(nameof(UseCustomAccentColor)); }
    }

    public bool UseCustomAccentColor
    {
        get => !_config.UseWindowsAccentColor;
        set { UseWindowsAccentColor = !value; }
    }

    public string AccentColor
    {
        get => _config.AccentColor;
        set { _config.AccentColor = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    public string FontFamilySetting
    {
        get => _config.FontFamily;
        set { if (!string.IsNullOrWhiteSpace(value)) { _config.FontFamily = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); } }
    }

    public double FontSize
    {
        get => _config.FontSize;
        set { _config.FontSize = Math.Clamp(value, 10, 24); Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    public double LauncherWidth
    {
        get => _config.LauncherWidth;
        set { _config.LauncherWidth = Math.Clamp(value, 400, 1200); Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    public bool ShowCategoryLabels
    {
        get => _config.ShowCategoryLabels;
        set { _config.ShowCategoryLabels = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    public double CornerRadius
    {
        get => _config.CornerRadius;
        set { _config.CornerRadius = Math.Clamp(value, 0, 20); Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    public string AnimationSpeed
    {
        get => _config.AnimationSpeed;
        set { _config.AnimationSpeed = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    public bool ShowAppIcons
    {
        get => _config.ShowAppIcons;
        set { _config.ShowAppIcons = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    public bool CompactMode
    {
        get => _config.CompactMode;
        set { _config.CompactMode = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
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
        get => _config.LastQueryStyle switch
        {
            "select" => "Select last Query",
            "keep" => "Keep last Query",
            _ => "Clear",
        };
        set
        {
            _config.LastQueryStyle = value switch
            {
                "Select last Query" => "select",
                "Keep last Query" => "keep",
                _ => "clear",
            };
            _main.Config = _config.Clone();
            Save();
            OnPropertyChanged();
        }
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
        set { _config.MaxFileDepth = Math.Clamp(value, 1, 5); Save(); OnPropertyChanged(); }
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
                    "Arc will monitor and store everything you copy, including passwords, " +
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
            if (value) StartupService.Enable(); else StartupService.Disable();
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
        set { _config.ClipboardHistorySize = Math.Clamp(value, 10, 200); Save(); OnPropertyChanged(); }
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

    public bool ShowPlaceholder
    {
        get => _config.ShowPlaceholder;
        set { _config.ShowPlaceholder = value; _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
    }

    public string PlaceholderText
    {
        get => _config.PlaceholderText;
        set { _config.PlaceholderText = value; _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
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

    public string[] SearchWindowLocations { get; } = ["Primary Monitor"];

    public string SearchWindowLocation
    {
        get => "Primary Monitor";
        set { _config.SearchWindowLocation = "primary"; _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
    }

    public string[] SearchWindowPositions { get; } = ["Center", "Center Top", "Left Top", "Right Top", "Custom Position"];

    public string SearchWindowPosition
    {
        get => _config.SearchWindowPosition switch
        {
            "centerTop" => "Center Top",
            "leftTop" => "Left Top",
            "rightTop" => "Right Top",
            "custom" => "Custom Position",
            _ => "Center",
        };
        set
        {
            _config.SearchWindowPosition = value switch
            {
                "Center Top" => "centerTop",
                "Left Top" => "leftTop",
                "Right Top" => "rightTop",
                "Custom Position" => "custom",
                _ => "center",
            };
            _main.Config = _config.Clone();
            Save();
            OnPropertyChanged();
        }
    }

    public bool AlwaysPreview
    {
        get => _config.AlwaysPreview;
        set { _config.AlwaysPreview = value; _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
    }

    // ═══════════════════════════════════════════════════════════════
    // AI Assistant
    // ═══════════════════════════════════════════════════════════════

    public string[] Providers { get; } = ["groq", "gemini", "openrouter", "deepseek"];

    public string AiProvider
    {
        get => _config.AiProvider;
        set
        {
            if (_config.AiProvider == value) return;
            _config.AiProvider = value;
            _main.Config = _config.Clone();
            Save();
            OnPropertyChanged();
            OnPropertyChanged(nameof(ApiKey));
            OnPropertyChanged(nameof(AiModel));
            OnPropertyChanged(nameof(CurrentModels));
        }
    }

    public string ApiKey
    {
        get => _config.AiProvider switch
        {
            "gemini"     => _config.GeminiApiKey,
            "openrouter" => _config.OpenRouterApiKey,
            "deepseek"   => _config.DeepSeekApiKey,
            _            => _config.GroqApiKey,
        };
        set
        {
            switch (_config.AiProvider)
            {
                case "gemini":     _config.GeminiApiKey     = value; break;
                case "openrouter": _config.OpenRouterApiKey = value; break;
                case "deepseek":   _config.DeepSeekApiKey   = value; break;
                default:           _config.GroqApiKey       = value; break;
            }
            _main.Config = _config.Clone();
            Save();
            OnPropertyChanged();
        }
    }

    public string[] CurrentModels => _config.AiProvider switch
    {
        "groq"       => ["llama-3.1-8b-instant", "llama-3.3-70b-versatile", "qwen/qwen3-32b"],
        "gemini"     => ["gemini-2.0-flash", "gemini-2.5-pro-exp-03-25", "gemini-1.5-flash"],
        "openrouter" => ["google/gemini-2.0-flash-001", "meta-llama/llama-3.1-8b-instruct", "deepseek/deepseek-chat"],
        "deepseek"   => ["deepseek-chat", "deepseek-reasoner"],
        _            => [],
    };

    public string AiModel
    {
        get => _config.AiProvider switch
        {
            "gemini"     => _config.GeminiModel,
            "openrouter" => _config.OpenRouterModel,
            "deepseek"   => _config.DeepSeekModel,
            _            => _config.GroqModel,
        };
        set
        {
            switch (_config.AiProvider)
            {
                case "gemini":     _config.GeminiModel     = value; break;
                case "openrouter": _config.OpenRouterModel = value; break;
                case "deepseek":   _config.DeepSeekModel   = value; break;
                default:           _config.GroqModel       = value; break;
            }
            _main.Config = _config.Clone();
            Save();
            OnPropertyChanged();
        }
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
        new FrequencyService().ClearAll();
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        _config = new ArcConfig();
        _configService.Save(_config);
        _main.Config = _config.Clone();
        LoadStartupState();

        // Notify all properties changed
        OnPropertyChanged(string.Empty);
    }

    // ═══════════════════════════════════════════════════════════════
    // About
    // ═══════════════════════════════════════════════════════════════

    public string Version => "Arc v0.2.0";
    public string Credits => "Built with .NET 9 + WPF";
    public string License => "MIT License";

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    private void LoadStartupState()
    {
        _launchOnStartup = StartupService.IsEnabled();
        OnPropertyChanged(nameof(LaunchOnStartup));
    }

    private void Save() => _configService.Save(_config);

    private static string ToTitle(string value)
        => string.IsNullOrWhiteSpace(value) ? "Regular" : char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();

    private static string ToKey(string value)
        => string.IsNullOrWhiteSpace(value) ? "regular" : value.Replace(" ", string.Empty).ToLowerInvariant();
}

/// <summary>A single sidebar section in the settings UI.</summary>
public record SettingsSection(string Name, string LucideIcon, string SegoeFluentIcon);
