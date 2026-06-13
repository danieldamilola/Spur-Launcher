using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Spur.ViewModels;
using Spur.Helpers;

namespace Spur.Views;

public partial class AiChatPanel : UserControl
{
    public AiChatPanel()
    {
        InitializeComponent();
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    /// <summary>Called by MainWindow when the user presses Enter in AI mode.</summary>
    public void HandleUserInput(string query)
    {
        if (Vm is null || string.IsNullOrWhiteSpace(query)) return;
        _ = Vm.SendAiQuery(query);
    }

    /// <summary>Subscribe to conversation changes when loaded.</summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Vm?.AiChat is not null)
            Vm.AiChat.ConversationChanged += OnConversationChanged;
        RefreshConversation();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Vm?.AiChat is not null)
            Vm.AiChat.ConversationChanged -= OnConversationChanged;
    }

    private void OnConversationChanged(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(RefreshConversation);
    }

    private void RefreshConversation()
    {
        if (Vm?.AiChat is null) return;

        var conversation = Vm.AiChat.AiConversation;
        var isLoading = Vm.AiChat.AiLoading;
        var error = Vm.AiChat.AiError;

        // Show/hide empty state
        EmptyState.Visibility = conversation.Count == 0 && !isLoading
            ? Visibility.Visible : Visibility.Collapsed;

        // Show/hide conversation
        ConversationPanel.Visibility = conversation.Count > 0 || isLoading
            ? Visibility.Visible : Visibility.Collapsed;

        // Build conversation cards
        ConversationItems.Children.Clear();

        // Group into Q&A pairs
        for (int i = 0; i < conversation.Count; i++)
        {
            var (role, content) = conversation[i];

            if (role == "user")
            {
                // Find the assistant response (next item)
                string? assistantContent = null;
                if (i + 1 < conversation.Count && conversation[i + 1].Role == "assistant")
                {
                    assistantContent = conversation[i + 1].Content;
                    i++; // skip assistant turn
                }

                var card = CreateConversationCard(content, assistantContent);
                ConversationItems.Children.Add(card);
            }
        }

        // Show thinking indicator if loading and no assistant response yet
        ThinkingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;

        // Show error
        ErrorBlock.Visibility = !string.IsNullOrEmpty(error) ? Visibility.Visible : Visibility.Collapsed;
        ErrorBlock.Text = error;

        // Model name
        ModelLabel.Text = Vm.AiModelName;

        // Clear button visibility
        ClearButton.Visibility = conversation.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        // Auto-scroll to bottom
        ChatScroll.ScrollToEnd();
    }

    private Border CreateConversationCard(string question, string? answer)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            BorderBrush = FindResource("Separator") as System.Windows.Media.Brush,
            Background = FindResource("Depth2") as System.Windows.Media.Brush,
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 0, 0, 12),
        };

        var stack = new StackPanel();

        // Question
        var questionBlock = new TextBlock
        {
            Text = question,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = FindResource("Token.Font.Primary") as System.Windows.Media.FontFamily ?? new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 13,
            Foreground = FindResource("TextTertiary") as System.Windows.Media.Brush,
            Margin = new Thickness(0, 0, 0, 10),
        };
        stack.Children.Add(questionBlock);

        // Answer (markdown rendered)
        if (!string.IsNullOrEmpty(answer))
        {
            var doc = MarkdownRenderer.Render(answer, Application.Current.Resources);
            var reader = new FlowDocumentScrollViewer
            {
                Document = doc,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                IsToolBarVisible = false,
                Focusable = false,
                Margin = new Thickness(-5, 0, -5, 0),
            };
            // Remove default padding/chrome
            reader.SetValue(Control.BorderThicknessProperty, new Thickness(0));
            reader.SetValue(Control.PaddingProperty, new Thickness(0));
            stack.Children.Add(reader);
        }

        card.Child = stack;
        return card;
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        Vm?.ClearAiChat();
    }

    private void OnPasteResponseClick(object sender, RoutedEventArgs e)
    {
        if (Vm?.AiChat is null) return;
        var text = Vm.AiChat.GetConversationText();
        if (!string.IsNullOrEmpty(text))
            System.Windows.Clipboard.SetText(text);
    }
}
