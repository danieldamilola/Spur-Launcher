using System.Collections.ObjectModel;
using Arc.Extensions;
using Arc.Services;
using Arc.Models;

namespace Arc.ViewModels;

// ═══════════════════════════════════════════════════════════════════
// AiChatViewModel — owns AI streaming state and conversation
// ═══════════════════════════════════════════════════════════════════

public sealed partial class AiChatViewModel : ObservableObject
{
    private readonly IAiService _aiService;
    private readonly ArcConfig  _config;
    private CancellationTokenSource? _aiCts;
    private readonly List<(string Role, string Content)> _aiConversation = [];

    public AiChatViewModel(IAiService aiService, ArcConfig config)
    {
        _aiService = aiService;
        _config = config;
        AiFollowUpCommand = new RelayCommand<string>(OnAiFollowUp);
    }

    [ObservableProperty] private string _aiText    = string.Empty;
    [ObservableProperty] private bool   _aiLoading = false;
    [ObservableProperty] private string _aiError   = string.Empty;

    public IRelayCommand AiFollowUpCommand { get; }

    /// <summary>Raised when the AI conversation changes (messages added).</summary>
    public event EventHandler? ConversationChanged;

    /// <summary>Public view of the AI conversation for binding.</summary>
    public IReadOnlyList<(string Role, string Content)> AiConversation => _aiConversation;

    public void CancelPending()
    {
        _aiCts?.Cancel();
        _aiCts?.Dispose();
        _aiCts = null;
    }

    /// <summary>Clears the conversation, cancels any in-flight request, and resets text/error state.</summary>
    public void ClearConversation()
    {
        CancelPending();
        _aiConversation.Clear();
        AiText = string.Empty;
        AiLoading = false;
        AiError = string.Empty;
        ConversationChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Returns the full conversation formatted as a copyable text block.</summary>
    public string GetConversationText()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var (role, content) in _aiConversation)
        {
            sb.AppendLine(role == "user" ? "You:" : "AI:");
            sb.AppendLine(content);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    public async Task StartAiAsync(string query)
    {
        CancelPending();
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
            AiError   = $"Add your {_config.AiProvider} API key in Settings (Ctrl+,) to use AI.";
            AiLoading = false;
            return;
        }

        try
        {
            await _aiService.StreamAsync(_config.AiProvider, model, key, _aiConversation, token =>
            {
                Application.Current?.Dispatcher.InvokeAsync(() =>
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

        CancelPending();
        _aiCts = new CancellationTokenSource();
        var ct = _aiCts.Token;

        _aiConversation.Add(("user", followUp));
        AiError   = string.Empty;
        AiLoading = true;
        ConversationChanged?.Invoke(this, EventArgs.Empty);

        var (key, model) = GetAiConfig();
        if (string.IsNullOrWhiteSpace(key))
        {
            AiError   = $"Add your {_config.AiProvider} API key in Settings (Ctrl+,) to use AI.";
            AiLoading = false;
            return;
        }

        try
        {
            string? newResponse = null;
            await _aiService.StreamAsync(_config.AiProvider, model, key, _aiConversation, token =>
            {
                Application.Current?.Dispatcher.InvokeAsync(() =>
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

    private (string Key, string Model) GetAiConfig() => _config.AiProvider switch
    {
        "gemini"     => (_config.GeminiApiKey,     _config.GeminiModel),
        "openrouter" => (_config.OpenRouterApiKey, _config.OpenRouterModel),
        "deepseek"   => (_config.DeepSeekApiKey,   _config.DeepSeekModel),
        _            => (_config.GroqApiKey,        _config.GroqModel),
    };
}

// ═══════════════════════════════════════════════════════════════════
// TimerViewModel — owns countdown timer state and logic
// ═══════════════════════════════════════════════════════════════════

public sealed partial class TimerViewModel : ObservableObject
{
    private readonly INotificationService _notification;

    [ObservableProperty] private string _timerDisplay = "00:00";
    [ObservableProperty] private double _timerProgress = 100;
    [ObservableProperty] private bool   _timerRunning = false;

    private TimeSpan _timerRemaining;
    private TimeSpan _timerTotal;
    private DispatcherTimer? _timerTick;

    public TimerViewModel(INotificationService notification)
    {
        _notification = notification;
        StartCommand  = new RelayCommand(Start);
        CancelCommand = new RelayCommand(Cancel);
    }

    public IRelayCommand StartCommand  { get; }
    public IRelayCommand CancelCommand { get; }

    public void StartTimerPreview(string query)
    {
        if (!TimerAction.TryParse(query, out var duration)) return;
        _timerTotal     = duration;
        _timerRemaining = duration;
        UpdateTimerDisplay();
        TimerRunning = false;
    }

    private void Start()
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
            _notification.Show("Arc Timer", "Your timer has finished!");
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

    private void Cancel()
    {
        _timerTick?.Stop();
        _timerTick  = null;
        TimerRunning = false;
    }

    /// <summary>Stops the timer without resetting the display (for CancelActionWork).</summary>
    public void Stop()
    {
        _timerTick?.Stop();
        _timerTick  = null;
        TimerRunning = false;
    }
}

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

    // ── Sub-ViewModels ───────────────────────────────────────────────
    private readonly AiChatViewModel _ai;
    private readonly TimerViewModel  _timer;

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
        IAiService            aiService)
    {
        _log          = log;
        _apps         = apps;
        _files        = files;
        _freq         = freq;
        _configSvc    = configSvc;
        _clipboard    = clipboard;
        _notification = notification;
        _aiService    = aiService;

        Config   = config;
        Settings = new SettingsViewModel(Config, _configSvc, this);

        // Push initial config to services
        _files.MaxDepth     = Config.MaxFileDepth;
        _clipboard.MaxItems = Config.ClipboardHistorySize;

        // Sub-ViewModels
        _ai    = new AiChatViewModel(_aiService, Config);
        _timer = new TimerViewModel(_notification);

        // Forward sub-VM property changes for backward-compatible bindings
        _ai.PropertyChanged    += (_, args) => OnPropertyChanged(args.PropertyName);
        _timer.PropertyChanged += (_, args) => OnPropertyChanged(args.PropertyName);
        _ai.ConversationChanged += (_, args) => ConversationChanged?.Invoke(this, args);

        _ = LoadAppsAsync();
        Results.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasResults));
    }

    /// <summary>Pushes config values to services whenever the config object is replaced.</summary>
    partial void OnConfigChanged(ArcConfig value)
    {
        _files.MaxDepth     = value.MaxFileDepth;
        _clipboard.MaxItems = value.ClipboardHistorySize;
        if (value.CompactMode)
            Application.Current?.Dispatcher.Invoke(() => Results.Clear());
        OnPropertyChanged(nameof(IsHubVisible));
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

    // Timer pass-through properties — owned by TimerViewModel
    public string TimerDisplay  => _timer.TimerDisplay;
    public double TimerProgress => _timer.TimerProgress;
    public bool   TimerRunning  { get => _timer.TimerRunning; set => _timer.TimerRunning = value; }
    public IRelayCommand StartTimerCommand  => _timer.StartCommand;
    public IRelayCommand CancelTimerCommand => _timer.CancelCommand;

    [ObservableProperty] private string  _ipLocal      = "Fetching…";
    [ObservableProperty] private string  _ipPublic     = "Fetching…";

    // AI pass-through properties — owned by AiChatViewModel
    public string AiText    => _ai.AiText;
    public bool   AiLoading => _ai.AiLoading;
    public string AiError   { get => _ai.AiError; set => _ai.AiError = value; }
    public IRelayCommand AiFollowUpCommand => _ai.AiFollowUpCommand;

    /// <summary>Raised when the AI conversation changes (messages added).</summary>
    public event EventHandler? ConversationChanged;

    /// <summary>Public view of the AI conversation for binding.</summary>
    public IReadOnlyList<(string Role, string Content)> AiConversation => _ai.AiConversation;

    /// <summary>Clears the AI conversation, cancels streaming, and returns to Hub.</summary>
    public void BackFromAiChat()
    {
        _ai.ClearConversation();
        CancelActionWork();
        ActiveCategory = null;
        Query = string.Empty;
        ShowHub();
    }

    /// <summary>Returns the full AI conversation formatted as a copyable text block.</summary>
    public string GetAiConversationText() => _ai.GetConversationText();

    /// <summary>Cancels the in-flight AI generation without clearing the conversation.</summary>
    public void CancelAiGeneration() => _ai.CancelPending();

    // ═══════════════════════════════════════════════════════════════
    // Computed properties
    // ═══════════════════════════════════════════════════════════════

    public bool HasQuery          => !string.IsNullOrEmpty(Query);
    public bool HasResults         => Results.Count > 0;
    public bool IsPreviewVisible   => ActiveActionId is not null;
    public bool IsBrowsePanelVisible => ActiveCategory is not null;

    /// <summary>True when the launcher is idle — empty query, no category, no settings, no action. False when Compact Mode is enabled.</summary>
    public bool IsHubVisible => string.IsNullOrEmpty(Query) && ActiveCategory is null && ActiveActionId is null && !Config.CompactMode;

    /// <summary>Message shown when Hub has no recent items (first launch).</summary>
    [ObservableProperty]
    private string _emptyStateMessage = string.Empty;

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
                ShowHub();
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
        OnQueryChanged(Query ?? string.Empty);
    }

    // ═══════════
    /// <summary>Shows the Hub — recent items when the launcher is idle.</summary>
    private void ShowHub()
    {
        CancelActionWork();
        _searchCts?.Cancel();
        ActiveActionId = null;

        if (Config.CompactMode)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Results.Clear();
                SelectedIndex = -1;
                EmptyStateMessage = string.Empty;
                OnPropertyChanged(nameof(IsHubVisible));
            });
            return;
        }

        var hubItems = new List<object>();

        // Get top 3 recent items from app catalog by frequency
        var recent = _appCatalog
            .Where(a => a.FrequencyScore > 0)
            .OrderByDescending(a => a.FrequencyScore)
            .Take(3)
            .Select(a =>
            {
                var clone = Clone(a);
                clone.Score = a.FrequencyScore;
                return clone;
            })
            .ToList();

        if (recent.Count > 0)
        {
            hubItems.AddRange(recent);
            EmptyStateMessage = string.Empty;
        }
        else
        {
            EmptyStateMessage = "Your recent items will appear here.";
        }

        Application.Current?.Dispatcher.Invoke(() =>
        {
            Results.Clear();
            foreach (var item in hubItems) Results.Add(item);
            SelectedIndex = hubItems.Count > 0 ? 0 : -1;
            OnPropertyChanged(nameof(IsHubVisible));
        });
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
            bool showClip = Config.ClipboardEnabled && Config.IndexClipboard && ActiveCategory == "clipboard";
            if (showClip)
            {
                int limit = isBrowseMode ? 50 : Config.ResultsCount;
                var clips = _clipboard.GetHistory()
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
                var availableActions = ArcConstants.StaticActions.Where(a => IsActionEnabled(a.ActionId));

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
            case "timer":    _timer.StartTimerPreview(query); break;
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

    private async Task StartIpAsync()
    {
        IpLocal  = IpAction.GetLocalIp() ?? "Not connected";
        IpPublic = "Fetching…";
        var pub  = await IpAction.GetPublicIpAsync();
        IpPublic = pub ?? "Unavailable";
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
                // Timer: start countdown on Enter
                if (result.ActionId == "timer")
                    StartTimerCommand.Execute(null);
                // AI: start streaming on Enter
                else if (result.ActionId == "ai")
                {
                    try { _ = _ai.StartAiAsync(Query); }
                    catch (Exception ex) { _log.Warning("StartAiAsync error", ex); }
                }
                // Calc/Color/IP: Enter copies result to clipboard
                else if (result.ActionId == "calc")
                    _clipboard.CopyToSystem(CalcResult.TrimStart('=', ' '));
                else if (result.ActionId == "color")
                    _clipboard.CopyToSystem(ColorHex);
                else if (result.ActionId == "ip")
                    _clipboard.CopyToSystem(IpLocal);
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

    /// <summary>Clears the clipboard history, preserving pinned items.</summary>
    [RelayCommand]
    public void ClearClipboard()
    {
        var pinned = Config.PinnedClipboard.Select(p => p.Content).ToHashSet();
        _clipboard.KeepOnly(pinned);
        OnQueryChanged(Query ?? string.Empty);
    }

    /// <summary>Removes a single clipboard item from history by its content.</summary>
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
    // Settings
    // ═══════════════════════════════════════════════════════════════

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

    public void OnWindowShown()
    {
        if (IsHubVisible) ShowHub();
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

        // Show Hub after reset if no query remains
        if (string.IsNullOrEmpty(Query))
            ShowHub();
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
            if (IsHubVisible) ShowHub();
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

    private void ClearAll()
    {
        Results.Clear();
        SelectedIndex  = -1;
        if (!Config.AlwaysPreview)
            CancelActionWork();
        EmptyStateMessage = string.Empty;
    }

    private void CancelActionWork()
    {
        _ai.CancelPending();
        _timer.Stop();
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
