using System.Windows.Documents;

namespace Spur.Helpers;

/// <summary>Converts a markdown string into a WPF <see cref="FlowDocument"/>.</summary>
public static class MarkdownRenderer
{
    // ── Resource key constants ──────────────────────────────────────────
    private const string KeyTextPrimary  = "TextPrimary";
    private const string KeyTextTertiary = "TextTertiary";
    private const string KeyDepth2       = "Depth2";
    private const string KeyAccentBrush  = "AccentBrush";
    private const string KeySeparator    = "Separator";
    private const string KeyFontPrimary  = "Token.Font.Primary";

    private static readonly FontFamily CodeFont = new("Cascadia Code, Consolas");

    // ────────────────────────────────────────────────────────────────────
    //  Public API
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Render a markdown string to a <see cref="FlowDocument"/> themed
    /// with brushes / fonts from <paramref name="resources"/>.
    /// </summary>
    public static FlowDocument Render(string markdown, ResourceDictionary resources)
    {
        var textBrush   = GetBrush(resources, KeyTextPrimary,  Brushes.White);
        var mutedBrush  = GetBrush(resources, KeyTextTertiary, Brushes.Gray);
        var codeBg      = GetBrush(resources, KeyDepth2,       new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)));
        var accentBrush = GetBrush(resources, KeyAccentBrush,  Brushes.CornflowerBlue);
        var borderBrush = GetBrush(resources, KeySeparator,    Brushes.DimGray);
        var fontFamily  = resources[KeyFontPrimary] as FontFamily ?? new FontFamily("Segoe UI");

        var doc = new FlowDocument
        {
            PagePadding = new Thickness(0),
            Foreground  = textBrush,
            FontFamily  = fontFamily,
            FontSize    = 13.5,
            LineHeight  = 21,
            Background  = Brushes.Transparent,
        };

        if (string.IsNullOrEmpty(markdown))
            return doc;

        var lines = markdown.Replace("\r\n", "\n").Split('\n');

        var ctx = new ParseContext
        {
            TextBrush   = textBrush,
            MutedBrush  = mutedBrush,
            CodeBg      = codeBg,
            AccentBrush = accentBrush,
            BorderBrush = borderBrush,
        };

        bool inCodeBlock = false;
        string? codeLanguage = null;
        var codeLines = new List<string>();

        List<string>? bulletItems = null;
        List<string>? numberedItems = null;
        int numberedStart = 1;

        var paragraphLines = new List<string>();
        var tableLines = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            // ── Fenced code blocks ─────────────────────────────────
            if (line.TrimStart().StartsWith("```"))
            {
                if (!inCodeBlock)
                {
                    // Flush any pending content before starting code block.
                    FlushParagraph(doc, paragraphLines, ctx);
                    FlushList(doc, bulletItems, numberedItems, numberedStart, ctx);
                    bulletItems = null;
                    numberedItems = null;

                    inCodeBlock = true;
                    codeLanguage = line.TrimStart().Length > 3
                        ? line.TrimStart()[3..].Trim()
                        : null;
                    codeLines.Clear();
                }
                else
                {
                    // End code block — emit it.
                    EmitCodeBlock(doc, codeLines, ctx);
                    inCodeBlock = false;
                    codeLanguage = null;
                    codeLines.Clear();
                }
                continue;
            }

            if (inCodeBlock)
            {
                codeLines.Add(line);
                continue;
            }

            // ── Markdown tables ────────────────────────────────────
            if (line.TrimStart().StartsWith('|'))
            {
                if (tableLines.Count == 0)
                {
                    FlushParagraph(doc, paragraphLines, ctx);
                    FlushList(doc, bulletItems, numberedItems, numberedStart, ctx);
                    bulletItems = null;
                    numberedItems = null;
                }
                tableLines.Add(line);
                continue;
            }

            // If we were accumulating table lines and hit a non-table line, flush the table
            if (tableLines.Count > 0)
            {
                EmitTable(doc, tableLines, ctx);
                tableLines.Clear();
            }

            // ── Headers ────────────────────────────────────────────
            if (line.StartsWith('#'))
            {
                FlushParagraph(doc, paragraphLines, ctx);
                FlushList(doc, bulletItems, numberedItems, numberedStart, ctx);
                bulletItems = null;
                numberedItems = null;

                int level = 0;
                while (level < line.Length && line[level] == '#') level++;
                if (level <= 6 && level < line.Length && line[level] == ' ')
                {
                    EmitHeader(doc, line[(level + 1)..], level, ctx);
                    continue;
                }
                // Not a valid header — fall through to paragraph.
            }

            // ── Bullet lists ───────────────────────────────────────
            if ((line.StartsWith("- ") || line.StartsWith("* ")) && line.Length > 2)
            {
                FlushParagraph(doc, paragraphLines, ctx);
                if (numberedItems is not null)
                {
                    FlushList(doc, null, numberedItems, numberedStart, ctx);
                    numberedItems = null;
                }
                bulletItems ??= [];
                bulletItems.Add(line[2..]);
                continue;
            }

            // ── Numbered lists ─────────────────────────────────────
            if (TryParseNumberedItem(line, out int num, out string? itemText))
            {
                FlushParagraph(doc, paragraphLines, ctx);
                if (bulletItems is not null)
                {
                    FlushList(doc, bulletItems, null, 1, ctx);
                    bulletItems = null;
                }
                if (numberedItems is null)
                {
                    numberedItems = [];
                    numberedStart = num;
                }
                numberedItems.Add(itemText!);
                continue;
            }

            // ── Blank line → flush paragraph ───────────────────────
            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph(doc, paragraphLines, ctx);
                FlushList(doc, bulletItems, numberedItems, numberedStart, ctx);
                bulletItems = null;
                numberedItems = null;
                continue;
            }

            // ── Regular text line ──────────────────────────────────
            // If we were building a list, a non-list line ends the list.
            if (bulletItems is not null || numberedItems is not null)
            {
                FlushList(doc, bulletItems, numberedItems, numberedStart, ctx);
                bulletItems = null;
                numberedItems = null;
            }
            paragraphLines.Add(line);
        }

        // Flush remaining content.
        if (inCodeBlock && codeLines.Count > 0)
            EmitCodeBlock(doc, codeLines, ctx);

        if (tableLines.Count > 0)
        {
            EmitTable(doc, tableLines, ctx);
            tableLines.Clear();
        }

        FlushParagraph(doc, paragraphLines, ctx);
        FlushList(doc, bulletItems, numberedItems, numberedStart, ctx);

        return doc;
    }

    // ────────────────────────────────────────────────────────────────────
    //  Block builders
    // ────────────────────────────────────────────────────────────────────

    private static void FlushParagraph(FlowDocument doc, List<string> lines, ParseContext ctx)
    {
        if (lines.Count == 0) return;

        var para = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0) para.Inlines.Add(new LineBreak());
            AddInlines(para.Inlines, lines[i], ctx);
        }
        doc.Blocks.Add(para);
        lines.Clear();
    }

    private static void FlushList(
        FlowDocument doc,
        List<string>? bulletItems,
        List<string>? numberedItems,
        int numberedStart,
        ParseContext ctx)
    {
        if (bulletItems is { Count: > 0 })
        {
            var list = new List
            {
                MarkerStyle  = TextMarkerStyle.Disc,
                Margin       = new Thickness(16, 0, 0, 8),
                Padding      = new Thickness(0),
            };
            foreach (var text in bulletItems)
            {
                var li = new ListItem { Margin = new Thickness(0, 0, 0, 2) };
                var p = new Paragraph { Margin = new Thickness(0) };
                AddInlines(p.Inlines, text, ctx);
                li.Blocks.Add(p);
                list.ListItems.Add(li);
            }
            doc.Blocks.Add(list);
            bulletItems.Clear();
        }

        if (numberedItems is { Count: > 0 })
        {
            var list = new List
            {
                MarkerStyle = TextMarkerStyle.Decimal,
                StartIndex  = numberedStart,
                Margin      = new Thickness(16, 0, 0, 8),
                Padding     = new Thickness(0),
            };
            foreach (var text in numberedItems)
            {
                var li = new ListItem { Margin = new Thickness(0, 0, 0, 2) };
                var p = new Paragraph { Margin = new Thickness(0) };
                AddInlines(p.Inlines, text, ctx);
                li.Blocks.Add(p);
                list.ListItems.Add(li);
            }
            doc.Blocks.Add(list);
            numberedItems.Clear();
        }
    }

    private static void EmitHeader(FlowDocument doc, string text, int level, ParseContext ctx)
    {
        double fontSize = level switch
        {
            1 => 22,
            2 => 18,
            3 => 15.5,
            _ => 14,
        };

        var para = new Paragraph
        {
            FontSize   = fontSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = ctx.AccentBrush,
            Margin     = new Thickness(0, level <= 2 ? 12 : 8, 0, 6),
        };

        AddInlines(para.Inlines, text, ctx, overrideForeground: ctx.AccentBrush);
        doc.Blocks.Add(para);
    }

    private static void EmitCodeBlock(FlowDocument doc, List<string> lines, ParseContext ctx)
    {
        var codeText = string.Join("\n", lines);

        // Code content
        var textBlock = new TextBlock
        {
            Text = codeText,
            FontFamily = CodeFont,
            FontSize = 12.5,
            Foreground = ctx.TextBrush,
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(12, 10, 40, 10),
            LineHeight = 18,
        };

        // Copy button
        var copyButton = new System.Windows.Controls.Button
        {
            Content = "\uE8C8",  // Segoe MDL2 Copy glyph
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 6, 6, 0),
            Padding = new Thickness(5, 3, 5, 3),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "Copy code",
            Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
            Foreground = ctx.MutedBrush,
            BorderThickness = new Thickness(0),
        };

        // Style the button with a simple template
        var buttonTemplate = new ControlTemplate(typeof(System.Windows.Controls.Button));
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
        var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        borderFactory.AppendChild(contentPresenterFactory);
        buttonTemplate.VisualTree = borderFactory;
        copyButton.Template = buttonTemplate;

        copyButton.Click += (sender, e) =>
        {
            try
            {
                System.Windows.Clipboard.SetText(codeText);
                var btn = (System.Windows.Controls.Button)sender!;
                var originalContent = btn.Content;
                var originalFontFamily = btn.FontFamily;
                btn.Content = "Copied!";
                btn.FontFamily = new FontFamily("Segoe UI");
                btn.FontSize = 10;

                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
                timer.Tick += (_, _) =>
                {
                    btn.Content = originalContent;
                    btn.FontFamily = originalFontFamily;
                    btn.FontSize = 12;
                    timer.Stop();
                };
                timer.Start();
            }
            catch { /* clipboard may be locked */ }
        };

        // Grid container
        var grid = new Grid
        {
            Background = ctx.CodeBg,
            Margin = new Thickness(0, 4, 0, 8),
        };

        // Add a subtle border
        var border = new Border
        {
            BorderBrush = ctx.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = grid,
        };

        grid.Children.Add(textBlock);
        grid.Children.Add(copyButton);

        doc.Blocks.Add(new BlockUIContainer(border));
    }

    // ────────────────────────────────────────────────────────────────────
    //  Inline parser — simple state machine for bold, italic, code
    // ────────────────────────────────────────────────────────────────────

    private static void AddInlines(
        InlineCollection inlines,
        string text,
        ParseContext ctx,
        Brush? overrideForeground = null)
    {
        var fg = overrideForeground ?? ctx.TextBrush;
        int i = 0;
        int len = text.Length;
        var buffer = new StringBuilder();

        void FlushBuffer()
        {
            if (buffer.Length == 0) return;
            inlines.Add(new Run(buffer.ToString()) { Foreground = fg });
            buffer.Clear();
        }

        while (i < len)
        {
            char c = text[i];

            // ── Inline code: `…` ───────────────────────────────────
            if (c == '`')
            {
                int end = text.IndexOf('`', i + 1);
                if (end > i)
                {
                    FlushBuffer();
                    var run = new Run(text[(i + 1)..end])
                    {
                        FontFamily = CodeFont,
                        FontSize   = 12,
                        Foreground = ctx.MutedBrush,
                        Background = ctx.CodeBg,
                    };
                    inlines.Add(run);
                    i = end + 1;
                    continue;
                }
            }

            // ── Bold: **…** ────────────────────────────────────────
            if (c == '*' && i + 1 < len && text[i + 1] == '*')
            {
                int close = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                if (close > i)
                {
                    FlushBuffer();
                    var bold = new Bold();
                    bold.Inlines.Add(new Run(text[(i + 2)..close]) { Foreground = fg });
                    inlines.Add(bold);
                    i = close + 2;
                    continue;
                }
            }

            // ── Italic: *…* (single asterisk, not followed by another *) ──
            if (c == '*' && (i + 1 >= len || text[i + 1] != '*'))
            {
                // Find closing single * that is not part of **
                int search = i + 1;
                int close = -1;
                while (search < len)
                {
                    int idx = text.IndexOf('*', search);
                    if (idx < 0) break;
                    // Make sure it's a single * (not **)
                    if (idx + 1 < len && text[idx + 1] == '*') { search = idx + 2; continue; }
                    close = idx;
                    break;
                }

                if (close > i)
                {
                    FlushBuffer();
                    var italic = new Italic();
                    italic.Inlines.Add(new Run(text[(i + 1)..close]) { Foreground = fg });
                    inlines.Add(italic);
                    i = close + 1;
                    continue;
                }
            }

            // ── Links: [text](url) ─────────────────────────────────
            if (c == '[' && i + 1 < len)
            {
                int closeBracket = text.IndexOf(']', i + 1);
                if (closeBracket > i && closeBracket + 1 < len && text[closeBracket + 1] == '(')
                {
                    int closeParen = text.IndexOf(')', closeBracket + 2);
                    if (closeParen > closeBracket + 2)
                    {
                        FlushBuffer();
                        var linkText = text[(i + 1)..closeBracket];
                        var linkUrl = text[(closeBracket + 2)..closeParen];

                        var hyperlink = new Hyperlink(new Run(linkText))
                        {
                            Foreground = ctx.AccentBrush,
                            TextDecorations = null,
                            Cursor = System.Windows.Input.Cursors.Hand,
                        };
                        var capturedUrl = linkUrl;
                        hyperlink.Click += (_, _) =>
                        {
                            try
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(capturedUrl)
                                {
                                    UseShellExecute = true
                                });
                            }
                            catch { /* invalid URL */ }
                        };
                        inlines.Add(hyperlink);
                        i = closeParen + 1;
                        continue;
                    }
                }
            }

            buffer.Append(c);
            i++;
        }

        FlushBuffer();
    }

    // ────────────────────────────────────────────────────────────────────
    //  Table rendering
    // ────────────────────────────────────────────────────────────────────

    /// <summary>Parses pipe-delimited table lines and emits a WPF Grid.</summary>
    private static void EmitTable(FlowDocument doc, List<string> tableLines, ParseContext ctx)
    {
        if (tableLines.Count < 2) return; // Need at least header + separator

        var rows = new List<string[]>();
        int separatorIndex = -1;

        for (int r = 0; r < tableLines.Count; r++)
        {
            var line = tableLines[r].Trim();
            if (line.StartsWith('|')) line = line[1..];
            if (line.EndsWith('|')) line = line[..^1];

            var cells = line.Split('|').Select(c => c.Trim()).ToArray();

            // Detect separator row (all cells are dashes/colons like "---", ":---:", etc.)
            if (separatorIndex < 0 && cells.All(c => Regex.IsMatch(c, @"^:?-{1,}:?$")))
            {
                separatorIndex = r;
                continue;
            }

            rows.Add(cells);
        }

        if (rows.Count == 0) return;

        int colCount = rows.Max(r => r.Length);
        int headerRows = separatorIndex > 0 ? separatorIndex : (separatorIndex == 1 ? 1 : 0);

        var grid = new Grid
        {
            Margin = new Thickness(0, 4, 0, 8),
        };

        // Define columns
        for (int c = 0; c < colCount; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Define rows
        for (int r = 0; r < rows.Count; r++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Populate cells
        for (int r = 0; r < rows.Count; r++)
        {
            bool isHeader = r < headerRows;
            var rowCells = rows[r];

            for (int c = 0; c < colCount; c++)
            {
                string cellText = c < rowCells.Length ? rowCells[c] : "";

                var tb = new TextBlock
                {
                    Text = cellText,
                    Foreground = ctx.TextBrush,
                    FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
                    Padding = new Thickness(8, 5, 8, 5),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13,
                };

                var cellBorder = new Border
                {
                    BorderBrush = ctx.BorderBrush,
                    BorderThickness = new Thickness(0, 0, c < colCount - 1 ? 1 : 0, r < rows.Count - 1 ? 1 : 0),
                    Background = isHeader ? ctx.CodeBg : Brushes.Transparent,
                    Child = tb,
                };

                Grid.SetRow(cellBorder, r);
                Grid.SetColumn(cellBorder, c);
                grid.Children.Add(cellBorder);
            }
        }

        // Wrap in a border
        var tableBorder = new Border
        {
            BorderBrush = ctx.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = grid,
        };

        doc.Blocks.Add(new BlockUIContainer(tableBorder));
    }

    // ────────────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────────────

    private static bool TryParseNumberedItem(string line, out int number, out string? text)
    {
        number = 0;
        text = null;

        int dotIdx = line.IndexOf('.');
        if (dotIdx <= 0 || dotIdx + 1 >= line.Length || line[dotIdx + 1] != ' ')
            return false;

        if (!int.TryParse(line[..dotIdx], out number))
            return false;

        text = line[(dotIdx + 2)..];
        return true;
    }

    private static Brush GetBrush(ResourceDictionary res, string key, Brush fallback)
    {
        if (res.Contains(key) && res[key] is Brush b) return b;
        return fallback;
    }

    // ── Internal state bag passed through parse methods ─────────────
    private sealed class ParseContext
    {
        public required Brush TextBrush   { get; init; }
        public required Brush MutedBrush  { get; init; }
        public required Brush CodeBg      { get; init; }
        public required Brush AccentBrush { get; init; }
        public required Brush BorderBrush { get; init; }
    }
}
