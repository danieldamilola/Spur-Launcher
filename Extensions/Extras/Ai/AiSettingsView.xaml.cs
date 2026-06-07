using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using Spur.ViewModels;

namespace Spur.Extensions.Extras.Ai;

public partial class AiSettingsView : UserControl
{
    private bool _isLoaded;
    private SettingsViewModel? _vm;

    public AiSettingsView()
    {
        InitializeComponent();
        
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm = Ioc.Default.GetService<SettingsViewModel>();
        DataContext = _vm;

        if (_vm != null)
        {
            AiApiKeyBox.Password = _vm.ApiKey;
            _vm.PropertyChanged += OnVmPropertyChanged;
        }
        
        _isLoaded = true;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.AiProvider))
        {
            // Update the password box when the provider changes so it shows the right API key
            AiApiKeyBox.Password = _vm?.ApiKey ?? string.Empty;
        }
    }

    private void OnAiApiKeyChanged(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || _vm == null) return;
        _vm.ApiKey = AiApiKeyBox.Password;
    }
}
