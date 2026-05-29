using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media;
using Arc.ViewModels;

namespace Arc.Views;

public partial class SearchBar : UserControl
{
    private MainViewModel? _vm;

    public SearchBar()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => FocusInput();
        ModeIconBtn.Loaded += (_, _) => UpdateModeIcon();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null) _vm.PropertyChanged -= OnVmPropertyChanged;
        _vm = e.NewValue as MainViewModel;
        if (_vm != null) _vm.PropertyChanged += OnVmPropertyChanged;
        UpdateModeIcon();
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.ActiveCategory)
                           or nameof(MainViewModel.IsSettingsOpen)
                           or nameof(MainViewModel.Config))
        {
            Dispatcher.InvokeAsync(() =>
            {
                UpdateModeIcon();
                UpdatePlaceholder();
            });
        }
    }

    public void FocusInput()
    {
        SearchInput.Focus();
        if (_vm?.Config.LastQueryStyle == "select")
            SearchInput.SelectAll();
        else
            SearchInput.CaretIndex = SearchInput.Text.Length;
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePlaceholder();
    }

    private void UpdatePlaceholder()
    {
        Placeholder.Visibility = SearchInput.Text.Length == 0 && _vm?.Config.ShowPlaceholder != false
            ? Visibility.Visible : Visibility.Collapsed;
    }

    // ═══════════════════════════════════════════════════════════════
    // Mode icon (left — search / back-arrow when category is active)
    // ═══════════════════════════════════════════════════════════════

    private bool _isIconHovered;
    private const string IconSearch = "M11 19C15.4183 19 19 15.4183 19 11C19 6.58172 15.4183 3 11 3C6.58172 3 3 6.58172 3 11C3 15.4183 6.58172 19 11 19ZM21 21L16.65 16.65";
    private const string IconBack   = "M19 12H5M12 19l-7-7 7-7";

    private void UpdateModeIcon()
    {
        if (ModeIconBtn.Template?.FindName("IconPath", ModeIconBtn) is not System.Windows.Shapes.Path icon)
            return;

        bool settingsOpen = _vm?.IsSettingsOpen == true;
        bool hasCategory  = _vm?.ActiveCategory != null;

        if (!settingsOpen && !hasCategory)
        {
            icon.Data   = Geometry.Parse(IconSearch);
            icon.Stroke = TryFindResource("TextMuted") as Brush;
            ModeIconBtn.Cursor = Cursors.IBeam;
            return;
        }

        ModeIconBtn.Cursor = Cursors.Hand;

        if (settingsOpen)
        {
            icon.Data   = Geometry.Parse(IconBack);
            icon.Stroke = TryFindResource("TextPrimary") as Brush;
            return;
        }

        if (_isIconHovered)
        {
            icon.Data   = Geometry.Parse(IconBack);
            icon.Stroke = TryFindResource("TextPrimary") as Brush;
        }
        else
        {
            icon.Stroke = TryFindResource("TextPrimary") as Brush;
            icon.Data   = Geometry.Parse(_vm?.ActiveCategory switch
            {
                "apps"      => "M3 3h7v7H3V3zm11 0h7v7h-7V3zm0 11h7v7h-7v-7zM3 14h7v7H3v-7z",
                "files"     => "M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V7z",
                "clipboard" => "M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2M9 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M9 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2",
                "actions"   => "M13 2L3 14h9l-1 8 10-12h-9l1-8z",
                _           => IconSearch,
            });
        }
    }

    private void OnModeIconMouseEnter(object sender, MouseEventArgs e)
    {
        _isIconHovered = true;
        UpdateModeIcon();
    }

    private void OnModeIconMouseLeave(object sender, MouseEventArgs e)
    {
        _isIconHovered = false;
        UpdateModeIcon();
    }

    private void OnModeIconClick(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;

        if (_vm.IsSettingsOpen)
        {
            _vm.IsSettingsOpen = false;
            FocusInput();
        }
        else if (_vm.ActiveCategory != null)
        {
            _vm.ActiveCategory = null;
            FocusInput();
        }
    }
}

