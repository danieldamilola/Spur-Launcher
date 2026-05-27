using System.Collections.ObjectModel;
using Volt.Extensions;
using Volt.ViewModels;

namespace Volt.ViewModels;

/// <summary>
/// Central orchestrator for the Volt launcher.
/// Owns the search query, result list, selection, category filter,
/// and all action state (AI streaming, timer countdown, color/IP data).
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    // ── Services ──────────────────────────────────────────────────────
    private readonly AppDiscoveryService  _apps      = new();
    private readonly FileSearchService    _files     = new();
    private readonly FrequencyService     _freq      = new();
    private readonly ConfigService        _configSvc = new();

    private static readonly IAction[] Actions =
    [
        new CalculatorAction(),
        new ColorAction(),
        new TimerAction(),
        new IpAction(),
        new AiAction(),
    ];

    // ── App catalog (loaded once on startup) ─────────────────────────
    private List<SearchResult> _appCatalog = [];

    // ── Search debounce ──────────────────────────────────────────────
    private CancellationTokenSource? _searchCts;

    // ── Constructor ──────────────────────────────────────────────────
    public MainViewModel()
    {
        Config   = _configSvc.Load();
        Settings = new SettingsViewModel(Config, _configSvc, this);
        _ = LoadAppsAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    // Observable properties
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQuery))]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    private string _query = string.Empty;

    [ObservableProperty]
    private VoltConfig _config;

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

    // ═══════════════════════════════════════════════════════════════
    // Computed properties
    // ═══════════════════════════════════════════════════════════════

    public bool HasQuery   => !string.IsNullOrEmpty(Query);
    public bool HasResults => Results.Count > 0;
    public bool IsPreviewVisible => ActiveActionId is not null;

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
            _searchCts = new CancellationTokenSource();
            var ct = _searchCts.Token;

            if (string.IsNullOrEmpty(value) && ActiveCategory is null)
            {
                ClearAll();
                return;
            }

            var delay = Task.Delay(60, ct);
            delay.ContinueWith(_ =>
            {
                if (!ct.IsCancellationRequested)
                    Application.Current?.Dispatcher.InvokeAsync(() => RunSearch(value, ct));
            }, TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Volt] OnQueryChanged error: {ex.Message}");
            Console.WriteLine($"[Volt] OnQueryChanged error: {ex.Message}");
        }
    }

    partial void OnActiveCategoryChanged(string? value)
    {
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

            // 1. Check built-in actions first (calculator, color, timer, ip, ai)
            var action = Actions.FirstOrDefault(a => a.CanHandle(query));
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
            Debug.WriteLine($"[Volt] RunSearch error: {ex.Message}");
            Console.WriteLine($"[Volt] RunSearch error: {ex.Message}");
        }
    }

    private async Task SearchAsync(string query, CancellationToken ct)
    {
        try
        {
            var newResults = new List<object>();

            // Apps
            bool showApps = ActiveCategory is null or "apps";
            if (showApps && _appCatalog is not null)
        {
            var appMatches = _appCatalog
                .Select(a =>
                {
                    var score = string.IsNullOrEmpty(query) ? 0 : FuzzySearch.Score(query, a.Name);
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
                .OrderByDescending(a => a.Score)
                .Take(Config.ResultsCount)
                .ToList();

            if (appMatches.Count > 0)
            {
                newResults.Add(new SectionLabel("APPLICATIONS"));
                newResults.AddRange(appMatches);
            }
        }

        if (ct.IsCancellationRequested) return;

        // Files
        bool showFiles = Config.FileSearchEnabled && ActiveCategory is null or "files";
        if (showFiles)
        {
            var fileMatches = await _files.SearchAsync(query, Config.ResultsCount);
            if (ct.IsCancellationRequested) return;
            if (fileMatches.Count > 0)
            {
                newResults.Add(new SectionLabel("FILES"));
                newResults.AddRange(fileMatches);
            }
        }

        // Clipboard
        bool showClip = Config.ClipboardEnabled && ActiveCategory is null or "clipboard";
        if (showClip)
        {
            int limit = string.IsNullOrEmpty(query) && ActiveCategory == "clipboard" ? 5 : Config.ResultsCount;
            var clips = ClipboardService.GetHistory()
                .Where(c => string.IsNullOrEmpty(query) || FuzzySearch.Score(query, c.Preview) >= 0)
                .Take(limit)
                .Select(c => new SearchResult
                {
                    Id         = $"clip:{c.Timestamp.Ticks}",
                    Type       = ResultType.Clipboard,
                    Name       = c.Preview,
                    Subtitle   = c.TimeAgo,
                    LucideIcon = "clipboard",
                    ClipContent = c.Content,
                    ClipTimestamp = c.Timestamp,
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
            Debug.WriteLine($"[Volt] SearchAsync error: {ex.Message}");
            Console.WriteLine($"[Volt] SearchAsync error: {ex.Message}");
        }
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
            case "calc":  StartCalc(query);           break;
            case "color": StartColor(query);          break;
            case "timer": StartTimerPreview(query);   break;
            case "ip":    _ = StartIpAsync();         break;
            // AI does NOT auto-start — user must press Enter
            case "ai":    break;
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

        var (key, model) = GetAiConfig();
        if (string.IsNullOrWhiteSpace(key))
        {
            AiError   = $"Add your {Config.AiProvider} API key in Settings (Ctrl+,) to use AI.";
            AiLoading = false;
            return;
        }

        try
        {
            await AiService.StreamAsync(Config.AiProvider, model, key, question, token =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    AiText    += token;
                    AiLoading  = AiText.Length == 0;
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
            NotificationService.Show("Volt Timer", "Your timer has finished!");
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
                _freq.Increment(result.ExePath ?? result.LnkPath ?? "");
                result.FrequencyScore = _freq.Get(result.ExePath ?? "");
                RequestHide?.Invoke();
                break;

            case ResultType.File:
                if (result.FilePath is not null)
                    Launch(result.FilePath);
                RequestHide?.Invoke();
                break;

            case ResultType.Clipboard:
                if (result.ClipContent is not null)
                    ClipboardService.CopyToSystem(result.ClipContent);
                RequestHide?.Invoke();
                break;

            case ResultType.Action:
                // Timer: start countdown on Enter
                if (result.ActionId == "timer")
                    StartTimerCommand.Execute(null);
                // AI: start streaming on Enter
                else if (result.ActionId == "ai")
                {
                    try { _ = StartAiAsync(Query); }
                    catch (Exception ex) { Debug.WriteLine($"[Volt] StartAiAsync error: {ex.Message}"); Console.WriteLine($"[Volt] StartAiAsync error: {ex.Message}"); }
                }
                // Calc/Color/IP: Enter copies result to clipboard
                else if (result.ActionId == "calc")
                    ClipboardService.CopyToSystem(CalcResult.TrimStart('=', ' '));
                else if (result.ActionId == "color")
                    ClipboardService.CopyToSystem(ColorHex);
                else if (result.ActionId == "ip")
                    ClipboardService.CopyToSystem(IpLocal);
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
            Debug.WriteLine($"[Volt] RunAsAdmin failed: {ex.Message}");
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

    private static void Launch(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { Debug.WriteLine($"[Volt] Launch failed: {ex.Message}"); }
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

    public void OnWindowShown()
    {
        // Refresh clipboard category when window opens
    }

    public void Reset()
    {
        Query          = string.Empty;
        ActiveCategory = null;
        IsSettingsOpen = false;
        CancelActionWork();
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    private async Task LoadAppsAsync()
    {
        _appCatalog = await _apps.DiscoverAsync();

        // Apply persisted frequency scores
        foreach (var app in _appCatalog)
            if (app.ExePath is not null)
                app.FrequencyScore = _freq.Get(app.ExePath);

        // Pre-warm icon cache in background
        _ = IconService.PreloadAsync(_appCatalog
            .Where(a => a.IconPath is not null)
            .Select(a => a.IconPath!));
    }

    private void ClearAll()
    {
        Results.Clear();
        SelectedIndex  = -1;
        CancelActionWork();
        ActiveActionId = null;
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
