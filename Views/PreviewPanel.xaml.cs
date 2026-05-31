using Arc.ViewModels;

namespace Arc.Views;

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
        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnVmChanged;
            _vm.ConversationChanged -= OnConversationChanged;
        }
        _vm = e.NewValue as MainViewModel;
        if (_vm is not null)
        {
            _vm.PropertyChanged += OnVmChanged;
            _vm.ConversationChanged += OnConversationChanged;
            Refresh();
        }
    }

    private void OnConversationChanged(object? sender, EventArgs e)
    {
        if (_vm is null) return;
        Dispatcher.InvokeAsync(() =>
        {
            // Force redraw by setting to null first, since the underlying list instance hasn't changed
            ChatMessages.ItemsSource = null;
            ChatMessages.ItemsSource = _vm.AiConversation;
            UpdateAiConversationState();
            AiScroll.ScrollToEnd();
        });
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
                AiPanel.Visibility = Visibility.Visible;
                ChatMessages.ItemsSource = _vm.AiConversation;
                UpdateAiConversationState();
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

    private void UpdateAiConversationState()
    {
        var hasConversation = _vm?.AiConversation.Count > 0;
        AiEmptyPrompt.Visibility = hasConversation ? Visibility.Collapsed : Visibility.Visible;
        AiActions.Visibility = hasConversation ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnCancelTimer(object sender, RoutedEventArgs e)
    {
        _vm?.CancelTimerCommand.Execute(null);
    }

    private void OnAiFollowUpSend(object sender, RoutedEventArgs e)
        => SendAiFollowUp();

    private void OnAiFollowUpPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                // Allow TextBox to process it as a new line since AcceptsReturn="True"
                return;
            }
            else
            {
                e.Handled = true;
                SendAiFollowUp();
            }
        }
    }

    private void OnAiFollowUpFocus(object sender, RoutedEventArgs e)
    {
        if (AiFollowUp.Text is "Type a message..." or "Type a message…")
            AiFollowUp.Text = string.Empty;
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (AiPanel.Visibility != Visibility.Visible) return;
        AiScroll.ScrollToVerticalOffset(AiScroll.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private void SendAiFollowUp()
    {
        if (_vm is null) return;
        var text = AiFollowUp.Text;
        if (string.IsNullOrWhiteSpace(text) || text is "Type a message..." or "Type a message…") return;
        _vm.AiFollowUpCommand.Execute(text);
        AiFollowUp.Text = string.Empty;
    }

    private void OnAiBack(object sender, RoutedEventArgs e)
    {
        _vm?.BackFromAiChat();
    }

    private void OnAiCopy(object sender, RoutedEventArgs e)
    {
        var text = _vm?.GetAiConversationText();
        if (string.IsNullOrWhiteSpace(text)) return;

        try { Clipboard.SetText(text); }
        catch { /* Clipboard can be temporarily locked by another process. */ }
    }

    private void OnAiStop(object sender, RoutedEventArgs e)
    {
        _vm?.CancelAiGeneration();
    }
}
