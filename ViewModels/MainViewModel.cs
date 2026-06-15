using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spur.Actions;
using Spur.Actions.Handlers;
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
    private readonly AddOnRegistry        _addOns;

    // ── Sub-ViewModels ───────────────────────────────────────────────
    private readonly AiChatViewModel    _ai;
    private readonly TimerViewModel     _timer;
    private readonly ClipboardViewModel _clipboardVm;

    // ── Action Dispatcher ─────────────────────────────────────────────
    private readonly ActionDispatcher _actionDispatcher;

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
        ISecureStorageService secureStorage,
        AddOnRegistry         addOns,
        Spur.Services.AddOnStoreService storeService)
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
        _addOns = addOns;

        Config   = config;
        Settings = new SettingsViewModel(Config, _configSvc, this, _themeManager, _startupService, _freq, _secureStorage, _addOns, storeService);

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

        // Initialize ActionDispatcher
        _actionDispatcher = CreateActionDispatcher();

        PopulatePaletteCommands();

        _apps.CatalogRefreshed += HandleCatalogRefreshed;

        Helpers.SafeFireAndForget.Run(LoadAppsAsync, _log, "LoadApps");
        Results.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasResults));
        UpdatePinnedCategories();
    }

    private ActionDispatcher CreateActionDispatcher()
    {
        var dispatcher = new ActionDispatcher(_log);

        // Register handlers in priority order (most specific first)
        var catalogHandler = new ActionCatalogHandler();
        catalogHandler.ScopeChangeRequested += (actionId, iconGlyph) =>
        {
            ActiveCategory = actionId;
            ScopeIconGlyph = iconGlyph;
            Query = string.Empty;
        };
        dispatcher.Register(catalogHandler);

        var settingsHandler = new SettingsActionHandler();
        settingsHandler.OpenSettingsRequested += () => OpenSettingsRequested?.Invoke();
        dispatcher.Register(settingsHandler);

        var urlHandler = new UrlActionHandler();
        urlHandler.HideRequested += HideAfterLaunch;
        dispatcher.Register(urlHandler);

        var webHandler = new WebSearchActionHandler();
        webHandler.HideRequested += HideAfterLaunch;
        dispatcher.Register(webHandler);

        dispatcher.Register(new TimerActionHandler(_timer));
        dispatcher.Register(new AiActionHandler(_ai, _log));
        dispatcher.Register(new ShellActionHandler(_addOns));
        dispatcher.Register(new GenericAddOnHandler(_addOns, _clipboard));

        return dispatcher;
    }

    public void UpdatePinnedCategories()
    {
        PinnedCategories.Clear();
        foreach (var id in Config.PinnedCategories)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;

            if (id == "files")
                PinnedCategories.Add(new PinnedCategoryItem { Id = "files", Label = "Files", IconGlyph = "\uE8B7" });
            else if (id == "clipboard")
                PinnedCategories.Add(new PinnedCategoryItem { Id = "clipboard", Label = "Clips", IconGlyph = "\uE77F" });
            else if (_addOns.FindById(id) is { } extra)
                PinnedCategories.Add(new PinnedCategoryItem { Id = extra.Id, Label = extra.Name, IconGlyph = extra.IconGlyph });
        }
    }

    private void HandleCatalogRefreshed(List<SearchResult> freshCatalog)
    {
        // The catalog has been refreshed in the search engine.
        // Re-trigger the current query so results update against the fresh data.
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            if (!string.IsNullOrEmpty(Query))
                OnQueryChanged(Query);
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
    [NotifyPropertyChangedFor(nameof(IsExpandedHome))]
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
    private ObservableCollection<PinnedCategoryItem> _pinnedCategories = [];

    [ObservableProperty]
    private ObservableCollection<Spur.Models.ScopeFilterItem> _scopeFilters = [];

    private List<object> _rawResults = [];

    /// <summary>Null = all categories. Values: "apps" | "files" | "clipboard" | "actions".</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExpandedHome))]
    private string? _activeCategory;

    /// <summary>Lucide icon glyph shown in the search bar when a keyword scope is active.</summary>
    [ObservableProperty]
    private string? _scopeIconGlyph;

    [ObservableProperty]
    private bool _isSettingsOpen;

    partial void OnIsSettingsOpenChanged(bool value)
    {
        if (!value)
            Settings.Save();
    }

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

    // ── Full-Panel Mode (generic for all add-ons) ─────────────────
    private string? _activeFullPanel;
    /// <summary>Which add-on panel is active (e.g. "ai", "calc", "timer"). null = normal search.</summary>
    public string? ActiveFullPanel
    {
        get => _activeFullPanel;
        set
        {
            if (SetProperty(ref _activeFullPanel, value))
            {
                OnPropertyChanged(nameof(IsFullPanelActive));
                OnPropertyChanged(nameof(IsAiModeActive));
                OnPropertyChanged(nameof(SearchPlaceholder));
            }
        }
    }

    /// <summary>True when any add-on panel is active.</summary>
    public bool IsFullPanelActive => _activeFullPanel is not null;

    /// <summary>Backward compat — true when AI panel specifically is active.</summary>
    public bool IsAiModeActive => _activeFullPanel == "ai";

    /// <summary>Returns the current AI model name for display (e.g. "GPT-4o").</summary>
    public string AiModelName
    {
        get
        {
            try { return Config.AiProvider.ToLowerInvariant() switch
            {
                "groq"       => Config.GroqModel,
                "gemini"     => Config.GeminiModel,
                "openrouter" => Config.OpenRouterModel,
                "deepseek"   => Config.DeepSeekModel,
                _            => Config.AiProvider,
            }; }
            catch { return "AI"; }
        }
    }

    /// <summary>Enter a full-panel add-on mode.</summary>
    public void EnterAddOnPanel(string panelId)
    {
        ActiveFullPanel = panelId;
        Query = string.Empty;
        ActiveActionPanel = null;
    }

    /// <summary>Exit any full-panel mode — returns to normal search.</summary>
    public void ExitAddOnPanel()
    {
        var wasAi = _activeFullPanel == "ai";
        ActiveFullPanel = null;
        if (wasAi) _ai.CancelPending();
    }

    /// <summary>Shortcut for entering AI mode.</summary>
    public void EnterAiMode() => EnterAddOnPanel("ai");

    /// <summary>Shortcut for exiting AI mode.</summary>
    public void ExitAiMode() => ExitAddOnPanel();

    /// <summary>Send a query to AI.</summary>
    public async Task SendAiQuery(string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return;

        if (_ai.AiConversation.Count == 0)
            await _ai.StartAiAsync(question);
        else
            await _ai.AiFollowUpCommand.ExecuteAsync(question);
    }

    /// <summary>Clear AI conversation history.</summary>
    public void ClearAiChat()
    {
        _ai.ClearConversation();
    }

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

    private string _actionPreviewTitle = string.Empty;
    public string ActionPreviewTitle
    {
        get => _actionPreviewTitle;
        set => SetProperty(ref _actionPreviewTitle, value);
    }

    private string _actionPreviewSubtitle = string.Empty;
    public string ActionPreviewSubtitle
    {
        get => _actionPreviewSubtitle;
        set => SetProperty(ref _actionPreviewSubtitle, value);
    }

    private string _actionPreviewState = string.Empty;
    public string ActionPreviewState
    {
        get => _actionPreviewState;
        set => SetProperty(ref _actionPreviewState, value);
    }

    /// <summary>The original query that triggered the action (shown in calc/system previews).</summary>
    private string _actionPreviewQuery = string.Empty;
    public string ActionPreviewQuery
    {
        get => _actionPreviewQuery;
        set => SetProperty(ref _actionPreviewQuery, value);
    }

    public void CancelSearch() => _searchCts?.Cancel();

    // ═══════════════════════════════════════════════════════════════
    // Computed properties
    // ═══════════════════════════════════════════════════════════════

    public bool HasQuery          => !string.IsNullOrEmpty(Query);
    public bool HasResults         => Results.Count > 0;
    public bool IsBrowsePanelVisible => ActiveCategory is not null;

    /// <summary>True when expanded mode should show the homepage (empty query, no category, no full panel).</summary>
    public bool IsExpandedHome =>
        Config.WindowMode == "expanded"
        && !HasQuery
        && ActiveCategory is null
        && !IsFullPanelActive;

    /// <summary>All enabled add-ons for the homepage quick-actions list.</summary>
    public IReadOnlyList<Extensions.IAddOn> EnabledAddOns => _addOns.GetEnabled().ToList();

    /// <summary>True when ActiveCategory points to a keyword-scoped action (not a category filter).</summary>
    private static bool IsActionScope(string? category) => category switch
    {
        null or "apps" or "files" or "clipboard" or "actions" => false,
        _ => true,
    };

    /// <summary>Hub removed per ux.md.</summary>
    public bool IsHubVisible => false;

    /// <summary>Scope bar controlled dynamically by UpdateScopeFilters.</summary>
    public bool IsScopeBarVisible => false;

    public string SearchPlaceholder
    {
        get
        {
            if (_activeFullPanel is not null)
            {
                return _activeFullPanel switch
                {
                    "ai"         => _ai.AiConversation.Count > 0 ? "Ask follow-up…" : "Ask anything…",
                    "calc"       => "Type a math expression…",
                    "timer"      => "e.g. 5m, 30s, 1h…",
                    "color"      => "Type a hex code like #ff0055…",
                    "ip"         => "IP Address",
                    "currency"   => "e.g. 100 usd to eur…",
                    "pw"         => "e.g. pw 16…",
                    "kill"       => "Search processes…",
                    "note"       => "Type a note…",
                    "screenshot" => "Screenshot",
                    "system"     => "System commands…",
                    "shell"      => "Type a command…",
                    _            => "Search",
                };
            }

            return ActiveCategory switch
            {
                "files"     => "Search files…",
                "actions"   => "Search actions…",
                "clipboard" => "Filter clipboard…",
                _           => "Search",
            };
        }
    }

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
            // In full panel mode (AI, Timer), the panel owns input — skip search
            if (IsFullPanelActive) return;

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
            else if (ActiveCategory is not null && IsActionScope(ActiveCategory) && ScopeIconGlyph is not null)
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

            _ = DebouncedSearchAsync(effectiveQuery, ct)
                .ContinueWith(t => _log.Warning("DebouncedSearchAsync unexpected error", t.Exception!.InnerException!),
                    TaskContinuationOptions.OnlyOnFaulted);
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
            await Task.Delay(Config.SearchDelay, ct);
            if (Application.Current is not null)
                await Application.Current.Dispatcher.InvokeAsync(() => RunSearch(effectiveQuery, ct));
            else
                RunSearch(effectiveQuery, ct);
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
        ActiveScopeId = "all";
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
        UpdateFilePreview();
    }

    // ── File Preview ─────────────────────────────────────────────────

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".cs", ".py", ".js", ".json", ".xml", ".md", ".log", ".cfg", ".ini",
        ".ts", ".html", ".css", ".yaml", ".yml", ".toml", ".sh", ".bat", ".ps1",
        ".java", ".cpp", ".c", ".h", ".hpp", ".rs", ".go", ".rb", ".php", ".sql",
        ".csv", ".env", ".gitignore", ".editorconfig", ".sln", ".csproj", ".xaml",
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico",
    };

    private string? _previewContent;
    public string? PreviewContent
    {
        get => _previewContent;
        set => SetProperty(ref _previewContent, value);
    }

    private System.Windows.Media.Imaging.BitmapImage? _previewImage;
    public System.Windows.Media.Imaging.BitmapImage? PreviewImage
    {
        get => _previewImage;
        set { if (SetProperty(ref _previewImage, value)) OnPropertyChanged(nameof(HasPreviewImage)); }
    }

    private string? _previewFileName;
    public string? PreviewFileName
    {
        get => _previewFileName;
        set => SetProperty(ref _previewFileName, value);
    }

    private string? _previewFileInfo;
    public string? PreviewFileInfo
    {
        get => _previewFileInfo;
        set => SetProperty(ref _previewFileInfo, value);
    }

    private bool _isPreviewVisible;
    public bool IsPreviewVisible
    {
        get => _isPreviewVisible;
        set => SetProperty(ref _isPreviewVisible, value);
    }

    public bool HasPreviewImage => PreviewImage is not null;

    private void UpdateFilePreview()
    {
        if (!Config.FilePreviewEnabled)
        {
            IsPreviewVisible = false;
            return;
        }

        var result = SelectedResult;
        if (result is null || result.Type != ResultType.File || string.IsNullOrEmpty(result.FilePath))
        {
            IsPreviewVisible = false;
            PreviewContent = null;
            PreviewImage = null;
            PreviewFileName = null;
            PreviewFileInfo = null;
            return;
        }

        var filePath = result.FilePath;
        if (!File.Exists(filePath))
        {
            IsPreviewVisible = false;
            return;
        }

        var ext = Path.GetExtension(filePath);
        PreviewFileName = Path.GetFileName(filePath);

        try
        {
            var fileInfo = new FileInfo(filePath);
            var sizeStr = fileInfo.Length < 1024 ? $"{fileInfo.Length} B"
                        : fileInfo.Length < 1024 * 1024 ? $"{fileInfo.Length / 1024.0:F1} KB"
                        : $"{fileInfo.Length / (1024.0 * 1024.0):F1} MB";
            PreviewFileInfo = $"{sizeStr}  ·  {fileInfo.LastWriteTime:g}";

            if (TextExtensions.Contains(ext) && fileInfo.Length <= 50 * 1024)
            {
                // Text file preview — first 100 lines
                var lines = File.ReadLines(filePath).Take(100);
                PreviewContent = string.Join(Environment.NewLine, lines);
                PreviewImage = null;
                IsPreviewVisible = true;
            }
            else if (ImageExtensions.Contains(ext))
            {
                // Image preview
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 400;
                bitmap.EndInit();
                bitmap.Freeze();
                PreviewImage = bitmap;
                PreviewContent = null;
                IsPreviewVisible = true;
            }
            else
            {
                // Unsupported file — show info only
                PreviewContent = null;
                PreviewImage = null;
                IsPreviewVisible = true;
            }
        }
        catch
        {
            PreviewContent = "Preview unavailable";
            PreviewImage = null;
            IsPreviewVisible = true;
        }
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

        // Find current index without allocating a list copy
        int currentIdx = -1;
        for (int i = 0; i < ScopeFilters.Count; i++)
        {
            if (ScopeFilters[i].Id == ActiveScopeId) { currentIdx = i; break; }
        }
        ActiveScopeId = currentIdx + 1 >= ScopeFilters.Count ? "all" : ScopeFilters[currentIdx + 1].Id;
    }

    private void CommitResults(List<object> items)
    {
        var deduped = new List<object>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (item is SearchResult sr)
            {
                if (!seenIds.Add(sr.Id)) continue;
            }
            deduped.Add(item);
        }

        // Clean up any empty section labels left over after deduplication
        var cleaned = new List<object>();
        for (int i = 0; i < deduped.Count; i++)
        {
            if (deduped[i] is SectionLabel)
            {
                // If this is the last item, or the next item is also a SectionLabel, skip it
                if (i == deduped.Count - 1 || deduped[i + 1] is SectionLabel) continue;
            }
            cleaned.Add(deduped[i]);
        }

        _rawResults = cleaned;
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

    /// <summary>Strips known action keyword prefix from Query, returning the sub-query (e.g. "5m" from "timer 5m").</summary>
    private string GetActionSubQuery()
    {
        var detected = DetectActionKeyword(Query);
        return detected?.subQuery ?? Query;
    }

    private void UpdateActionPreview()
    {
        var result = SelectedResult;
        if (result?.Type != ResultType.Action || result.ActionId is null)
        {
            // Only clear if no action is currently executing (timer running)
            if (ActiveActionPanel is "timer" && _timer.TimerRunning) return;
            ActiveActionPanel = null;
            return;
        }

        var subQuery = GetActionSubQuery();
        ActionPreviewTitle = result.Name;
        ActionPreviewSubtitle = result.Subtitle ?? string.Empty;
        ActionPreviewState = "Ready";
        ActionResultText = result.Name;
        ActionResultSubText = result.Subtitle ?? string.Empty;
        ActionPreviewQuery = string.IsNullOrWhiteSpace(subQuery) ? Query : subQuery;

        // Only timer needs the old ActiveActionPanel for live preview
        if (result.ActionId == "timer")
        {
            ActiveActionPanel = result.ActionId;
            _timer.StartTimerPreview(subQuery);
        }
        else
        {
            ActiveActionPanel = null;
        }
    }

    /// <summary>Close the action preview panel (called from XAML close button or Escape).</summary>
    public void CloseActionPanel()
    {
        ActiveActionPanel = null;
        ActionResultText = string.Empty;
        ActionResultSubText = string.Empty;
        ActionPreviewTitle = string.Empty;
        ActionPreviewSubtitle = string.Empty;
        ActionPreviewState = string.Empty;
        ActionPreviewQuery = string.Empty;
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

        bool activeScopeStillValid = ActiveScopeId == "all";

        foreach (var (id, (label, count)) in counts)
        {
            ScopeFilters.Add(new Spur.Models.ScopeFilterItem { Id = id, Label = label, Count = count });
            if (id == ActiveScopeId) activeScopeStillValid = true;
        }

        if (!activeScopeStillValid)
            ActiveScopeId = "all";
    }



    /// <summary>
    /// Detects action keywords at the start of the query (e.g. "sys ", "timer ").
    /// Returns the action ID, icon glyph, and stripped sub-query, or null.
    /// </summary>
    private (string actionId, string iconGlyph, string subQuery)? DetectActionKeyword(string query)
    {
        if (string.IsNullOrEmpty(query)) return null;

        // Auto-build from registry — each add-on owns its own Id, Keyword, IconGlyph.
        // No hand-coded IDs means no mismatch bugs.
        if (_keywordMap is null)
        {
            _keywordMap = new();
            foreach (var addOn in _addOns.GetEnabled())
            {
                if (string.IsNullOrEmpty(addOn.Keyword)) continue;
                _keywordMap[addOn.Keyword] = (addOn.Id, addOn.IconGlyph);
            }
            // Category filters (not add-ons — they filter existing results)
            _keywordMap[Config.KeywordClipboard] = ("clipboard", "clipboard");
            _keywordMap[Config.KeywordFiles]     = ("files",     "\ue70a");
            _keywordMap[Config.KeywordApps]      = ("apps",      "\ue71d");
        }

        foreach (var (keyword, (actionId, icon)) in _keywordMap)
        {
            if (string.IsNullOrEmpty(keyword)) continue;
            if (keyword == ">" && query.StartsWith(">", StringComparison.Ordinal))
            {
                var subQuery = query[1..];
                return (actionId, icon, subQuery);
            }

            var prefix = keyword + " ";
            if (query.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var subQuery = query[prefix.Length..];
                return (actionId, icon, subQuery);
            }

            if (keyword.Length > 1 && query.Equals(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return (actionId, icon, string.Empty);
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
            _ = SearchAsync(query, ct)
                .ContinueWith(t => _log.Warning("SearchAsync unexpected error", t.Exception!.InnerException!),
                    TaskContinuationOptions.OnlyOnFaulted);
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

            if (Application.Current is not null)
                Application.Current.Dispatcher.Invoke(() => CommitResults(newResults));
            else
                CommitResults(newResults);
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
        var actionInput = GetActionExecutionInput(result);

        switch (result.Type)
        {
            case ResultType.App:       OpenAppResult(result); break;
            case ResultType.File:      OpenFileResult(result); break;
            case ResultType.Clipboard: OpenClipboardResult(result); break;
            case ResultType.Action:    await OpenActionResult(result, actionInput); break;
        }
    }

    private void OpenAppResult(SearchResult result)
    {
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
    }

    private void OpenFileResult(SearchResult result)
    {
        if (result.FilePath is not null)
            Launch(result.FilePath);
        HideAfterLaunch();
    }

    private void OpenClipboardResult(SearchResult result)
    {
        if (result.ClipContent is not null)
        {
            // Suppress the watcher to prevent duplicate entries
            if (_clipboard is ClipboardServiceImpl impl)
                impl.SuppressNextCapture();

            _clipboard.Add(result.ClipContent);   // promote to top first
            _clipboard.CopyTextToSystem(result.ClipContent);
        }
        // TODO: Add image clipboard support via result.ClipImage (BitmapSource).
        HideAfterLaunch();
    }

    private async Task OpenActionResult(SearchResult result, string actionInput)
    {
        // AI action enters AI chat mode directly (no dispatcher)
        if (result.ActionId == "ai")
        {
            EnterAiMode();
            return;
        }

        var state = await _actionDispatcher.DispatchAsync(result, actionInput);
        ApplyActionPanelState(state);
    }

    private void ApplyActionPanelState(ActionPanelState state)
    {
        if (state.ShouldHide) return;

        if (!string.IsNullOrEmpty(state.Title))
            ActionPreviewTitle = state.Title;

        if (!string.IsNullOrEmpty(state.Subtitle))
            ActionPreviewSubtitle = state.Subtitle;

        if (!string.IsNullOrEmpty(state.State))
            ActionPreviewState = state.State;

        if (!string.IsNullOrEmpty(state.ResultText))
            ActionResultText = state.ResultText;

        if (!string.IsNullOrEmpty(state.ResultSubText))
            ActionResultSubText = state.ResultSubText;

        // Tier 3: Sustained add-ons get full panel takeover
        if (state.PanelId is "ai" or "timer")
        {
            EnterAddOnPanel(state.PanelId);
            return;
        }

        // Tier 1 & 2: Instant/interactive — show confirmation toast, then auto-hide
        var toastText = state.State == "Copied" ? $"✓ Copied to clipboard"
                      : state.State == "Completed" && !string.IsNullOrEmpty(state.ResultSubText) ? $"✓ {state.ResultSubText}"
                      : state.State == "Completed" ? $"✓ {state.Title}"
                      : state.State == "Error" ? $"✗ {state.ResultText}"
                      : !string.IsNullOrEmpty(state.ResultSubText) ? $"✓ {state.ResultSubText}"
                      : !string.IsNullOrEmpty(state.ResultText) ? $"✓ {state.ResultText}"
                      : null;

        if (toastText is not null)
        {
            ShowToast(toastText);

            // Open the file for Quick Note after saving
            if (state.PanelId == "note" && state.State == "Completed" && !string.IsNullOrEmpty(state.ResultText))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = state.ResultText,
                        UseShellExecute = true
                    });
                }
                catch { /* file might not exist */ }
            }
        }
    }

    // ── Toast Notification ──────────────────────────────────────────
    private string? _toastMessage;
    public string? ToastMessage
    {
        get => _toastMessage;
        set => SetProperty(ref _toastMessage, value);
    }

    private bool _isToastVisible;
    public bool IsToastVisible
    {
        get => _isToastVisible;
        set => SetProperty(ref _isToastVisible, value);
    }

    private CancellationTokenSource? _toastCts;

    public void ShowToast(string message, int durationMs = 1500)
    {
        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();
        var token = _toastCts.Token;

        ToastMessage = message;
        IsToastVisible = true;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(durationMs, token);
                if (!token.IsCancellationRequested)
                {
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        IsToastVisible = false;
                        ToastMessage = null;
                    });
                }
            }
            catch (TaskCanceledException) { /* Intentional: toast dismissed early */ }
            catch (Exception ex) { _log.Warning("Toast timer failed", ex); }
        }, token);
    }

    private string GetActionExecutionInput(SearchResult result)
    {
        var subQuery = GetActionSubQuery().Trim();
        var id = result.Id ?? string.Empty;

        if (result.ActionId == "timer" && id.StartsWith("timer:", StringComparison.OrdinalIgnoreCase))
            return id["timer:".Length..].Trim();

        if (result.ActionId == "system" && id.StartsWith("system:", StringComparison.OrdinalIgnoreCase))
            return id["system:".Length..].Trim();

        if (result.ActionId == "pw" && id.StartsWith("action:pw:", StringComparison.OrdinalIgnoreCase))
            return id["action:pw:".Length..].Trim();

        if (result.ActionId == "kill" && id.StartsWith("kill:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = id.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return parts[1].Trim();
        }

        if (result.ActionId == "shell")
        {
            var extra = _addOns.FindByKeyword(result.ActionId);
            var keyword = extra?.Keyword ?? string.Empty;
            var trimmed = Query.Trim();
            if (string.IsNullOrWhiteSpace(keyword)) return trimmed;

            if (keyword == ">" && trimmed.StartsWith(">", StringComparison.Ordinal))
                return trimmed[1..].Trim();

            var prefix = keyword + " ";
            return trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? trimmed[prefix.Length..].Trim()
                : trimmed;
        }

        return string.IsNullOrWhiteSpace(subQuery) ? Query.Trim() : subQuery;
    }

    [RelayCommand]
    public void OpenFolder()
    {
        var result = SelectedResult;
        if (result is null) return;

        switch (result.Type)
        {
            case ResultType.Clipboard:
                if (result.ClipContent is not null)
                    _clipboard.CopyTextToSystem(result.ClipContent);
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

        if (string.IsNullOrWhiteSpace(targetPath) || !File.Exists(targetPath))
        {
            _notification.Show("Spur", "File not found or has been moved.");
            return;
        }

        try
        {
            Process.Start("explorer.exe", $"/select,\"{targetPath}\"");
        }
        catch (Exception ex) { _log.Warning("OpenFolder failed", ex); }
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
            _clipboard.CopyTextToSystem(path);
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

    /// <summary>Opens the Windows file properties dialog for the selected result.</summary>
    [RelayCommand]
    public void ShowProperties()
    {
        var result = SelectedResult;
        if (result is null) return;

        string? target = result.Type switch
        {
            ResultType.App  => result.ExePath ?? result.LnkPath,
            ResultType.File => result.FilePath,
            _               => null,
        };

        if (string.IsNullOrWhiteSpace(target)) return;

        try
        {
            var info = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{target}\"",
                UseShellExecute = true,
            };
            // Use shell verb "properties" via ShellExecuteEx
            Helpers.ShellProperties.Show(target);
        }
        catch (Exception ex)
        {
            _log.Warning("ShowProperties failed", ex);
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

    /// <summary>Removes a single clipboard item from history.</summary>
    [RelayCommand]
    public void RemoveClipboardItem(SearchResult result)
    {
        if (result.Type != ResultType.Clipboard) return;
        if (result.ClipContent is null) return;

        var entry = _clipboard.GetHistory().FirstOrDefault(e => e.Timestamp == result.ClipTimestamp);
        if (entry is not null)
            _clipboard.RemoveById(entry.Id);

        OnQueryChanged(Query ?? string.Empty);
    }

    private void Launch(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex)
        {
            _log.Warning("Launch failed", ex);
            _notification.Show("Spur", $"Failed to launch: {ex.Message}");
        }
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
        _ai.Dispose();
        _clipboardVm.Dispose();
    }

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
        // Note: Timer is intentionally NOT stopped here.
        // The timer persists across window hide/show cycles and search changes.
        // Only the user's explicit Cancel command stops it.
    }

    private int FindFirstResultIndex()
    {
        for (int i = 0; i < Results.Count; i++)
            if (Results[i] is SearchResult) return i;
        return -1;
    }
}
