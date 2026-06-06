using System.Windows.Interop;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Spur.Models;
using Spur.ViewModels;
using Spur.Views;

namespace Spur;

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
        try { InitializeComponent(); } catch(Exception ex) { try { var crashDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Spur"); System.IO.Directory.CreateDirectory(crashDir); System.IO.File.WriteAllText(System.IO.Path.Combine(crashDir, "crash.txt"), ex.ToString() + "\nInner: " + ex.InnerException?.ToString()); } catch { } throw; }
        Loaded += OnLoaded;
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
        ApplyConfigWidth();
        PositionWindow();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
    }

    private void ApplyConfigWidth()
    {
        if (_vm?.Config != null)
        {
            Width = _vm.Config.BarWidth;
            MinWidth = _vm.Config.BarWidth;
            MaxWidth = _vm.Config.BarWidth;
        }
        else
        {
            Width = 640;
            MinWidth = 640;
            MaxWidth = 640;
        }
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

        SpurMotion.Show(this, WindowScale, _vm?.Config.AnimationEnabled != false);
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
            
            // Aggressively clear memory while idle
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return;
        }

        SpurMotion.Hide(this, WindowScale, _vm?.Config.AnimationEnabled != false, () =>
        {
            Hide();
            _isVisible = false;
            _vm?.Reset();
            _queryWasEmpty = true;
            ApplySpotlightLayout(animate: false);
            
            // Aggressively clear memory while idle
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
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
        if (e.PropertyName == nameof(MainViewModel.Config))
        {
            ApplyConfigWidth();
        }

        var layoutProps = new[]
        {
            nameof(MainViewModel.HasResults),
            nameof(MainViewModel.IsBrowsePanelVisible),
            nameof(MainViewModel.ActiveCategory),
            nameof(MainViewModel.Query),
            nameof(MainViewModel.IsScopeBarVisible),
            nameof(MainViewModel.FooterHint),
            nameof(MainViewModel.ActiveActionPanel),
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
    /// Empty query + pointer over window: category circles stagger in beside the bar.
    /// Typing hides circles immediately.
    /// </summary>
    private bool ShouldShowCategoryRail()
    {
        if (_vm is null) return false;
        if (!string.IsNullOrEmpty(_vm.Query)) return false;
        if (_vm.ActiveCategory is not null) return false;
        return _isPointerInside;
    }

    private void ApplySpotlightLayout(bool animate, bool fastAnchorHide = false)
    {
        if (_vm is null) return;

        bool isBrowse = _vm.IsBrowsePanelVisible;
        bool hasResults = _vm.HasResults || isBrowse;
        bool showContent = isBrowse || hasResults;

        bool isClipboard = _vm.ActiveCategory == "clipboard";
        ClipboardManagerControl.Visibility = isClipboard ? Visibility.Visible : Visibility.Collapsed;
        UnifiedResultsControl.Visibility = isClipboard ? Visibility.Collapsed : Visibility.Visible;

        bool expandRail = ShouldShowCategoryRail();
        var animEnabled = animate && _vm.Config.AnimationEnabled;

        // Animate content area expand/collapse
        if (showContent)
        {
            if (ContentArea.Visibility != Visibility.Visible)
            {
                if (animEnabled)
                {
                    ExpandedScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    ContentArea.BeginAnimation(UIElement.OpacityProperty, null);
                    FooterArea.BeginAnimation(UIElement.OpacityProperty, null);
                    
                    ExpandedScale.ScaleY = 0;
                    ContentArea.Opacity = 0;
                    if (_vm.SelectedResult is not null) FooterArea.Opacity = 0;
                    
                    ContentArea.Visibility = Visibility.Visible;
                    ActionPreviewPanel.Visibility = _vm.IsActionPanelVisible ? Visibility.Visible : Visibility.Collapsed;
                    FooterArea.Visibility = _vm.SelectedResult is not null ? Visibility.Visible : Visibility.Collapsed;

                    var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
                    { EasingFunction = SpurMotion.EaseOut() };
                    
                    var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
                    { EasingFunction = SpurMotion.EaseOut() };

                    ExpandedScale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
                    ContentArea.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
                    if (_vm.SelectedResult is not null) FooterArea.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
                }
                else
                {
                    ExpandedScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    ContentArea.BeginAnimation(UIElement.OpacityProperty, null);
                    FooterArea.BeginAnimation(UIElement.OpacityProperty, null);
                    ExpandedScale.ScaleY = 1;
                    ContentArea.Opacity = 1;
                    FooterArea.Opacity = 1;
                    ContentArea.Visibility = Visibility.Visible;
                    ActionPreviewPanel.Visibility = _vm.IsActionPanelVisible ? Visibility.Visible : Visibility.Collapsed;
                    FooterArea.Visibility = _vm.SelectedResult is not null ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            else
            {
                FooterArea.Visibility = _vm.SelectedResult is not null ? Visibility.Visible : Visibility.Collapsed;
                ActionPreviewPanel.Visibility = _vm.IsActionPanelVisible ? Visibility.Visible : Visibility.Collapsed;
                FooterArea.Opacity = 1;
            }
        }
        else
        {
            if (ContentArea.Visibility != Visibility.Collapsed)
            {
                if (animEnabled)
                {
                    var anim = new DoubleAnimation(ExpandedScale.ScaleY, 0, TimeSpan.FromMilliseconds(120))
                    { EasingFunction = SpurMotion.EaseIn() };
                    
                    var fadeAnim = new DoubleAnimation(ContentArea.Opacity, 0, TimeSpan.FromMilliseconds(120))
                    { EasingFunction = SpurMotion.EaseIn() };

                    anim.Completed += (s, e) => 
                    { 
                        ContentArea.Visibility = Visibility.Collapsed; 
                        ActionPreviewPanel.Visibility = Visibility.Collapsed;
                        FooterArea.Visibility = Visibility.Collapsed; 
                    };
                    
                    ExpandedScale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
                    ContentArea.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
                    FooterArea.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
                }
                else
                {
                    ExpandedScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    ContentArea.BeginAnimation(UIElement.OpacityProperty, null);
                    FooterArea.BeginAnimation(UIElement.OpacityProperty, null);
                    ExpandedScale.ScaleY = 1;
                    ContentArea.Visibility = Visibility.Collapsed;
                    FooterArea.Visibility = Visibility.Collapsed;
                }
            }
        }

        if (expandRail)
            SpurMotion.RevealAnchors(
                CategoryColumn,
                CategoryColumn.Width,
                LauncherLayout.CategoryZoneWidth,
                CategoryDivider,
                CategoryButtons,
                AnchorCircles,
                animEnabled);
        else
            SpurMotion.HideAnchors(
                CategoryColumn,
                CategoryColumn.Width,
                CategoryDivider,
                CategoryButtons,
                AnchorCircles,
                animEnabled,
                fastForTyping: fastAnchorHide);

        // Set color swatch background when the color action panel is active
        if (_vm.ActiveActionPanel == "color" && !string.IsNullOrEmpty(_vm.ActionResultText))
        {
            try { ColorSwatch.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_vm.ActionResultText)); }
            catch { ColorSwatch.Background = Brushes.Transparent; }
        }

        _categoryExpanded = expandRail;
    }

    private CancellationTokenSource? _hoverHideCts;

    private void OnWindowMouseEnter(object sender, MouseEventArgs e)
    {
        _hoverHideCts?.Cancel();
        _isPointerInside = true;
        if (ShouldShowCategoryRail() != _categoryExpanded)
            ApplySpotlightLayout(animate: true);
    }

    private async void OnWindowMouseLeave(object sender, MouseEventArgs e)
    {
        _isPointerInside = false;
        _hoverHideCts?.Cancel();
        _hoverHideCts = new CancellationTokenSource();
        var token = _hoverHideCts.Token;

        try
        {
            await System.Threading.Tasks.Task.Delay(150, token);
            await Dispatcher.InvokeAsync(() => ApplySpotlightLayout(animate: true));
        }
        catch (OperationCanceledException) { /* expected if mouse re-enters */ }
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

        if (_vm.CommandPalette.IsOpen) return;

        if (e.Key is Key.Down or Key.Up)
        {
            if (_vm.ActiveCategory == "clipboard")
                ClipboardManagerControl.MoveSelection(e.Key == Key.Down ? 1 : -1);
            else
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
                if (_vm.CommandPalette.IsOpen)
                    _vm.CommandPalette.IsOpen = false;
                else if (!string.IsNullOrEmpty(_vm.Query))
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

            case Key.P when Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift):
                _vm.CommandPalette.IsOpen = !_vm.CommandPalette.IsOpen;
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

    private void OnActionCopyClick(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        var text = _vm.ActiveActionPanel switch
        {
            "ai" => _vm.AiChat.AiText,
            "color" => _vm.ActionResultText,
            "pw" => _vm.ActionResultText,
            "ip" => _vm.ActionResultText,
            _ => _vm.ActionResultText
        };
        if (!string.IsNullOrEmpty(text))
            System.Windows.Clipboard.SetText(text);
    }

    private void OnCloseActionPanelClick(object sender, RoutedEventArgs e)
    {
        _vm?.CloseActionPanel();
    }
}
