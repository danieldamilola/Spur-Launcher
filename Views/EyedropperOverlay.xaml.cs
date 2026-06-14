using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Spur.Views;

/// <summary>
/// Fullscreen transparent overlay for eyedropper color picking.
/// Shows a magnified pixel grid near the cursor plus hex/RGB readout.
/// Click to capture the color, Escape to cancel.
/// </summary>
public partial class EyedropperOverlay : Window
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    /// <summary>The captured hex color (null if cancelled).</summary>
    public string? CapturedHex { get; private set; }

    /// <summary>The captured R value.</summary>
    public int CapturedR { get; private set; }

    /// <summary>The captured G value.</summary>
    public int CapturedG { get; private set; }

    /// <summary>The captured B value.</summary>
    public int CapturedB { get; private set; }

    /// <summary>Whether the user clicked to capture (true) or cancelled (false).</summary>
    public bool WasCaptured { get; private set; }

    private readonly Border[,] _zoomCells = new Border[9, 9];

    public EyedropperOverlay()
    {
        InitializeComponent();
        InitZoomGrid();
        Loaded += OnLoaded;
    }

    private void InitZoomGrid()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var cell = new Border
                {
                    Width = 12,
                    Height = 12,
                    Background = Brushes.Black,
                };
                // Center crosshair cell gets a border
                if (row == 4 && col == 4)
                {
                    cell.BorderBrush = Brushes.White;
                    cell.BorderThickness = new Thickness(1.5);
                }
                _zoomCells[row, col] = cell;
                ZoomGrid.Children.Add(cell);
            }
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Start tracking mouse
        MouseMove += OnOverlayMouseMove;
        MouseLeftButtonDown += OnOverlayClick;
        KeyDown += OnOverlayKeyDown;

        // Initial update
        UpdateColorInfo();
    }

    private void OnOverlayMouseMove(object sender, MouseEventArgs e)
    {
        UpdateColorInfo();
    }

    private void OnOverlayClick(object sender, MouseButtonEventArgs e)
    {
        // Capture the color at current cursor position
        GetCursorPos(out POINT pt);
        var (r, g, b) = SamplePixel(pt.X, pt.Y);

        CapturedR = r;
        CapturedG = g;
        CapturedB = b;
        CapturedHex = $"#{r:X2}{g:X2}{b:X2}";
        WasCaptured = true;

        // Copy to clipboard
        try { Clipboard.SetText(CapturedHex); } catch { /* Intentional: clipboard may be locked */ }

        Close();
    }

    private void OnOverlayKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            WasCaptured = false;
            Close();
        }
    }

    private void UpdateColorInfo()
    {
        GetCursorPos(out POINT pt);

        // Update zoom grid — sample 9x9 pixels centered on cursor
        IntPtr hdc = GetDC(IntPtr.Zero);
        try
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    int sx = pt.X + (col - 4);
                    int sy = pt.Y + (row - 4);
                    uint pixel = GetPixel(hdc, sx, sy);

                    int cr = (int)(pixel & 0xFF);
                    int cg = (int)((pixel >> 8) & 0xFF);
                    int cb = (int)((pixel >> 16) & 0xFF);

                    _zoomCells[row, col].Background = new SolidColorBrush(Color.FromRgb((byte)cr, (byte)cg, (byte)cb));
                }
            }

            // Center pixel is the "active" color
            uint centerPixel = GetPixel(hdc, pt.X, pt.Y);
            int r = (int)(centerPixel & 0xFF);
            int g = (int)((centerPixel >> 8) & 0xFF);
            int b = (int)((centerPixel >> 16) & 0xFF);

            var color = Color.FromRgb((byte)r, (byte)g, (byte)b);
            ColorSwatch.Background = new SolidColorBrush(color);
            HexText.Text = $"#{r:X2}{g:X2}{b:X2}";
            RgbText.Text = $"rgb({r}, {g}, {b})";
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdc);
        }

        // Position info panel near cursor (offset so it doesn't obscure the pick point)
        // Convert screen coords to WPF coords (handle DPI)
        var wpfPoint = PointFromScreen(new Point(pt.X, pt.Y));
        double offsetX = 20;
        double offsetY = 20;

        double panelLeft = wpfPoint.X + offsetX;
        double panelTop = wpfPoint.Y + offsetY;

        // Keep panel on screen
        double panelWidth = InfoPanel.ActualWidth > 0 ? InfoPanel.ActualWidth : 140;
        double panelHeight = InfoPanel.ActualHeight > 0 ? InfoPanel.ActualHeight : 200;

        if (panelLeft + panelWidth > ActualWidth - 10)
            panelLeft = wpfPoint.X - offsetX - panelWidth;
        if (panelTop + panelHeight > ActualHeight - 10)
            panelTop = wpfPoint.Y - offsetY - panelHeight;

        Canvas.SetLeft(InfoPanel, Math.Max(0, panelLeft));
        Canvas.SetTop(InfoPanel, Math.Max(0, panelTop));
    }

    private static (int R, int G, int B) SamplePixel(int x, int y)
    {
        IntPtr hdc = GetDC(IntPtr.Zero);
        uint pixel = GetPixel(hdc, x, y);
        ReleaseDC(IntPtr.Zero, hdc);

        int r = (int)(pixel & 0xFF);
        int g = (int)((pixel >> 8) & 0xFF);
        int b = (int)((pixel >> 16) & 0xFF);
        return (r, g, b);
    }
}
