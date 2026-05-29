using System.Windows.Interop;
using Hardcodet.Wpf.TaskbarNotification;
using Arc.ViewModels;

namespace Arc;

public partial class App : Application
{
    private MainWindow?         _window;
    private MainViewModel?      _vm;
    private HotkeyService?      _hotkey;
    private ClipboardWatcher?   _clipboard;
    private TaskbarIcon?        _trayIcon;
    private ILogger?            _fileLogger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ── Logging ──────────────────────────────────────────────────
        _fileLogger = new FileLogger();

        // ── Global exception handlers ─────────────────────────────────
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            _fileLogger.Fatal($"Unhandled: {ex?.GetType().Name}: {ex?.Message}", ex);
        };
        DispatcherUnhandledException += (_, args) =>
        {
            _fileLogger.Error($"UI: {args.Exception.GetType().Name}: {args.Exception.Message}", args.Exception);
            args.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _fileLogger.Warning($"Unobserved task: {args.Exception.GetType().Name}: {args.Exception.Message}", args.Exception);
            args.SetObserved();
        };

        // ── Initialize service facades with logger ────────────────────
        ClipboardService.Initialize(new ClipboardServiceImpl(_fileLogger), _fileLogger);
        ThemeManager.Initialize(new ThemeManagerImpl(_fileLogger), _fileLogger);
        StartupService.Initialize(new StartupServiceImpl(_fileLogger), _fileLogger);
        IconService.Initialize(new IconServiceImpl(_fileLogger), _fileLogger);
        NotificationService.Initialize(new NotificationServiceImpl(_fileLogger), _fileLogger);

        // ── Load and validate config ──────────────────────────────────
        var configSvc = new ConfigService(_fileLogger);
        var config = configSvc.Load();
        config.Validate();
        configSvc.Save(config);
        ThemeManager.Apply(config.Theme);

        // ── Surface colors ────────────────────────────────────────────
        try { UpdateSurfaceColors(config.BackgroundColor); }
        catch (Exception ex) { _fileLogger.Warning("UpdateSurfaceColors failed", ex); }

        // ── Create ViewModel ──────────────────────────────────────────
        _vm = new MainViewModel(config, _fileLogger);

        // ── Create main window (hidden until hotkey) ───────────────────
        _window = new MainWindow();
        _window.SetViewModel(_vm);

        _window.Opacity = config.WindowOpacity;
        _window.Width   = config.LauncherWidth;
        _window.Show();   // Show once to get HWND
        _window.Hide();

        // ── Register global hotkey ────────────────────────────────────
        var hwnd = new WindowInteropHelper(_window).Handle;

        if (config.HotkeyEnabled)
        {
            _hotkey = new HotkeyService(_fileLogger);
            _hotkey.Register(hwnd, config.Shortcut, ToggleWindow);
        }

        // ── Clipboard watcher ─────────────────────────────────────────
        if (config.ClipboardEnabled)
        {
            _clipboard = new ClipboardWatcher(_fileLogger);
            _clipboard.Attach(hwnd);
        }

        // ── Force reindex if configured ───────────────────────────────
        if (config.ReIndexOnStartup)
        {
            try
            {
                var cachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Arc", "arc.catalog.json");
                if (File.Exists(cachePath)) File.Delete(cachePath);
            }
            catch (Exception ex) { _fileLogger.Warning("Cache delete failed", ex); }
        }

        // ── Settings → behaviour bridge ───────────────────────────────
        WireSettings(_vm.Settings);

        // ── System tray icon ──────────────────────────────────────────
        if (config.ShowTrayIcon)
            BuildTrayIcon();

        _fileLogger.Info("Arc started successfully.");
    }

    // ═══════════════════════════════════════════════════════════════
    // Settings → behaviour bridge
    // ═══════════════════════════════════════════════════════════════

    private void WireSettings(SettingsViewModel settings)
    {
        settings.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                // Full reset (ResetToDefaults sends string.Empty)
                case "":
                case null:
                    if (_window is not null)
                    {
                        _window.Opacity = settings.WindowOpacity;
                        _window.Width   = settings.LauncherWidth;
                    }
                    ClipboardService.MaxItems  = settings.ClipboardHistorySize;
                    FileSearchService.MaxDepth = settings.MaxFileDepth;
                    break;

                case nameof(SettingsViewModel.WindowOpacity):
                    if (_window is not null)
                        _window.Opacity = settings.WindowOpacity;
                    break;

                case nameof(SettingsViewModel.LauncherWidth):
                    if (_window is not null)
                        _window.Width = settings.LauncherWidth;
                    break;

                case nameof(SettingsViewModel.HotkeyEnabled):
                    if (_window is null) break;
                    var hwnd = new WindowInteropHelper(_window).Handle;
                    if (settings.HotkeyEnabled)
                    {
                        _hotkey?.Dispose();
                        _hotkey = new HotkeyService(_fileLogger ?? NullLogger.Instance);
                        _hotkey.Register(hwnd, settings.Shortcut, ToggleWindow);
                    }
                    else
                    {
                        _hotkey?.Dispose();
                        _hotkey = null;
                    }
                    break;

                case nameof(SettingsViewModel.Shortcut):
                    if (_window is not null && settings.HotkeyEnabled && _hotkey is not null)
                    {
                        _hotkey.Dispose();
                        _hotkey = new HotkeyService(_fileLogger ?? NullLogger.Instance);
                        var h = new WindowInteropHelper(_window).Handle;
                        _hotkey.Register(h, settings.Shortcut, ToggleWindow);
                    }
                    break;

                case nameof(SettingsViewModel.ClipboardHistorySize):
                    ClipboardService.MaxItems = settings.ClipboardHistorySize;
                    break;

                case nameof(SettingsViewModel.MaxFileDepth):
                    FileSearchService.MaxDepth = settings.MaxFileDepth;
                    break;

                case nameof(SettingsViewModel.ReIndexOnStartup):
                    // No runtime action — read on next launch
                    break;

                case nameof(SettingsViewModel.ShowTrayIcon):
                    if (settings.ShowTrayIcon)
                    {
                        if (_trayIcon is null) BuildTrayIcon();
                    }
                    else
                    {
                        _trayIcon?.Dispose();
                        _trayIcon = null;
                    }
                    break;

                case nameof(SettingsViewModel.BackgroundColor):
                    UpdateSurfaceColors(settings.BackgroundColor);
                    break;
            }
        };
    }

    private void UpdateSurfaceColors(string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        var lightColor = Color.FromRgb(
            (byte)Math.Min(255, color.R + 80),
            (byte)Math.Min(255, color.G + 80),
            (byte)Math.Min(255, color.B + 80)
        );

        if (Resources["Surface"] is SolidColorBrush surfaceBrush)
            surfaceBrush.Color = color;
        if (Resources["SurfaceLow"] is SolidColorBrush surfaceLowBrush)
            surfaceLowBrush.Color = lightColor;
        if (Resources["DynamicSurface"] is SolidColorBrush dynamicSurfaceBrush)
            dynamicSurfaceBrush.Color = color;
        if (Resources["DynamicSurfaceLow"] is SolidColorBrush dynamicSurfaceLowBrush)
            dynamicSurfaceLowBrush.Color = lightColor;
        if (Resources["HoverBg"] is SolidColorBrush hoverBrush)
            hoverBrush.Color = Color.FromArgb(26, 255, 255, 255);
    }

    // ═══════════════════════════════════════════════════════════════
    // Hotkey toggle
    // ═══════════════════════════════════════════════════════════════

    private void ToggleWindow()
    {
        if (_window is null) return;
        Dispatcher.InvokeAsync(() =>
        {
            if (_window.IsVisible)
                _window.HideWindow();
            else
                _window.ShowWindow();
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // Tray icon
    // ═══════════════════════════════════════════════════════════════

    private void BuildTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Arc",
            ContextMenu = BuildTrayMenu(),
        };

        try
        {
            using var stream = System.Reflection.Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Arc.Assets.arc.ico");
            if (stream is not null)
            {
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bmp.StreamSource = stream;
                bmp.EndInit();
                _trayIcon.IconSource = bmp;
            }
        }
        catch (Exception ex) { _fileLogger?.Warning("Tray icon load failed", ex); }

        _trayIcon.TrayLeftMouseDown += (_, _) => ToggleWindow();
    }

    private ContextMenu BuildTrayMenu()
    {
        var menu     = new ContextMenu();
        var open     = new MenuItem { Header = "Open Arc" };
        var settings = new MenuItem { Header = "Settings" };
        var quit     = new MenuItem { Header = "Quit" };

        open.Click     += (_, _) => { _window?.ShowWindow(); };
        settings.Click += (_, _) => { _window?.ShowWindow(); _vm?.OpenSettingsCommand.Execute(null); };
        quit.Click     += (_, _) => Shutdown();

        menu.Items.Add(open);
        menu.Items.Add(settings);
        menu.Items.Add(new Separator());
        menu.Items.Add(quit);
        return menu;
    }

    // ═══════════════════════════════════════════════════════════════
    // Shutdown
    // ═══════════════════════════════════════════════════════════════

    protected override void OnExit(ExitEventArgs e)
    {
        if (_vm?.Config.ClearClipboardOnExit == true)
            ClipboardService.Clear();

        _vm?.Shutdown();
        _trayIcon?.Dispose();
        _hotkey?.Dispose();
        _clipboard?.Dispose();
        _fileLogger?.Info("Arc shutting down.");
        if (_fileLogger is IDisposable d) d.Dispose();
        base.OnExit(e);
    }
}
