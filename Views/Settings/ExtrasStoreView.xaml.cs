using System;
using System.Windows;
using System.Windows.Controls;
using Spur.Models;
using Spur.ViewModels;

namespace Spur.Views.Settings;

public partial class ExtrasStoreView : UserControl
{
    private SettingsViewModel? _vm;

    public ExtrasStoreView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) => _vm = e.NewValue as SettingsViewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_vm == null || StoreItems.ItemsSource != null) return;
        await LoadManifestAsync();
    }

    private async void OnRetryClicked(object sender, RoutedEventArgs e)
    {
        await LoadManifestAsync();
    }

    private async System.Threading.Tasks.Task LoadManifestAsync()
    {
        if (_vm?.StoreService == null) return;

        LoadingPanel.Visibility = Visibility.Visible;
        ErrorPanel.Visibility = Visibility.Collapsed;
        DataPanel.Visibility = Visibility.Collapsed;

        try
        {
            var manifest = await _vm.StoreService.GetManifestAsync();

            if (manifest.Count == 0)
            {
                ErrorPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StoreItems.ItemsSource = manifest;
                DataPanel.Visibility = Visibility.Visible;
            }
        }
        catch (Exception)
        {
            ErrorPanel.Visibility = Visibility.Visible;
        }
        finally
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
        }
    }

    private async void OnInstallClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.DataContext is not StoreManifestEntry entry || _vm?.StoreService == null)
            return;

        btn.IsEnabled = false;
        btn.Content = "Installing...";

        try
        {
            var success = await _vm.StoreService.InstallExtraAsync(entry);

            if (success)
            {
                btn.Content = "Installed";
            }
            else
            {
                btn.IsEnabled = true;
                btn.Content = "Install Failed";
            }
        }
        catch (Exception)
        {
            btn.IsEnabled = true;
            btn.Content = "Install Failed";
        }
    }
}
