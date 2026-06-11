using Spur.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Spur.Views;

public partial class SettingsView : UserControl
{
    private SettingsViewModel? _vm;
    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            _vm = e.NewValue as SettingsViewModel;
        };
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => _vm?.CloseSettings();
}

// ── Section visibility converter ────────────────────────────────────
public sealed class SectionVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter,
        System.Globalization.CultureInfo culture)
    {
        var sectionName = (value as Spur.ViewModels.SettingsSection)?.Name ?? "";
        var target = parameter as string ?? "";
        return sectionName == target
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter,
        System.Globalization.CultureInfo culture) => throw new NotSupportedException();
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
                      ?? new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF));
        var offBg   = TryFindResource("Depth4")      as Brush
                      ?? new SolidColorBrush(Color.FromRgb(0x27, 0x27, 0x2A));
        var knobClr = TryFindResource("TextPrimary") as Brush ?? Brushes.White;

        _track.Background = IsOn ? accent : offBg;
        _knob.Background  = knobClr;
        _knob.Margin      = IsOn
            ? new Thickness(21, 0, 0, 0)
            : new Thickness(3, 0, 0, 0);
    }
}
