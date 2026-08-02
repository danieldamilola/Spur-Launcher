using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Spur.Extensions;
using Spur.ViewModels;

namespace Spur.Views;

/// <summary>
/// Expanded-mode homepage showing category shortcuts and enabled add-ons.
/// DataContext is <see cref="MainViewModel"/>.
/// </summary>
public partial class HomePanel : UserControl
{
    public HomePanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        CategoryCardCommand = new RelayCommand<string>(OnCategoryCardExecute);
        AddOnCommand = new RelayCommand<AddOnDisplay>(OnAddOnExecute);
    }

    public ICommand CategoryCardCommand { get; }
    public ICommand AddOnCommand { get; }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
            oldVm.PropertyChanged -= OnVmPropertyChanged;

        if (e.NewValue is MainViewModel newVm)
        {
            newVm.PropertyChanged += OnVmPropertyChanged;
            PopulateAddOns();
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.IsExpandedHome))
        {
            if (Vm?.IsExpandedHome == true)
                Dispatcher.InvokeAsync(PopulateAddOns);
        }
    }

    private void PopulateAddOns()
    {
        if (Vm is null) return;

        var items = Vm.EnabledAddOns.Select(a => new AddOnDisplay
        {
            AddOn = a,
            Name = a.Name,
            DisplayKeyword = string.IsNullOrEmpty(a.Keyword) ? "=" : a.Keyword,
            Description = a.Description,
            IconGlyph = a.IconGlyph,
            IconPath = a.IconPath,
        }).ToList();

        AddOnsList.ItemsSource = items;
    }

    private void OnCategoryCardExecute(string? categoryId)
    {
        if (Vm is null || categoryId is null) return;
        Vm.ActiveCategory = categoryId;
    }

    private void OnAddOnExecute(AddOnDisplay? display)
    {
        if (Vm is null || display is null) return;

        var addOn = display.AddOn;
        if (!string.IsNullOrEmpty(addOn.Keyword))
            Vm.Query = addOn.Keyword + " ";
        else if (addOn.IsGlobal)
            Vm.Query = "= ";
    }

    /// <summary>Category card click — immediately switch to that category.</summary>
    private void OnCategoryCardClick(object sender, MouseButtonEventArgs e)
    {
        if (Vm is null) return;
        if (sender is not FrameworkElement { Tag: string categoryId }) return;
        Vm.ActiveCategory = categoryId;
    }

    /// <summary>Add-on row click — type the keyword into the search bar to activate scope.</summary>
    private void OnAddOnClick(object sender, MouseButtonEventArgs e)
    {
        if (Vm is null) return;
        if (sender is not FrameworkElement { DataContext: AddOnDisplay display }) return;

        var addOn = display.AddOn;
        if (!string.IsNullOrEmpty(addOn.Keyword))
            Vm.Query = addOn.Keyword + " ";
        else if (addOn.IsGlobal)
            Vm.Query = "= ";
    }

    /// <summary>Display model for homepage add-on rows.</summary>
    internal sealed class AddOnDisplay
    {
        public required IAddOn AddOn { get; init; }
        public required string Name { get; init; }
        public required string DisplayKeyword { get; init; }
        public required string Description { get; init; }
        public required string IconGlyph { get; init; }
        public string? IconPath { get; init; }
    }
}
