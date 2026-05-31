using System.Windows.Interop;
using System.Windows.Media.Animation;
using Arc.ViewModels;

namespace Arc;

public partial class MainWindow : Window
{
    private bool _isVisible;
    private MainViewModel? _vm;
    private bool _isHovering;
    private bool _previewWasVisible;
    private System.Windows.Threading.DispatcherTimer? _cornerRadiusTimer;

    public MainWindow()
    {
        InitializeComponent();
        PreviewPanelControl.RenderTransform = new TranslateTransform(0, 0);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_NCHITTEST = 0x0084;
        if (msg != WM_NCHITTEST) return IntPtr.Zero;

        // Let child elements handle their own hit tests (scrollbars, buttons, etc.)
        var result = NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
        int ht = result.ToInt32() & 0xFFFF;
        if (ht is 1 or 6 or 7) // HTCLIENT, HTHSCROLL, HTVSCROLL
            return result;

        var point = new Point(
            (short)(lParam.ToInt32() & 0xFFFF),
            (short)(lParam.ToInt32() >> 16));
        point = PointFromScreen(point);

        const int border = 6;
        bool left   = point.X <= border;
        bool right  = point.X >= ActualWidth - border;
        bool bottom = point.Y >= ActualHeight - border;
        bool top    = point.Y <= border;

        if (left && bottom)      { handled = true; return (IntPtr)16; }
        if (right && bottom)     { handled = true; return (IntPtr)17; }
        if (left && top)         { handled = true; return (IntPtr)13; }
        if (right && top)        { handled = true; return (IntPtr)14; }
        if (left)                { handled = true; return (IntPtr)10; }
        if (right)               { handled = true; return (IntPtr)11; }
        if (bottom)              { handled = true; return (IntPtr)15; }
        if (top)                 { handled = true; return (IntPtr)12; }

        return IntPtr.Zero;
    }

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }

    public void SetViewModel(MainViewModel vm)
    {
        _vm = vm;
        DataContext = vm;

        vm.PropertyChanged += OnVmChanged;
        vm.RequestHide     += HideWindow;

        UpdateCategoryVisuals();
    }

    // ── Loaded ───────────────────────────────────────────────────
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
    }

    // ── Show / Hide with animation ────────────────────────────────
    public void ShowWindow()
    {
        if (_isVisible) { Activate(); return; }
        _isVisible = true;

        PositionWindow();

        Show();
        Activate();
        SearchBarControl.FocusInput();

        if (_vm?.Config.SoundEffectEnabled == true)
            System.Media.SystemSounds.Asterisk.Play();

        if (_vm?.Config.AnimationEnabled == false)
            Opacity = 1;
        else
            AnimateIn();
    }

    public void HideWindow()
    {
        if (!_isVisible) return;
        _isHovering = false;
        if (_vm?.Config.AnimationEnabled == false)
        {
            Hide();
            _isVisible = false;
            _vm?.Reset();
            return;
        }

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

    private void PositionWindow()
    {
        var screen = System.Windows.SystemParameters.WorkArea;
        var left = (screen.Width - Width) / 2 + screen.Left;
        var top = (screen.Height - Height) / 2 + screen.Top;

        switch (_vm?.Config.SearchWindowPosition)
        {
            case "centerTop":
                top = screen.Height * 0.15 + screen.Top;
                break;
            case "leftTop":
                left = screen.Left + 32;
                top = screen.Top + 32;
                break;
            case "rightTop":
                left = screen.Right - Width - 32;
                top = screen.Top + 32;
                break;
            case "custom":
                return;
        }

        Left = left;
        Top = top;
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

    // ── Preview panel slide-in/out ───────────────────────────────

    private void AnimatePreviewIn()
    {
        var slide = (TranslateTransform)PreviewPanelControl.RenderTransform;
        PreviewPanelControl.Visibility = Visibility.Visible;
        PreviewPanelControl.Opacity = 0;
        slide.X = 8;

        var slideAnim = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(180)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var fadeAnim = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(180)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        slide.BeginAnimation(TranslateTransform.XProperty, slideAnim);
        PreviewPanelControl.BeginAnimation(OpacityProperty, fadeAnim);
    }

    private void AnimatePreviewOut()
    {
        var slide = (TranslateTransform)PreviewPanelControl.RenderTransform;

        var slideAnim = new DoubleAnimation(8, new Duration(TimeSpan.FromMilliseconds(180)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        var fadeAnim = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(180)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        fadeAnim.Completed += (_, _) =>
        {
            if (!_previewWasVisible) return;
            PreviewPanelControl.Visibility = Visibility.Collapsed;
        };

        slide.BeginAnimation(TranslateTransform.XProperty, slideAnim);
        PreviewPanelControl.BeginAnimation(OpacityProperty, fadeAnim);
    }

    // ── ViewModel changes → update window shape ──────────────────
    private void OnVmChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.HasResults)
                           or nameof(MainViewModel.IsSettingsOpen)
                           or nameof(MainViewModel.IsPreviewVisible)
                           or nameof(MainViewModel.IsBrowsePanelVisible)
                           or nameof(MainViewModel.IsHubVisible)
                           or nameof(MainViewModel.ActiveActionId)
                           or nameof(MainViewModel.EmptyStateMessage))
        {
            Dispatcher.InvokeAsync(UpdateWindowState);
        }
        if (e.PropertyName is nameof(MainViewModel.ActiveCategory)
                            or nameof(MainViewModel.Query))
        {
            Dispatcher.InvokeAsync(UpdateCategoryVisuals);
            Dispatcher.InvokeAsync(UpdateCategoryVisibility);
        }
    }

    private void UpdateWindowState()
    {
        if (_vm is null) return;

        bool isBrowse   = _vm.IsBrowsePanelVisible;
        bool hasContent = _vm.HasResults || isBrowse || _vm.IsHubVisible;
        bool isAction   = _vm.ActiveActionId is not null;

        ContentArea.Visibility = (hasContent || isAction) ? Visibility.Visible : Visibility.Collapsed;

        var targetRadius = (hasContent || isAction)
            ? (CornerRadius)TryFindResource("RadiusWindow")
            : (CornerRadius)TryFindResource("RadiusPill");
        AnimateCornerRadius(targetRadius);

        if (isAction)
        {
            // Action active – full-width preview, hide results list
            ResultsListControl.Visibility  = Visibility.Collapsed;
            BrowsePanelControl.Visibility  = Visibility.Collapsed;
            PreviewColumn.Width            = new GridLength(1, GridUnitType.Star);
            PreviewPanelControl.Visibility = Visibility.Visible;
            PreviewPanelControl.Opacity    = 1;
            PreviewPanelControl.Width      = double.NaN;
            Grid.SetColumn(PreviewPanelControl, 0);
            Grid.SetColumnSpan(PreviewPanelControl, 2);
            _previewWasVisible = true;
        }
        else
        {
            // Normal mode: side-by-side results + optional preview
            Grid.SetColumn(PreviewPanelControl, 1);
            Grid.SetColumnSpan(PreviewPanelControl, 1);

            var previewWidth = _vm.IsPreviewVisible
                ? Math.Max(260, ActualWidth * 0.35)
                : 0.0;
            PreviewColumn.Width = new GridLength(previewWidth);
            PreviewPanelControl.Width = double.NaN;

            var previewVisible = previewWidth > 0;
            if (previewVisible && !_previewWasVisible)
                AnimatePreviewIn();
            else if (!previewVisible && _previewWasVisible)
                AnimatePreviewOut();
            _previewWasVisible = previewVisible;

            bool showBrowse = isBrowse;
            BrowsePanelControl.Visibility = showBrowse ? Visibility.Visible : Visibility.Collapsed;
            ResultsListControl.Visibility = !showBrowse ? Visibility.Visible : Visibility.Collapsed;
        }

        // Hub empty state — shown when idle with no recent items
        var isHubEmpty = _vm.IsHubVisible && !_vm.HasResults && !string.IsNullOrEmpty(_vm.EmptyStateMessage);
        EmptyStateText.Visibility = isHubEmpty ? Visibility.Visible : Visibility.Collapsed;

        // Settings footer — shown in Hub or search states (not in browse/settings/AI)
        var showFooter = !_vm.IsBrowsePanelVisible && _vm.ActiveActionId is null;
        SettingsFooter.Visibility = showFooter ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Keyboard ─────────────────────────────────────────────────
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (_vm is null) return;

        switch (e.Key)
        {
            case Key.Down: _vm.MoveSelection(+1); e.Handled = true; break;
            case Key.Up:   _vm.MoveSelection(-1); e.Handled = true; break;
            case Key.Tab:  _vm.CycleCategory();    e.Handled = true; break;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_vm is null) return;

        switch (e.Key)
        {
            case Key.Escape:
                if (string.Equals(_vm.ActiveActionId, "ai", StringComparison.OrdinalIgnoreCase))
                {
                    _vm.BackFromAiChat();
                }
                // Clear query (most common undo)
                else if (!string.IsNullOrEmpty(_vm.Query))
                    _vm.Query = string.Empty;
                // Exit action preview
                else if (_vm.ActiveActionId is not null)
                    _vm.ClearActiveMode();
                // Exit browse mode
                else if (_vm.ActiveCategory is not null)
                    _vm.ActiveCategory = null;
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
                _vm.ActiveCategory = _vm.ActiveCategory == "files" ? null : "files";
                e.Handled = true; break;
            case Key.D2 when Keyboard.Modifiers == ModifierKeys.Control:
                _vm.ActivateClipboardCategory();
                e.Handled = true; break;
            case Key.D3 when Keyboard.Modifiers == ModifierKeys.Control:
                _vm.ActiveCategory = _vm.ActiveCategory == "actions" ? null : "actions";
                e.Handled = true; break;

            case Key.OemComma when Keyboard.Modifiers == ModifierKeys.Control:
                _vm.OpenSettingsCommand.Execute(null);
                e.Handled = true; break;
        }
    }

    // ── Click outside to close ────────────────────────────────────
    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        HideWindow();
    }

    // ── Floating category buttons ────────────────────────────────
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

    private void UpdateCategoryVisibility()
    {
        bool visible = _isHovering
            && (_vm is null || _vm.ActiveCategory is null)
            && (_vm is null || _vm.ActiveActionId is null)
            && (_vm is null || string.IsNullOrEmpty(_vm.Query));
        double targetWidth = visible ? 160.0 : 0.0;

        if (Math.Abs(CategoryPanelContainer.Width - targetWidth) < 0.5) return;

        // Animate category panel. Grid layout handles card resize naturally.
        var slide = new DoubleAnimation(targetWidth,
                new Duration(TimeSpan.FromMilliseconds(180)))
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
            _vm.ActivateClipboardCategory();
        else
            _vm.ActiveCategory = _vm.ActiveCategory == category ? null : category;

        SearchBarControl.FocusInput();
    }

    private void UpdateCategoryVisuals()
    {
        if (_vm is null) return;
        SetCatActive(BtnFiles,     IconFiles,     _vm.ActiveCategory == "files");
        SetCatActive(BtnClipboard, IconClipboard, _vm.ActiveCategory == "clipboard");
        SetCatActive(BtnActions,   IconActions,   _vm.ActiveCategory == "actions");
    }

    private void SetCatActive(Button btn, System.Windows.Shapes.Path icon, bool active)
    {
        var surface = TryFindResource("Surface")     as Brush ?? Brushes.DimGray;
        var primary = TryFindResource("TextPrimary") as Brush ?? Brushes.White;
        var muted   = TryFindResource("TextMuted")   as Brush ?? Brushes.Gray;

        btn.Background = active ? surface : Brushes.Transparent;
        icon.Stroke    = active ? primary : muted;
    }

    private void OnSettingsFooterClick(object sender, RoutedEventArgs e)
    {
        _vm?.OpenSettingsCommand.Execute(null);
    }

    // ── Corner radius animation ───────────────────────────────────
    /// <summary>Animates SearchCardBorder.CornerRadius from current to target over 180ms with cubic ease-out.</summary>
    private void AnimateCornerRadius(CornerRadius target)
    {
        _cornerRadiusTimer?.Stop();

        var current = SearchCardBorder.CornerRadius;
        if (NearlyEqual(current, target))
        {
            SearchCardBorder.CornerRadius = target;
            return;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        const double durationMs = 180.0;

        _cornerRadiusTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(16),
            System.Windows.Threading.DispatcherPriority.Render,
            (s, _) =>
            {
                var t = Math.Min(1.0, sw.Elapsed.TotalMilliseconds / durationMs);
                var eased = 1.0 - Math.Pow(1.0 - t, 3); // cubic ease-out

                SearchCardBorder.CornerRadius = new CornerRadius(
                    current.TopLeft     + (target.TopLeft     - current.TopLeft)     * eased,
                    current.TopRight    + (target.TopRight    - current.TopRight)    * eased,
                    current.BottomRight + (target.BottomRight - current.BottomRight) * eased,
                    current.BottomLeft  + (target.BottomLeft  - current.BottomLeft)  * eased);

                if (t >= 1.0)
                {
                    _cornerRadiusTimer?.Stop();
                    _cornerRadiusTimer = null;
                }
            },
            Dispatcher);
        _cornerRadiusTimer.Start();
    }

    private static bool NearlyEqual(CornerRadius a, CornerRadius b, double epsilon = 0.1)
        => Math.Abs(a.TopLeft     - b.TopLeft)     < epsilon
        && Math.Abs(a.TopRight    - b.TopRight)    < epsilon
        && Math.Abs(a.BottomRight - b.BottomRight) < epsilon
        && Math.Abs(a.BottomLeft  - b.BottomLeft)  < epsilon;

    // ── Drag to reposition ────────────────────────────────────────
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.Source is TextBox or System.Windows.Controls.Primitives.ScrollBar) return;
        try { DragMove(); } catch { }
    }

}
