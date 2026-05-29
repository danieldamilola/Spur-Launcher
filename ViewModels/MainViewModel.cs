using System.Collections.ObjectModel;
using Arc.Extensions;
using Arc.Services;
using Arc.ViewModels;

namespace Arc.ViewModels;

/// <summary>
/// Central orchestrator for the Arc launcher.
/// Owns the search query, result list, selection, category filter,
/// and all action state (AI streaming, timer countdown, color/IP data).
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    // ── Services ──────────────────────────────────────────────────────
    private readonly ILogger _log;
    private readonly AppDiscoveryService  _apps;
    private readonly FileSearchService    _files     = new();
    private readonly FrequencyService     _freq;
    private readonly ConfigService        _configSvc;

    private static readonly IAction[] Actions =
    [
        new CalculatorAction(),
        new ColorAction(),
        new TimerAction(),
        new IpAction(),
        new AiAction(),
        new SettingsAction(),
        new SystemAction(),
    ];

    private static readonly SearchResult[] _windowsSettings =
    [
        new() { Id="ws:network",   Type=ResultType.App, Name="Network & Internet",        Subtitle="ms-settings:network",              LucideIcon="wifi",        ExePath="ms-settings:network" },
        new() { Id="ws:wifi",      Type=ResultType.App, Name="Wi-Fi Settings",             Subtitle="ms-settings:network-wifi",         LucideIcon="wifi",        ExePath="ms-settings:network-wifi" },
        new() { Id="ws:cellular",  Type=ResultType.App, Name="Cellular & Mobile Data",     Subtitle="ms-settings:network-cellular",     LucideIcon="signal",      ExePath="ms-settings:network-cellular" },
        new() { Id="ws:airplane",  Type=ResultType.App, Name="Airplane Mode",              Subtitle="ms-settings:network-airplanemode",  LucideIcon="plane",       ExePath="ms-settings:network-airplanemode" },
        new() { Id="ws:hotspot",  Type=ResultType.App, Name="Mobile Hotspot",             Subtitle="ms-settings:network-mobilehotspot", LucideIcon="share-2",     ExePath="ms-settings:network-mobilehotspot" },
        new() { Id="ws:bt",        Type=ResultType.App, Name="Bluetooth & Devices",        Subtitle="ms-settings:bluetooth",            LucideIcon="bluetooth",   ExePath="ms-settings:bluetooth" },
        new() { Id="ws:display",   Type=ResultType.App, Name="Display & Brightness",       Subtitle="ms-settings:display",              LucideIcon="monitor",     ExePath="ms-settings:display" },
        new() { Id="ws:sound",     Type=ResultType.App, Name="Sound & Audio",              Subtitle="ms-settings:sound",                LucideIcon="volume-2",    ExePath="ms-settings:sound" },
        new() { Id="ws:power",     Type=ResultType.App, Name="Power, Sleep & Lid",         Subtitle="ms-settings:powersleep",           LucideIcon="battery",     ExePath="ms-settings:powersleep" },
        new() { Id="ws:battery",   Type=ResultType.App, Name="Battery Saver",              Subtitle="ms-settings:batterysaver",         LucideIcon="battery",     ExePath="ms-settings:batterysaver" },
        new() { Id="ws:update",    Type=ResultType.App, Name="Windows Update",             Subtitle="ms-settings:windowsupdate",        LucideIcon="refresh-cw",  ExePath="ms-settings:windowsupdate" },
        new() { Id="ws:privacy",   Type=ResultType.App, Name="Privacy & Security",         Subtitle="ms-settings:privacy",              LucideIcon="shield",      ExePath="ms-settings:privacy" },
        new() { Id="ws:apps",      Type=ResultType.App, Name="Apps & Features",            Subtitle="ms-settings:appsfeatures",         LucideIcon="package",     ExePath="ms-settings:appsfeatures" },
        new() { Id="ws:default",   Type=ResultType.App, Name="Default Apps",               Subtitle="ms-settings:defaultapps",          LucideIcon="layout-grid", ExePath="ms-settings:defaultapps" },
        new() { Id="ws:notif",     Type=ResultType.App, Name="Notifications & Alerts",     Subtitle="ms-settings:notifications",        LucideIcon="bell",        ExePath="ms-settings:notifications" },
        new() { Id="ws:themes",    Type=ResultType.App, Name="Personalization & Themes",   Subtitle="ms-settings:personalization",      LucideIcon="palette",     ExePath="ms-settings:personalization" },
        new() { Id="ws:accounts",  Type=ResultType.App, Name="Accounts & Users",           Subtitle="ms-settings:accounts",             LucideIcon="user",        ExePath="ms-settings:accounts" },
        new() { Id="ws:datetime",  Type=ResultType.App, Name="Date, Time & Clock",         Subtitle="ms-settings:dateandtime",          LucideIcon="clock",       ExePath="ms-settings:dateandtime" },
        new() { Id="ws:region",    Type=ResultType.App, Name="Language & Region",          Subtitle="ms-settings:regionlanguage",       LucideIcon="globe",       ExePath="ms-settings:regionlanguage" },
        new() { Id="ws:access",    Type=ResultType.App, Name="Ease of Access",             Subtitle="ms-settings:easeofaccess-display", LucideIcon="eye",         ExePath="ms-settings:easeofaccess-display" },
        new() { Id="ws:storage",   Type=ResultType.App, Name="Storage & Disk Space",       Subtitle="ms-settings:storagesense",         LucideIcon="hard-drive",  ExePath="ms-settings:storagesense" },
        new() { Id="ws:about",     Type=ResultType.App, Name="About This PC",              Subtitle="ms-settings:about",                LucideIcon="info",        ExePath="ms-settings:about" },
        new() { Id="ws:taskmgr",   Type=ResultType.App, Name="Task Manager",               Subtitle="taskmgr.exe",                      LucideIcon="activity",    ExePath="taskmgr.exe" },
        new() { Id="ws:devmgmt",   Type=ResultType.App, Name="Device Manager",             Subtitle="devmgmt.msc",                      LucideIcon="cpu",         ExePath="devmgmt.msc" },
    ];

    private static readonly SearchResult[] _staticActions =
    [
        new() { Id = "act:timer",    Type = ResultType.Action, Name = "Start Timer",   Subtitle = "Type 'timer 5m' to start a countdown",          LucideIcon = "timer",      ActionId = "timer"    },
        new() { Id = "act:calc",     Type = ResultType.Action, Name = "Calculator",    Subtitle = "Type a math expression like '100 / 4'",          LucideIcon = "calculator", ActionId = "calc"     },
        new() { Id = "act:color",    Type = ResultType.Action, Name = "Color Picker",  Subtitle = "Type a hex code like '#ff0055'",                 LucideIcon = "palette",    ActionId = "color"    },
        new() { Id = "act:ip",       Type = ResultType.Action, Name = "IP Address",    Subtitle = "Type 'ip' to show your public and local IP",     LucideIcon = "globe",      ActionId = "ip"       },
        new() { Id = "act:ai",       Type = ResultType.Action, Name = "Ask AI",        Subtitle = "Type 'ai what is the capital of France?'",       LucideIcon = "sparkles",   ActionId = "ai"       },
        new() { Id = "act:settings", Type = ResultType.Action, Name = "Settings",      Subtitle = "Open application settings",                     LucideIcon = "settings",   ActionId = "settings" },
    ];

    // ── App catalog (loaded once on startup) ─────────────────────────
    private List<SearchResult> _appCatalog = [];

    // ── Catalog loading state ────────────────────────────────────────
    /// <summary>True while the app catalog is being discovered.</summary>
    [ObservableProperty]
    private bool _catalogLoading;

    /// <summary>Apps sorted by frequency for the "Suggested" row in browse mode.</summary>
    public List<SearchResult> SuggestedApps => _appCatalog
        .Where(a => a.FrequencyScore > 0)
        .OrderByDescending(a => a.FrequencyScore)
        .Take(8)
        .ToList();

    /// <summary>Grid (true) or list (false) view for Apps browse mode.</summary>
    [ObservableProperty]
    private bool _appsViewGrid = true;

    // ── Search debounce ──────────────────────────────────────────────
    private CancellationTokenSource? _searchCts;

    // ── Constructor ──────────────────────────────────────────────────
    public MainViewModel(ArcConfig config, ILogger? log = null)
{
    _log        = log ?? NullLogger.Instance;
    _apps       = new AppDiscoveryService(_log);
    _freq       = new FrequencyService(_log);
    _configSvc  = new ConfigService(_log);
    Config      = config;
    Settings = new SettingsViewModel(Config, _configSvc, this);

        // Push initial config to services
        FileSearchService.MaxDepth = Config.MaxFileDepth;
        ClipboardService.MaxItems  = Config.ClipboardHistorySize;

        AiFollowUpCommand = new RelayCommand<string>(OnAiFollowUp);

        _ = LoadAppsAsync();
        Results.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasResults));
    }

    /// <summary>Pushes config values to services whenever the config object is replaced.</summary>
    partial void OnConfigChanged(ArcConfig value)
    {
        FileSearchService.MaxDepth = value.MaxFileDepth;
        ClipboardService.MaxItems  = value.ClipboardHistorySize;
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
    private int _selectedIndex = -1;

    /// <summary>Null = all categories. Values: "apps" | "files" | "clipboard" | "actions".</summary>
    [ObservableProperty]
    private string? _activeCategory;

    [ObservableProperty]
    private bool _isSettingsOpen;

    // ── Action preview state ─────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPreviewVisible))]
    private string? _activeActionId;   // "calc", "color", "timer", "ip", "ai"

    [ObservableProperty] private string  _calcResult   = string.Empty;
    [ObservableProperty] private string  _calcExpr     = string.Empty;

    [ObservableProperty] private string  _colorHex     = string.Empty;
    [ObservableProperty] private string  _colorRgb     = string.Empty;
    [ObservableProperty] private string  _colorHsl     = string.Empty;
    [ObservableProperty] private Color   _colorSwatch  = Colors.Transparent;

    [ObservableProperty] private string  _timerDisplay = "00:00";
    [ObservableProperty] private double  _timerProgress = 100;
    [ObservableProperty] private bool    _timerRunning = false;
    private TimeSpan _timerRemaining;
    private TimeSpan _timerTotal;
    private DispatcherTimer? _timerTick;

    [ObservableProperty] private string  _ipLocal      = "Fetching…";
    [ObservableProperty] private string  _ipPublic     = "Fetching…";

    [ObservableProperty] private string  _aiText       = string.Empty;
    [ObservableProperty] private bool    _aiLoading    = false;
    [ObservableProperty] private string  _aiError      = string.Empty;
    private CancellationTokenSource? _aiCts;
    private readonly List<(string Role, string Content)> _aiConversation = [];

    public IRelayCommand AiFollowUpCommand { get; }

    /// <summary>Raised when the AI conversation changes (messages added).</summary>
    public event EventHandler? ConversationChanged;

    /// <summary>Public view of the AI conversation for binding.</summary>
    public IReadOnlyList<(string Role, string Content)> AiConversation => _aiConversation;

    // ═══════════════════════════════════════════════════════════════
    // Computed properties
    // ═══════════════════════════════════════════════════════════════

    public bool HasQuery          => !string.IsNullOrEmpty(Query);
    public bool HasResults         => Results.Count > 0;
    public bool IsPreviewVisible   => ActiveActionId is not null;
    public bool IsBrowsePanelVisible => ActiveCategory is not null;

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
                ClearAll();
                return;
            }

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
        OnPropertyChanged(nameof(SuggestedApps));
        OnQueryChanged(Query ?? string.Empty);
    }

    partial void OnAppsViewGridChanged(bool value)
    {
        // Re-trigger the browse panel to switch grid/list
        OnQueryChanged(Query ?? string.Empty);
    }

    // ═══════════════════════════════════════════════════════════════
    // Search pipeline
    // ═══════════════════════════════════════════════════════════════

    private void RunSearch(string query, CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested) return;

            // 1. Check built-in actions first (calculator, color, timer, ip, ai, settings)
            var action = Actions.FirstOrDefault(a => IsActionEnabled(a.Id) && a.CanHandle(query));
            if (action is not null)
            {
                ActivateAction(action, query);
                return;
            }

            // 2. Cancel any running action work
            CancelActionWork();

            // 3. Fuzzy search
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
                        clone.Score = score + a.FrequencyScore * 0.3;
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
                    newResults.Add(new SectionLabel("APPLICATIONS"));
                    newResults.AddRange(appList);
                }
            }

            if (ct.IsCancellationRequested) return;

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
                    newResults.Add(new SectionLabel("FILES"));
                    newResults.AddRange(fileMatches);
                }
            }

            // ── Clipboard ─────────────────────────────────────────────
            bool showClip = Config.ClipboardEnabled && Config.IndexClipboard && (ActiveCategory is null or "clipboard");
            if (showClip)
            {
                int limit = isBrowseMode ? 50 : Config.ResultsCount;
                var clips = ClipboardService.GetHistory()
                    .Where(c => MatchScore(query, c.Preview) >= 0)
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
                    newResults.Add(new SectionLabel("CLIPBOARD"));
                    newResults.AddRange(clips);
                }
            }

            // ── Actions ───────────────────────────────────────────────
            bool showActions = ActiveCategory is null or "actions";
            if (showActions)
            {
                var availableActions = _staticActions.Where(a => IsActionEnabled(a.ActionId));

                var actionMatches = availableActions
                    .Where(a => MatchScore(query, a.Name) >= 0)
                    .Select(a => { a.Score = MatchScore(query, a.Name); return a; })
                    .OrderByDescending(a => a.Score)
                    .ToList();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    AddDynamicAction(actionMatches, BuildUrlAction(query));
                    AddDynamicAction(actionMatches, BuildWebSearchAction(query));
                    AddDynamicAction(actionMatches, BuildShellAction(query));
                }

                if (actionMatches.Count > 0)
                {
                    newResults.Add(new SectionLabel("ACTIONS"));
                    newResults.AddRange(actionMatches);
                }
            }

            // ── Windows Settings (only when there is a query) ───────────
            if (Config.IndexWindowsSettings && !string.IsNullOrEmpty(query) && (ActiveCategory is null or "apps"))
            {
                var settingsMatches = _windowsSettings
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
                    newResults.Add(new SectionLabel("SETTINGS"));
                    newResults.AddRange(settingsMatches);
                }
            }

            if (ct.IsCancellationRequested) return;

            // Commit results on UI thread
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Results.Clear();
                foreach (var r in newResults) Results.Add(r);
                SelectedIndex = Results.Count > 0 ? FindFirstResultIndex() : -1;
            });
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
        "calc" => Config.IndexCalculator,
        "system" => Config.IndexSystemCommands,
        "settings" => Config.IndexWindowsSettings,
        "url" => Config.IndexUrls,
        "web" => Config.IndexWebSearches,
        "shell" => Config.IndexShell,
        _ => true,
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
        if (!Config.IndexWebSearches || string.IsNullOrWhiteSpace(query)) return null;
        return new SearchResult
        {
            Id = $"web:{query}",
            Type = ResultType.Action,
            Name = $"Search web for {query}",
            Subtitle = "Web search",
            LucideIcon = "search",
            ActionId = "web",
            Score = 100,
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

    private static string NormalizeUrl(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Contains("://", StringComparison.Ordinal)
            ? trimmed
            : $"https://{trimmed}";
    }

    // ═══════════════════════════════════════════════════════════════
    // Action activation
    // ═══════════════════════════════════════════════════════════════

    private void ActivateAction(IAction action, string query)
    {
        CancelActionWork();

        var result = action.BuildResult(query);
        Results.Clear();
        Results.Add(new SectionLabel("ACTIONS"));
        Results.Add(result);
        SelectedIndex = 1; // the result row (index 0 = section label)

        ActiveActionId = action.Id;

        switch (action.Id)
        {
            case "calc":     StartCalc(query);           break;
            case "color":    StartColor(query);          break;
            case "timer":    StartTimerPreview(query);   break;
            case "ip":       _ = StartIpAsync();         break;
            case "settings": break; // Settings panel shows immediately
            // AI does NOT auto-start — user must press Enter
            case "ai":       break;
        }
    }

    private void StartCalc(string query)
    {
        CalcExpr   = query.Trim();
        CalcResult = CalculatorAction.Evaluate(query);
    }

    private void StartColor(string query)
    {
        var hex = ColorAction.Normalize(query.Trim());
        ColorAction.ParseHex(hex, out var r, out var g, out var b);
        ColorAction.RgbToHsl(r, g, b, out var h, out var s, out var l);

        ColorHex    = hex.ToUpperInvariant();
        ColorRgb    = $"RGB({r}, {g}, {b})";
        ColorHsl    = $"HSL({h:F0}°, {s:F0}%, {l:F0}%)";
        ColorSwatch = Color.FromRgb(r, g, b);
    }

    private void StartTimerPreview(string query)
    {
        if (!TimerAction.TryParse(query, out var duration)) return;
        _timerTotal     = duration;
        _timerRemaining = duration;
        UpdateTimerDisplay();
        TimerRunning = false;
    }

    private async Task StartIpAsync()
    {
        IpLocal  = IpAction.GetLocalIp() ?? "Not connected";
        IpPublic = "Fetching…";
        var pub  = await IpAction.GetPublicIpAsync();
        IpPublic = pub ?? "Unavailable";
    }

    private async Task StartAiAsync(string query)
    {
        _aiCts?.Cancel();
        _aiCts = new CancellationTokenSource();
        var ct = _aiCts.Token;

        var question = AiAction.ExtractQuestion(query);
        AiText    = string.Empty;
        AiError   = string.Empty;
        AiLoading = true;

        // Start fresh conversation for new "ai " queries
        _aiConversation.Clear();
        _aiConversation.Add(("user", question));
        ConversationChanged?.Invoke(this, EventArgs.Empty);

        var (key, model) = GetAiConfig();
        if (string.IsNullOrWhiteSpace(key))
        {
            AiError   = $"Add your {Config.AiProvider} API key in Settings (Ctrl+,) to use AI.";
            AiLoading = false;
            return;
        }

        try
        {
            await AiService.StreamAsync(Config.AiProvider, model, key, _aiConversation, token =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    AiText    += token;
                    AiLoading  = AiText.Length == 0;
                    if (_aiConversation.Count == 1)
                        _aiConversation.Add(("assistant", AiText));
                    else
                        _aiConversation[^1] = ("assistant", AiText);
                    ConversationChanged?.Invoke(this, EventArgs.Empty);
                });
            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // User cancelled — silent
        }
        catch (TaskCanceledException)
        {
            AiError = "Request timed out. Please try again.";
        }
        catch (HttpRequestException ex)
        {
            AiError = ex.Message;
        }
        catch (Exception ex)
        {
            AiError = $"Unexpected error: {ex.Message}";
        }
        finally { AiLoading = false; }
    }

    private async void OnAiFollowUp(string? followUp)
    {
        if (string.IsNullOrWhiteSpace(followUp)) return;

        _aiCts?.Cancel();
        _aiCts = new CancellationTokenSource();
        var ct = _aiCts.Token;

        _aiConversation.Add(("user", followUp));
        AiError   = string.Empty;
        AiLoading = true;
        ConversationChanged?.Invoke(this, EventArgs.Empty);

        var (key, model) = GetAiConfig();
        if (string.IsNullOrWhiteSpace(key))
        {
            AiError   = $"Add your {Config.AiProvider} API key in Settings (Ctrl+,) to use AI.";
            AiLoading = false;
            return;
        }

        try
        {
            string? newResponse = null;
            await AiService.StreamAsync(Config.AiProvider, model, key, _aiConversation, token =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (newResponse is null)
                    {
                        // First token: append to existing response
                        newResponse = token;
                        AiText += "\n\n" + token;
                        _aiConversation.Add(("assistant", newResponse));
                    }
                    else
                    {
                        newResponse += token;
                        AiText += token;
                        _aiConversation[^1] = ("assistant", newResponse);
                    }
                    AiLoading = false;
                    ConversationChanged?.Invoke(this, EventArgs.Empty);
                });
            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // User cancelled — silent
        }
        catch (TaskCanceledException)
        {
            AiError = "Request timed out. Please try again.";
        }
        catch (HttpRequestException ex)
        {
            AiError = ex.Message;
        }
        catch (Exception ex)
        {
            AiError = $"Unexpected error: {ex.Message}";
        }
        finally { AiLoading = false; }
    }

    private (string Key, string Model) GetAiConfig() => Config.AiProvider switch
    {
        "gemini"     => (Config.GeminiApiKey,     Config.GeminiModel),
        "openrouter" => (Config.OpenRouterApiKey, Config.OpenRouterModel),
        "deepseek"   => (Config.DeepSeekApiKey,   Config.DeepSeekModel),
        _            => (Config.GroqApiKey,        Config.GroqModel),
    };

    // ═══════════════════════════════════════════════════════════════
    // Timer commands
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private void StartTimer()
    {
        if (TimerRunning || _timerTotal == TimeSpan.Zero) return;
        _timerRemaining = _timerTotal;
        TimerRunning    = true;

        _timerTick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timerTick.Tick += OnTimerTick;
        _timerTick.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _timerRemaining -= TimeSpan.FromMilliseconds(100);
        if (_timerRemaining <= TimeSpan.Zero)
        {
            _timerRemaining = TimeSpan.Zero;
            _timerTick?.Stop();
            TimerRunning = false;
            NotificationService.Show("Arc Timer", "Your timer has finished!");
        }
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        TimerDisplay = _timerRemaining.TotalHours >= 1
            ? _timerRemaining.ToString(@"hh\:mm\:ss")
            : _timerRemaining.ToString(@"mm\:ss");

        TimerProgress = _timerTotal > TimeSpan.Zero
            ? _timerRemaining.TotalMilliseconds / _timerTotal.TotalMilliseconds * 100.0
            : 0;
    }

    [RelayCommand]
    private void CancelTimer()
    {
        _timerTick?.Stop();
        _timerTick  = null;
        TimerRunning = false;
    }

    // ═══════════════════════════════════════════════════════════════
    // Open / Execute
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    public void OpenSelected()
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
                var catalogEntry = _appCatalog.Find(a =>
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
                    ClipboardService.CopyToSystem(result.ClipContent);
                HideAfterLaunch();
                break;

            case ResultType.Action:
                // System commands: shutdown / restart / sleep / lock etc.
                if (result.ActionId == "system")
                {
                    SystemAction.Execute(Query);
                    return;
                }
                // Timer: start countdown on Enter
                if (result.ActionId == "timer")
                    StartTimerCommand.Execute(null);
                // AI: start streaming on Enter
                else if (result.ActionId == "ai")
                {
                    try { _ = StartAiAsync(Query); }
                    catch (Exception ex) { _log.Warning("StartAiAsync error", ex); }
                }
                // Calc/Color/IP: Enter copies result to clipboard
                else if (result.ActionId == "calc")
                    ClipboardService.CopyToSystem(CalcResult.TrimStart('=', ' '));
                else if (result.ActionId == "color")
                    ClipboardService.CopyToSystem(ColorHex);
                else if (result.ActionId == "ip")
                    ClipboardService.CopyToSystem(IpLocal);
                // Settings: open settings panel when clicked
                else if (result.ActionId == "settings")
                {
                    IsSettingsOpen = true;
                    return; // Don't break the switch, just return so we don't hide window
                }
                else if (result.ActionId == "url")
                {
                    Launch(NormalizeUrl(Query));
                    HideAfterLaunch();
                }
                else if (result.ActionId == "web")
                {
                    Launch($"https://www.google.com/search?q={Uri.EscapeDataString(Query)}");
                    HideAfterLaunch();
                }
                else if (result.ActionId == "shell")
                {
                    var command = Query.StartsWith(">", StringComparison.Ordinal) ? Query[1..].Trim() : Query.Trim();
                    if (!string.IsNullOrWhiteSpace(command))
                        Process.Start(new ProcessStartInfo("cmd.exe", $"/c {command}") { UseShellExecute = false, CreateNoWindow = true });
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
                    ClipboardService.CopyToSystem(result.ClipContent);
                return;

            case ResultType.App:
            case ResultType.File:
                break;

            default:
                return;
        }

        string? folder = result.Type switch
        {
            ResultType.App  => result.ExePath is not null ? Path.GetDirectoryName(result.ExePath) : null,
            ResultType.File => result.FilePath is not null ? Path.GetDirectoryName(result.FilePath) : null,
            _               => null,
        };

        if (folder is not null && Directory.Exists(folder))
            Process.Start("explorer.exe", folder);
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
            null        => "apps",
            "apps"      => "files",
            "files"     => "clipboard",
            "clipboard" => "actions",
            _           => null,
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Settings
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    public void OpenSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    // ═══════════════════════════════════════════════════════════════
    // Window events
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Raised when the VM wants the window to hide itself.</summary>
    public event Action? RequestHide;

    public void Shutdown()
    {
        _freq.Flush();
        _freq.Dispose();
        _searchCts?.Dispose();
        _aiCts?.Dispose();
        _timerTick?.Stop();
    }

    public void OnWindowShown()
    {
        // Refresh clipboard category when window opens
    }

    public void Reset()
    {
        if (Config.LastQueryStyle == "clear")
            Query = string.Empty;
        ActiveCategory = null;
        IsSettingsOpen = false;

        // Preserve the active action preview if AlwaysPreview is enabled
        if (!Config.AlwaysPreview)
            CancelActionWork();
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
        ActiveActionId = null;
        ActiveCategory = null;
        SelectedIndex = -1;
        AiError = string.Empty;
    }

    /// <summary>Toggles the Apps browse view between grid and list.</summary>
    [RelayCommand]
    private void ToggleAppsView()
    {
        AppsViewGrid = !AppsViewGrid;
    }

    private void ClearAll()
    {
        Results.Clear();
        SelectedIndex  = -1;
        if (!Config.AlwaysPreview)
            CancelActionWork();
    }

    private void CancelActionWork()
    {
        _aiCts?.Cancel();
        _timerTick?.Stop();
        _timerTick   = null;
        TimerRunning = false;
        ActiveActionId = null;
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
