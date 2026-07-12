using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Spur.Models;
using Spur.Services;
using Spur.ViewModels;

namespace Spur.Views;

/// <summary>
/// Code-behind for ClipboardManager.xaml. Refreshes the ViewModel on load
/// and handles paste-on-click and merge button interactions.
/// </summary>
public partial class ClipboardManager : UserControl
{
    private ClipboardViewModel? _vm;
    private readonly System.Windows.Threading.DispatcherTimer _refreshTimer;

    public ClipboardManager()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        IsVisibleChanged += OnIsVisibleChanged;

        _refreshTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(60)
        };
        _refreshTimer.Tick += (_, _) => _vm?.RefreshTimestamps();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ClipboardViewModel oldVm)
            oldVm.PropertyChanged -= OnVmPropertyChanged;

        if (e.NewValue is ClipboardViewModel vm)
        {
            _vm = vm;
            vm.PropertyChanged += OnVmPropertyChanged;
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ClipboardViewModel.SelectedEntry))
            Dispatcher.InvokeAsync(UpdateDetailPinState);
    }

    /// <summary>Syncs the detail panel pin button glyph with the selected entry's pinned state.</summary>
    private void UpdateDetailPinState()
    {
        var entry = _vm?.SelectedEntry;
        bool isPinned = entry?.IsPinned == true;
        DetailPinGlyph.Text = isPinned ? "Unpin" : "Pin to top";
        DetailPinBtn.ToolTip = isPinned ? "Unpin" : "Pin";
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible && _vm is not null)
        {
            _vm.Refresh();
            _refreshTimer.Start();
        }
        else
        {
            _refreshTimer.Stop();
        }
    }

    private void OnListMouseClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject src)
        {
            // Don't trigger paste if the click was on an action button
            if (FindAncestor<Button>(src) is not null) return;

            var item = ItemsControl.ContainerFromElement(ClipList, src) as ListBoxItem;
            if (item?.DataContext is ClipboardEntry entry && _vm != null)
            {
                _vm.SelectedEntry = entry;
                PasteAndHide(entry);
                e.Handled = true;
            }
        }
    }

    private static T? FindAncestor<T>(DependencyObject? obj) where T : DependencyObject
    {
        while (obj is not null)
        {
            if (obj is T target) return target;
            obj = System.Windows.Media.VisualTreeHelper.GetParent(obj);
        }
        return null;
    }

    /// <summary>
    /// Copies the entry to clipboard, hides Spur, and simulates Ctrl+V
    /// to paste into the previously focused application.
    /// </summary>
    public void PasteAndHide(ClipboardEntry? entry)
    {
        if (entry is null || _vm is null) return;

        // Copy to clipboard (with suppression to avoid duplication)
        _vm.Copy(entry);

        // Hide the window
        var mainWindow = Window.GetWindow(this) as MainWindow;
        mainWindow?.HideWindow();

        // Small delay to let the previous app regain focus, then simulate Ctrl+V
        Task.Delay(150).ContinueWith(_ =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                SimulateCtrlV();
            });
        });
    }

    /// <summary>Simulates a Ctrl+V keypress using Win32 SendInput.</summary>
    private static void SimulateCtrlV()
    {
        var inputs = new INPUT[4];

        // Ctrl down
        inputs[0].type = 1; // INPUT_KEYBOARD
        inputs[0].ki.wVk = 0x11; // VK_CONTROL

        // V down
        inputs[1].type = 1;
        inputs[1].ki.wVk = 0x56; // VK_V

        // V up
        inputs[2].type = 1;
        inputs[2].ki.wVk = 0x56;
        inputs[2].ki.dwFlags = 0x0002; // KEYEVENTF_KEYUP

        // Ctrl up
        inputs[3].type = 1;
        inputs[3].ki.wVk = 0x11;
        inputs[3].ki.dwFlags = 0x0002;

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    // ── Win32 SendInput interop ───────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
        // Padding to match the union size of INPUT (mouse/hardware)
        private readonly ulong _padding;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);


    public void MoveSelection(int delta) => _vm?.MoveSelection(delta);

    /// <summary>Called from the keyboard handler when Enter is pressed in clipboard mode.</summary>
    public void PasteSelected() => PasteAndHide(_vm?.SelectedEntry);

    private void OnPreviewClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ClipboardEntry entry && _vm is not null)
        {
            _vm.SelectedEntry = entry;
            if (Window.GetWindow(this) is MainWindow win)
                win.ShowClipboardPreview();
        }
    }

    private void OnMergeClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu is not null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            btn.ContextMenu.IsOpen = true;
        }
    }

    /// <summary>Hides the inline detail panel when floating mode is active.</summary>
    public void SetFloatingMode(bool floating)
    {
        DetailSeparator.Visibility = floating ? Visibility.Collapsed : Visibility.Visible;
        DetailPanel.Visibility = floating ? Visibility.Collapsed : Visibility.Visible;
    }
}
