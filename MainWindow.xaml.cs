using System.Windows.Interop;
using System.Windows.Media.Animation;
using Arc.Models;
using Arc.ViewModels;

namespace Arc;

public partial class MainWindow : Window
{
    private bool _isVisible;
    private bool _isPointerInside;
    private bool _categoryExpanded;
    private MainViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        // DWM Mica + AllowsTransparency causes a white fringe on frameless WPF windows.
        // Capsule uses an opaque Surface fill + drop shadow only (Spotlight-style).
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_NCHITTEST = 0x0084;
        if (msg != WM_NCHITTEST) return IntPtr.Zero;

        var result = NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
        int ht = result.ToInt32() & 0xFFFF;
        if (ht is 1 or 6 or 7) return result;

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
        vm.RequestHide += HideWindow;
        UpdateCategoryVisuals();
        ApplySpotlightLayout(animate: false);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Width = LauncherLayout.WidthCompact;
        MinWidth = LauncherLayout.WidthCompact;
        MaxWidth = LauncherLayout.WidthExpanded;
        Capsule.CornerRadius = (CornerRadius)FindResource("RadiusWindow");
        PositionWindow();
    }

    public void ShowWindow()
    {
        if (_isVisible) { Activate(); return; }
        _isVisible = true;
        PositionWindow();

        Show();
        Activate();
        SearchBarControl.FocusInput();

        SyncPointerHoverState();
        ApplySpotlightLayout(animate: true);

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
        _isPointerInside = false;
        _categoryExpanded = false;

        if (_vm?.Config.AnimationEnabled == false)
        {
            Hide();
            _isVisible = false;
            _vm?.Reset();
            ApplySpotlightLayout(animate: false);
            return;
        }

        AnimateOut(() =>
        {
            Hide();
            _isVisible = false;
            _vm?.Reset();
            ApplySpotlightLayout(animate: false);
        });
    }

    private void AnimateIn()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease });
        var scale = new DoubleAnimation(0.98, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease };
        WindowScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scale);
        WindowScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scale);
    }

    private void AnimateOut(Action onComplete)
    {
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseIn };
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease };
        fade.Completed += (_, _) => onComplete();
        BeginAnimation(OpacityProperty, fade);
    }

    private void PositionWindow()
    {
        var screen = SystemParameters.WorkArea;
        var left = screen.Left + (screen.Width - Width) / 2;
        var top = screen.Top + (screen.Height - ActualHeight) / 2;

        switch (_vm?.Config.SearchWindowPosition)
        {
            case "centerTop":
                top = screen.Top + screen.Height * 0.15;
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

    private void OnVmChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        var layoutProps = new[]
        {
            nameof(MainViewModel.HasResults),
            nameof(MainViewModel.IsBrowsePanelVisible),
            nameof(MainViewModel.ActiveCategory),
            nameof(MainViewModel.Query),
            nameof(MainViewModel.ActiveActionId),
            nameof(MainViewModel.IsScopeBarVisible),
            nameof(MainViewModel.FooterHint),
        };

        if (layoutProps.Contains(e.PropertyName))
            Dispatcher.InvokeAsync(() => ApplySpotlightLayout(animate: true));

        if (e.PropertyName is nameof(MainViewModel.ActiveCategory) or nameof(MainViewModel.Query))
            Dispatcher.InvokeAsync(UpdateCategoryVisuals);
    }

    /// <summary>
    /// Spotlight-style: empty query and pointer in window widens the capsule; category rail fades in on the right.
    /// Typing collapses width and hides categories.
    /// </summary>
    private bool ShouldShowCategoryRail()
    {
        if (_vm is null) return false;
        if (!string.IsNullOrEmpty(_vm.Query)) return false;
        if (_vm.ActiveActionId is not null) return false;
        return _isPointerInside || _vm.ActiveCategory is not null;
    }

    private void ApplySpotlightLayout(bool animate)
    {
        if (_vm is null) return;

        bool hasQuery = !string.IsNullOrEmpty(_vm.Query);
        bool isBrowse = _vm.IsBrowsePanelVisible;
        bool hasResults = _vm.HasResults || isBrowse;
        bool showContent = hasResults || _vm.ActiveActionId is not null;

        ContentArea.Visibility = showContent ? Visibility.Visible : Visibility.Collapsed;
        FooterArea.Visibility = showContent && _vm.ActiveActionId is null ? Visibility.Visible : Visibility.Collapsed;

        BrowsePanelControl.Visibility = isBrowse ? Visibility.Visible : Visibility.Collapsed;
        ResultsListControl.Visibility = isBrowse ? Visibility.Collapsed : Visibility.Visible;

        bool expandRail = ShouldShowCategoryRail();
        double targetWidth = expandRail ? LauncherLayout.WidthExpanded : LauncherLayout.WidthCompact;

        if (animate && _vm.Config.AnimationEnabled)
        {
            AnimateWindowWidth(targetWidth);
            AnimateCategoryRail(expandRail);
        }
        else
        {
            Width = targetWidth;
            SetCategoryRailInstant(expandRail);
            PositionWindow();
        }

        _categoryExpanded = expandRail;
    }

    private void AnimateWindowWidth(double target)
    {
        if (Math.Abs(Width - target) < 0.5)
        {
            PositionWindow();
            return;
        }

        var anim = new DoubleAnimation(target, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        anim.CurrentTimeInvalidated += (_, _) => PositionWindow();
        anim.Completed += (_, _) => PositionWindow();
        BeginAnimation(WidthProperty, anim);
    }

    private void AnimateCategoryRail(bool show)
    {
        var duration = TimeSpan.FromMilliseconds(show ? 200 : 140);
        var ease = new CubicEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn };

        CategoryColumn.BeginAnimation(ColumnDefinition.WidthProperty, null);
        var widthAnim = new GridLengthAnimation(
            CategoryColumn.Width,
            new GridLength(show ? LauncherLayout.CategoryZoneWidth : 0, GridUnitType.Pixel),
            duration)
        { EasingFunction = ease };
        CategoryColumn.BeginAnimation(ColumnDefinition.WidthProperty, widthAnim);

        CategoryDivider.BeginAnimation(UIElement.OpacityProperty, null);
        CategoryButtons.BeginAnimation(UIElement.OpacityProperty, null);

        var fade = new DoubleAnimation(show ? 1 : 0, duration) { EasingFunction = ease };
        CategoryDivider.BeginAnimation(UIElement.OpacityProperty, fade);
        CategoryButtons.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    private void SetCategoryRailInstant(bool show)
    {
        CategoryColumn.Width = new GridLength(show ? LauncherLayout.CategoryZoneWidth : 0, GridUnitType.Pixel);
        CategoryDivider.Opacity = show ? 1 : 0;
        CategoryButtons.Opacity = show ? 1 : 0;
    }

    private void OnWindowMouseEnter(object sender, MouseEventArgs e)
    {
        _isPointerInside = true;
        if (ShouldShowCategoryRail() != _categoryExpanded)
            ApplySpotlightLayout(animate: true);
    }

    private void OnWindowMouseLeave(object sender, MouseEventArgs e)
    {
        _isPointerInside = false;
        if (_vm?.ActiveCategory is null)
            ApplySpotlightLayout(animate: true);
    }

    private void SyncPointerHoverState()
    {
        _isPointerInside = IsMouseOver;
    }

    private void OnCategoryClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || _vm is null) return;
        var category = btn.Tag as string;

        if (category == "clipboard")
            _vm.ActivateClipboardCategory();
        else
            _vm.ActiveCategory = _vm.ActiveCategory == category ? null : category;

        SearchBarControl.FocusInput();
        UpdateCategoryVisuals();
        ApplySpotlightLayout(animate: true);
    }

    private void UpdateCategoryVisuals()
    {
        if (_vm is null) return;
        SetCatActive(BtnFiles, IconFiles, _vm.ActiveCategory == "files");
        SetCatActive(BtnCommands, IconCommands, _vm.ActiveCategory == "actions");
        SetCatActive(BtnClipboard, IconClipboard, _vm.ActiveCategory == "clipboard");
    }

    private static void SetCatActive(System.Windows.Controls.Button btn, System.Windows.Shapes.Path icon, bool active)
    {
        var primary = btn.TryFindResource("TextPrimary") as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.White;
        var secondary = btn.TryFindResource("TextSecondary") as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Gray;
        icon.Stroke = active ? primary : secondary;
        icon.StrokeThickness = active ? 2.0 : 1.5;
        btn.Background = System.Windows.Media.Brushes.Transparent;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (_vm is null) return;

        if (e.Key is Key.Down or Key.Up)
        {
            _vm.MoveSelection(e.Key == Key.Down ? 1 : -1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab && _vm.IsScopeBarVisible)
        {
            _vm.CycleScope();
            e.Handled = true;
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
                    _vm.BackFromAiChat();
                else if (!string.IsNullOrEmpty(_vm.Query))
                    _vm.Query = string.Empty;
                else if (_vm.ActiveActionId is not null)
                    _vm.ClearActiveMode();
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

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        HideWindow();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.Source is System.Windows.Controls.TextBox or System.Windows.Controls.Primitives.ScrollBar) return;
        try { DragMove(); } catch { }
    }
}

/// <summary>Animates <see cref="ColumnDefinition.Width"/> between grid length values.</summary>
internal sealed class GridLengthAnimation : AnimationTimeline
{
    public GridLength From { get; set; }
    public GridLength To { get; set; }

    public GridLengthAnimation() { }

    public GridLengthAnimation(GridLength from, GridLength to, Duration duration)
    {
        From = from;
        To = to;
        Duration = duration;
    }

    public IEasingFunction? EasingFunction { get; set; }

    public override Type TargetPropertyType => typeof(GridLength);

    protected override Freezable CreateInstanceCore() => new GridLengthAnimation();

    public override object GetCurrentValue(object defaultOriginValue, object originValue, AnimationClock clock)
    {
        if (clock.CurrentProgress is not double t) return To;
        if (EasingFunction is not null)
            t = EasingFunction.Ease(t);

        var from = From.Value;
        var to = To.Value;
        return new GridLength(from + (to - from) * t, GridUnitType.Pixel);
    }
}
