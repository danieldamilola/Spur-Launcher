using System.Collections.ObjectModel;
using Arc.Extensions;
using Arc.Services;
using Arc.Models;

namespace Arc.ViewModels;

// ═══════════════════════════════════════════════════════════════════
// MainViewModel — central orchestrator for the Arc launcher
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// Central orchestrator for the Arc launcher.
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

    // ── Sub-ViewModels ───────────────────────────────────────────────
    private readonly AiChatViewModel    _ai;
    private readonly TimerViewModel     _timer;
    private readonly ClipboardViewModel _clipboardVm;

    private static readonly IAction[] Actions =
    [
        new CalculatorAction(),
        new ColorAction(),
        new TimerAction(),
        new IpAction(),
        new AiAction(),
        new SettingsAction(),
        new SystemAction(),
        new CurrencyAction(),
        new PasswordGenAction(),
        new QuickNoteAction(),
        new KillProcessAction(),
        new ScreenshotAction(),
    ];

    // ── App catalog (loaded once on startup; volatile for safe cross-thread reads) ──
    private volatile IReadOnlyList<SearchResult> _appCatalog = [];

    // ── Catalog loading state ────────────────────────────────────────
    /// <summary>True while the app catalog is being discovered.</summary>
    [ObservableProperty]
    private bool _catalogLoading;

    // ── Search debounce ──────────────────────────────────────────────
    private CancellationTokenSource? _searchCts;

    // ── Constructor ──────────────────────────────────────────────────
    public MainViewModel(
        ArcConfig             config,
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
        CommandPaletteViewModel commandPalette)
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

        Config   = config;
        Settings = new SettingsViewModel(Config, _configSvc, this, _themeManager, _startupService);

        // Push initial config to services
        _files.MaxDepth     = Config.MaxFileDepth;
        _clipboard.MaxItems = Config.ClipboardHistorySize;

        // Sub-ViewModels
        _ai          = new AiChatViewModel(_aiService, Config);
        _timer       = new TimerViewModel(_notification);
        _clipboardVm = new ClipboardViewModel(_clipboard, Config, _configSvc);

        // Forward sub-VM property changes for backward-compatible bindings
        _ai.PropertyChanged    += (_, args) => OnPropertyChanged(args.PropertyName);
        _timer.PropertyChanged += (_, args) => OnPropertyChanged(args.PropertyName);

        CommandPalette = commandPalette;

        PopulatePaletteCommands();

        _ = LoadAppsAsync();
        Results.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasResults));
    }

    /// <summary>Pushes config values to services whenever the config object is replaced.</summary>
    partial void OnConfigChanged(ArcConfig value)
    {
        _files.MaxDepth     = value.MaxFileDepth;
        _clipboard.MaxItems = value.ClipboardHistorySize;
    }

    // ═══════════════════════════════════════════════════════════════
    // Observable properties
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQuery))]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    private string _query = string.Empty;

    [ObservableProperty]
    private ArcConfig _config;

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

    [ObservableProperty]
    private bool _isSettingsOpen;

    // Sub-ViewModels — exposed for direct XAML binding (no pass-throughs)
    public TimerViewModel      Timer      => _timer;
    public AiChatViewModel     AiChat     => _ai;
    public ClipboardViewModel  Clipboard  => _clipboardVm;

    public void CancelSearch() => _searchCts?.Cancel();

    // ═══════════════════════════════════════════════════════════════
    // Computed properties
    // ═══════════════════════════════════════════════════════════════

    public bool HasQuery          => !string.IsNullOrEmpty(Query);
    public bool HasResults         => Results.Count > 0;
    public bool IsBrowsePanelVisible => ActiveCategory is not null;

    /// <summary>Hub removed per ux.md — always false.</summary>
    public bool IsHubVisible => false;

    public bool IsScopeBarVisible => ScopeFilters.Count >= 2 && !string.IsNullOrEmpty(Query) && ActiveCategory is null;

    public string SearchPlaceholder => ActiveCategory switch
    {
        "files"     => "Search files…",
        "actions"   => "Search actions…",
        "clipboard" => "Filter clipboard…",
        _           => "Search apps, files, actions…",
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

            if (string.IsNullOrEmpty(value) && ActiveCategory is null)
            {
                ClearIdleState();
                return;
            }

            ActiveScopeId = "all";

            var delay = Task.Delay(150, ct);
            delay.ContinueWith(_ =>
            {
                if (!ct.IsCancellationRequested)
                    Application.Current?.Dispatcher.InvokeAsync(() => RunSearch(value, ct));
            }, TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            _log.Warning("OnQueryChanged error", ex);
        }
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

    partial void OnSelectedIndexChanged(int value) => UpdateFooterHint();

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

    // ═══════════════════════════════════════════════════════════════
    // Search pipeline
    // ═══════════════════════════════════════════════════════════════

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
            var newResults = new List<object>();
            bool isBrowseMode = ActiveCategory is not null;

            // ── Apps ──────────────────────────────────────────────────
            bool showApps = Config.IndexApps && (ActiveCategory is null or "apps");
            if (showApps && _appCatalog is not null)
            {
                var appMatches = _appCatalog
                    .Select(a =>
                    {
                        var score = MatchScore(query, a.Name);
                        if (score < 0) return null;
                        var clone = Clone(a);
                        var freqBoost = Math.Log2(a.FrequencyScore + 1) * 0.5;
                        clone.Score = score + freqBoost;
                        if (Config.PinnedItems.Contains(a.Id))
                        {
                            clone.IsPinned = true;
                            clone.Score += 10000;
                        }
                        return clone;
                    })
                    .Where(a => a is not null)
                    .Cast<SearchResult>()
                    .OrderByDescending(a => a.Score);

                // In browse mode (clicked Apps circle), show ALL apps; otherwise limit
                var appList = isBrowseMode
                    ? appMatches.ToList()
                    : appMatches.Take(Config.ResultsCount).ToList();

                if (appList.Count > 0)
                {
                    newResults.Add(new SectionLabel("Applications"));
                    newResults.AddRange(appList);
                }
            }

            if (ct.IsCancellationRequested) return;

            // ── Inline answers (typing only) ───────────────────────────
            if (ActiveCategory is null)
            {
                foreach (var inline in BuildInlineResults(query))
                    newResults.Add(inline);
            }

            // ── Files ─────────────────────────────────────────────────
            bool showFiles = Config.FileSearchEnabled && (ActiveCategory is null or "files");
            if (showFiles)
            {
                int fileLimit = isBrowseMode ? 50 : Config.ResultsCount;

                List<SearchResult> fileMatches;
                if (string.IsNullOrEmpty(query) && ActiveCategory == "files")
                {
                    // Browse mode with no query: show recent files
                    fileMatches = await _files.BrowseRecentAsync(fileLimit);
                }
                else
                {
                    fileMatches = await _files.SearchAsync(query, fileLimit, ct);
                }

                if (ct.IsCancellationRequested) return;
                if (fileMatches.Count > 0)
                {
                    newResults.Add(new SectionLabel("Files"));
                    newResults.AddRange(fileMatches);
                }
            }

            // ── Clipboard ─────────────────────────────────────────────
            bool showClip = Config.ClipboardEnabled && Config.IndexClipboard && ActiveCategory == "clipboard";
            if (showClip)
            {
                int limit = isBrowseMode ? 50 : Config.ResultsCount;
                var clips = _clipboard.GetHistory()
                    .Where(c => string.IsNullOrEmpty(query) || MatchScore(query, c.Preview) >= 0)
                    .Take(limit)
                    .Select(c => new SearchResult
                    {
                        Id         = $"clip:{c.Timestamp.Ticks}",
                        Type       = ResultType.Clipboard,
                        Name       = c.Preview,
                        Subtitle   = c.TimeAgo,
                        LucideIcon = c.IsImage ? "image" : "clipboard",
                        ClipContent = c.Content,
                        ClipTimestamp = c.Timestamp,
                        ClipImage = c.Image,
                    })
                    .Select(c =>
                    {
                        if (Config.PinnedItems.Contains(c.Id))
                        {
                            c.IsPinned = true;
                            c.Score = 10000; // Force to top
                        }
                        return c;
                    })
                    .OrderByDescending(c => c.IsPinned)
                    .ToList();

                if (clips.Count > 0)
                {
                    newResults.Add(new SectionLabel("Clipboard"));
                    newResults.AddRange(clips);
                }
            }

            // ── Actions ───────────────────────────────────────────────
            bool showActions = ActiveCategory is null or "actions";
            if (showActions)
            {
                var availableActions = ArcConstants.StaticActions.Where(a => IsActionEnabled(a.ActionId));

                var actionMatches = availableActions
                    .Where(a => MatchScore(query, a.Name) >= 0)
                    .Select(a => { a.Score = MatchScore(query, a.Name); return a; })
                    .ToList();

                foreach (var kw in BuildKeywordActionResults(query))
                {
                    if (actionMatches.All(a => a.ActionId != kw.ActionId))
                        actionMatches.Add(kw);
                }

                AddDynamicAction(actionMatches, BuildShellAction(query));

                actionMatches = actionMatches.OrderByDescending(a => a.Score).ToList();

                if (actionMatches.Count > 0)
                {
                    newResults.Add(new SectionLabel("Commands"));
                    newResults.AddRange(actionMatches);
                }
            }

            // ── Windows Settings (only when there is a query) ───────────
            if (Config.IndexWindowsSettings && !string.IsNullOrEmpty(query) && (ActiveCategory is null or "apps"))
            {
                var settingsMatches = ArcConstants.WindowsSettings
                    .Select(s =>
                    {
                        var sc = MatchScore(query, s.Name);
                        if (sc < 0) return null;
                        var c = Clone(s); c.Score = sc; return c;
                    })
                    .Where(s => s is not null)
                    .Cast<SearchResult>()
                    .OrderByDescending(s => s.Score)
                    .Take(4)
                    .ToList();

                if (settingsMatches.Count > 0)
                {
                    newResults.Add(new SectionLabel("Settings"));
                    newResults.AddRange(settingsMatches);
                }
            }

            if (ct.IsCancellationRequested) return;

            if (ActiveCategory is null && ShouldOfferWebFallback(query, newResults))
            {
                var web = BuildWebSearchAction(query);
                if (web is not null)
                {
                    newResults.Add(new SectionLabel("Web"));
                    newResults.Add(web);
                }
            }

            // Commit results on UI thread
            Application.Current?.Dispatcher.Invoke(() => CommitResults(newResults));
        }
        catch (Exception ex)
        {
            _log.Warning("SearchAsync error", ex);
        }
    }

    private double MatchScore(string query, string target)
    {
        if (string.IsNullOrEmpty(query)) return 0;
        var score = Config.FuzzySearch
            ? FuzzySearch.Score(query, target)
            : (target.Contains(query, StringComparison.OrdinalIgnoreCase) ? 1 : -1);
        return score >= MinMatchScore() ? score : -1;
    }

    private double MinMatchScore() => Config.QuerySearchPrecision switch
    {
        "low" => 0,
        "strict" => 1.2,
        _ => 0.35,
    };

    private bool IsActionEnabled(string? id) => id switch
    {
        "calc"       => Config.IndexCalculator,
        "color"      => Config.ActionColor,
        "timer"      => Config.ActionTimer,
        "ip"         => Config.ActionIp,
        "ai"         => Config.ActionAi,
        "currency"   => Config.ActionCurrency,
        "pw"         => Config.ActionPasswordGen,
        "note"       => Config.ActionQuickNote,
        "kill"       => Config.ActionKillProcess,
        "screenshot" => Config.ActionScreenshot,
        "system"     => Config.IndexSystemCommands,
        "settings"   => Config.IndexWindowsSettings,
        "url"        => Config.IndexUrls,
        "web"        => Config.IndexWebSearches,
        "shell"      => Config.IndexShell,
        _            => true,
    };

    private static void AddDynamicAction(List<SearchResult> actions, SearchResult? action)
    {
        if (action is not null)
            actions.Insert(0, action);
    }

    private SearchResult? BuildUrlAction(string query)
    {
        if (!Config.IndexUrls || !LooksLikeUrl(query)) return null;
        return new SearchResult
        {
            Id = $"url:{query}",
            Type = ResultType.Action,
            Name = $"Open {query}",
            Subtitle = "Open URL",
            LucideIcon = "globe",
            ActionId = "url",
            Score = 500,
        };
    }

    private SearchResult? BuildWebSearchAction(string query)
    {
        var q = NormalizeWebQuery(query);
        if (!Config.IndexWebSearches || string.IsNullOrWhiteSpace(q)) return null;
        return new SearchResult
        {
            Id = $"web:{q}",
            Type = ResultType.Action,
            Name = $"Search the web for \"{q}\"",
            Subtitle = "Web",
            LucideIcon = "search",
            ActionId = "web",
            Score = 50,
        };
    }

    private SearchResult? BuildShellAction(string query)
    {
        if (!Config.IndexShell || !query.StartsWith(">", StringComparison.Ordinal)) return null;
        var command = query[1..].Trim();
        if (command.Length == 0) return null;
        return new SearchResult
        {
            Id = $"shell:{command}",
            Type = ResultType.Action,
            Name = $"Run {command}",
            Subtitle = "Shell command",
            LucideIcon = "terminal",
            ActionId = "shell",
            Score = 600,
        };
    }

    private static bool LooksLikeUrl(string query)
        => Uri.TryCreate(NormalizeUrl(query), UriKind.Absolute, out var uri)
           && uri.Scheme is "http" or "https";

    private static string NormalizeWebQuery(string query)
    {
        var q = query.Trim();
        return q.StartsWith('?') ? q[1..].Trim() : q;
    }

    private IEnumerable<SearchResult> BuildInlineResults(string query)
    {
        foreach (var id in new[] { "calc", "color", "currency" })
        {
            var action = Actions.FirstOrDefault(a => a.Id == id);
            if (action is null || !IsActionEnabled(id) || !action.CanHandle(query)) continue;
            yield return action.BuildResult(query);
        }

        var url = BuildUrlAction(query);
        if (url is not null)
            yield return url;
    }

    private IEnumerable<SearchResult> BuildKeywordActionResults(string query)
    {
        foreach (var action in Actions)
        {
            if (action.Id is "calc" or "color" or "currency" or "ai") continue;
            if (!IsActionEnabled(action.Id) || !action.CanHandle(query)) continue;
            yield return action.BuildResult(query);
        }
    }

    private static bool ShouldOfferWebFallback(string query, List<object> results)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;
        var localCount = results.OfType<SearchResult>().Count();
        return localCount < 3;
    }

    private static string NormalizeUrl(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Contains("://", StringComparison.Ordinal)
            ? trimmed
            : $"https://{trimmed}";
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
                var catalogEntry = _appCatalog.FirstOrDefault(a =>
                    string.Equals(a.ExePath, appKey, StringComparison.OrdinalIgnoreCase));
                if (catalogEntry is not null)
                    catalogEntry.FrequencyScore = newScore;
                HideAfterLaunch();
                break;

            case ResultType.File:
                if (result.FilePath is not null)
                    Launch(result.FilePath);
                HideAfterLaunch();
                break;

            case ResultType.Clipboard:
                if (result.ClipContent is not null)
                    _clipboard.CopyToSystem(result.ClipContent);
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
                    Timer.StartCommand.Execute(null);
                    _notification.Show("Timer", Timer.TimerDisplay);
                    HideAfterLaunch();
                }
                else if (result.ActionId == "ai")
                {
                    try
                    {
                        await _ai.StartAiAsync(Query);
                        if (!string.IsNullOrEmpty(_ai.AiText))
                            _clipboard.CopyToSystem(_ai.AiText);
                        _notification.Show("AI", string.IsNullOrEmpty(_ai.AiError) ? "Response copied" : _ai.AiError);
                    }
                    catch (Exception ex) { _log.Warning("StartAiAsync error", ex); }
                    HideAfterLaunch();
                }
                // Calc/Color/IP: Enter copies result to clipboard
                else if (result.ActionId == "calc")
                {
                    _clipboard.CopyToSystem(result.Name.TrimStart('=', ' '));
                    HideAfterLaunch();
                }
                else if (result.ActionId == "color")
                {
                    _clipboard.CopyToSystem(result.Name);
                    HideAfterLaunch();
                }
                else if (result.ActionId == "ip")
                {
                    var local = IpAction.GetLocalIp() ?? "Not connected";
                    var pub = await IpAction.GetPublicIpAsync();
                    _clipboard.CopyToSystem($"{local} / {pub ?? "Unavailable"}");
                    _notification.Show("IP Address", $"{local} · {pub ?? "Unavailable"}");
                    HideAfterLaunch();
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
                    ScreenshotAction.Execute();
                    HideAfterLaunch();
                }
                // Kill Process: force-close by name
                else if (result.ActionId == "kill")
                {
                    var killed = KillProcessAction.Execute(Query);
                    _notification.Show($"Killed {killed} process(es)", "");
                    HideAfterLaunch();
                }
                // Password Gen: generate and copy
                else if (result.ActionId == "pw")
                {
                    var pw = PasswordGenAction.Generate(Query);
                    _clipboard.CopyToSystem(pw);
                    _notification.Show("Password copied", $"{pw.Length} characters");
                    HideAfterLaunch();
                }
                // Quick Note: save and open
                else if (result.ActionId == "note")
                {
                    QuickNoteAction.Execute(Query);
                    _notification.Show("Note saved", "Documents\\Arc\\notes.txt");
                    HideAfterLaunch();
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
                            _notification.Show("Currency", result2);
                        }
                    }
                    catch (Exception ex) { _log.Warning("Currency error", ex); }
                    HideAfterLaunch();
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

        // Refresh display so pin icon updates
        var idx = Results.IndexOf(result);
        if (idx >= 0)
        {
            Results.RemoveAt(idx);
            Results.Insert(idx, result);
        }
    }

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
            LucideIcon = "SunMoon",
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
            LucideIcon = "Settings",
            Execute = () => OpenSettingsRequested?.Invoke()
        });

        _registry.Register(new CommandPaletteEntry
        {
            Id = "clear-clipboard",
            Label = "Clear Clipboard History",
            Description = "Remove all clipboard entries",
            LucideIcon = "Trash2",
            Execute = ClearClipboard
        });

        _registry.Register(new CommandPaletteEntry
        {
            Id = "cycle-scope",
            Label = "Cycle Search Scope",
            Description = "Switch between All, Files, Commands, Clipboard",
            LucideIcon = "RefreshCw",
            Execute = CycleScope
        });

        _registry.Register(new CommandPaletteEntry
        {
            Id = "open-folder",
            Label = "Open Containing Folder",
            Description = "Open the folder of the selected item",
            LucideIcon = "FolderOpen",
            Execute = OpenFolder
        });

        _registry.Register(new CommandPaletteEntry
        {
            Id = "copy-path",
            Label = "Copy Path",
            Description = "Copy the selected item path to clipboard",
            LucideIcon = "Copy",
            Execute = CopySelectedPath
        });

        _registry.Register(new CommandPaletteEntry
        {
            Id = "run-as-admin",
            Label = "Run as Administrator",
            Description = "Launch the selected app with elevated privileges",
            LucideIcon = "Shield",
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
            _appCatalog = await _apps.DiscoverAsync();

            // Apply persisted frequency scores
            foreach (var app in _appCatalog)
                if (app.ExePath is not null)
                    app.FrequencyScore = _freq.Get(app.ExePath);

            // Icons load lazily on demand via PathToIconConverter / GetIcon
        }
        catch (Exception ex)
        {
            _log.Warning("App catalog load failed", ex);
            _appCatalog = [];
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
        IconPath = s.IconPath, LucideIcon = s.LucideIcon,
        Score = s.Score, FrequencyScore = s.FrequencyScore,
        ExePath = s.ExePath, LnkPath = s.LnkPath,
        FilePath = s.FilePath, FileExtension = s.FileExtension,
        ClipContent = s.ClipContent, ClipTimestamp = s.ClipTimestamp,
        ActionId = s.ActionId,
    };
}
