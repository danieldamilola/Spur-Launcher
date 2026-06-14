using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Spur.Views.Controls;

public partial class ColorPickerControl : UserControl
{
    public static readonly DependencyProperty ColorHexProperty =
        DependencyProperty.Register(
            nameof(ColorHex),
            typeof(string),
            typeof(ColorPickerControl),
            new FrameworkPropertyMetadata(
                "#0078D7",
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnColorHexChanged));

    public string ColorHex
    {
        get => (string)GetValue(ColorHexProperty);
        set => SetValue(ColorHexProperty, value);
    }

    public ColorPickerControl()
    {
        InitializeComponent();
        UpdatePreview(ColorHex);
    }

    private static void OnColorHexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorPickerControl picker && e.NewValue is string hex)
        {
            picker.UpdatePreview(hex);
        }
    }

    private void UpdatePreview(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return;

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            PreviewBorder.Background = new SolidColorBrush(color);
        }
        catch
        {
            // Invalid hex — keep the current preview
        }
    }

    private void OnPreviewClick(object sender, MouseButtonEventArgs e)
    {
        SwatchPopup.IsOpen = !SwatchPopup.IsOpen;
    }

    private void OnDropdownClick(object sender, MouseButtonEventArgs e)
    {
        SwatchPopup.IsOpen = !SwatchPopup.IsOpen;
    }

    private void OnSwatchClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string hex)
        {
            ColorHex = hex;
            SwatchPopup.IsOpen = false;
        }
    }
}
