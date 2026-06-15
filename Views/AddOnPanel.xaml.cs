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
    public async void HandleUserInput(string query)
    {
        if (Vm is null || string.IsNullOrWhiteSpace(query)) return;

        var panel = Vm.ActiveFullPanel;
        if (panel == "ai")
        {
            try
            {
                await Vm.SendAiQuery(query);
            }
            catch (Exception ex)
            {
                Vm.AiChat.AiError = $"Error: {ex.Message}";
                Vm.AiChat.AiLoading = false;
            }
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
                Background = TryFindResource("Accent") as Brush ?? Brushes.CornflowerBlue,
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

            // Forward mouse wheel to the parent ScrollViewer so AI content is scrollable
            reader.PreviewMouseWheel += (s, ev) =>
            {
                ev.Handled = true;
                ContentScroll.RaiseEvent(new System.Windows.Input.MouseWheelEventArgs(
                    ev.MouseDevice, ev.Timestamp, ev.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = s
                });
            };

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

        var stack = new StackPanel();
        var queryLabel = CreateLabel(query ?? "", muted: true);
        queryLabel.Margin = new Thickness(0, 0, 0, 8);
        stack.Children.Add(queryLabel);
        stack.Children.Add(CreateLabel($"= {result}", fontSize: 28, mono: true, weight: FontWeights.Medium));
        ContentArea.Children.Add(CreateCard(stack));
    }

    // ─── TIMER ───────────────────────────────────────────────
    private void BuildTimerContent()
    {
        ModelLabel.Visibility = Visibility.Collapsed;
        if (Vm is null) return;

        var monoFont = TryFindResource("Token.Font.Mono") as FontFamily ?? new FontFamily("Consolas");
        var primaryBrush = TryFindResource("TextPrimary") as Brush ?? Brushes.White;
        var mutedBrush = TryFindResource("TextTertiary") as Brush ?? Brushes.Gray;
        var accentBrush = TryFindResource("Accent") as Brush ?? Brushes.CornflowerBlue;
        var separatorBrush = TryFindResource("Separator") as Brush ?? Brushes.DimGray;
        var depth2Brush = TryFindResource("Depth2") as Brush;

        // ── Active timers list ─────────────────────────────
        var activeTimers = Vm.Timer.ActiveTimers;
        if (activeTimers.Count > 0)
        {
            foreach (var timer in activeTimers)
            {
                var timerCard = new Border
                {
                    CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1),
                    BorderBrush = separatorBrush, Background = depth2Brush,
                    Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 8),
                    Tag = timer
                };

                var timerStack = new StackPanel();

                // Top row: label + buttons
                var topRow = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };

                var label = new TextBlock
                {
                    Text = timer.Label, FontSize = 12, FontWeight = FontWeights.Medium,
                    Foreground = primaryBrush, VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(label, Dock.Left);
                topRow.Children.Add(label);

                // Button panel (right-aligned)
                var btnPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                // Pause/Resume button
                var pauseResumeBtn = new Button
                {
                    Padding = new Thickness(6, 3, 6, 3), Margin = new Thickness(0, 0, 4, 0),
                    Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand, Tag = timer
                };
                var pauseResumeIcon = new TextBlock
                {
                    FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 12,
                    Foreground = primaryBrush, VerticalAlignment = VerticalAlignment.Center,
                    Text = timer.IsPaused ? "\uE768" : "\uE769"
                };
                pauseResumeBtn.Content = pauseResumeIcon;
                pauseResumeBtn.ToolTip = timer.IsPaused ? "Resume" : "Pause";
                pauseResumeBtn.Click += (s, e) =>
                {
                    if (s is Button btn && btn.Tag is TimerInstance inst)
                    {
                        if (inst.IsPaused) Vm.Timer.Resume(inst);
                        else Vm.Timer.Pause(inst);
                        _lastPanel = null; // Force rebuild to update icons
                        Refresh();
                    }
                };
                btnPanel.Children.Add(pauseResumeBtn);

                // Cancel (X) button
                var cancelBtn = new Button
                {
                    Padding = new Thickness(6, 3, 6, 3),
                    Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand, Tag = timer
                };
                cancelBtn.Content = new TextBlock
                {
                    FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 12,
                    Foreground = Brushes.IndianRed, VerticalAlignment = VerticalAlignment.Center,
                    Text = "\uE711"
                };
                cancelBtn.ToolTip = "Cancel timer";
                cancelBtn.Click += (s, e) =>
                {
                    if (s is Button btn && btn.Tag is TimerInstance inst)
                    {
                        Vm.Timer.Cancel(inst);
                        _lastPanel = null; // Force rebuild
                        Refresh();
                    }
                };
                btnPanel.Children.Add(cancelBtn);

                DockPanel.SetDock(btnPanel, Dock.Right);
                topRow.Children.Add(btnPanel);
                timerStack.Children.Add(topRow);

                // Countdown display
                var display = new TextBlock
                {
                    Text = timer.DisplayTime, FontSize = 28, FontWeight = FontWeights.Medium,
                    FontFamily = monoFont, Foreground = primaryBrush,
                    HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 2),
                    Tag = timer
                };
                // Live update via PropertyChanged
                timer.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(TimerInstance.DisplayTime) && s is TimerInstance inst)
                        Dispatcher.InvokeAsync(() => display.Text = inst.DisplayTime);
                };
                timerStack.Children.Add(display);

                // Status text
                var statusText = timer.IsPaused ? "Paused" : timer.IsRunning ? "Counting down" : "Finished";
                var statusLabel = new TextBlock
                {
                    Text = statusText, FontSize = 10, Foreground = mutedBrush,
                    HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4)
                };
                timer.PropertyChanged += (s, e) =>
                {
                    if (s is TimerInstance inst && (e.PropertyName == nameof(TimerInstance.IsPaused) || e.PropertyName == nameof(TimerInstance.IsRunning)))
                        Dispatcher.InvokeAsync(() => statusLabel.Text = inst.IsPaused ? "Paused" : inst.IsRunning ? "Counting down" : "Finished");
                };
                timerStack.Children.Add(statusLabel);

                // Progress bar
                var progress = new ProgressBar
                {
                    Minimum = 0, Maximum = 100, Value = timer.Progress,
                    Height = 4, Margin = new Thickness(0, 4, 0, 0)
                };
                timer.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(TimerInstance.Progress) && s is TimerInstance inst)
                        Dispatcher.InvokeAsync(() => progress.Value = inst.Progress);
                };
                timerStack.Children.Add(progress);

                timerCard.Child = timerStack;
                ContentArea.Children.Add(timerCard);
            }

            // Status row: "X/3 timers active"
            ContentArea.Children.Add(new TextBlock
            {
                Text = $"{activeTimers.Count}/3 timers active",
                FontSize = 11, Foreground = mutedBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 8)
            });
        }

        // ── New timer card (always shown) ──────────────────
        var card = new Border
        {
            CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1),
            BorderBrush = separatorBrush, Background = depth2Brush,
            Padding = new Thickness(16, 14, 16, 14), Margin = new Thickness(0, 0, 0, 12),
        };
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

        // Timer display (preview for new timer)
        var previewDisplay = new TextBlock
        {
            FontSize = 32, FontWeight = FontWeights.Medium,
            FontFamily = monoFont, Foreground = primaryBrush,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 4)
        };
        previewDisplay.SetBinding(TextBlock.TextProperty, new Binding("Timer.TimerDisplay"));
        stack.Children.Add(previewDisplay);

        // Status
        var status = new TextBlock
        {
            FontSize = 11, Foreground = mutedBrush,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4)
        };
        status.SetBinding(TextBlock.TextProperty, new Binding("Timer.TimerStatus"));
        stack.Children.Add(status);

        // Progress bar (preview)
        var previewProgress = new ProgressBar
        {
            Minimum = 0, Maximum = 100, Height = 4, Margin = new Thickness(0, 8, 0, 8)
        };
        previewProgress.SetBinding(ProgressBar.ValueProperty, new Binding("Timer.TimerProgress"));
        stack.Children.Add(previewProgress);

        // Buttons
        var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };
        var startBtn = new Button { Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(12, 6, 12, 6) };
        startBtn.SetBinding(Button.CommandProperty, new Binding("Timer.StartCommand"));
        var startContent = new StackPanel { Orientation = Orientation.Horizontal };
        startContent.Children.Add(new TextBlock { Text = "▶ Start", VerticalAlignment = VerticalAlignment.Center });
        startBtn.Content = startContent;
        btns.Children.Add(startBtn);

        var cancelAllBtn = new Button { Padding = new Thickness(12, 6, 12, 6) };
        cancelAllBtn.SetBinding(Button.CommandProperty, new Binding("Timer.CancelCommand"));
        var cancelContent = new StackPanel { Orientation = Orientation.Horizontal };
        cancelContent.Children.Add(new TextBlock { Text = "✕ Cancel", VerticalAlignment = VerticalAlignment.Center });
        cancelAllBtn.Content = cancelContent;
        btns.Children.Add(cancelAllBtn);

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
        catch { /* Intentional: invalid color string — skip swatch rendering */ }

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

        ContentArea.Children.Add(CreateCard(CreateLabel(text, fontSize: 14, mono: true)));
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

        ContentArea.Children.Add(CreateCard(CreateLabel(result, fontSize: 20, weight: FontWeights.Medium)));
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

        var stack = new StackPanel();
        var pw = CreateLabel(result, fontSize: 16, mono: true);
        pw.Margin = new Thickness(0, 0, 0, 4);
        stack.Children.Add(pw);
        stack.Children.Add(CreateLabel("Password copied to clipboard", fontSize: 11, muted: true));
        ContentArea.Children.Add(CreateCard(stack));
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

        var stack = new StackPanel();
        stack.Children.Add(CreateLabel(result, fontSize: 14, weight: FontWeights.Medium));
        if (!string.IsNullOrEmpty(sub))
        {
            var subLabel = CreateLabel(sub, fontSize: 11, muted: true);
            subLabel.Margin = new Thickness(0, 2, 0, 0);
            stack.Children.Add(subLabel);
        }
        ContentArea.Children.Add(CreateCard(stack));
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

    // ─── UI Factory Helpers ─────────────────────────────────────
    /// <summary>Creates a themed content card with standard padding, border, and corner radius.</summary>
    private Border CreateCard(UIElement content)
    {
        return new Border
        {
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            BorderBrush = TryFindResource("Separator") as Brush ?? Brushes.DimGray,
            Background = TryFindResource("Depth2") as Brush,
            Padding = new Thickness(16, 14, 16, 14),
            Margin = new Thickness(0, 0, 0, 12),
            Child = content
        };
    }

    /// <summary>Creates a themed TextBlock with common defaults.</summary>
    private TextBlock CreateLabel(string text, double fontSize = 13, bool muted = false, bool mono = false, FontWeight? weight = null)
    {
        var block = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontSize = fontSize,
            Foreground = muted
                ? TryFindResource("TextTertiary") as Brush ?? Brushes.Gray
                : TryFindResource("TextPrimary") as Brush ?? Brushes.White,
        };
        if (weight.HasValue) block.FontWeight = weight.Value;
        if (mono) block.FontFamily = TryFindResource("Token.Font.Mono") as FontFamily ?? new FontFamily("Consolas");
        return block;
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
        catch { return null; /* Intentional: icon resource may be missing */ }
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
