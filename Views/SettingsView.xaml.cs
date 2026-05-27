using Volt.ViewModels;

namespace Volt.Views;

public partial class SettingsView : UserControl
{
    private SettingsViewModel? _vm;
    private bool _suppressApiKeyChange;

    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null) _vm.PropertyChanged -= OnVmPropertyChanged;
        _vm = e.NewValue as SettingsViewModel;
        if (_vm is not null)
        {
            _vm.PropertyChanged += OnVmPropertyChanged;
            RefreshApiKeyBox();
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.AiProvider))
            Dispatcher.InvokeAsync(RefreshApiKeyBox);
    }

    private void RefreshApiKeyBox()
    {
        if (_vm is null) return;
        _suppressApiKeyChange = true;
        ApiKeyBox.Password = string.IsNullOrEmpty(_vm.ApiKey) ? "" : "••••••••••••";
        _suppressApiKeyChange = false;
    }

    private void OnApiKeyChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressApiKeyChange || _vm is null) return;
        var pw = ApiKeyBox.Password;
        if (pw != "••••••••••••")
            _vm.ApiKey = pw;
    }
}

// ── ToggleSwitch control ────────────────────────────────────────────
public sealed class ToggleSwitch : System.Windows.Controls.Control
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

    private Border? _track;
    private Border? _knob;

    static ToggleSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleSwitch),
            new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _track = GetTemplateChild("Track") as Border;
        _knob  = GetTemplateChild("Knob")  as Border;

        if (Template is null) BuildInlineTemplate();
        UpdateVisuals();

        MouseLeftButtonDown += (_, _) =>
        {
            IsOn = !IsOn;
        };
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        IsOn = !IsOn;
        e.Handled = true;
    }

    // Build the toggle inline since we have no generic.xaml
    private void BuildInlineTemplate()
    {
        Width  = 40;
        Height = 22;
        Cursor = Cursors.Hand;

        var track = new Border
        {
            Width        = 40,
            Height       = 22,
            CornerRadius = new CornerRadius(11),
        };

        var knob = new Border
        {
            Width        = 16,
            Height       = 16,
            CornerRadius = new CornerRadius(8),
            Background   = Brushes.White,
            Margin       = new Thickness(3, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment   = VerticalAlignment.Center,
        };

        track.Child = knob;
        AddVisualChild(track);
        AddLogicalChild(track);
        _track = track;
        _knob  = knob;

        // Measure/arrange manually
        SizeChanged += (_, _) => UpdateVisuals();
    }

    protected override int VisualChildrenCount =>
        _track is not null ? 1 : base.VisualChildrenCount;

    protected override Visual GetVisualChild(int index) =>
        _track is not null ? _track : base.GetVisualChild(index);

    protected override Size MeasureOverride(Size constraint)
    {
        _track?.Measure(constraint);
        return new Size(40, 22);
    }

    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        _track?.Arrange(new Rect(0, 0, 40, 22));
        return arrangeBounds;
    }

    private void UpdateVisuals()
    {
        if (_track is null || _knob is null) return;

        var accent  = Application.Current.TryFindResource("Accent")      as Brush;
        var border  = Application.Current.TryFindResource("BorderStrong") as Brush;

        _track.Background = IsOn
            ? (accent  ?? new SolidColorBrush(Color.FromRgb(0x5B, 0x7E, 0xFF)))
            : (border  ?? new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)));

        _knob.Margin = IsOn
            ? new Thickness(21, 0, 0, 0)
            : new Thickness(3, 0, 0, 0);
    }
}
