using Arc.ViewModels;

namespace Arc.Views;

public partial class SettingsView : UserControl
{
    private SettingsViewModel? _vm;
    private bool _recordingShortcut;

    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (_vm is not null) _vm.PropertyChanged -= OnVmChanged;
            _vm = e.NewValue as SettingsViewModel;
            if (_vm is not null)
            {
                _vm.PropertyChanged += OnVmChanged;
                AiApiKeyBox.Password = _vm.ApiKey;
            }
        };
    }

    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsViewModel.ApiKey) or nameof(SettingsViewModel.AiProvider))
            AiApiKeyBox.Password = _vm?.ApiKey ?? string.Empty;
    }

    private void OnAiApiKeyChanged(object sender, RoutedEventArgs e)
    {
        if (_vm is null || AiApiKeyBox.Password == _vm.ApiKey) return;
        _vm.ApiKey = AiApiKeyBox.Password;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => _vm?.CloseSettings();

    private void OnBrowseFolderClick(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select a folder to include in search"
        };
        if (dlg.ShowDialog() == true)
        {
            var path = dlg.FolderName;
            if (!_vm.IndexedFoldersList.Contains(path, StringComparer.OrdinalIgnoreCase))
                _vm.IndexedFoldersList.Add(path);
            _vm.NewFolderPath = string.Empty;
        }
    }

    private void OnEditShortcutClick(object sender, RoutedEventArgs e)
    {
        if (_recordingShortcut) return;
        _recordingShortcut = true;
        EditShortcutBtn.Content = "Recording…";
        if (TryFindResource("TextPrimary") is System.Windows.Media.Brush b)
            EditShortcutBtn.Foreground = b;
        Keyboard.Focus(EditShortcutBtn);
        EditShortcutBtn.PreviewKeyDown += OnShortcutKeyDown;
        EditShortcutBtn.LostFocus      += OnShortcutLostFocus;
    }

    private void OnShortcutKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                 or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        var mods = Keyboard.Modifiers;
        var parts = new System.Collections.Generic.List<string>();
        if ((mods & ModifierKeys.Control) != 0) parts.Add("Ctrl");
        if ((mods & ModifierKeys.Alt)     != 0) parts.Add("Alt");
        if ((mods & ModifierKeys.Shift)   != 0) parts.Add("Shift");
        parts.Add(key.ToString());

        if (_vm is not null) _vm.Shortcut = string.Join("+", parts);
        StopRecording();
    }

    private void OnShortcutLostFocus(object sender, RoutedEventArgs e) => StopRecording();

    private void StopRecording()
    {
        if (!_recordingShortcut) return;
        _recordingShortcut = false;
        EditShortcutBtn.Content = "Edit";
        EditShortcutBtn.PreviewKeyDown -= OnShortcutKeyDown;
        EditShortcutBtn.LostFocus      -= OnShortcutLostFocus;
        if (TryFindResource("TextSecondary") is System.Windows.Media.Brush b)
            EditShortcutBtn.Foreground = b;
    }
}

// ── ToggleSwitch control ─────────────────────────────────────────────
// Inherits FrameworkElement (not Control) so the visual tree is built
// directly in the constructor — no WPF template-lookup required.
public sealed class ToggleSwitch : FrameworkElement
{
    public static readonly DependencyProperty IsOnProperty =
        DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(ToggleSwitch),
            new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsOnChanged));

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    private static void OnIsOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((ToggleSwitch)d).UpdateVisuals();

    private readonly Border _track;
    private readonly Border _knob;

    public ToggleSwitch()
    {
        Width  = 40;
        Height = 22;
        Cursor = Cursors.Hand;

        _knob = new Border
        {
            Width               = 16,
            Height              = 16,
            CornerRadius        = new CornerRadius(8),
            Background          = Brushes.White,
            Margin              = new Thickness(3, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment   = VerticalAlignment.Center,
        };

        _track = new Border
        {
            Width        = 40,
            Height       = 22,
            CornerRadius = new CornerRadius(11),
            Background   = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
            Child        = _knob,
        };

        AddVisualChild(_track);
        AddLogicalChild(_track);

        Loaded += (_, _) => UpdateVisuals();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        IsOn = !IsOn;
        e.Handled = true;
    }

    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index) => _track;

    protected override Size MeasureOverride(Size availableSize)
    {
        _track.Measure(new Size(40, 22));
        return new Size(40, 22);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _track.Arrange(new Rect(0, 0, 40, 22));
        return new Size(40, 22);
    }

    private void UpdateVisuals()
    {
        var accent  = TryFindResource("Accent")      as Brush
                      ?? new SolidColorBrush(Color.FromRgb(0x5B, 0x7E, 0xFF));
        var offBg   = TryFindResource("SurfaceLow")  as Brush
                      ?? new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
        var knobClr = TryFindResource("TextPrimary") as Brush ?? Brushes.White;

        _track.Background = IsOn ? accent : offBg;
        _knob.Background  = knobClr;
        _knob.Margin      = IsOn
            ? new Thickness(21, 0, 0, 0)
            : new Thickness(3, 0, 0, 0);
    }
}

