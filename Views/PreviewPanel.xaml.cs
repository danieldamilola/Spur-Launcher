using Volt.ViewModels;

namespace Volt.Views;

public partial class PreviewPanel : UserControl
{
    private MainViewModel? _vm;

    public PreviewPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null) _vm.PropertyChanged -= OnVmChanged;
        _vm = e.NewValue as MainViewModel;
        if (_vm is not null)
        {
            _vm.PropertyChanged += OnVmChanged;
            Refresh();
        }
    }

    private void OnVmChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is
            nameof(MainViewModel.ActiveActionId) or
            nameof(MainViewModel.CalcResult) or
            nameof(MainViewModel.CalcExpr) or
            nameof(MainViewModel.ColorHex) or
            nameof(MainViewModel.ColorRgb) or
            nameof(MainViewModel.ColorHsl) or
            nameof(MainViewModel.ColorSwatch) or
            nameof(MainViewModel.TimerDisplay) or
            nameof(MainViewModel.TimerProgress) or
            nameof(MainViewModel.TimerRunning) or
            nameof(MainViewModel.IpLocal) or
            nameof(MainViewModel.IpPublic) or
            nameof(MainViewModel.AiText) or
            nameof(MainViewModel.AiLoading) or
            nameof(MainViewModel.AiError))
        {
            Dispatcher.InvokeAsync(Refresh);
        }
    }

    private void Refresh()
    {
        if (_vm is null) return;

        HideAll();

        switch (_vm.ActiveActionId)
        {
            case "calc":
                CalcPanel.Visibility    = Visibility.Visible;
                CalcExprText.Text       = _vm.CalcExpr;
                CalcResultText.Text     = _vm.CalcResult;
                break;

            case "color":
                ColorPanel.Visibility   = Visibility.Visible;
                ColorSwatch.Background  = new SolidColorBrush(_vm.ColorSwatch);
                ColorHexText.Text       = _vm.ColorHex;
                ColorRgbText.Text       = _vm.ColorRgb;
                ColorHslText.Text       = _vm.ColorHsl;
                break;

            case "timer":
                TimerPanel.Visibility   = Visibility.Visible;
                TimerCountdown.Text     = _vm.TimerDisplay;
                SetTimerBar(_vm.TimerProgress);
                TimerStatus.Text        = _vm.TimerRunning ? "Running…" : "Press ↵ to start";
                CancelTimerBtn.Visibility = _vm.TimerRunning
                    ? Visibility.Visible : Visibility.Collapsed;
                break;

            case "ip":
                IpPanel.Visibility      = Visibility.Visible;
                IpLocalText.Text        = _vm.IpLocal;
                IpPublicText.Text       = _vm.IpPublic;
                break;

            case "ai":
                AiPanel.Visibility      = Visibility.Visible;
                AiLoadingText.Visibility = _vm.AiLoading && string.IsNullOrEmpty(_vm.AiText)
                    ? Visibility.Visible : Visibility.Collapsed;
                AiResponseText.Text     = _vm.AiText;
                AiErrorText.Text        = _vm.AiError;
                AiErrorText.Visibility  = string.IsNullOrEmpty(_vm.AiError)
                    ? Visibility.Collapsed : Visibility.Visible;
                AiFooter.Visibility     = !_vm.AiLoading && !string.IsNullOrEmpty(_vm.AiText)
                    ? Visibility.Visible : Visibility.Collapsed;
                AiScroll.ScrollToEnd();
                break;

            default:
                EmptyState.Visibility   = Visibility.Visible;
                break;
        }
    }

    private void SetTimerBar(double progress)
    {
        // TimerBar width = parent width * progress%
        var parentWidth = ((Border)TimerBar.Parent).ActualWidth;
        if (parentWidth <= 0) parentWidth = 260;
        TimerBar.Width = Math.Max(0, parentWidth * progress / 100.0);
    }

    private void HideAll()
    {
        CalcPanel.Visibility  = Visibility.Collapsed;
        ColorPanel.Visibility = Visibility.Collapsed;
        TimerPanel.Visibility = Visibility.Collapsed;
        IpPanel.Visibility    = Visibility.Collapsed;
        AiPanel.Visibility    = Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Collapsed;
    }

    private void OnCancelTimer(object sender, RoutedEventArgs e)
    {
        _vm?.CancelTimerCommand.Execute(null);
    }
}
