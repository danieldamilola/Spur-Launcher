using System.Windows.Controls.Primitives;
using Volt.ViewModels;

namespace Volt.Views;

/// <summary>DataTemplateSelector that routes SectionLabel vs SearchResult.</summary>
public sealed class ResultTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SectionTemplate { get; set; }
    public DataTemplate? ResultTemplate  { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is SectionLabel ? SectionTemplate : ResultTemplate;
}

public partial class ResultsList : UserControl
{
    private int _lastSelectedIndex = -1;

    public ResultsList()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel old)
            old.PropertyChanged -= OnVmChanged;
        if (e.NewValue is MainViewModel vm)
            vm.PropertyChanged += OnVmChanged;
    }

    private void OnVmChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedIndex))
            Dispatcher.InvokeAsync(UpdateSelection);
    }

    // ── Selection visual update ───────────────────────────────────
    private void UpdateSelection()
    {
        if (DataContext is not MainViewModel vm) return;
        int newIdx = vm.SelectedIndex;

        Deselect(_lastSelectedIndex);
        Select(newIdx);
        _lastSelectedIndex = newIdx;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // ListBox selection changed — sync visuals
        if (ResultsBox.SelectedIndex >= 0)
            UpdateSelection();
    }

    private void Select(int index)
    {
        var container = GetContainer(index);
        if (container is null) return;

        SetRowState(container, selected: true);
        container.BringIntoView();
    }

    private void Deselect(int index)
    {
        var container = GetContainer(index);
        if (container is not null)
            SetRowState(container, selected: false);
    }

    private FrameworkElement? GetContainer(int index)
    {
        if (index < 0) return null;
        return ResultsBox.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
    }

    private static void SetRowState(FrameworkElement container, bool selected)
    {
        // Walk down to find named elements in the DataTemplate
        var rowBg      = FindChild<Border>(container,    "RowBg");
        var accentRail = FindChild<Border>(container,    "AccentRail");
        var enterHint  = FindChild<Border>(container,    "EnterHint");
        var nameText   = FindChild<TextBlock>(container, "NameText");
        var subtitle   = FindChild<TextBlock>(container, "SubtitleText");

        if (rowBg is null) return;

        var selectedBg   = Res("SelectedBg")      as Brush ?? Brushes.White;
        var selectedText = Res("SelectedText")    as Brush ?? Brushes.Black;
        var selectedMuted= Res("SelectedMuted")   as Brush ?? Brushes.Gray;
        var primaryText  = Res("TextPrimary")     as Brush ?? Brushes.White;
        var secondaryText= Res("TextSecondary")   as Brush ?? Brushes.Gray;
        var accent       = Res("Accent")          as Brush ?? Brushes.Blue;

        rowBg.Background = selected ? selectedBg : Brushes.Transparent;

        if (nameText  is not null)
            nameText.Foreground = selected ? selectedText : primaryText;

        if (subtitle is not null)
        {
            subtitle.Foreground  = selected ? selectedMuted : secondaryText;
            subtitle.Visibility  = selected ? Visibility.Visible : Visibility.Collapsed;
        }

        if (accentRail is not null)
            accentRail.Visibility = selected ? Visibility.Visible : Visibility.Collapsed;

        if (enterHint is not null)
            enterHint.Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnRowMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement row) return;
        var rowBg    = FindChild<Border>(row, "RowBg");
        var hint     = FindChild<Border>(row, "EnterHint");
        var subtitle = FindChild<TextBlock>(row, "SubtitleText");

        if (IsRowSelected(row)) return;

        if (rowBg is not null)
            rowBg.Background = Res("HoverBg") as Brush ?? Brushes.Transparent;
        if (hint is not null)     hint.Visibility    = Visibility.Visible;
        if (subtitle is not null) subtitle.Visibility = Visibility.Visible;
    }

    private void OnRowMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement row) return;
        if (IsRowSelected(row)) return;

        var rowBg    = FindChild<Border>(row, "RowBg");
        var hint     = FindChild<Border>(row, "EnterHint");
        var subtitle = FindChild<TextBlock>(row, "SubtitleText");

        if (rowBg is not null)    rowBg.Background     = Brushes.Transparent;
        if (hint is not null)     hint.Visibility       = Visibility.Collapsed;
        if (subtitle is not null) subtitle.Visibility   = Visibility.Collapsed;
    }

    private void OnRowClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (sender is not FrameworkElement row) return;

        // Find which index this row corresponds to
        var item = (row as Grid)?.DataContext ?? row.DataContext;
        for (int i = 0; i < vm.Results.Count; i++)
        {
            if (ReferenceEquals(vm.Results[i], item))
            {
                vm.SelectedIndex = i;
                vm.OpenSelectedCommand.Execute(null);
                return;
            }
        }
    }

    private bool IsRowSelected(FrameworkElement row)
    {
        if (DataContext is not MainViewModel vm) return false;
        var item = (row as Grid)?.DataContext ?? row.DataContext;
        return vm.SelectedIndex >= 0 &&
               vm.SelectedIndex < vm.Results.Count &&
               ReferenceEquals(vm.Results[vm.SelectedIndex], item);
    }

    // ── Tree helpers ─────────────────────────────────────────────
    private static T? FindChild<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T fe && fe.Name == name) return fe;
            var found = FindChild<T>(child, name);
            if (found is not null) return found;
        }
        return null;
    }

    private static object? Res(string key) =>
        Application.Current.TryFindResource(key);
}
