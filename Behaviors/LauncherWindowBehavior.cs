using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Spur.ViewModels;

namespace Spur.Behaviors;

/// <summary>
/// Attached behavior for the main launcher window. Handles positioning,
/// keyboard navigation, drag-move, and window resize hit-testing.
/// </summary>
public static class LauncherWindowBehavior
{
    public static readonly DependencyProperty AttachProperty =
        DependencyProperty.RegisterAttached(
            "Attach", typeof(bool), typeof(LauncherWindowBehavior),
            new PropertyMetadata(false, OnAttachChanged));

    public static bool GetAttach(DependencyObject obj) => (bool)obj.GetValue(AttachProperty);
    public static void SetAttach(DependencyObject obj, bool value) => obj.SetValue(AttachProperty, value);

    private static void OnAttachChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window) return;
        if ((bool)e.NewValue)
        {
            window.SourceInitialized += OnSourceInitialized;
            window.PreviewKeyDown += OnPreviewKeyDown;
            window.KeyDown += OnKeyDown;
            window.MouseLeftButtonDown += OnMouseLeftButtonDown;
            window.Loaded += OnLoaded;
        }
        else
        {
            window.SourceInitialized -= OnSourceInitialized;
            window.PreviewKeyDown -= OnPreviewKeyDown;
            window.KeyDown -= OnKeyDown;
            window.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            window.Loaded -= OnLoaded;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window && window.DataContext is MainViewModel vm)
        {
            PositionWindow(window, vm);
        }
    }

    public static void PositionWindow(Window window, MainViewModel vm)
    {
        var screen = SystemParameters.WorkArea;
        var width = window.Width > 0 && !double.IsNaN(window.Width) ? window.Width : window.ActualWidth;
        var height = window.Height > 0 && !double.IsNaN(window.Height) ? window.Height : window.ActualHeight;

        // The RootBorder has Padding matching ShadowMargin to give DropShadowEffect rendering room.
        // Subtract this from the position so the visible capsule stays centered.
        const double shadowMargin = Spur.Models.LauncherLayout.ShadowMargin;

        // Center based on the search bar height (56px) so the drop down expands downward from the center
        var barHeight = 56.0;
        var left = screen.Left + (screen.Width - width) / 2;
        var top = screen.Top + (screen.Height - barHeight) / 2;

        switch (vm.Config.SearchWindowPosition?.ToLowerInvariant())
        {
            case "centertop":
            case "center top":
                top = screen.Top + screen.Height * 0.15;
                break;
            case "lefttop":
            case "left top":
                left = screen.Left + 32;
                top = screen.Top + 32;
                break;
            case "righttop":
            case "right top":
                left = screen.Right - width - 32;
                top = screen.Top + 32;
                break;
            case "custom":
                if (vm.Config.CustomWindowLeft >= 0) left = vm.Config.CustomWindowLeft;
                if (vm.Config.CustomWindowTop >= 0) top = vm.Config.CustomWindowTop;
                break;
        }

        window.Left = left - shadowMargin;
        window.Top = top - shadowMargin;
    }

    private static void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
            window.Closed += (_, _) => WindowBlur.DisableBlur(hwnd);
        }
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_NCHITTEST = 0x0084;
        if (msg != WM_NCHITTEST) return IntPtr.Zero;

        var result = NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
        int ht = result.ToInt32() & 0xFFFF;
        if (ht is 1 or 6 or 7) return result;

        var source = HwndSource.FromHwnd(hwnd);
        if (source?.RootVisual is not Window window) return IntPtr.Zero;

        var point = new Point(
            (short)(lParam.ToInt32() & 0xFFFF),
            (short)(lParam.ToInt32() >> 16));
        point = window.PointFromScreen(point);

        const int border = 6;
        bool left   = point.X <= border;
        bool right  = point.X >= window.ActualWidth - border;
        bool bottom = point.Y >= window.ActualHeight - border;
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

    private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Source is System.Windows.Controls.TextBox or System.Windows.Controls.Primitives.ScrollBar) return;
        if (sender is Window window)
        {
            try 
            { 
                window.DragMove(); 
                if (window.DataContext is MainViewModel vm && vm.Config.SearchWindowPosition == "custom")
                {
                    vm.Config.CustomWindowLeft = window.Left;
                    vm.Config.CustomWindowTop = window.Top;
                    vm.SaveConfig();
                }
            } 
            catch { /* Intentional: DragMove throws if button released mid-drag */ }
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not Window window || window.DataContext is not MainViewModel vm) return;

        if (vm.CommandPalette.IsOpen) return;

        if (e.Key is Key.Down or Key.Up)
        {
            vm.MoveSelection(e.Key == Key.Down ? 1 : -1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab && vm.IsScopeBarVisible)
        {
            vm.CycleScope();
            e.Handled = true;
        }
    }

    private static void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not Window window || window.DataContext is not MainViewModel vm) return;

        switch (e.Key)
        {
            case Key.Escape:
                if (vm.CommandPalette.IsOpen)
                    vm.CommandPalette.IsOpen = false;
                else if (!string.IsNullOrEmpty(vm.Query))
                    vm.Query = string.Empty;
                else if (vm.ActiveCategory is not null)
                    vm.ActiveCategory = null;
                else if (window is MainWindow mw)
                    mw.HideWindow();
                e.Handled = true;
                break;

            case Key.Left:
                if (vm.ActiveCategory is not null)
                {
                    vm.ActiveCategory = null;
                    e.Handled = true;
                }
                break;

            case Key.Enter:
                // Clipboard mode: paste selected entry and hide
                if (vm.ActiveCategory == "clipboard" && window is MainWindow cmw)
                {
                    var clipManager = FindClipboardManager(cmw);
                    clipManager?.PasteSelected();
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control)
                    vm.RunAsAdminCommand.Execute(null);
                else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                    vm.OpenFolderCommand.Execute(null);
                else
                    vm.OpenSelectedCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.P when Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift):
                vm.CommandPalette.IsOpen = !vm.CommandPalette.IsOpen;
                e.Handled = true;
                break;

            case Key.P when Keyboard.Modifiers == ModifierKeys.Control:
                vm.TogglePinCommand.Execute(vm.SelectedResult);
                e.Handled = true;
                break;

            case Key.D1 when Keyboard.Modifiers == ModifierKeys.Control:
                vm.ActiveCategory = vm.ActiveCategory == "files" ? null : "files";
                e.Handled = true; break;
            case Key.D2 when Keyboard.Modifiers == ModifierKeys.Control:
                vm.ActivateClipboardCategory();
                e.Handled = true; break;
            case Key.D3 when Keyboard.Modifiers == ModifierKeys.Control:
                vm.ActiveCategory = vm.ActiveCategory == "actions" ? null : "actions";
                e.Handled = true; break;

            case Key.OemComma when Keyboard.Modifiers == ModifierKeys.Control:
                vm.OpenSettingsCommand.Execute(null);
                e.Handled = true; break;

            case Key.C when Keyboard.Modifiers == ModifierKeys.Control:
                vm.CopySelectedPathCommand.Execute(null);
                e.Handled = true; break;

            case Key.E when Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift):
                vm.OpenFolderCommand.Execute(null);
                e.Handled = true; break;
        }
    }

    /// <summary>Walks the visual tree to find the ClipboardManager control.</summary>
    private static Views.ClipboardManager? FindClipboardManager(DependencyObject root)
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is Views.ClipboardManager cm) return cm;
            var result = FindClipboardManager(child);
            if (result is not null) return result;
        }
        return null;
    }
}
