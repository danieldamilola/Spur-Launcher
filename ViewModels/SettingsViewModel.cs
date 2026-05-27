namespace Volt.ViewModels;

/// <summary>
/// Wraps VoltConfig with instant-save semantics.
/// Every property setter persists the config to disk immediately.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ConfigService  _configService;
    private readonly MainViewModel  _main;
    private VoltConfig              _config;

    public SettingsViewModel(VoltConfig config, ConfigService configService, MainViewModel main)
    {
        _config        = config;
        _configService = configService;
        _main          = main;
    }

    // ── Theme ──────────────────────────────────────────────────────
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
        Save();
        ThemeManager.Apply(theme);
        OnPropertyChanged(nameof(ThemeDark));
        OnPropertyChanged(nameof(ThemeLight));
        OnPropertyChanged(nameof(ThemeSystem));
    }

    // ── Shortcut ───────────────────────────────────────────────────
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

    // ── AI Provider ────────────────────────────────────────────────
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

    // ── API Key (routed to current provider) ───────────────────────
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

    // ── AI Model ───────────────────────────────────────────────────
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

    // ── Results count ──────────────────────────────────────────────
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

    // ── Toggles ────────────────────────────────────────────────────
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
            _config.ClipboardEnabled = value;
            _main.Config = _config.Clone();
            Save();
            OnPropertyChanged();
        }
    }

    // ── Clear usage data ───────────────────────────────────────────
    [RelayCommand]
    private void ClearUsageData()
    {
        new FrequencyService().ClearAll();
        OnPropertyChanged(nameof(Version));
    }

    // ── Version ────────────────────────────────────────────────────
    public string Version => "Volt v0.1.0";

    // ── Persist ───────────────────────────────────────────────────
    private void Save() => _configService.Save(_config);
}
