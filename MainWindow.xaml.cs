using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using Volt.ViewModels;

namespace Volt;

public partial class MainWindow : Window
{
    // ── Win32 imports ─────────────────────────────────────────────
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr,
        ref int attrValue, int attrSize);

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND  = 2;

    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMSBT_TRANSIENTWINDOW    = 3;

    // ── State ─────────────────────────────────────────────────────
    private bool _isVisible;
    private MainViewModel? _vm;
    private bool _isHovering;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void SetViewModel(MainViewModel vm)
    {
        _vm = vm;
        DataContext = vm;

        vm.PropertyChanged += OnVmChanged;
        vm.RequestHide     += HideWindow;

        // Initialize category circles visually
        UpdateCategoryVisuals();
        UpdateCategoryVisibility();
    }

    // ── Loaded ───────────────────────────────────────────────────
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        TryApplyDwmEffects(hwnd);
    }

    private static void TryApplyDwmEffects(IntPtr hwnd)
    {
        // No DWM effects needed. The RootBorder handles rounding and shadow.
        // Applying DWMWCP_ROUND to an AllowsTransparency=True window causes
        // a faint rectangular grey line bug on Windows 11.
    }

    // ── Show / Hide with animation ────────────────────────────────
    public void ShowWindow()
    {
        if (_isVisible) { Activate(); return; }
        _isVisible = true;

        // Center on primary screen
        var screen = System.Windows.SystemParameters.WorkArea;
        Left = (screen.Width  - Width)  / 2 + screen.Left;
        Top  = screen.Height  * 0.30    + screen.Top;

        Show();
        Activate();
        SearchBarControl.FocusInput();

        AnimateIn();
    }

    public void HideWindow()
    {
        if (!_isVisible) return;
        _isHovering = false;
        UpdateCategoryVisibility();
        AnimateOut(() =>
        {
            Hide();
            _isVisible = false;
            _vm?.Reset();
        });
    }

    private void AnimateIn()
    {
        var fadeIn = new DoubleAnimation(0, 1,
            new Duration(TimeSpan.FromMilliseconds(160)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var scaleIn = new DoubleAnimation(0.96, 1,
            new Duration(TimeSpan.FromMilliseconds(160)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        BeginAnimation(OpacityProperty, fadeIn);
        WindowScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleIn);
        WindowScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleIn);
    }

    private void AnimateOut(Action onComplete)
    {
        var fadeOut = new DoubleAnimation(1, 0,
            new Duration(TimeSpan.FromMilliseconds(100)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };

        var scaleOut = new DoubleAnimation(1, 0.96,
            new Duration(TimeSpan.FromMilliseconds(100)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };

        fadeOut.Completed += (_, _) => onComplete();

        BeginAnimation(OpacityProperty, fadeOut);
        WindowScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleOut);
        WindowScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleOut);
    }

    // ── ViewModel property change → update window shape ──────────
    private void OnVmChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.HasResults)
                           or nameof(MainViewModel.IsSettingsOpen)
                           or nameof(MainViewModel.IsPreviewVisible))
        {
            Dispatcher.InvokeAsync(UpdateWindowState);
        }
        if (e.PropertyName == nameof(MainViewModel.ActiveCategory))
        {
            Dispatcher.InvokeAsync(UpdateCategoryVisuals);
            Dispatcher.InvokeAsync(UpdateCategoryVisibility);
        }
    }

    private void UpdateWindowState()
    {
        if (_vm is null) return;

        bool hasContent = _vm.HasResults || _vm.IsSettingsOpen;

        // Show/hide content area
        ContentArea.Visibility = hasContent ? Visibility.Visible : Visibility.Collapsed;

        // Switch corner radius: pill when idle, rounded rect when expanded (applied to SearchCardBorder instead of RootBorder)
        SearchCardBorder.CornerRadius = hasContent
            ? (CornerRadius)TryFindResource("RadiusWindow")
            : (CornerRadius)TryFindResource("RadiusPill");

        // Show/hide preview panel column
        var previewWidth = _vm.IsPreviewVisible && !_vm.IsSettingsOpen ? 300.0 : 0.0;
        PreviewColumn.Width = new GridLength(previewWidth);
        PreviewPanelControl.Visibility = previewWidth > 0
            ? Visibility.Visible : Visibility.Collapsed;

        // Show/hide settings overlay
        SettingsViewControl.Visibility = _vm.IsSettingsOpen
            ? Visibility.Visible : Visibility.Collapsed;
        ResultsListControl.Visibility  = _vm.IsSettingsOpen
            ? Visibility.Collapsed : Visibility.Visible;
    }

    // ── Keyboard handling ─────────────────────────────────────────
    /// <summary>Intercept nav keys before the TextBox swallows them.</summary>
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (_vm is null) return;

        switch (e.Key)
        {
            case Key.Down:
                _vm.MoveSelection(+1);
                e.Handled = true;
                break;

            case Key.Up:
                _vm.MoveSelection(-1);
                e.Handled = true;
                break;

            case Key.Tab:
                _vm.CycleCategory();
                e.Handled = true;
                break;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_vm is null) return;

        switch (e.Key)
        {
            case Key.Escape:
                if (_vm.IsSettingsOpen)
                    _vm.IsSettingsOpen = false;
                else if (!string.IsNullOrEmpty(_vm.Query))
                    _vm.Query = string.Empty;
                else
                    HideWindow();
                e.Handled = true;
                break;

            case Key.Enter:
                if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                    _vm.RunAsAdminCommand.Execute(null);
                else if (Keyboard.Modifiers == ModifierKeys.Control)
                    _vm.OpenFolderCommand.Execute(null);
                else
                    _vm.OpenSelectedCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.P when Keyboard.Modifiers == ModifierKeys.Control:
                _vm.TogglePinCommand.Execute(_vm.SelectedResult);
                e.Handled = true;
                break;

            case Key.D1 when Keyboard.Modifiers == ModifierKeys.Control:
                _vm.ActiveCategory = _vm.ActiveCategory == "apps" ? null : "apps";
                e.Handled = true;
                break;

            case Key.D2 when Keyboard.Modifiers == ModifierKeys.Control:
                _vm.ActiveCategory = _vm.ActiveCategory == "files" ? null : "files";
                e.Handled = true;
                break;

            case Key.D3 when Keyboard.Modifiers == ModifierKeys.Control:
                _vm.ActivateClipboardCategory();
                e.Handled = true;
                break;

            case Key.D4 when Keyboard.Modifiers == ModifierKeys.Control:
                _vm.ActiveCategory = _vm.ActiveCategory == "actions" ? null : "actions";
                e.Handled = true;
                break;

            case Key.OemComma when Keyboard.Modifiers == ModifierKeys.Control:
                _vm.OpenSettingsCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    // ── Click outside to close ────────────────────────────────────
    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        HideWindow();
    }

    // ── Drag to reposition ────────────────────────────────────────
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.Source is TextBox or System.Windows.Controls.Primitives.ScrollBar) return;
        try { DragMove(); } catch { }
    }

    // ── Separate Category Circles Hover & Visual Sync ──────────────
    private void OnMainGridMouseEnter(object sender, MouseEventArgs e)
    {
        _isHovering = true;
        UpdateCategoryVisibility();
    }

    private void OnMainGridMouseLeave(object sender, MouseEventArgs e)
    {
        _isHovering = false;
        UpdateCategoryVisibility();
    }

    /// <summary>Show separate category circles on cursor hover or active category filter.</summary>
    private void UpdateCategoryVisibility()
    {
        bool visible = _isHovering || (_vm is not null && _vm.ActiveCategory is not null);
        double targetWidth = visible ? 180.0 : 0.0;

        if (Math.Abs(CategoryPanelContainer.Width - targetWidth) < 0.5) return;

        var slide = new DoubleAnimation(targetWidth,
            new Duration(TimeSpan.FromMilliseconds(200)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        CategoryPanelContainer.BeginAnimation(Border.WidthProperty, slide);
    }

    private void OnCategoryClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || _vm is null) return;
        var category = btn.Tag as string;

        if (category == "clipboard")
        {
            _vm.ActivateClipboardCategory();
        }
        else
        {
            _vm.ActiveCategory = _vm.ActiveCategory == category ? null : category;
        }

        SearchBarControl.FocusInput();
    }

    private void UpdateCategoryVisuals()
    {
        if (_vm is null) return;
        SetActive(BtnApps,      IconApps,      _vm.ActiveCategory == "apps");
        SetActive(BtnFiles,     IconFiles,     _vm.ActiveCategory == "files");
        SetActive(BtnClipboard, IconClipboard, _vm.ActiveCategory == "clipboard");
        SetActive(BtnActions,   IconActions,   _vm.ActiveCategory == "actions");
    }

    private static void SetActive(Button btn, System.Windows.Shapes.Path icon, bool active)
    {
        var accent     = TryBrush("Accent")     ?? Brushes.DodgerBlue;
        var accentWash = TryBrush("AccentWash") ?? Brushes.Transparent;
        var muted      = TryBrush("TextMuted")  ?? Brushes.Gray;

        btn.Background = active ? accentWash : Brushes.Transparent;
        icon.Stroke    = active ? accent      : muted;
    }

    private static Brush? TryBrush(string key) =>
        Application.Current.TryFindResource(key) as Brush;
}
