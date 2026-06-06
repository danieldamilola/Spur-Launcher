using System.Collections.ObjectModel;
using Spur.Extensions;
using Spur.Services;
using Spur.Models;

namespace Spur.ViewModels;

// ═══════════════════════════════════════════════════════════════════
// MainViewModel — central orchestrator for the Spur launcher
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// Central orchestrator for the Spur launcher.
/// Owns the search query, result list, selection, category filter,
/// and all action state (AI streaming, timer countdown, color/IP data).
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    // ── Injected services ────────────────────────────────────────────
    private readonly ILogger              _log;
    private readonly IAppDiscoveryService _apps;
    private readonly IFileSearchService   _files;
    private readonly IFrequencyService    _freq;
    private readonly IConfigService       _configSvc;
    private readonly IClipboardService    _clipboard;
    private readonly INotificationService _notification;
    private readonly IAiService           _aiService;
    private readonly IThemeManager        _themeManager;
    private readonly IStartupService      _startupService;
    private readonly ICommandRegistry     _registry;
    private readonly ISearchEngineService _searchEngine;
    private readonly ISecureStorageService _secureStorage;

    // ── Sub-ViewModels ───────────────────────────────────────────────
    private readonly AiChatViewModel    _ai;
    private readonly TimerViewModel     _timer;
    private readonly ClipboardViewModel _clipboardVm;

    // (Search actions and catalog moved to SearchEngineService)

    // ── Catalog loading state ────────────────────────────────────────
    /// <summary>True while the app catalog is being discovered.</summary>
    [ObservableProperty]
    private bool _catalogLoading;

    // ── Search debounce ──────────────────────────────────────────────
    private CancellationTokenSource? _searchCts;

    // ── Keyword mapping cache ──────────────────────────────────────
    private Dictionary<string, (string actionId, string icon)>? _keywordMap;

    // ── Constructor ──────────────────────────────────────────────────
    public MainViewModel(
        SpurConfig             config,
        ILogger               log,
        IAppDiscoveryService  apps,
        IFileSearchService    files,
        IFrequencyService     freq,
        IConfigService        configSvc,
        IClipboardService     clipboard,
        INotificationService  notification,
        IAiService            aiService,
        IThemeManager         themeManager,
        IStartupService       startupService,
        ICommandRegistry      registry,
        CommandPaletteViewModel commandPalette,
        ISearchEngineService  searchEngine,
        ISecureStorageService secureStorage)
    {
        _log          = log;
        _apps         = apps;
        _files        = files;
        _freq         = freq;
        _configSvc    = configSvc;
        _clipboard    = clipboard;
        _notification  = notification;
        _aiService     = aiService;
        _themeManager  = themeManager;
        _startupService = startupService;
        _registry = registry;
        _searchEngine = searchEngine;
        _secureStorage = secureStorage;

        Config   = config;
        Settings = new SettingsViewModel(Config, _configSvc, this, _themeManager, _startupService, _freq, _secureStorage);

        // Push initial config to services
        _files.MaxDepth     = Config.MaxFileDepth;
        _clipboard.MaxItems = Config.ClipboardHistorySize;

        // Sub-ViewModels
        _ai          = new AiChatViewModel(_aiService, _secureStorage, Config);
        _timer       = new TimerViewModel(_notification);
        _clipboardVm = new ClipboardViewModel(_clipboard, Config, _configSvc);

        // Forward sub-VM property changes for backward-compatible bindings
        _ai.PropertyChanged    += (_, args) => OnPropertyChanged(args.PropertyName);
        _timer.PropertyChanged += (_, args) => OnPropertyChanged(args.PropertyName);

        CommandPalette = commandPalette;

        PopulatePaletteCommands();

        _apps.CatalogRefreshed += HandleCatalogRefreshed;

        Helpers.SafeFireAndForget.Run(LoadAppsAsync, _log, "LoadApps");
        Results.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasResults));
    }

    private void HandleCatalogRefreshed(List<SearchResult> freshCatalog)
    {
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            if (!string.IsNullOrEmpty(Query))
                OnPropertyChanged(nameof(Query));
        });
    }

    /// <summary>Pushes config values to services whenever the config object is replaced.</summary>
    partial void OnConfigChanged(SpurConfig value)
    {
        _files.MaxDepth     = value.MaxFileDepth;
        _clipboard.MaxItems = value.ClipboardHistorySize;
        _keywordMap = null; // Rebuild on next use — keyword configs may have changed
    }

    // ═══════════════════════════════════════════════════════════════
    // Observable properties
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQuery))]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    private string _query = string.Empty;

    [ObservableProperty]
    private SpurConfig _config;

    [ObservableProperty]
    private SettingsViewModel _settings;

    /// <summary>Flat list: items are either <see cref="SectionLabel"/> or <see cref="SearchResult"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    private ObservableCollection<object> _results = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedResult))]
    [NotifyPropertyChangedFor(nameof(FooterHint))]
    private int _selectedIndex = -1;

    [ObservableProperty]
    private string _footerHint = string.Empty;

    [ObservableProperty]
    private string _activeScopeId = "all";

    [ObservableProperty]
    private ObservableCollection<ScopeFilterItem> _scopeFilters = [];

    private List<object> _rawResults = [];

    /// <summary>Null = all categories. Values: "apps" | "files" | "clipboard" | "actions".</summary>
    [ObservableProperty]
    private string? _activeCategory;

    /// <summary>Lucide icon glyph shown in the search bar when a keyword scope is active.</summary>
    [ObservableProperty]
    private string? _scopeIconGlyph;

    [ObservableProperty]
    private bool _isSettingsOpen;

    // Sub-ViewModels — exposed for direct XAML binding (no pass-throughs)
    public TimerViewModel      Timer      => _timer;
    public AiChatViewModel     AiChat     => _ai;
    public ClipboardViewModel  Clipboard  => _clipboardVm;

    /// <summary>Which action preview panel to show (timer / ai / color / ip / …).</summary>
    private string? _activeActionPanel;
    public string? ActiveActionPanel
    {
        get => _activeActionPanel;
        set
        {
            if (SetProperty(ref _activeActionPanel, value))
                OnPropertyChanged(nameof(IsActionPanelVisible));
        }
    }
    public bool IsActionPanelVisible => _activeActionPanel is not null;

    private string _actionResultText = string.Empty;
    public string ActionResultText
    {
        get => _actionResultText;
        set => SetProperty(ref _actionResultText, value);
    }

    private string _actionResultSubText = string.Empty;
    public string ActionResultSubText
    {
        get => _actionResultSubText;
        set => SetProperty(ref _actionResultSubText, value);
    }

    private string _ipPreviewText = string.Empty;
    public string IpPreviewText
    {
        get => _ipPreviewText;
        set => SetProperty(ref _ipPreviewText, value);
    }
    private bool _ipPreviewLoading;

    public void CancelSearch() => _searchCts?.Cancel();

    // ═══════════════════════════════════════════════════════════════
    // Computed properties
    // ═══════════════════════════════════════════════════════════════

    public bool HasQuery          => !string.IsNullOrEmpty(Query);
    public bool HasResults         => Results.Count > 0;
    public bool IsBrowsePanelVisible => ActiveCategory is not null;

    /// <summary>True when ActiveCategory points to a keyword-scoped action (not a category filter).</summary>
    private static bool IsActionScope(string? category) => category switch
    {
        null or "apps" or "files" or "clipboard" or "actions" => false,
        _ => true,
    };

    /// <summary>Hub removed per ux.md — always false.</summary>
    public bool IsHubVisible => false;

    public bool IsScopeBarVisible => false;

    public string SearchPlaceholder => ActiveCategory switch
    {
        "files"     => "Search files…",
        "actions"   => "Search actions…",
        "clipboard" => "Filter clipboard…",
        _           => "Search",
    };

    public SearchResult? SelectedResult
    {
        get
        {
            if (SelectedIndex < 0 || SelectedIndex >= Results.Count) return null;
            return Results[SelectedIndex] as SearchResult;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Query change handler — debounced search
    // ═══════════════════════════════════════════════════════════════

        partial void OnQueryChanged(string value)
    {
        try
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var ct = _searchCts.Token;

            // Detect action keyword -> lock scope
            var effectiveQuery = value;
            var detected = DetectActionKeyword(value);
            if (detected is not null)
            {
                var (actionId, iconGlyph, subQuery) = detected.Value;
                ActiveCategory = actionId;
                ScopeIconGlyph = iconGlyph;
                effectiveQuery = subQuery;
            }
            else if (ActiveCategory is not null && IsActionScope(ActiveCategory))
            {
                // User released the keyword -> exit scope
                ActiveCategory = null;
                ScopeIconGlyph = null;
            }

            if (string.IsNullOrEmpty(effectiveQuery) && ActiveCategory is null)
            {
                ClearIdleState();
                return;
            }

            ActiveScopeId = "all";

            _ = DebouncedSearchAsync(effectiveQuery, ct);
        }
        catch (Exception ex)
        {
            _log.Warning("OnQueryChanged error", ex);
        }
    }

    private async Task DebouncedSearchAsync(string effectiveQuery, CancellationToken ct)
    {
        try
        {
            await Task.Delay(150, ct);
            await Application.Current!.Dispatcher.InvokeAsync(() => RunSearch(effectiveQuery, ct));
        }
        catch (OperationCanceledException) { /* expected on new keystroke */ }
    }
partial void OnActiveCategoryChanged(string? value)
    {
        OnPropertyChanged(nameof(IsBrowsePanelVisible));
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnQueryChanged(Query ?? string.Empty);
    }

    /// <summary>Empty launcher — no hub grid (ux.md).</summary>
    private void ClearIdleState()
    {
        CancelActionWork();
        _searchCts?.Cancel();
        _rawResults.Clear();
        ScopeFilters.Clear();
        OnPropertyChanged(nameof(IsScopeBarVisible));

        Application.Current?.Dispatcher.Invoke(() =>
        {
            Results.Clear();
            SelectedIndex = -1;
            FooterHint = string.Empty;
        });
    }

    partial void OnSelectedIndexChanged(int value)
    {
        UpdateFooterHint();
        UpdateActionPreview();
    }

    partial void OnActiveScopeIdChanged(string value)
    {
        ApplyScopeFilter();
        OnPropertyChanged(nameof(IsScopeBarVisible));
    }

    public void SetActiveScope(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        ActiveScopeId = id;
    }

    public void CycleScope()
    {
        if (ScopeFilters.Count < 2) return;

        if (ActiveScopeId == "all")
        {
            ActiveScopeId = ScopeFilters[0].Id;
            return;
        }

        var idx = ScopeFilters.ToList().FindIndex(s => s.Id == ActiveScopeId);
        ActiveScopeId = idx + 1 >= ScopeFilters.Count ? "all" : ScopeFilters[idx + 1].Id;
    }

    private void CommitResults(List<object> items)
    {
        _rawResults = items;
        UpdateScopeFilters();
        ApplyScopeFilter();
    }

    private void ApplyScopeFilter()
    {
        var display = ActiveScopeId == "all"
            ? _rawResults
            : FilterResultsByScope(_rawResults, ActiveScopeId);

        Results.Clear();
        foreach (var item in display)
            Results.Add(item);

        SelectedIndex = Results.Count > 0 ? FindFirstResultIndex() : -1;
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(IsScopeBarVisible));
        UpdateFooterHint();
    }

    private static List<object> FilterResultsByScope(List<object> source, string scopeId)
    {
        var result = new List<object>();
        string? currentSection = null;
        var sectionItems = new List<object>();

        void FlushSection()
        {
            if (currentSection is null || sectionItems.Count == 0) return;
            if (SectionMatchesScope(currentSection, scopeId))
            {
                result.Add(new SectionLabel(currentSection));
                result.AddRange(sectionItems);
            }
            sectionItems.Clear();
        }

        foreach (var item in source)
        {
            if (item is SectionLabel label)
            {
                FlushSection();
                currentSection = label.Title;
            }
            else
            {
                sectionItems.Add(item);
            }
        }
        FlushSection();
        return result;
    }

    private static bool SectionMatchesScope(string sectionTitle, string scopeId) => scopeId switch
    {
        "apps"      => sectionTitle is "Applications" or "Settings",
        "files"     => sectionTitle == "Files",
        "clipboard" => sectionTitle == "Clipboard",
        "commands"  => sectionTitle == "Commands",
        "web"       => sectionTitle == "Web",
        _           => true,
    };

    private static string? SectionToScopeId(string title) => title switch
    {
        "Applications" or "Settings" => "apps",
        "Files" => "files",
        "Clipboard" => "clipboard",
        "Commands" => "commands",
        "Web"      => "web",
        _ => null,
    };

    private static string SectionToScopeLabel(string title) => title switch
    {
        "Applications" or "Settings" => "Apps",
        "Commands" or "Actions"      => "Actions",
        "Web"                          => "Web",
        _                              => title,
    };

    private void UpdateFooterHint()
    {
        var r = SelectedResult;
        if (r is null)
        {
            FooterHint = string.Empty;
            return;
        }

        FooterHint = r.Type switch
        {
            ResultType.App       => "↵ Open  ·  Ctrl+↵ Admin  ·  Ctrl+Shift+E Reveal",
            ResultType.File      => "↵ Open  ·  Ctrl+Shift+E Reveal  ·  Ctrl+C Copy path",
            ResultType.Clipboard => "↵ Paste  ·  Ctrl+P Pin  ·  Delete Remove",
            ResultType.Action    => r.ActionId switch
            {
                "calc" or "color" or "currency" => "↵ Copy result",
                "web" or "url"                  => "↵ Open",
                _                               => "↵ Run",
            },
            _ => "↵ Open",
        };
    }

    private void UpdateActionPreview()
    {
        var result = SelectedResult;
        if (result?.Type != ResultType.Action || result.ActionId is null)
        {
            // Only clear if no action is currently executing (timer running / AI streaming)
            if (ActiveActionPanel is "timer" && _timer.TimerRunning) return;
            if (ActiveActionPanel is "ai" && !string.IsNullOrEmpty(_ai.AiText)) return;
            ActiveActionPanel = null;
            return;
        }

        ActiveActionPanel = result.ActionId;

        switch (result.ActionId)
        {
            case "timer":
                _timer.StartTimerPreview(Query);
                break;
            case "ip":
                _ = FetchIpPreviewAsync();
                break;
            default:
                break;
        }
    }

    private async Task FetchIpPreviewAsync()
    {
        if (_ipPreviewLoading) return;
        _ipPreviewLoading = true;

        var local = IpAction.GetLocalIp();
        IpPreviewText = $"Local: {local ?? "Not connected"}  ·  Public: fetching…";

        try
        {
            var pub = await IpAction.GetPublicIpAsync();
            IpPreviewText = $"Local: {local ?? "Not connected"}  ·  Public: {pub ?? "Unavailable"}";
        }
        catch
        {
            IpPreviewText = $"Local: {local ?? "Not connected"}  ·  Public: unavailable";
        }
        finally
        {
            _ipPreviewLoading = false;
        }
    }

    /// <summary>Close the action preview panel (called from XAML close button or Escape).</summary>
    public void CloseActionPanel()
    {
        ActiveActionPanel = null;
        ActionResultText = string.Empty;
        ActionResultSubText = string.Empty;
    }

    private void UpdateScopeFilters()
    {
        var counts = new Dictionary<string, (string Label, int Count)>();
        string? section = null;
        int sectionCount = 0;

        void AddSection()
        {
            if (section is null || sectionCount == 0) return;
            var id = SectionToScopeId(section);
            if (id is null) return;
            if (counts.TryGetValue(id, out var existing))
                counts[id] = (existing.Label, existing.Count + sectionCount);
            else
                counts[id] = (SectionToScopeLabel(section), sectionCount);
        }

        foreach (var item in _rawResults)
        {
            if (item is SectionLabel label)
            {
                AddSection();
                section = label.Title;
                sectionCount = 0;
            }
            else if (item is SearchResult)
            {
                sectionCount++;
            }
        }
        AddSection();

        ScopeFilters.Clear();
        if (counts.Count < 2) return;

        foreach (var (id, (label, count)) in counts)
            ScopeFilters.Add(new ScopeFilterItem { Id = id, Label = label, Count = count });

        if (!ScopeFilters.Any(s => s.Id == ActiveScopeId) && ActiveScopeId != "all")
            ActiveScopeId = "all";
    }

    private static string NormalizeUrl(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Contains("://", StringComparison.Ordinal) ? trimmed : $"https://{trimmed}";
    }

    private static string NormalizeWebQuery(string query)
    {
        var q = query.Trim();
        return q.StartsWith('?') ? q[1..].Trim() : q;
    }

    /// <summary>
    /// Detects action keywords at the start of the query (e.g. "sys ", "timer ").
    /// Returns the action ID, icon glyph, and stripped sub-query, or null.
    /// </summary>
    private (string actionId, string iconGlyph, string subQuery)? DetectActionKeyword(string query)
    {
        if (string.IsNullOrEmpty(query)) return null;

        // Use cached keyword map — rebuilt only when config changes (see OnConfigChanged)
        if (_keywordMap is null)
        {
            _keywordMap = new()
            {
                [Config.KeywordSystem]     = ("system",     "power"),
                [Config.KeywordColor]      = ("color",      "\ue790"),
                [Config.KeywordTimer]      = ("timer",      "\ue121"),
                [Config.KeywordIp]         = ("ip",         "\ue701"),
                [Config.KeywordAi]         = ("ai",         "\ue113"),
                [Config.KeywordCurrency]   = ("currency",   "\ue825"),
                [Config.KeywordPassword]   = ("pw",         "\ue722"),
                [Config.KeywordNote]       = ("note",       "\ue727"),
                [Config.KeywordKill]       = ("kill",       "\ue747"),
                [Config.KeywordScreenshot] = ("screenshot", "\ue74c"),
                [Config.KeywordClipboard]  = ("clipboard",  "clipboard"),
                [Config.KeywordFiles]      = ("files",      "\ue70a"),
                [Config.KeywordApps]       = ("apps",       "\ue71d"),
            };
        }

        foreach (var (keyword, (actionId, icon)) in _keywordMap)
        {
            if (string.IsNullOrEmpty(keyword)) continue;
            var prefix = keyword + " ";
            if (query.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var subQuery = query[prefix.Length..];
                return (actionId, icon, subQuery);
            }
        }
        return null;
    }

    private void RunSearch(string query, CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested) return;
            CancelActionWork();
            _ = SearchAsync(query, ct);
        }
        catch (Exception ex)
        {
            _log.Warning("RunSearch error", ex);
        }
    }

    private async Task SearchAsync(string query, CancellationToken ct)
    {
        try
        {
            var newResults = await _searchEngine.SearchAsync(query, ActiveCategory, ct);
            if (ct.IsCancellationRequested) return;

            Application.Current?.Dispatcher.Invoke(() => CommitResults(newResults));
        }
        catch (Exception ex)
        {
            _log.Warning("SearchAsync error", ex);
        }
    }




    // ═══════════════════════════════════════════════════════════════
    // Open / Execute
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    public async Task OpenSelected()
    {
        var result = SelectedResult;
        if (result is null) return;

        switch (result.Type)
        {
            case ResultType.App:
                if (result.LnkPath is not null)
                    Launch(result.LnkPath);
                else if (result.ExePath is not null)
                    Launch(result.ExePath);
                var appKey = result.ExePath ?? result.LnkPath ?? "";
                _freq.Increment(appKey);
                // Update both the result and the catalog entry so SuggestedApps picks it up
                var newScore = _freq.Get(appKey);
                result.FrequencyScore = newScore;
                HideAfterLaunch();
                break;

            case ResultType.File:
                if (result.FilePath is not null)
                    Launch(result.FilePath);
                HideAfterLaunch();
                break;

            case ResultType.Clipboard:
                if (result.ClipContent is not null)
                {
                    _clipboard.Add(result.ClipContent);   // promote to top first
                    _clipboard.CopyToSystem(result.ClipContent);
                }
                HideAfterLaunch();
                break;

            case ResultType.Action:
                // System commands: shutdown / restart / sleep / lock etc.
                if (result.ActionId == "system")
                {
                    SystemAction.Execute(Query);
                    return;
                }
                if (result.ActionId == "timer")
                {
                    _timer.StartTimerPreview(Query);
                    Timer.StartCommand.Execute(null);
                    ActiveActionPanel = "timer";
                    // panel stays visible so the user sees the countdown
                }
                else if (result.ActionId == "ai")
                {
                    try
                    {
                        ActiveActionPanel = "ai";
                        await _ai.StartAiAsync(Query);
                        // response streams into AiChat.AiText — panel stays visible
                    }
                    catch (Exception ex) { _log.Warning("StartAiAsync error", ex); }
                }
                // Calc/Color/IP: Enter copies result to clipboard
                else if (result.ActionId == "calc")
                {
                    var calcResult = result.Name.TrimStart('=', ' ');
                    _clipboard.CopyToSystem(calcResult);
                    ActionResultText = calcResult;
                    ActionResultSubText = "Copied to clipboard";
                    ActiveActionPanel = "calc";
                }
                else if (result.ActionId == "color")
                {
                    _clipboard.CopyToSystem(result.Name);
                    ActionResultText = result.Name;
                    ActionResultSubText = "Copied to clipboard";
                    ActiveActionPanel = "color";
                }
                else if (result.ActionId == "ip")
                {
                    var local = IpAction.GetLocalIp() ?? "Not connected";
                    var pub = await IpAction.GetPublicIpAsync();
                    var text = $"{local} · {pub ?? "Unavailable"}";
                    _clipboard.CopyToSystem(text);
                    ActionResultText = text;
                    ActiveActionPanel = "ip";
                }
                // Settings: open settings panel when clicked
                else if (result.ActionId == "settings")
                {
                    OpenSettingsRequested?.Invoke();
                    return;
                }
                else if (result.ActionId == "url")
                {
                    Launch(NormalizeUrl(Query));
                    HideAfterLaunch();
                }
                else if (result.ActionId == "web")
                {
                    var q = NormalizeWebQuery(Query);
                    Launch($"https://www.google.com/search?q={Uri.EscapeDataString(q)}");
                    HideAfterLaunch();
                }
                else if (result.ActionId == "shell")
                {
                    var command = Query.StartsWith(">", StringComparison.Ordinal) ? Query[1..].Trim() : Query.Trim();
                    if (!string.IsNullOrWhiteSpace(command))
                        Process.Start(new ProcessStartInfo("cmd.exe", $"/c {command}") { UseShellExecute = false, CreateNoWindow = true });
                    HideAfterLaunch();
                }
                // Screenshot: capture and save
                else if (result.ActionId == "screenshot")
                {
                    var path = ScreenshotAction.Execute();
                    ActionResultText = "Screenshot saved";
                    ActionResultSubText = path ?? "Pictures folder";
                    ActiveActionPanel = "screenshot";
                }
                // Kill Process: force-close by name
                else if (result.ActionId == "kill")
                {
                    var killed = KillProcessAction.Execute(Query);
                    ActionResultText = $"Killed {killed} process(es)";
                    ActiveActionPanel = "kill";
                }
                // Password Gen: generate and copy
                else if (result.ActionId == "pw")
                {
                    var pw = PasswordGenAction.Generate(Query);
                    _clipboard.CopyToSystem(pw);
                    ActionResultText = pw;
                    ActionResultSubText = "Password copied to clipboard";
                    ActiveActionPanel = "pw";
                }
                // Quick Note: save and open
                else if (result.ActionId == "note")
                {
                    var path = QuickNoteAction.Execute(Query);
                    ActionResultText = "Note saved";
                    ActionResultSubText = path ?? "Documents\\Spur\\notes.txt";
                    ActiveActionPanel = "note";
                }
                // Currency: fetch conversion and copy
                else if (result.ActionId == "currency")
                {
                    try
                    {
                        var result2 = await CurrencyAction.ConvertAsync(Query);
                        if (result2 is not null)
                        {
                            _clipboard.CopyToSystem(result2);
                            ActionResultText = result2;
                            ActiveActionPanel = "currency";
                        }
                    }
                    catch (Exception ex) { _log.Warning("Currency error", ex); }
                }
                break;
        }
    }

    [RelayCommand]
    public void OpenFolder()
    {
        var result = SelectedResult;
        if (result is null) return;

        switch (result.Type)
        {
            case ResultType.Clipboard:
                // Ctrl+Enter on clipboard: copy without hiding window
                if (result.ClipContent is not null)
                    _clipboard.CopyToSystem(result.ClipContent);
                return;

            case ResultType.App:
            case ResultType.File:
                break;

            default:
                return;
        }

        string? targetPath = result.Type switch
        {
            ResultType.App  => result.ExePath ?? result.LnkPath,
            ResultType.File => result.FilePath,
            _               => null,
        };

        if (!string.IsNullOrWhiteSpace(targetPath) && File.Exists(targetPath))
            Process.Start("explorer.exe", $"/select,\"{targetPath}\"");
    }

    [RelayCommand]
    public void CopySelectedPath()
    {
        var result = SelectedResult;
        if (result is null) return;

        string? path = result.Type switch
        {
            ResultType.App  => result.ExePath ?? result.LnkPath,
            ResultType.File => result.FilePath,
            _               => null,
        };

        if (!string.IsNullOrWhiteSpace(path))
            _clipboard.CopyToSystem(path);
    }

    /// <summary>Launches the selected app as administrator (UAC elevation).</summary>
    [RelayCommand]
    public void RunAsAdmin()
    {
        var result = SelectedResult;
        if (result is null) return;

        string? target = result.Type switch
        {
            ResultType.App  => result.ExePath,
            ResultType.File => result.FilePath,
            _               => null,
        };

        if (target is null) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName  = target,
                UseShellExecute = true,
                Verb      = "runas",
            });
        }
        catch (Exception ex)
        {
            _log.Warning("RunAsAdmin failed", ex);
        }
    }

    /// <summary>Pins or unpins the given result. Persists to config.</summary>
    [RelayCommand]
    public void TogglePin(SearchResult? result)
    {
        if (result is null) return;
        if (Config.PinnedItems.Contains(result.Id))
            Config.PinnedItems.Remove(result.Id);
        else
            Config.PinnedItems.Add(result.Id);

        result.IsPinned = Config.PinnedItems.Contains(result.Id);
        _configSvc.Save(Config);

        var idx = Results.IndexOf(result);
        if (idx >= 0)
        {
            Results.RemoveAt(idx);
            Results.Insert(idx, result);
        }
    }

    public void SaveConfig() => _configSvc.Save(Config);

    /// <summary>Called when the user clicks the clipboard category button.</summary>
    public void ActivateClipboardCategory()
    {
        ActiveCategory = ActiveCategory == "clipboard" ? null : "clipboard";
        if (ActiveCategory == "clipboard" && !string.IsNullOrEmpty(Query))
        {
            Query = string.Empty;
        }
    }

    /// <summary>Clears the clipboard history, preserving pinned items.</summary>
    [RelayCommand]
    public void ClearClipboard()
    {
        var pinned = Config.PinnedClipboard.Select(p => p.Content).ToHashSet();
        _clipboard.KeepOnly(pinned);
        OnQueryChanged(Query ?? string.Empty);
    }

    /// <summary>Removes a single clipboard item from history by its content.</summary>
    [RelayCommand]
    public void RemoveClipboardItem(SearchResult result)
    {
        if (result.Type != ResultType.Clipboard || result.ClipContent is null) return;
        var keep = _clipboard.GetHistory()
            .Where(e => e.Content != result.ClipContent)
            .Select(e => e.Content)
            .ToHashSet();
        _clipboard.KeepOnly(keep);
        OnQueryChanged(Query ?? string.Empty);
    }

    private void Launch(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { _log.Warning("Launch failed", ex); }
    }

    // ═══════════════════════════════════════════════════════════════
    // Keyboard navigation
    // ═══════════════════════════════════════════════════════════════

    public void MoveSelection(int delta)
    {
        if (Results.Count == 0) return;

        var next = SelectedIndex + delta;
        // Skip section labels
        while (next >= 0 && next < Results.Count && Results[next] is SectionLabel)
            next += delta;

        if (next >= 0 && next < Results.Count)
            SelectedIndex = next;
    }

    public void CycleCategory()
    {
        ActiveCategory = ActiveCategory switch
        {
            null        => "files",
            "files"     => "clipboard",
            "clipboard" => "actions",
            _           => null,
        };
    }
    // ═══════════════════════════════════════════════════════════════

    // ═══════════════════════════════════════════════════════════════
    // Command Palette
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Exposed for the command palette overlay.</summary>
    public CommandPaletteViewModel CommandPalette { get; }

    private void PopulatePaletteCommands()
    {
        _registry.Register(new CommandPaletteEntry
        {
            Id = "toggle-theme",
            Label = "Toggle Theme",
            Description = "Switch between dark and light mode",
            IconGlyph = "\ue706",
            Execute = () =>
            {
                var current = Config.Theme;
                var next = current switch
                {
                    "dark" => "light",
                    "light" => "dark",
                    "system" => "dark",
                    _ => "dark"
                };
                Config.Theme = next;
                _themeManager.Apply(next);
            }
        });

        _registry.Register(new CommandPaletteEntry
        {
            Id = "open-settings",
            Label = "Open Settings",
            Description = "Open the settings window",
            IconGlyph = "\ue713",
            Execute = () => OpenSettingsRequested?.Invoke()
        });

        _registry.Register(new CommandPaletteEntry
        {
            Id = "clear-clipboard",
            Label = "Clear Clipboard History",
            Description = "Remove all clipboard entries",
            IconGlyph = "\ue74d",
            Execute = ClearClipboard
        });

        _registry.Register(new CommandPaletteEntry
        {
            Id = "cycle-scope",
            Label = "Cycle Search Scope",
            Description = "Switch between All, Files, Commands, Clipboard",
            IconGlyph = "\ue72c",
            Execute = CycleScope
        });

        _registry.Register(new CommandPaletteEntry
        {
            Id = "open-folder",
            Label = "Open Containing Folder",
            Description = "Open the folder of the selected item",
            IconGlyph = "\ue838",
            Execute = OpenFolder
        });

        _registry.Register(new CommandPaletteEntry
        {
            Id = "copy-path",
            Label = "Copy Path",
            Description = "Copy the selected item path to clipboard",
            IconGlyph = "\ue8c8",
            Execute = CopySelectedPath
        });

        _registry.Register(new CommandPaletteEntry
        {
            Id = "run-as-admin",
            Label = "Run as Administrator",
            Description = "Launch the selected app with elevated privileges",
            IconGlyph = "\ueea1",
            Execute = RunAsAdmin
        });
    }


    [RelayCommand]
    public void OpenSettings()
    {
        OpenSettingsRequested?.Invoke();
    }

    // ═══════════════════════════════════════════════════════════════
    // Window events
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Raised when the VM wants the window to hide itself.</summary>

    public event Action? RequestHide;

    /// <summary>Raised when the user requests the Settings window.</summary>
    public event Action? OpenSettingsRequested;

    public void Shutdown()
    {
        _freq.Flush();
        _freq.Dispose();
        _searchCts?.Dispose();
        _ai.CancelPending();
        _timer.Stop();
    }

    public void OnWindowShown() { }

    public void Reset()
    {
        if (Config.LastQueryStyle == "clear")
            Query = string.Empty;
        ActiveCategory = null;
        ActiveScopeId = "all";
        IsSettingsOpen = false;
        CancelActionWork();

        if (string.IsNullOrEmpty(Query))
            ClearIdleState();
    }

    private void HideAfterLaunch()
    {
        if (Config.CloseAfterLaunch)
            RequestHide?.Invoke();
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    private async Task LoadAppsAsync()
    {
        CatalogLoading = true;
        try
        {
            await _apps.DiscoverAsync();
        }
        catch (Exception ex)
        {
            _log.Warning("App catalog load failed", ex);
        }
        finally
        {
            CatalogLoading = false;
        }
    }

    /// <summary>
    /// Deletes the on-disk catalog cache and re-runs a full discovery so that
    /// apps installed since the last scan become visible immediately.
    /// </summary>
    public async Task RefreshAppCatalogAsync()
    {
        _apps.ClearCache();
        await LoadAppsAsync();
        // Re-run the current query so results update instantly
        if (!string.IsNullOrEmpty(Query))
            OnPropertyChanged(nameof(Query));
    }

    public void ClearActiveMode()
    {
        ActiveCategory = null;
        SelectedIndex = -1;
    }

    private void ClearAll()
    {
        Results.Clear();
        SelectedIndex = -1;
        CancelActionWork();
    }

    private void CancelActionWork()
    {
        _ai.CancelPending();
        _timer.Stop();
    }

    private int FindFirstResultIndex()
    {
        for (int i = 0; i < Results.Count; i++)
            if (Results[i] is SearchResult) return i;
        return -1;
    }

    private static SearchResult Clone(SearchResult s) => new()
    {
        Id = s.Id, Type = s.Type, Name = s.Name, Subtitle = s.Subtitle,
        IconPath = s.IconPath, IconGlyph = s.IconGlyph,
        Score = s.Score, FrequencyScore = s.FrequencyScore,
        ExePath = s.ExePath, LnkPath = s.LnkPath,
        FilePath = s.FilePath, FileExtension = s.FileExtension,
        ClipContent = s.ClipContent, ClipTimestamp = s.ClipTimestamp,
        ActionId = s.ActionId,
    };
}


