using System.Windows.Interop;
using System.Windows.Media;
using Arc.Models;
using Arc.ViewModels;
using Arc.Views;

namespace Arc;

public partial class MainWindow : Window
{
    private bool _isVisible;
    private bool _isPointerInside;
    private bool _categoryExpanded;
    private bool _queryWasEmpty = true;
    private MainViewModel? _vm;

    private CategoryCircle[] AnchorCircles => [CircleFiles, CircleCommands, CircleClipboard];

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);

        // Item 8 — Glassmorphism: acrylic blur backdrop
        EnableAcrylicBlur(hwnd);

        Closed += (_, _) => WindowBlur.DisableBlur(hwnd);
    }

    private void EnableAcrylicBlur(IntPtr hwnd)
    {
        try
        {
            bool isLight = IsSystemLightTheme();
            uint tint = isLight ? 0x44FFFFFFu : 0x99000000u;
            WindowBlur.EnableBlur(hwnd, tint);
        }
        catch
        {
            // Fallback: keep SurfaceAcrylic but it'll show as semi-transparent
            // without blur. Still acceptable on unsupported systems.
        }
    }

    private static bool IsSystemLightTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int i && i == 1;
        }
        catch { return false; }
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
        Width = LauncherLayout.BarWidth;
        MinWidth = LauncherLayout.BarWidth;
        MaxWidth = LauncherLayout.BarWidth;


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

        ArcMotion.Show(this, WindowScale, _vm?.Config.AnimationEnabled != false);
    }

    public void HideWindow()
    {
        if (!_isVisible) return;
        _vm?.CancelSearch();
        _isPointerInside = false;
        _categoryExpanded = false;

        if (_vm?.Config.AnimationEnabled == false)
        {
            Hide();
            _isVisible = false;
            _vm?.Reset();
            _queryWasEmpty = true;
            ApplySpotlightLayout(animate: false);
            return;
        }

        ArcMotion.Hide(this, WindowScale, _vm?.Config.AnimationEnabled != false, () =>
        {
            Hide();
            _isVisible = false;
            _vm?.Reset();
            _queryWasEmpty = true;
            ApplySpotlightLayout(animate: false);
        });
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
            nameof(MainViewModel.IsScopeBarVisible),
            nameof(MainViewModel.FooterHint),
            nameof(MainViewModel.SelectedIndex),
        };

        if (layoutProps.Contains(e.PropertyName))
        {
            var fastAnchorHide = e.PropertyName == nameof(MainViewModel.Query)
                && _vm is not null
                && !string.IsNullOrEmpty(_vm.Query)
                && _queryWasEmpty;
            if (e.PropertyName == nameof(MainViewModel.Query))
                _queryWasEmpty = string.IsNullOrEmpty(_vm?.Query);

            Dispatcher.InvokeAsync(() => ApplySpotlightLayout(animate: true, fastAnchorHide: fastAnchorHide));
        }

        if (e.PropertyName is nameof(MainViewModel.ActiveCategory) or nameof(MainViewModel.Query))
            Dispatcher.InvokeAsync(UpdateCategoryVisuals);
    }

    /// <summary>
    /// Empty query + pointer over window: category circles stagger in on the right (640px bar stays fixed).
    /// Typing hides circles immediately.
    /// </summary>
    private bool ShouldShowCategoryRail()
    {
        if (_vm is null) return false;
        if (!string.IsNullOrEmpty(_vm.Query)) return false;
        return _isPointerInside || _vm.ActiveCategory is not null;
    }

    private void ApplySpotlightLayout(bool animate, bool fastAnchorHide = false)
    {
        if (_vm is null) return;

        bool isBrowse = _vm.IsBrowsePanelVisible;
        bool hasResults = _vm.HasResults || isBrowse;
        bool showContent = isBrowse || hasResults;

        ContentArea.Visibility = showContent ? Visibility.Visible : Visibility.Collapsed;
        FooterArea.Visibility = showContent && _vm.SelectedResult is not null ? Visibility.Visible : Visibility.Collapsed;

        BrowsePanelControl.Visibility = isBrowse ? Visibility.Visible : Visibility.Collapsed;
        ResultsListControl.Visibility = isBrowse ? Visibility.Collapsed : Visibility.Visible;

        bool expandRail = ShouldShowCategoryRail();
        var animEnabled = animate && _vm.Config.AnimationEnabled;

        if (expandRail)
            ArcMotion.RevealAnchors(
                CategoryColumn,
                CategoryColumn.Width,
                LauncherLayout.CategoryZoneWidth,
                CategoryDivider,
                CategoryButtons,
                AnchorCircles,
                animEnabled);
        else
            ArcMotion.HideAnchors(
                CategoryColumn,
                CategoryColumn.Width,
                CategoryDivider,
                CategoryButtons,
                AnchorCircles,
                animEnabled,
                fastForTyping: fastAnchorHide);

        _categoryExpanded = expandRail;
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
        if (sender is not FrameworkElement el || _vm is null) return;
        var category = el.Tag as string;

        if (category == "clipboard")
            _vm.ActivateClipboardCategory();
        else
            _vm.ActiveCategory = _vm.ActiveCategory == category ? null : category;

        SearchBarControl.FocusInput();
        UpdateCategoryVisuals();
        ApplySpotlightLayout(animate: true);
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        _vm.ActiveCategory = null;
        SearchBarControl.FocusInput();
        UpdateCategoryVisuals();
        ApplySpotlightLayout(animate: true);
    }

    private void UpdateCategoryVisuals()
    {
        if (_vm is null) return;
        CircleFiles.IsActive     = _vm.ActiveCategory == "files";
        CircleCommands.IsActive  = _vm.ActiveCategory == "actions";
        CircleClipboard.IsActive = _vm.ActiveCategory == "clipboard";
        CategoryBackButton.Visibility = _vm.ActiveCategory is null ? Visibility.Collapsed : Visibility.Visible;
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
                if (!string.IsNullOrEmpty(_vm.Query))
                    _vm.Query = string.Empty;
                else if (_vm.ActiveCategory is not null)
                    _vm.ActiveCategory = null;
                else
                    HideWindow();
                e.Handled = true;
                break;

            case Key.Left:
                if (_vm.ActiveCategory is not null)
                {
                    _vm.ActiveCategory = null;
                    e.Handled = true;
                }
                break;

            case Key.Enter:
                if (Keyboard.Modifiers == ModifierKeys.Control)
                    _vm.RunAsAdminCommand.Execute(null);
                else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
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

            case Key.C when Keyboard.Modifiers == ModifierKeys.Control:
                _vm.CopySelectedPathCommand.Execute(null);
                e.Handled = true; break;

            case Key.E when Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift):
                _vm.OpenFolderCommand.Execute(null);
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
