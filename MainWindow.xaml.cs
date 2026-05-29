using System.Windows.Interop;
using System.Windows.Media.Animation;
using Arc.ViewModels;

namespace Arc;

public partial class MainWindow : Window
{
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

    // ── ViewModel changes → update window shape ──────────────────
    private void OnVmChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.HasResults)
                           or nameof(MainViewModel.IsSettingsOpen)
                           or nameof(MainViewModel.IsPreviewVisible)
                           or nameof(MainViewModel.IsBrowsePanelVisible)
                           or nameof(MainViewModel.ActiveActionId))
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
        bool hasContent = _vm.HasResults || _vm.IsSettingsOpen || isBrowse;

        ContentArea.Visibility = hasContent ? Visibility.Visible : Visibility.Collapsed;

        SearchCardBorder.CornerRadius = hasContent
            ? (CornerRadius)TryFindResource("RadiusWindow")
            : (CornerRadius)TryFindResource("RadiusPill");

        var isAiMode = string.Equals(_vm.ActiveActionId, "ai", StringComparison.OrdinalIgnoreCase);
        var previewWidth = _vm.IsPreviewVisible && !_vm.IsSettingsOpen
            ? (isAiMode ? ActualWidth - 80 : 300.0)
            : 0.0;
        PreviewColumn.Width = new GridLength(previewWidth);
        PreviewPanelControl.Visibility = previewWidth > 0
            ? Visibility.Visible : Visibility.Collapsed;
        PreviewPanelControl.Width = isAiMode ? double.NaN : 300.0;
        Grid.SetColumn(PreviewPanelControl, isAiMode ? 0 : 1);
        Grid.SetColumnSpan(PreviewPanelControl, isAiMode ? 2 : 1);

        SettingsViewControl.Visibility = _vm.IsSettingsOpen
            ? Visibility.Visible : Visibility.Collapsed;

        bool showBrowse = isBrowse && !_vm.IsSettingsOpen && !isAiMode;
        BrowsePanelControl.Visibility = showBrowse ? Visibility.Visible : Visibility.Collapsed;
        ResultsListControl.Visibility = (!showBrowse && !_vm.IsSettingsOpen && !isAiMode) ? Visibility.Visible : Visibility.Collapsed;
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
                if (_vm.IsSettingsOpen)
                {
                    _vm.IsSettingsOpen = false;
                    if (string.Equals(_vm.Query, "settings", StringComparison.OrdinalIgnoreCase))
                        _vm.Query = string.Empty;
                }
                else if (!string.IsNullOrEmpty(_vm.Query))
                    _vm.Query = string.Empty;
                else if (_vm.ActiveActionId is not null)
                    _vm.ClearActiveMode();
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
                e.Handled = true; break;
            case Key.D2 when Keyboard.Modifiers == ModifierKeys.Control:
                _vm.ActiveCategory = _vm.ActiveCategory == "files" ? null : "files";
                e.Handled = true; break;
            case Key.D3 when Keyboard.Modifiers == ModifierKeys.Control:
                _vm.ActivateClipboardCategory();
                e.Handled = true; break;
            case Key.D4 when Keyboard.Modifiers == ModifierKeys.Control:
                _vm.ActiveCategory = _vm.ActiveCategory == "actions" ? null : "actions";
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
        double targetWidth = visible ? 212.0 : 0.0;

        if (Math.Abs(CategoryPanelContainer.Width - targetWidth) < 0.5) return;

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
        SetCatActive(BtnApps,      IconApps,      _vm.ActiveCategory == "apps");
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

    // ── Drag to reposition ────────────────────────────────────────
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.Source is TextBox or System.Windows.Controls.Primitives.ScrollBar) return;
        try { DragMove(); } catch { }
    }

}

