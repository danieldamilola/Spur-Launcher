using System.Windows.Interop;
using Hardcodet.Wpf.TaskbarNotification;
using Volt.ViewModels;

namespace Volt;

public partial class App : Application
{
    private MainWindow?         _window;
    private MainViewModel?      _vm;
    private HotkeyService?      _hotkey;
    private ClipboardWatcher?   _clipboard;
    private TaskbarIcon?        _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handlers — prevent silent crashes, log to console
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Console.WriteLine($"[Volt] FATAL: {ex?.GetType().Name}: {ex?.Message}");
            Console.WriteLine(ex?.StackTrace);
        };
        DispatcherUnhandledException += (_, args) =>
        {
            Console.WriteLine($"[Volt] UI ERROR: {args.Exception.GetType().Name}: {args.Exception.Message}");
            Console.WriteLine(args.Exception.StackTrace);
            args.Handled = true; // keep app alive
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Console.WriteLine($"[Volt] TASK ERROR: {args.Exception.GetType().Name}: {args.Exception.Message}");
            args.SetObserved();
        };

        // Apply configured theme
        var config = new ConfigService().Load();
        ThemeManager.Apply(config.Theme);

        // Create ViewModel
        _vm = new MainViewModel();

        // Create main window (hidden until hotkey)
        _window = new MainWindow();
        _window.SetViewModel(_vm);

        // Apply visual config to window before showing
        _window.Opacity = config.WindowOpacity;
        _window.Width   = config.LauncherWidth;
        _window.Show();   // Show once to get HWND
        _window.Hide();

        // Register global hotkey after HWND is ready
        var hwnd = new WindowInteropHelper(_window).Handle;

        if (config.HotkeyEnabled)
        {
            _hotkey = new HotkeyService();
            _hotkey.Register(hwnd, config.Shortcut, ToggleWindow);
        }

        // Clipboard watcher
        if (config.ClipboardEnabled)
        {
            _clipboard = new ClipboardWatcher();
            _clipboard.Attach(hwnd);
        }

        // Force reindex if configured
        if (config.ReIndexOnStartup)
        {
            try
            {
                var cachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Volt", "volt.catalog.json");
                if (File.Exists(cachePath)) File.Delete(cachePath);
            }
            catch { /* cache may be in use — next launch will refresh */ }
        }

        // Wire settings changes to live behaviour
        WireSettings(_vm.Settings);

        // System tray icon
        BuildTrayIcon();
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
                        _hotkey = new HotkeyService();
                        _hotkey.Register(hwnd, settings.Shortcut, ToggleWindow);
                    }
                    else
                    {
                        _hotkey?.Dispose();
                        _hotkey = null;
                    }
                    break;

                case nameof(SettingsViewModel.Shortcut):
                    // Re-register with new shortcut if hotkey is enabled
                    if (_window is not null && settings.HotkeyEnabled && _hotkey is not null)
                    {
                        _hotkey.Dispose();
                        _hotkey = new HotkeyService();
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
            }
        };
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
            ToolTipText = "Volt",
            ContextMenu = BuildTrayMenu(),
        };

        try
        {
            using var stream = System.Reflection.Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Volt.Assets.volt.ico");
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
        catch { /* no icon — tray shows default */ }

        _trayIcon.TrayLeftMouseDown += (_, _) => ToggleWindow();
    }

    private ContextMenu BuildTrayMenu()
    {
        var menu     = new ContextMenu();
        var open     = new MenuItem { Header = "Open Volt" };
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
        // Clear clipboard history if configured
        if (_vm?.Config.ClearClipboardOnExit == true)
            ClipboardService.Clear();

        _trayIcon?.Dispose();
        _hotkey?.Dispose();
        _clipboard?.Dispose();
        base.OnExit(e);
    }
}
