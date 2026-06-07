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
    private readonly IConfigService   _configService;
    private readonly MainViewModel    _main;
    private readonly IThemeManager    _themeManager;
    private readonly IStartupService  _startupService;
    private readonly IFrequencyService _frequencyService;
    private readonly ISecureStorageService _secureStorage;
    private          SpurConfig       _config;

    public SettingsViewModel(SpurConfig config, IConfigService configService, MainViewModel main,
                             IThemeManager themeManager, IStartupService startupService, IFrequencyService frequencyService,
                             ISecureStorageService secureStorage)
    {
        _config          = config;
        _configService   = configService;
        _main            = main;
        _themeManager    = themeManager;
        _startupService  = startupService;
        _frequencyService = frequencyService;
        _secureStorage   = secureStorage;

        // Init sidebar — features.md §6
        Sections = new ObservableCollection<SettingsSection>
        {
            new("General",  "\ue713"),
            new("Search",   "\ue11A"),
            new("Actions",  "\ue945"),
            new("Extras",   "\ue113"),
            new("About",    "\ue946"),
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
        set { _config.IndexShell = value; _config.Shell.Enabled = value; _main.Config = _config.Clone(); Save(); OnPropertyChanged(); }
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
            OnPropertyChanged(nameof(PositionCenter));
            OnPropertyChanged(nameof(PositionTop));
            OnPropertyChanged(nameof(PositionLeft));
            OnPropertyChanged(nameof(PositionRight));
            OnPropertyChanged(nameof(PositionCustom));
        }
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
    public bool SystemEnabled
    {
        get => _config.System.Enabled;
        set { _config.System.Enabled = value; _config.IndexSystemCommands = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string SystemKeyword
    {
        get => _config.System.Keyword;
        set { _config.System.Keyword = value; _config.KeywordSystem = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    // ── Timer ─────────────────────────────────────────────────────
    public bool TimerEnabled
    {
        get => _config.Timer.Enabled;
        set { _config.Timer.Enabled = value; _config.ActionTimer = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string TimerKeyword
    {
        get => _config.Timer.Keyword;
        set { _config.Timer.Keyword = value; _config.KeywordTimer = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string TimerPresets
    {
        get => _config.Timer.DefaultPresets;
        set { _config.Timer.DefaultPresets = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    // ── Kill Process ──────────────────────────────────────────────
    public bool KillProcessEnabled
    {
        get => _config.KillProcess.Enabled;
        set { _config.KillProcess.Enabled = value; _config.ActionKillProcess = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string KillProcessKeyword
    {
        get => _config.KillProcess.Keyword;
        set { _config.KillProcess.Keyword = value; _config.KeywordKill = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool KillProcessShowWindowTitles
    {
        get => _config.KillProcess.ShowWindowTitles;
        set { _config.KillProcess.ShowWindowTitles = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool KillProcessPrioritizeVisibleWindows
    {
        get => _config.KillProcess.PrioritizeVisibleWindows;
        set { _config.KillProcess.PrioritizeVisibleWindows = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    // ── Password Generator ────────────────────────────────────────
    public bool PasswordGenEnabled
    {
        get => _config.PasswordGen.Enabled;
        set { _config.PasswordGen.Enabled = value; _config.ActionPasswordGen = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string PasswordGenKeyword
    {
        get => _config.PasswordGen.Keyword;
        set { _config.PasswordGen.Keyword = value; _config.KeywordPassword = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public int PasswordGenLength
    {
        get => _config.PasswordGen.DefaultLength;
        set { _config.PasswordGen.DefaultLength = Math.Clamp(value, 4, 128); Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool PasswordGenSymbols
    {
        get => _config.PasswordGen.IncludeSymbols;
        set { _config.PasswordGen.IncludeSymbols = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool PasswordGenNumbers
    {
        get => _config.PasswordGen.IncludeNumbers;
        set { _config.PasswordGen.IncludeNumbers = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool PasswordGenUppercase
    {
        get => _config.PasswordGen.IncludeUppercase;
        set { _config.PasswordGen.IncludeUppercase = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    // ── Screenshot ────────────────────────────────────────────────
    public bool ScreenshotEnabled
    {
        get => _config.Screenshot.Enabled;
        set { _config.Screenshot.Enabled = value; _config.ActionScreenshot = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string ScreenshotKeyword
    {
        get => _config.Screenshot.Keyword;
        set { _config.Screenshot.Keyword = value; _config.KeywordScreenshot = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string ScreenshotFormat
    {
        get => _config.Screenshot.SaveFormat;
        set { _config.Screenshot.SaveFormat = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string[] ScreenshotFormatOptions { get; } = ["png", "jpg", "bmp"];
    public string ScreenshotSaveFolder
    {
        get => _config.Screenshot.SaveFolder;
        set { _config.Screenshot.SaveFolder = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    // ── Quick Note ────────────────────────────────────────────────
    public bool QuickNoteEnabled
    {
        get => _config.QuickNote.Enabled;
        set { _config.QuickNote.Enabled = value; _config.ActionQuickNote = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string QuickNoteKeyword
    {
        get => _config.QuickNote.Keyword;
        set { _config.QuickNote.Keyword = value; _config.KeywordNote = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string QuickNoteSaveFolder
    {
        get => _config.QuickNote.SaveFolder;
        set { _config.QuickNote.SaveFolder = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    // ── Currency ──────────────────────────────────────────────────
    public bool CurrencyEnabled
    {
        get => _config.Currency.Enabled;
        set { _config.Currency.Enabled = value; _config.ActionCurrency = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string CurrencyKeyword
    {
        get => _config.Currency.Keyword;
        set { _config.Currency.Keyword = value; _config.KeywordCurrency = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    // ── Color ─────────────────────────────────────────────────────
    public bool ColorEnabled
    {
        get => _config.Color.Enabled;
        set { _config.Color.Enabled = value; _config.ActionColor = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string ColorKeyword
    {
        get => _config.Color.Keyword;
        set { _config.Color.Keyword = value; _config.KeywordColor = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    // ── IP ────────────────────────────────────────────────────────
    public bool IpEnabled
    {
        get => _config.Ip.Enabled;
        set { _config.Ip.Enabled = value; _config.ActionIp = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string IpKeyword
    {
        get => _config.Ip.Keyword;
        set { _config.Ip.Keyword = value; _config.KeywordIp = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    // ── AI ────────────────────────────────────────────────────────
    public bool AiEnabled
    {
        get => _config.Ai.Enabled;
        set { _config.Ai.Enabled = value; _config.ActionAi = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string AiKeyword
    {
        get => _config.Ai.Keyword;
        set { _config.Ai.Keyword = value; _config.KeywordAi = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }

    // ── Shell ─────────────────────────────────────────────────────
    public bool ShellEnabled
    {
        get => _config.Shell.Enabled;
        set { _config.Shell.Enabled = value; _config.IndexShell = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string ShellKeyword
    {
        get => _config.Shell.Keyword;
        set { _config.Shell.Keyword = value; _config.KeywordShell = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool ShellCloseAfterExecution
    {
        get => _config.Shell.CloseAfterExecution;
        set { _config.Shell.CloseAfterExecution = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool ShellAlwaysRunAsAdministrator
    {
        get => _config.Shell.AlwaysRunAsAdministrator;
        set { _config.Shell.AlwaysRunAsAdministrator = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public bool ShellUseWindowsTerminal
    {
        get => _config.Shell.UseWindowsTerminal;
        set { _config.Shell.UseWindowsTerminal = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string ShellTerminal
    {
        get => _config.Shell.Terminal;
        set { _config.Shell.Terminal = value; Save(); OnPropertyChanged(); _main.Config = _config.Clone(); }
    }
    public string[] ShellTerminalOptions { get; } = ["cmd", "powershell", "pwsh"];

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
            "gemini"     => _secureStorage.Decrypt(_config.EncryptedGeminiApiKey),
            "openrouter" => _secureStorage.Decrypt(_config.EncryptedOpenRouterApiKey),
            "deepseek"   => _secureStorage.Decrypt(_config.EncryptedDeepSeekApiKey),
            _            => _secureStorage.Decrypt(_config.EncryptedGroqApiKey),
        };
        set
        {
            var encrypted = _secureStorage.Encrypt(value);
            switch (_config.AiProvider)
            {
                case "gemini":     _config.EncryptedGeminiApiKey     = encrypted; break;
                case "openrouter": _config.EncryptedOpenRouterApiKey = encrypted; break;
                case "deepseek":   _config.EncryptedDeepSeekApiKey   = encrypted; break;
                default:           _config.EncryptedGroqApiKey       = encrypted; break;
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
        _frequencyService.ClearAll();
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        _config = new SpurConfig();
        _configService.Save(_config);
        _main.Config = _config.Clone();
        LoadStartupState();

        // Notify all properties changed
        OnPropertyChanged(string.Empty);
    }

    // ═══════════════════════════════════════════════════════════════
    // About
    // ═══════════════════════════════════════════════════════════════

    public string Version =>
        $"Spur v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.2.0"}";

    public string Credits => "Built with .NET 9 + WPF · Monochrome v2 design system";
    public string License => "MIT License";

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    private void LoadStartupState()
    {
        _launchOnStartup = _startupService.IsEnabled();
        OnPropertyChanged(nameof(LaunchOnStartup));
    }

    private void Save() => _configService.Save(_config);

    private static string ToTitle(string value)
        => string.IsNullOrWhiteSpace(value) ? "Regular" : char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();

    private static string ToKey(string value)
        => string.IsNullOrWhiteSpace(value) ? "regular" : value.Replace(" ", string.Empty).ToLowerInvariant();
}

/// <summary>A single sidebar section in the settings UI.</summary>
public record SettingsSection(string Name, string IconGlyph);
