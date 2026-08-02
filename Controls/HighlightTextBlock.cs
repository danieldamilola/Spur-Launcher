using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Spur.Controls;

public class HighlightTextBlock : TextBlock
{
    public HighlightTextBlock()
    {
        Loaded += (_, _) => BuildInlines();
    }

    public static readonly DependencyProperty SourceTextProperty =
        DependencyProperty.Register(
            nameof(SourceText),
            typeof(string),
            typeof(HighlightTextBlock),
            new PropertyMetadata(string.Empty, OnPropertyChanged));

    public string SourceText
    {
        get => (string)GetValue(SourceTextProperty);
        set => SetValue(SourceTextProperty, value);
    }

    public static readonly DependencyProperty HighlightIndicesProperty =
        DependencyProperty.Register(
            nameof(HighlightIndices),
            typeof(IReadOnlySet<int>),
            typeof(HighlightTextBlock),
            new PropertyMetadata(null, OnPropertyChanged));

    public IReadOnlySet<int>? HighlightIndices
    {
        get => (IReadOnlySet<int>?)GetValue(HighlightIndicesProperty);
        set => SetValue(HighlightIndicesProperty, value);
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock tb)
            tb.BuildInlines();
    }

    private void BuildInlines()
    {
        Inlines.Clear();
        var text = SourceText;
        if (string.IsNullOrEmpty(text))
            return;

        var hl = HighlightIndices;
        if (hl is null || hl.Count == 0)
        {
            Inlines.Add(new Run(text) { Foreground = Foreground });
            return;
        }

        var defaultBrush = Foreground;
        var highlightBrush = TryFindResource("Accent") as Brush ?? Brushes.DodgerBlue;

        int i = 0;
        while (i < text.Length)
        {
            if (hl.Contains(i))
            {
                int start = i;
                while (i < text.Length && hl.Contains(i))
                    i++;
                Inlines.Add(new Run(text[start..i])
                {
                    Foreground = highlightBrush,
                    FontWeight = FontWeights.SemiBold,
                });
            }
            else
            {
                int start = i;
                while (i < text.Length && !hl.Contains(i))
                    i++;
                Inlines.Add(new Run(text[start..i]) { Foreground = defaultBrush });
            }
        }
    }
}
