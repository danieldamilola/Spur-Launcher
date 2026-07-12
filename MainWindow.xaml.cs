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

    private CategoryCircle[] AnchorCircles => GetCategoryCircles();

    private CategoryCircle[] GetCategoryCircles()
    {
        var circles = new System.Collections.Generic.List<CategoryCircle>();
        for (int i = 0; i < CategoryButtons.Items.Count; i++)
        {
            if (CategoryButtons.ItemContainerGenerator.ContainerFromIndex(i) is ContentPresenter cp)
            {
                cp.ApplyTemplate();
                var circle = cp.ContentTemplate?.FindName("PART_Circle", cp) as CategoryCircle
                             ?? FindVisualChild<CategoryCircle>(cp);
                if (circle != null) circles.Add(circle);
            }
        }
        return circles.ToArray();
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    public MainWindow()
    {
        try { InitializeComponent(); } catch(Exception ex) { try { var crashDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Spur"); System.IO.Directory.CreateDirectory(crashDir); System.IO.File.WriteAllText(System.IO.Path.Combine(crashDir, "crash.txt"), ex.ToString() + "\nInner: " + ex.InnerException?.ToString()); } catch { /* intentional: crash-log write failed, still re-throw original */ } throw; }
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

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool SetProcessWorkingSetSize(IntPtr process, nint minimumWorkingSetSize, nint maximumWorkingSetSize);
    }

    public void SetViewModel(MainViewModel vm)
    {
        if (_vm != null)
        {
            _vm.PropertyChanged -= OnVmChanged;
            _vm.RequestHide -= HideWindow;
        }

        _vm = vm;
        DataContext = _vm;
        _vm.PropertyChanged += OnVmChanged;
        _vm.RequestHide += HideWindow;
        _vm.Clipboard.PropertyChanged += OnClipboardVmPropertyChanged;
        
        ApplyConfigWidth();
        UpdateCategoryVisuals();
        ApplySpotlightLayout(animate: false);
    }

    private void ApplyConfigWidth()
    {
        if (_vm?.Config != null)
        {
            Width = _vm.Config.BarWidth;
        }
        else
        {
            Width = 640;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
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
        }

        SpurMotion.Hide(this, WindowScale, _vm?.Config.AnimationEnabled != false, () =>
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
        if (_vm is not null)
            Behaviors.LauncherWindowBehavior.PositionWindow(this, _vm);
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
            nameof(MainViewModel.IsFullPanelActive),
            nameof(MainViewModel.IsToastVisible),
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
        // In expanded mode, categories are on the homepage — no hover rail needed
        if (_vm.Config.WindowMode == "expanded") return false;
        if (_vm.IsFullPanelActive) return false;
        if (_vm.ActiveActionPanel is not null) return false;
        if (!string.IsNullOrEmpty(_vm.Query)) return false;
        if (_vm.ActiveCategory is not null) return false;
        return _isPointerInside;
    }

    private void ApplySpotlightLayout(bool animate, bool fastAnchorHide = false)
    {
        if (_vm is null) return;

        bool isCompactWindow = _vm.Config.WindowMode == "compact";
        bool isBrowse = _vm.IsBrowsePanelVisible;
        bool hasResults = _vm.HasResults || isBrowse;
        bool hasQuery = !string.IsNullOrEmpty(_vm.Query);
        bool isExpandedHome = _vm.IsExpandedHome;

        bool showContent = _vm.IsFullPanelActive || isBrowse || isExpandedHome
            || (isCompactWindow ? (hasQuery && hasResults) : hasResults);

        UpdatePanelVisibility(isBrowse, isExpandedHome);

        var animEnabled = animate && _vm.Config.AnimationEnabled;

        if (showContent)
            AnimateContentExpand(animEnabled);
        else
            AnimateContentCollapse(animEnabled);

        UpdateCategoryRail(animEnabled, fastAnchorHide);
    }

    /// <summary>Sets panel visibility based on active mode (homepage, clipboard, add-on, or results).</summary>
    private void UpdatePanelVisibility(bool isBrowse, bool isExpandedHome)
    {
        bool isClipboard = _vm!.ActiveCategory == "clipboard";
        bool isFloating = _vm.Config.PreviewStyle == "Floating";

        // Homepage is shown in expanded mode with empty query
        HomePanelControl.Visibility = isExpandedHome ? Visibility.Visible : Visibility.Collapsed;

        ClipboardManagerControl.Visibility = isClipboard ? Visibility.Visible : Visibility.Collapsed;
        UnifiedResultsControl.Visibility = _vm.IsFullPanelActive || isClipboard || isExpandedHome
            ? Visibility.Collapsed : Visibility.Visible;
        AddOnPanelControl.Visibility = _vm.IsFullPanelActive ? Visibility.Visible : Visibility.Collapsed;
        AnimateToast(_vm.IsToastVisible);

        // Clipboard preview: floating vs inline
        if (isClipboard)
        {
            ClipboardManagerControl.SetFloatingMode(isFloating);
        }
        ClipboardPreviewFloat.Visibility = Visibility.Collapsed;
    }

    private void OnClipboardVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Update the floating preview when selection changes while preview is open
        if (e.PropertyName is nameof(ClipboardViewModel.SelectedEntry)
            && ClipboardPreviewFloat.Visibility == Visibility.Visible)
        {
            Dispatcher.InvokeAsync(UpdateFloatingClipboardPreview);
        }
    }

    private void UpdateFloatingClipboardPreview()
    {
        var entry = _vm?.Clipboard.SelectedEntry;
        if (entry is null || _vm?.ActiveCategory != "clipboard" || _vm.Config.PreviewStyle != "Floating")
        {
            if (ClipboardPreviewFloat.Visibility == Visibility.Visible)
                AnimatePreviewHide();
            return;
        }

        bool wasHidden = ClipboardPreviewFloat.Visibility != Visibility.Visible;

        ClipboardPreviewFloat.Visibility = Visibility.Visible;
        ClipFloatMetadata.Text = _vm.Clipboard.SelectedMetadata ?? "";

        if (entry.IsImage && entry.Image != null)
        {
            ClipFloatImage.Source = entry.Image;
            ClipFloatImage.Visibility = Visibility.Visible;
            ClipFloatText.Visibility = Visibility.Collapsed;
        }
        else
        {
            ClipFloatText.Text = entry.Content ?? "";
            ClipFloatText.Visibility = Visibility.Visible;
            ClipFloatImage.Visibility = Visibility.Collapsed;
        }

        bool isPinned = _vm.Clipboard.IsPinned(entry);
        ClipFloatPinGlyph.Glyph = isPinned ? "\uE841" : "\uE718";
        ClipFloatPinBtn.ToolTip = isPinned ? "Unpin" : "Pin";

        if (wasHidden && _vm.Config.AnimationEnabled)
            AnimatePreviewShow();
    }

    private void AnimatePreviewShow()
    {
        var slide = new DoubleAnimation(-12, 0, TimeSpan.FromSeconds(0.12))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        ClipboardPreviewFloat.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slide);
    }

    private void AnimatePreviewHide()
    {
        var slide = new DoubleAnimation(0, -12, TimeSpan.FromSeconds(0.1))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        slide.Completed += (_, _) =>
        {
            ClipboardPreviewFloat.Visibility = Visibility.Collapsed;
            ClipboardPreviewFloat.RenderTransform.BeginAnimation(TranslateTransform.YProperty, null);
        };
        ClipboardPreviewFloat.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slide);
    }

    private void OnFloatCopyClick(object sender, RoutedEventArgs e)
    {
        _vm?.Clipboard.CopyCommand.Execute(_vm.Clipboard.SelectedEntry);
    }

    private void OnFloatPinClick(object sender, RoutedEventArgs e)
    {
        _vm?.Clipboard.TogglePinCommand.Execute(_vm.Clipboard.SelectedEntry);
        Dispatcher.InvokeAsync(UpdateFloatingClipboardPreview);
    }

    private void OnFloatDeleteClick(object sender, RoutedEventArgs e)
    {
        _vm?.Clipboard.DeleteCommand.Execute(_vm.Clipboard.SelectedEntry);
    }

    /// <summary>Fades the toast bar in or out with an opacity animation.</summary>
    private void AnimateToast(bool show)
    {
        if (show)
        {
            ToastBar.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
            {
                EasingFunction = Animations.SpurMotion.EaseOut()
            };
            ToastBar.BeginAnimation(OpacityProperty, fadeIn);
        }
        else if (ToastBar.Visibility == Visibility.Visible)
        {
            var fadeOut = new DoubleAnimation(ToastBar.Opacity, 0, TimeSpan.FromMilliseconds(150))
            {
                EasingFunction = Animations.SpurMotion.EaseOut()
            };
            fadeOut.Completed += (_, _) =>
            {
                ToastBar.Visibility = Visibility.Collapsed;
                ToastBar.BeginAnimation(OpacityProperty, null);
            };
            ToastBar.BeginAnimation(OpacityProperty, fadeOut);
        }
    }

    /// <summary>Expands the content area (with optional animation).</summary>
    private void AnimateContentExpand(bool animEnabled)
    {
        bool showFooter = _vm!.SelectedResult is not null && !_vm.IsFullPanelActive;

        if (ContentArea.Visibility != Visibility.Visible)
        {
            if (animEnabled)
            {
                ClearContentAnimations();
                ExpandedScale.ScaleY = 0;
                ContentArea.Opacity = 0;
                if (_vm.SelectedResult is not null) FooterArea.Opacity = 0;

                ContentArea.Visibility = Visibility.Visible;
                FooterArea.Visibility = showFooter ? Visibility.Visible : Visibility.Collapsed;

                var scaleAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
                { EasingFunction = SpurMotion.EaseOut() };
                var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
                { EasingFunction = SpurMotion.EaseOut() };

                ExpandedScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                ContentArea.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
                if (_vm.SelectedResult is not null) FooterArea.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            }
            else
            {
                ClearContentAnimations();
                ExpandedScale.ScaleY = 1;
                ContentArea.Opacity = 1;
                FooterArea.Opacity = 1;
                ContentArea.Visibility = Visibility.Visible;
                FooterArea.Visibility = showFooter ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        else
        {
            FooterArea.Visibility = showFooter ? Visibility.Visible : Visibility.Collapsed;
            FooterArea.Opacity = 1;
        }
    }

    /// <summary>Collapses the content area (with optional animation).</summary>
    private void AnimateContentCollapse(bool animEnabled)
    {
        if (ContentArea.Visibility == Visibility.Collapsed) return;

        if (animEnabled)
        {
            var scaleAnim = new DoubleAnimation(ExpandedScale.ScaleY, 0, TimeSpan.FromMilliseconds(120))
            { EasingFunction = SpurMotion.EaseIn() };
            var fadeAnim = new DoubleAnimation(ContentArea.Opacity, 0, TimeSpan.FromMilliseconds(120))
            { EasingFunction = SpurMotion.EaseIn() };

            scaleAnim.Completed += (_, _) =>
            {
                ContentArea.Visibility = Visibility.Collapsed;
                FooterArea.Visibility = Visibility.Collapsed;
            };

            ExpandedScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            ContentArea.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            FooterArea.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
        }
        else
        {
            ClearContentAnimations();
            ExpandedScale.ScaleY = 1;
            ContentArea.Visibility = Visibility.Collapsed;
            FooterArea.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>Clears pending animations on content, scale, and footer elements.</summary>
    private void ClearContentAnimations()
    {
        ExpandedScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        ContentArea.BeginAnimation(UIElement.OpacityProperty, null);
        FooterArea.BeginAnimation(UIElement.OpacityProperty, null);
    }

    /// <summary>Shows or hides the category rail on the left side.</summary>
    private void UpdateCategoryRail(bool animEnabled, bool fastAnchorHide)
    {
        bool expandRail = ShouldShowCategoryRail();

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
        foreach (var circle in AnchorCircles)
        {
            var id = circle.Tag as string ?? string.Empty;
            circle.IsActive = id == _vm.ActiveCategory;
        }
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

        var mods = Keyboard.Modifiers;

        switch (e.Key)
        {
            case Key.Escape:
                if (_vm.IsFullPanelActive)
                    _vm.ExitAddOnPanel();
                else if (_vm.CommandPalette.IsOpen)
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
                if (_vm.IsFullPanelActive)
                {
                    var aiQuery = _vm.Query?.Trim();
                    if (!string.IsNullOrWhiteSpace(aiQuery))
                    {
                        AddOnPanelControl.HandleUserInput(aiQuery);
                        _vm.Query = string.Empty;
                    }
                    e.Handled = true;
                    break;
                }
                if (_vm.ActiveCategory == "clipboard")
                {
                    ClipboardManagerControl.PasteSelected();
                    e.Handled = true;
                    break;
                }
                if (MatchShortcut(_vm.Config.RunAsAdminShortcut, e.Key, mods))
                    _vm.RunAsAdminCommand.Execute(null);
                else if (MatchShortcut(_vm.Config.OpenFolderLocationShortcut, e.Key, mods))
                    _vm.OpenFolderCommand.Execute(null);
                else
                    _vm.OpenSelectedCommand.Execute(null);
                e.Handled = true;
                break;

            default:
                if (MatchShortcut(_vm.Config.PreviewToggleShortcut, e.Key, mods))
                {
                    TogglePreview();
                    e.Handled = true;
                }
                else if (MatchShortcut(_vm.Config.CommandPaletteShortcut, e.Key, mods))
                {
                    _vm.CommandPalette.IsOpen = !_vm.CommandPalette.IsOpen;
                    e.Handled = true;
                }
                else if (MatchShortcut(_vm.Config.TogglePinShortcut, e.Key, mods))
                {
                    _vm.TogglePinCommand.Execute(_vm.SelectedResult);
                    e.Handled = true;
                }
                else if (MatchShortcut(_vm.Config.CategoryFilesShortcut, e.Key, mods))
                {
                    _vm.ActiveCategory = _vm.ActiveCategory == "files" ? null : "files";
                    e.Handled = true;
                }
                else if (MatchShortcut(_vm.Config.CategoryAiShortcut, e.Key, mods))
                {
                    _vm.ActiveCategory = _vm.ActiveCategory == "ai" ? null : "ai";
                    e.Handled = true;
                }
                else if (MatchShortcut(_vm.Config.CategoryClipboardShortcut, e.Key, mods))
                {
                    _vm.ActivateClipboardCategory();
                    e.Handled = true;
                }
                else if (MatchShortcut(_vm.Config.OpenSettingsShortcut, e.Key, mods))
                {
                    _vm.OpenSettingsCommand.Execute(null);
                    e.Handled = true;
                }
                else if (MatchShortcut(_vm.Config.CopyPathShortcut, e.Key, mods))
                {
                    _vm.CopySelectedPathCommand.Execute(null);
                    e.Handled = true;
                }
                break;
        }
    }

    private static bool MatchShortcut(string shortcut, Key key, ModifierKeys modifiers)
    {
        ModifierKeys required = ModifierKeys.None;
        Key? target = null;

        foreach (var part in shortcut.Split('+'))
        {
            var t = part.Trim();
            if      (t.Equals("Ctrl",  StringComparison.OrdinalIgnoreCase)) required |= ModifierKeys.Control;
            else if (t.Equals("Alt",   StringComparison.OrdinalIgnoreCase)) required |= ModifierKeys.Alt;
            else if (t.Equals("Shift", StringComparison.OrdinalIgnoreCase)) required |= ModifierKeys.Shift;
            else if (t.Equals("Win",   StringComparison.OrdinalIgnoreCase)) required |= ModifierKeys.Windows;
            else if (Enum.TryParse<Key>(t, true, out var k))                target = k;
        }

        return target == key && required == modifiers;
    }

    /// <summary>Shows the clipboard floating preview for the selected entry.</summary>
    public void ShowClipboardPreview()
    {
        if (_vm?.ActiveCategory == "clipboard" && _vm.Config.PreviewStyle == "Floating")
        {
            UpdateFloatingClipboardPreview();
        }
    }

    /// <summary>Toggles the preview panel for the currently selected result.</summary>
    private void TogglePreview()
    {
        if (_vm is null) return;

        if (_vm.ActiveCategory == "clipboard")
        {
            if (ClipboardPreviewFloat.Visibility == Visibility.Visible)
            {
                AnimatePreviewHide();
            }
            else
            {
                UpdateFloatingClipboardPreview();
            }
        }
        else if (_vm.SelectedResult?.Type == ResultType.File && _vm.Config.FilePreviewEnabled)
        {
            if (_vm.IsPreviewVisible)
            {
                _vm.IsPreviewVisible = false;
            }
            else
            {
                _vm.UpdateFilePreview();
                if (!_vm.IsPreviewVisible)
                    _vm.IsPreviewVisible = true;
            }
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
        try { DragMove(); } catch (InvalidOperationException) { /* DragMove throws if button released mid-drag */ }
    }


}
