using System.Linq;
using System.Windows;
using System.Windows.Controls;
using iNKORE.UI.WPF.Modern.Controls;
using Spur.ViewModels;
using Spur.Views.Settings;

namespace Spur.Views;

public partial class SettingsView : UserControl
{
    private SettingsViewModel? _vm;

    public SettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm = DataContext as SettingsViewModel;
        if (_vm is null) return;

        _vm.PropertyChanged += OnVmPropertyChanged;
        SelectSection(_vm.SelectedSection.Name);
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.SelectedSection) && _vm is not null)
            SelectSection(_vm.SelectedSection.Name);
    }

    private void SelectSection(string sectionName)
    {
        var target = sectionName switch
        {
            "General" => NavGeneral,
            "Search" => NavSearch,
            "Hotkeys" => NavHotkeys,
            "AI" => NavAi,
            "Add-ons" => NavAddOns,
            "Store" => NavStore,
            "About" => NavAbout,
            _ => NavGeneral
        };

        if (NavView.SelectedItem != target)
            NavView.SelectedItem = target;

        ContentPanel.Content = sectionName switch
        {
            "General" => new GeneralSettingsPanel(),
            "Search" => new SearchSettingsPanel(),
            "Hotkeys" => new HotkeySettingsPanel(),
            "AI" => new AiSettingsPanel(),
            "Add-ons" => new AddOnsSettingsPanel(),
            "Store" => new AddOnStoreView(),
            "About" => new AboutSettingsPanel(),
            _ => new GeneralSettingsPanel()
        };
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;

        var vm = _vm ??= DataContext as SettingsViewModel;
        if (vm is null) return;

        var sectionName = item.Name switch
        {
            nameof(NavGeneral) => "General",
            nameof(NavSearch) => "Search",
            nameof(NavHotkeys) => "Hotkeys",
            nameof(NavAi) => "AI",
            nameof(NavAddOns) => "Add-ons",
            nameof(NavStore) => "Store",
            nameof(NavAbout) => "About",
            _ => "General"
        };

        if (vm.SelectedSection.Name != sectionName)
            vm.SelectedSection = vm.Sections.First(s => s.Name == sectionName);

        ContentPanel.Content = sectionName switch
        {
            "General" => new GeneralSettingsPanel(),
            "Search" => new SearchSettingsPanel(),
            "Hotkeys" => new HotkeySettingsPanel(),
            "AI" => new AiSettingsPanel(),
            "Add-ons" => new AddOnsSettingsPanel(),
            "Store" => new AddOnStoreView(),
            "About" => new AboutSettingsPanel(),
            _ => new GeneralSettingsPanel()
        };
    }
}
