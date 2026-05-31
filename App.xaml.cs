using System.Windows.Interop;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Arc.ViewModels;
using Arc.Views;
using Arc.Services;
using Arc.Converters;

namespace Arc;

public partial class App : Application
{
    private MainWindow?         _window;
    private MainViewModel?      _vm;
    private IHotkeyService?     _hotkey;
    private ClipboardWatcher?   _clipboard;
    private TaskbarIcon?        _trayIcon;
    private ILogger?            _fileLogger;
    private IServiceProvider?   _services;
    private SettingsWindow?     _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        Velopack.VelopackApp.Build().Run();
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

        // ── DI Container ─────────────────────────────────────────────
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

        // Logging
        services.AddSingleton<ILogger>(_fileLogger);

        // Config (load before registering so it's available)
        var configSvc = new ConfigService(_fileLogger);
        var config = configSvc.Load();
        config.Validate();
        configSvc.Save(config);
        services.AddSingleton<IConfigService>(configSvc);
        services.AddSingleton(config); // Register ArcConfig directly

        // Theme
        var themeMgr = new ThemeManagerImpl(_fileLogger);
        themeMgr.Apply(config.Theme);
        services.AddSingleton<IThemeManager>(themeMgr);

        // ── Surface colors ────────────────────────────────────────────
        try { UpdateSurfaceColors(config.BackgroundColor); }
        catch (Exception ex) { _fileLogger.Warning("UpdateSurfaceColors failed", ex); }

        // Core services
        services.AddSingleton<IClipboardService, ClipboardServiceImpl>();
        services.AddSingleton<IStartupService, StartupServiceImpl>();
        services.AddSingleton<IIconService, IconServiceImpl>();
        services.AddSingleton<INotificationService, NotificationServiceImpl>();
        services.AddSingleton<IAppDiscoveryService, AppDiscoveryService>();
        services.AddSingleton<IFileSearchService, FileSearchService>();
        services.AddSingleton<IAiService, AiService>();
        services.AddSingleton<IFrequencyService, FrequencyService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();

        _services = services.BuildServiceProvider();

        // ── Initialize static facades (backward compat bridge) ───────
        ClipboardService.Initialize(_services.GetRequiredService<IClipboardService>(), _fileLogger);
        ThemeManager.Initialize(_services.GetRequiredService<IThemeManager>(), _fileLogger);
        StartupService.Initialize(_services.GetRequiredService<IStartupService>(), _fileLogger);
        IconService.Initialize(_services.GetRequiredService<IIconService>(), _fileLogger);
        NotificationService.Initialize(_services.GetRequiredService<INotificationService>(), _fileLogger);

        // ── Initialize converter references ─────────────────────────
        PathToIconConverter.IconService = _services.GetRequiredService<IIconService>();

        // ── Resolve MainViewModel ─────────────────────────────────────
        _vm = _services.GetRequiredService<MainViewModel>();

        // Push config to services
        _services.GetRequiredService<IFileSearchService>().MaxDepth = config.MaxFileDepth;
        _services.GetRequiredService<IClipboardService>().MaxItems = config.ClipboardHistorySize;

        // ── Create main window ────────────────────────────────────────
        _window = new MainWindow();
        _window.SetViewModel(_vm);
        _window.Opacity = config.WindowOpacity;
        _window.Width   = config.LauncherWidth;
        _window.Show();
        _window.Hide();

        // ── Settings window (singleton, independent of launcher bar) ──
        _settingsWindow = new SettingsWindow();
        _settingsWindow.SetViewModel(_vm.Settings);
        _vm.OpenSettingsRequested += () =>
        {
            if (_settingsWindow.IsVisible)
                _settingsWindow.Activate();
            else
                _settingsWindow.Show();
        };

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
            var clipSvc = _services.GetRequiredService<IClipboardService>();
            _clipboard = new ClipboardWatcher(clipSvc, _fileLogger);
            _clipboard.Attach(hwnd);
        }

        // ── Onboarding (first launch only) ───────────────────────────
        if (!config.OnboardingComplete)
        {
            var onboarding = new OnboardingWindow();
            onboarding.OnCompleted += () =>
            {
                config.OnboardingComplete = true;
                _services.GetRequiredService<IConfigService>().Save(config);
            };
            onboarding.ShowDialog();
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

        // ── Check for updates ─────────────────────────────────────────
        _ = CheckForUpdatesAsync();

        _fileLogger.Info("Arc started successfully.");
    }

    // ═══════════════════════════════════════════════════════════════
    // Settings → behaviour bridge
    // ═══════════════════════════════════════════════════════════════

    private void WireSettings(SettingsViewModel settings)
    {
        settings.PropertyChanged += (_, e) =>
        {
            if (_services is null) return;

            switch (e.PropertyName)
            {
                case "":
                case null:
                    if (_window is not null)
                    {
                        _window.Opacity = settings.WindowOpacity;
                        _window.Width   = settings.LauncherWidth;
                    }
                    _services.GetRequiredService<IClipboardService>().MaxItems = settings.ClipboardHistorySize;
                    _services.GetRequiredService<IFileSearchService>().MaxDepth = settings.MaxFileDepth;
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
                    _services.GetRequiredService<IClipboardService>().MaxItems = settings.ClipboardHistorySize;
                    break;

                case nameof(SettingsViewModel.MaxFileDepth):
                    _services.GetRequiredService<IFileSearchService>().MaxDepth = settings.MaxFileDepth;
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
        if (Resources["Surface"] is SolidColorBrush surfaceBrush) surfaceBrush.Color = color;
        if (Resources["SurfaceLow"] is SolidColorBrush surfaceLowBrush) surfaceLowBrush.Color = lightColor;
        if (Resources["DynamicSurface"] is SolidColorBrush dynamicSurfaceBrush) dynamicSurfaceBrush.Color = color;
        if (Resources["DynamicSurfaceLow"] is SolidColorBrush dynamicSurfaceLowBrush) dynamicSurfaceLowBrush.Color = lightColor;
        if (Resources["HoverBg"] is SolidColorBrush hoverBrush) hoverBrush.Color = Color.FromArgb(26, 255, 255, 255);
    }

    // ── Hotkey toggle ─────────────────────────────────────────────────
    private void ToggleWindow()
    {
        if (_window is null) return;
        Dispatcher.InvokeAsync(() =>
        {
            if (_window.IsVisible) _window.HideWindow();
            else _window.ShowWindow();
        });
    }

    // ── Tray icon ─────────────────────────────────────────────────────
    private void BuildTrayIcon()
    {
        _trayIcon = new TaskbarIcon { ToolTipText = "Arc", ContextMenu = BuildTrayMenu() };
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
        settings.Click += (_, _) => { _vm?.OpenSettingsCommand.Execute(null); };
        quit.Click     += (_, _) => Shutdown();
        menu.Items.Add(open);
        menu.Items.Add(settings);
        menu.Items.Add(new Separator());
        menu.Items.Add(quit);
        return menu;
    }

    // ── Updates ───────────────────────────────────────────────────────
    private const string UpdateUrl = "https://github.com/danieldamilola/Arc-Launcher/releases/latest/download";
    private async Task CheckForUpdatesAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var mgr = new Velopack.UpdateManager(UpdateUrl);
            var update = await mgr.CheckForUpdatesAsync();
            if (update is null) return;
            _fileLogger?.Info($"Update found: {update.TargetFullRelease.Version}");
            await mgr.DownloadUpdatesAsync(update, p => _fileLogger?.Info($"Downloading update: {p}%"));
            _fileLogger?.Info("Applying update and restarting...");
            mgr.ApplyUpdatesAndRestart(update);
        }
        catch (Exception ex) { _fileLogger?.Info($"Update check skipped: {ex.Message}"); }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_vm?.Config.ClearClipboardOnExit == true)
            _services?.GetRequiredService<IClipboardService>().Clear();
        _vm?.Shutdown();
        _trayIcon?.Dispose();
        _hotkey?.Dispose();
        _clipboard?.Dispose();
        // Close and dispose settings window
        _settingsWindow?.Close();
        // Dispose the DI container (frees all singleton IDisposable services)
        (_services as IDisposable)?.Dispose();
        _fileLogger?.Info("Arc shutting down.");
        if (_fileLogger is IDisposable d) d.Dispose();
        base.OnExit(e);
    }
}
