using System.Windows;
using System.Windows.Controls;
using Spur.ViewModels;

namespace Spur.Views.Settings;

public partial class AiSettingsPanel : UserControl
{
    private bool _isLoaded;
    private bool _isKeyVisible;
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
        }
    }

    private void OnAiApiKeyChanged(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || _vm == null) return;
        _vm.ApiKey = AiApiKeyBox.Password;
    }

    private void OnToggleApiKeyVisibility(object sender, RoutedEventArgs e)
    {
        _isKeyVisible = !_isKeyVisible;

        if (_isKeyVisible)
        {
            // Show plain text
            AiApiKeyTextBox.Text = AiApiKeyBox.Password;
            ApiKeyBorder.Visibility = Visibility.Collapsed;
            ApiKeyTextBorder.Visibility = Visibility.Visible;
            ApiKeyToggleIcon.Glyph = "\uE890"; // Eye icon (hiding)
        }
        else
        {
            // Show password
            AiApiKeyBox.Password = AiApiKeyTextBox.Text;
            ApiKeyTextBorder.Visibility = Visibility.Collapsed;
            ApiKeyBorder.Visibility = Visibility.Visible;
            ApiKeyToggleIcon.Glyph = "\uE8F2"; // Eye icon (showing)
        }
    }

    private void OnApiKeyTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isLoaded || _vm == null) return;
        _vm.ApiKey = AiApiKeyTextBox.Text;
    }
}
