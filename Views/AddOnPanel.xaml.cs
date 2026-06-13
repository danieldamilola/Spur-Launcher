using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Data;
using Spur.ViewModels;
using Spur.Helpers;

namespace Spur.Views;

public partial class AddOnPanel : UserControl
{
    public AddOnPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private MainViewModel? Vm => DataContext as MainViewModel;
    private string? _lastPanel;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnVmPropertyChanged;
            oldVm.AiChat.ConversationChanged -= OnAiConversationChanged;
        }
        if (e.NewValue is MainViewModel newVm)
        {
            newVm.PropertyChanged += OnVmPropertyChanged;
            newVm.AiChat.ConversationChanged += OnAiConversationChanged;
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.ActiveFullPanel)
            or nameof(MainViewModel.ActionResultText)
            or nameof(MainViewModel.ActionResultSubText)
            or nameof(MainViewModel.ActionPreviewQuery)
            or nameof(MainViewModel.ActionPreviewSubtitle))
        {
            Dispatcher.InvokeAsync(Refresh);
        }
    }

    private void OnAiConversationChanged(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(Refresh);
    }

    /// <summary>Called by MainWindow when the user presses Enter in panel mode.</summary>
    public void HandleUserInput(string query)
    {
        if (Vm is null || string.IsNullOrWhiteSpace(query)) return;

        var panel = Vm.ActiveFullPanel;
        if (panel == "ai")
        {
            _ = Vm.SendAiQuery(query);
        }
    }

    private void Refresh()
    {
        if (Vm is null) return;
        var panel = Vm.ActiveFullPanel;

        // Update footer
        FooterLabel.Text = GetPanelLabel(panel);
        FooterIcon.Source = GetPanelIconSource(panel);
        ClearButton.Visibility = panel == "ai" && Vm.AiChat.AiConversation.Count > 0
            ? Visibility.Visible : Visibility.Collapsed;
        CopyButton.Visibility = panel is "ai" or "calc" or "color" or "ip" or "pw" or "currency"
            ? Visibility.Visible : Visibility.Collapsed;

        // Only rebuild content if panel changed
        if (panel != _lastPanel)
        {
            _lastPanel = panel;
            BuildContent(panel);
        }
        else if (panel == "ai")
        {
            BuildAiContent();
        }
        else
        {
            UpdateDynamicContent(panel);
        }
    }

    private void BuildContent(string? panel)
    {
        ContentArea.Children.Clear();
        switch (panel)
        {
            case "ai":     BuildAiContent(); break;
            case "calc":   BuildCalcContent(); break;
            case "timer":  BuildTimerContent(); break;
            case "color":  BuildColorContent(); break;
            case "ip":     BuildIpContent(); break;
            case "currency": BuildCurrencyContent(); break;
            case "pw":     BuildPasswordContent(); break;
            case "kill":   BuildGenericContent("\uE747", "Kill Process"); break;
            case "note":   BuildGenericContent("\uE8A5", "Quick Note"); break;
            case "screenshot": BuildGenericContent("\uE74C", "Screenshot"); break;
            case "system": BuildGenericContent("\uE117", "System Command"); break;
            case "shell":  BuildGenericContent("\uE765", "Shell"); break;
            default:       BuildEmptyState(); break;
        }
        ContentScroll.ScrollToEnd();
    }

    private void UpdateDynamicContent(string? panel)
    {
        // Update text bindings for panels that show ActionResultText
        if (panel is "calc" or "color" or "currency" or "pw" or "ip" or "kill" or "note" or "screenshot" or "system" or "shell")
        {
            // Force rebuild to pick up new data
            BuildContent(panel);
        }
    }

    // ─── AI ───────────────────────────────────────────────────
    private void BuildAiContent()
    {
        ContentArea.Children.Clear();
        if (Vm is null) return;

        var conversation = Vm.AiChat.AiConversation;
        var isLoading = Vm.AiChat.AiLoading;
        var error = Vm.AiChat.AiError;

        if (conversation.Count == 0 && !isLoading)
        {
            BuildEmptyState("What can I help with?", "Type a question and press Enter");
            return;
        }

        // Build conversation cards
        for (int i = 0; i < conversation.Count; i++)
        {
            var (role, content) = conversation[i];
            if (role == "user")
            {
                string? assistantContent = null;
                if (i + 1 < conversation.Count && conversation[i + 1].Role == "assistant")
                {
                    assistantContent = conversation[i + 1].Content;
                    i++;
                }
                ContentArea.Children.Add(CreateConversationCard(content, assistantContent));
            }
        }

        // Thinking indicator
        if (isLoading)
        {
            var thinking = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 12) };
            thinking.Children.Add(new Border
            {
                Width = 8, Height = 8, CornerRadius = new CornerRadius(4),
                Background = TryFindResource("AccentBrush") as Brush ?? Brushes.CornflowerBlue,
                Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, Opacity = 0.7
            });
            thinking.Children.Add(new TextBlock
            {
                Text = "Thinking...", FontSize = 13, FontStyle = FontStyles.Italic,
                Foreground = TryFindResource("TextTertiary") as Brush ?? Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center
            });
            ContentArea.Children.Add(thinking);
        }

        // Error
        if (!string.IsNullOrEmpty(error))
        {
            ContentArea.Children.Add(new TextBlock
            {
                Text = error, TextWrapping = TextWrapping.Wrap, FontSize = 13,
                Foreground = Brushes.IndianRed, Margin = new Thickness(0, 0, 0, 8)
            });
        }

        // Model label
        ModelLabel.Text = Vm.AiModelName;
        ModelLabel.Visibility = Visibility.Visible;

        ContentScroll.ScrollToEnd();
    }

    private Border CreateConversationCard(string question, string? answer)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            BorderBrush = TryFindResource("Separator") as Brush ?? Brushes.DimGray,
            Background = TryFindResource("Depth2") as Brush,
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 0, 0, 12),
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = question, TextWrapping = TextWrapping.Wrap, FontSize = 13,
            Foreground = TryFindResource("TextTertiary") as Brush ?? Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 10)
        });
        if (!string.IsNullOrEmpty(answer))
        {
            var doc = MarkdownRenderer.Render(answer, Application.Current.Resources);
            var reader = new FlowDocumentScrollViewer
            {
                Document = doc,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                IsToolBarVisible = false, Focusable = false,
                Margin = new Thickness(-5, 0, -5, 0),
            };
            reader.SetValue(Control.BorderThicknessProperty, new Thickness(0));
            reader.SetValue(Control.PaddingProperty, new Thickness(0));
            stack.Children.Add(reader);
        }
        card.Child = stack;
        return card;
    }

    // ─── CALCULATOR ──────────────────────────────────────────
    private void BuildCalcContent()
    {
        ModelLabel.Visibility = Visibility.Collapsed;
        if (Vm is null) return;
        var result = Vm.ActionResultText;
        var query = Vm.ActionPreviewQuery;

        if (string.IsNullOrEmpty(result) || result == "Calculator")
        {
            BuildEmptyState("Calculator", "Type a math expression");
            return;
        }

        var card = new Border
        {
            CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1),
            BorderBrush = TryFindResource("Separator") as Brush ?? Brushes.DimGray,
            Background = TryFindResource("Depth2") as Brush,
            Padding = new Thickness(16, 14, 16, 14), Margin = new Thickness(0, 0, 0, 12),
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = query ?? "", FontSize = 13,
            Foreground = TryFindResource("TextTertiary") as Brush ?? Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 8)
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"= {result}", FontSize = 28, FontWeight = FontWeights.Medium,
            FontFamily = TryFindResource("Token.Font.Mono") as FontFamily ?? new FontFamily("Consolas"),
            Foreground = TryFindResource("TextPrimary") as Brush ?? Brushes.White
        });
        card.Child = stack;
        ContentArea.Children.Add(card);
    }

    // ─── TIMER ───────────────────────────────────────────────
    private void BuildTimerContent()
    {
        ModelLabel.Visibility = Visibility.Collapsed;
        if (Vm is null) return;

        var card = new Border
        {
            CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1),
            BorderBrush = TryFindResource("Separator") as Brush ?? Brushes.DimGray,
            Background = TryFindResource("Depth2") as Brush,
            Padding = new Thickness(16, 14, 16, 14), Margin = new Thickness(0, 0, 0, 12),
        };
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

        // Timer display
        var display = new TextBlock
        {
            FontSize = 32, FontWeight = FontWeights.Medium,
            FontFamily = TryFindResource("Token.Font.Mono") as FontFamily ?? new FontFamily("Consolas"),
            Foreground = TryFindResource("TextPrimary") as Brush ?? Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 4)
        };
        display.SetBinding(TextBlock.TextProperty, new Binding("Timer.TimerDisplay"));
        stack.Children.Add(display);

        // Status
        var status = new TextBlock
        {
            FontSize = 11,
            Foreground = TryFindResource("TextTertiary") as Brush ?? Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4)
        };
        status.SetBinding(TextBlock.TextProperty, new Binding("Timer.TimerStatus"));
        stack.Children.Add(status);

        // Progress bar
        var progress = new ProgressBar
        {
            Minimum = 0, Maximum = 100, Height = 4, Margin = new Thickness(0, 8, 0, 8)
        };
        progress.SetBinding(ProgressBar.ValueProperty, new Binding("Timer.TimerProgress"));
        stack.Children.Add(progress);

        // Buttons
        var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };
        var startBtn = new Button { Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(12, 6, 12, 6) };
        startBtn.SetBinding(Button.CommandProperty, new Binding("Timer.StartCommand"));
        var startContent = new StackPanel { Orientation = Orientation.Horizontal };
        startContent.Children.Add(new TextBlock { Text = "▶ Start", VerticalAlignment = VerticalAlignment.Center });
        startBtn.Content = startContent;
        btns.Children.Add(startBtn);

        var cancelBtn = new Button { Padding = new Thickness(12, 6, 12, 6) };
        cancelBtn.SetBinding(Button.CommandProperty, new Binding("Timer.CancelCommand"));
        var cancelContent = new StackPanel { Orientation = Orientation.Horizontal };
        cancelContent.Children.Add(new TextBlock { Text = "✕ Cancel", VerticalAlignment = VerticalAlignment.Center });
        cancelBtn.Content = cancelContent;
        btns.Children.Add(cancelBtn);

        stack.Children.Add(btns);
        card.Child = stack;
        ContentArea.Children.Add(card);
    }

    // ─── COLOR ───────────────────────────────────────────────
    private void BuildColorContent()
    {
        ModelLabel.Visibility = Visibility.Collapsed;
        if (Vm is null) return;
        var result = Vm.ActionResultText;
        if (string.IsNullOrEmpty(result) || result == "Color Picker")
        {
            BuildEmptyState("Color Picker", "Type a hex code like #ff0055");
            return;
        }

        var card = new Border
        {
            CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1),
            BorderBrush = TryFindResource("Separator") as Brush ?? Brushes.DimGray,
            Background = TryFindResource("Depth2") as Brush,
            Padding = new Thickness(16, 14, 16, 14), Margin = new Thickness(0, 0, 0, 12),
        };
        var stack = new StackPanel { Orientation = Orientation.Horizontal };

        // Color swatch
        try
        {
            var brush = new BrushConverter().ConvertFromString(result) as Brush;
            stack.Children.Add(new Border
            {
                Width = 42, Height = 42, CornerRadius = new CornerRadius(6),
                Background = brush, BorderBrush = TryFindResource("Separator") as Brush,
                BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 12, 0)
            });
        }
        catch { }

        var info = new StackPanel();
        info.Children.Add(new TextBlock
        {
            Text = result, FontSize = 16, FontWeight = FontWeights.Medium,
            FontFamily = TryFindResource("Token.Font.Mono") as FontFamily ?? new FontFamily("Consolas"),
            Foreground = TryFindResource("TextPrimary") as Brush ?? Brushes.White
        });
        info.Children.Add(new TextBlock
        {
            Text = "Copied to clipboard", FontSize = 11,
            Foreground = TryFindResource("TextTertiary") as Brush ?? Brushes.Gray,
            Margin = new Thickness(0, 2, 0, 0)
        });
        stack.Children.Add(info);
        card.Child = stack;
        ContentArea.Children.Add(card);
    }

    // ─── IP ──────────────────────────────────────────────────
    private void BuildIpContent()
    {
        ModelLabel.Visibility = Visibility.Collapsed;
        if (Vm is null) return;
        var text = Vm.ActionResultText;
        if (string.IsNullOrEmpty(text))
        {
            BuildEmptyState("IP Address", "Fetching your IP address...");
            return;
        }

        var card = new Border
        {
            CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1),
            BorderBrush = TryFindResource("Separator") as Brush ?? Brushes.DimGray,
            Background = TryFindResource("Depth2") as Brush,
            Padding = new Thickness(16, 14, 16, 14), Margin = new Thickness(0, 0, 0, 12),
        };
        card.Child = new TextBlock
        {
            Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 14,
            FontFamily = TryFindResource("Token.Font.Mono") as FontFamily ?? new FontFamily("Consolas"),
            Foreground = TryFindResource("TextPrimary") as Brush ?? Brushes.White
        };
        ContentArea.Children.Add(card);
    }

    // ─── CURRENCY ────────────────────────────────────────────
    private void BuildCurrencyContent()
    {
        ModelLabel.Visibility = Visibility.Collapsed;
        if (Vm is null) return;
        var result = Vm.ActionResultText;
        if (string.IsNullOrEmpty(result) || result == "Currency")
        {
            BuildEmptyState("Currency Converter", "Type '100 usd to eur'");
            return;
        }

        var card = new Border
        {
            CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1),
            BorderBrush = TryFindResource("Separator") as Brush ?? Brushes.DimGray,
            Background = TryFindResource("Depth2") as Brush,
            Padding = new Thickness(16, 14, 16, 14), Margin = new Thickness(0, 0, 0, 12),
        };
        card.Child = new TextBlock
        {
            Text = result, TextWrapping = TextWrapping.Wrap, FontSize = 20,
            FontWeight = FontWeights.Medium,
            Foreground = TryFindResource("TextPrimary") as Brush ?? Brushes.White
        };
        ContentArea.Children.Add(card);
    }

    // ─── PASSWORD ────────────────────────────────────────────
    private void BuildPasswordContent()
    {
        ModelLabel.Visibility = Visibility.Collapsed;
        if (Vm is null) return;
        var result = Vm.ActionResultText;
        if (string.IsNullOrEmpty(result) || result == "Password Gen")
        {
            BuildEmptyState("Password Generator", "Type 'pw 16' to generate");
            return;
        }

        var card = new Border
        {
            CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1),
            BorderBrush = TryFindResource("Separator") as Brush ?? Brushes.DimGray,
            Background = TryFindResource("Depth2") as Brush,
            Padding = new Thickness(16, 14, 16, 14), Margin = new Thickness(0, 0, 0, 12),
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = result, FontSize = 16,
            FontFamily = TryFindResource("Token.Font.Mono") as FontFamily ?? new FontFamily("Consolas"),
            Foreground = TryFindResource("TextPrimary") as Brush ?? Brushes.White,
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4)
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Password copied to clipboard", FontSize = 11,
            Foreground = TryFindResource("TextTertiary") as Brush ?? Brushes.Gray
        });
        card.Child = stack;
        ContentArea.Children.Add(card);
    }

    // ─── GENERIC (kill, note, screenshot, system, shell) ─────
    private void BuildGenericContent(string glyph, string title)
    {
        ModelLabel.Visibility = Visibility.Collapsed;
        if (Vm is null) return;
        var result = Vm.ActionResultText;
        var sub = Vm.ActionResultSubText;
        if (string.IsNullOrEmpty(result) || result == title)
        {
            BuildEmptyState(title, Vm.ActionPreviewSubtitle ?? "");
            return;
        }

        var card = new Border
        {
            CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1),
            BorderBrush = TryFindResource("Separator") as Brush ?? Brushes.DimGray,
            Background = TryFindResource("Depth2") as Brush,
            Padding = new Thickness(16, 14, 16, 14), Margin = new Thickness(0, 0, 0, 12),
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = result, FontSize = 14, FontWeight = FontWeights.Medium,
            Foreground = TryFindResource("TextPrimary") as Brush ?? Brushes.White
        });
        if (!string.IsNullOrEmpty(sub))
        {
            stack.Children.Add(new TextBlock
            {
                Text = sub, FontSize = 11,
                Foreground = TryFindResource("TextTertiary") as Brush ?? Brushes.Gray,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }
        card.Child = stack;
        ContentArea.Children.Add(card);
    }

    // ─── Empty state ─────────────────────────────────────────
    private void BuildEmptyState(string title = "Select an action", string subtitle = "")
    {
        ModelLabel.Visibility = Visibility.Collapsed;
        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 50, 0, 50)
        };
        stack.Children.Add(new TextBlock
        {
            Text = title, FontSize = 15, FontWeight = FontWeights.Medium,
            Foreground = TryFindResource("TextPrimary") as Brush ?? Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4)
        });
        if (!string.IsNullOrEmpty(subtitle))
        {
            stack.Children.Add(new TextBlock
            {
                Text = subtitle, FontSize = 12,
                Foreground = TryFindResource("TextTertiary") as Brush ?? Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }
        ContentArea.Children.Add(stack);
    }

    // ─── Helpers ─────────────────────────────────────────────
    private static string GetPanelLabel(string? panel) => panel switch
    {
        "ai" => "AI",
        "calc" => "Calculator",
        "timer" => "Timer",
        "color" => "Color",
        "ip" => "IP Address",
        "currency" => "Currency",
        "pw" => "Password",
        "kill" => "Kill Process",
        "note" => "Quick Note",
        "screenshot" => "Screenshot",
        "system" => "System",
        "shell" => "Shell",
        _ => "Add-on"
    };

    private ImageSource? GetPanelIconSource(string? panel)
    {
        var path = panel switch
        {
            "ai" => "/Assets/Icons/find.png",
            "calc" => "/Assets/Icons/calculator.png",
            "timer" => "/Assets/Icons/history.png",
            "color" => "/Assets/Icons/color.png",
            "ip" => "/Assets/Icons/url.png",
            "currency" => "/Assets/Icons/calculator.png",
            "pw" => "/Assets/Icons/lock.png",
            "kill" => "/Assets/Icons/shell.png",
            "note" => "/Assets/Icons/copy.png",
            "screenshot" => "/Assets/Icons/image.png",
            "system" => "/Assets/Icons/settings.png",
            "shell" => "/Assets/Icons/shell.png",
            _ => null
        };
        if (path is null) return null;
        try { return new System.Windows.Media.Imaging.BitmapImage(new Uri(path, UriKind.Relative)); }
        catch { return null; }
    }

    private void OnClearClick(object sender, RoutedEventArgs e) => Vm?.ClearAiChat();

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        if (Vm is null) return;
        var panel = Vm.ActiveFullPanel;
        var text = panel == "ai"
            ? Vm.AiChat.GetConversationText()
            : Vm.ActionResultText;
        if (!string.IsNullOrEmpty(text))
            System.Windows.Clipboard.SetText(text);
    }
}
