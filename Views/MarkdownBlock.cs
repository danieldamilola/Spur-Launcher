using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Spur.Views;

/// <summary>
/// A lightweight inline markdown renderer for WPF.
/// Supports: **bold**, *italic*, bullet lists (- or •), and plain text.
/// Renders into a RichTextBox (read-only) to allow text selection.
/// No external dependencies — parses line-by-line with simple rules.
/// </summary>
public sealed class MarkdownBlock : RichTextBox
{
    public static readonly DependencyProperty MarkdownProperty =
        DependencyProperty.Register(
            nameof(Markdown),
            typeof(string),
            typeof(MarkdownBlock),
            new FrameworkPropertyMetadata(string.Empty, OnMarkdownChanged));

    public string Markdown
    {
        get => (string)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public MarkdownBlock()
    {
        IsReadOnly        = true;
        Background        = Brushes.Transparent;
        BorderThickness   = new Thickness(0);
        IsDocumentEnabled = true;
        Cursor            = System.Windows.Input.Cursors.Arrow;

        // Suppress the default focus visual and caret
        FocusVisualStyle  = null;
        CaretBrush        = Brushes.Transparent;
    }

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownBlock mb)
            mb.Render((string)e.NewValue);
    }

    private void Render(string? markdown)
    {
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(0),
            LineHeight   = double.NaN,
            // Allow text to wrap to parent width (RichTextBox default clips at 6 inches).
            PageWidth    = double.MaxValue,
        };

        if (!string.IsNullOrEmpty(markdown))
        {
            var lines = markdown.Split('\n');

            // Accumulate consecutive bullet lines into one List element.
            List? currentList = null;

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');

                if (IsBullet(line, out var bulletText))
                {
                    if (currentList == null)
                    {
                        currentList = new List
                        {
                            MarkerStyle  = TextMarkerStyle.Disc,
                            Padding      = new Thickness(16, 0, 0, 0),
                            Margin       = new Thickness(0, 2, 0, 2),
                        };
                        doc.Blocks.Add(currentList);
                    }
                    var li = new ListItem(new Paragraph(BuildInlines(bulletText))
                        { Margin = new Thickness(0), Padding = new Thickness(0) });
                    currentList.ListItems.Add(li);
                }
                else
                {
                    currentList = null; // break the list run

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        // Empty line — small vertical gap
                        doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 2, 0, 0) });
                    }
                    else
                    {
                        var para = new Paragraph(BuildInlines(line))
                        {
                            Margin  = new Thickness(0, 0, 0, 2),
                            Padding = new Thickness(0),
                        };
                        doc.Blocks.Add(para);
                    }
                }
            }
        }

        Document = doc;
    }

    private static bool IsBullet(string line, out string text)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("• "))
        {
            text = trimmed[2..];
            return true;
        }
        text = string.Empty;
        return false;
    }

    /// <summary>
    /// Parses a single line for bold (**...**), italic (*...*), and plain text spans.
    /// </summary>
    private static Inline BuildInlines(string text)
    {
        // Wrap all parsed inlines in a Span so we can return a single Inline.
        var container = new Span();
        var remaining = text;

        // Pattern: **bold**, *italic* — only handle these two common cases.
        var pattern = @"(\*\*(.+?)\*\*|\*(.+?)\*)";
        var parts   = Regex.Split(remaining, pattern);

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (string.IsNullOrEmpty(part)) continue;

            if (part.StartsWith("**") && part.EndsWith("**") && part.Length > 4)
            {
                container.Inlines.Add(new Bold(new Run(part[2..^2])));
            }
            else if (part.StartsWith("*") && part.EndsWith("*") && part.Length > 2 && !part.StartsWith("**"))
            {
                container.Inlines.Add(new Italic(new Run(part[1..^1])));
            }
            else
            {
                container.Inlines.Add(new Run(part));
            }
        }

        return container;
    }
}
