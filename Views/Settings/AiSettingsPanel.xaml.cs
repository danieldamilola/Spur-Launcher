using System.Windows;
using System.Windows.Controls;
using Spur.ViewModels;

namespace Spur.Views.Settings;

public partial class AiSettingsPanel : UserControl
{
    private bool _isLoaded;
    private SettingsViewModel? _vm;

    public AiSettingsPanel()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm = DataContext as SettingsViewModel;

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
            // Sync model combo text when provider changes
            if (_vm != null)
                ModelComboBox.Text = _vm.AiModel;
        }
    }

    private void OnAiApiKeyChanged(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || _vm == null) return;
        _vm.ApiKey = AiApiKeyBox.Password;
    }

    private void ModelComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        // Set initial text from config (works even if value isn't in the dropdown list)
        if (_vm != null)
            ModelComboBox.Text = _vm.AiModel;
    }

    private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || _vm == null) return;
        if (ModelComboBox.SelectedItem is string selected && !string.IsNullOrEmpty(selected))
            _vm.AiModel = selected;
    }

    private void ModelComboBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || _vm == null) return;
        var text = ModelComboBox.Text?.Trim();
        if (!string.IsNullOrEmpty(text))
            _vm.AiModel = text;
    }
}
