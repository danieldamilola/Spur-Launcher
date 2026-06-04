using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Spur.ViewModels;

namespace Spur.Views.Settings;

public partial class SearchSettingsPanel : UserControl
{
    public SearchSettingsPanel()
    {
        InitializeComponent();
    }

    private void OnBrowseFolderClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select a folder to include in search"
        };
        if (dlg.ShowDialog() == true)
        {
            var path = dlg.FolderName;
            if (!vm.IndexedFoldersList.Contains(path, StringComparer.OrdinalIgnoreCase))
                vm.IndexedFoldersList.Add(path);
            vm.NewFolderPath = string.Empty;
        }
    }
}
