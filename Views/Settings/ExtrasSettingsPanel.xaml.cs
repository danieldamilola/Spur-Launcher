using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Spur.ViewModels;

namespace Spur.Views.Settings;

public partial class ExtrasSettingsPanel : UserControl
{
    private SettingsViewModel? _vm;

    public ExtrasSettingsPanel()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (_vm is not null) _vm.PropertyChanged -= OnVmChanged;
            _vm = e.NewValue as SettingsViewModel;
            if (_vm is not null)
            {
                _vm.PropertyChanged += OnVmChanged;
                AiApiKeyBox.Password = _vm.ApiKey;
            }
        };
    }

    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsViewModel.ApiKey) or nameof(SettingsViewModel.AiProvider))
            AiApiKeyBox.Password = _vm?.ApiKey ?? string.Empty;
    }

    private void OnAiApiKeyChanged(object sender, RoutedEventArgs e)
    {
        if (_vm is null || AiApiKeyBox.Password == _vm.ApiKey) return;
        _vm.ApiKey = AiApiKeyBox.Password;
    }
}
