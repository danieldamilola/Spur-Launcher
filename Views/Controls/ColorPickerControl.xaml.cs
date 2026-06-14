using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Spur.Views.Controls;

/// <summary>
/// A compact color picker with hex input, live preview, and preset swatch popup.
/// </summary>
public partial class ColorPickerControl : UserControl
{
    /// <summary>
    /// The hex color string (e.g. "#0078D7") bound two-way to the parent.
    /// </summary>
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

    // Preset palette: 20 colors — blues, purples, pinks, reds, oranges, greens
    private static readonly (string Hex, string Name)[] Presets = new[]
    {
        // Row 1 — Blues
        ("#0078D7", "Blue"),
        ("#0099BC", "Teal"),
        ("#2D7D9A", "Steel Teal"),
        ("#005A9E", "Dark Blue"),
        ("#004E8C", "Navy"),

        // Row 2 — Purples & Pinks
        ("#744DA9", "Purple"),
        ("#8764B8", "Soft Purple"),
        ("#B146C2", "Orchid"),
        ("#E3008C", "Magenta"),
        ("#C30052", "Rose"),

        // Row 3 — Reds & Oranges
        ("#E81123", "Red"),
        ("#EA005E", "Crimson"),
        ("#FF8C00", "Dark Orange"),
        ("#F7630C", "Orange"),
        ("#CA5010", "Burnt Orange"),

        // Row 4 — Greens
        ("#107C10", "Green"),
        ("#10893E", "Forest"),
        ("#00B294", "Mint"),
        ("#018574", "Dark Teal"),
        ("#486860", "Sage"),
    };

    public ColorPickerControl()
    {
        InitializeComponent();
        BuildSwatches();
        UpdatePreview(ColorHex);
    }

    private void BuildSwatches()
    {
        foreach (var (hex, name) in Presets)
        {
            var swatch = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(2),
                Cursor = Cursors.Hand,
                ToolTip = $"{name} ({hex})",
                Background = BrushFromHex(hex),
                BorderBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                BorderThickness = new Thickness(1),
            };

            // Capture hex in closure
            var capturedHex = hex;
            swatch.MouseLeftButtonDown += (_, _) =>
            {
                ColorHex = capturedHex;
                SwatchPopup.IsOpen = false;
            };

            // Hover feedback
            swatch.MouseEnter += (s, _) =>
            {
                if (s is Border b) b.BorderBrush = new SolidColorBrush(Colors.White);
            };
            swatch.MouseLeave += (s, _) =>
            {
                if (s is Border b) b.BorderBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
            };

            SwatchGrid.Children.Add(swatch);
        }
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
            PreviewBrush.Color = color;
        }
        catch
        {
            // Invalid hex — leave preview as-is
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

    private static SolidColorBrush BrushFromHex(string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        return new SolidColorBrush(color);
    }
}
