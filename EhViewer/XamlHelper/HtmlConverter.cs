using HtmlAgilityPack;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace EhViewer
{
    public static class HtmlRichTextBlock
    {
        public static string GetHtml(DependencyObject obj) => (string)obj.GetValue(HtmlProperty);
        public static void SetHtml(DependencyObject obj, string value) => obj.SetValue(HtmlProperty, value);

        public static readonly DependencyProperty HtmlProperty =
            DependencyProperty.RegisterAttached("Html", typeof(string), typeof(HtmlRichTextBlock),
                new PropertyMetadata(null, OnHtmlChanged));

        private static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichTextBlock rtb)
            {
                rtb.Blocks.Clear();
                if (e.NewValue is string html && !string.IsNullOrWhiteSpace(html))
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    ParseNodes(doc.DocumentNode.ChildNodes, rtb.Blocks);
                }
            }
        }

        private static void ParseNodes(HtmlNodeCollection nodes, BlockCollection blocks)
        {
            foreach (var node in nodes)
            {
                if (node.NodeType == HtmlNodeType.Element)
                {
                    switch (node.Name.ToLower())
                    {
                        case "p":
                        case "div":
                            blocks.Add(CreateParagraph(node));
                            break;
                        case "h1":
                        case "h2":
                        case "h3":
                        case "h4":
                        case "h5":
                        case "h6":
                            blocks.Add(CreateHeading(node));
                            break;
                        case "ul":
                        case "ol":
                            blocks.Add(CreateList(node));
                            break;
                        case "hr":
                            blocks.Add(CreateHorizontalRule());
                            break;
                        default:
                            var paragraph = CreateParagraph(node);
                            if (paragraph.Inlines.Count > 0)
                                blocks.Add(paragraph);
                            break;
                    }
                }
                else if (node.NodeType == HtmlNodeType.Text)
                {
                    var paragraph = new Paragraph();
                    paragraph.Inlines.Add(new Run { Text = node.InnerText.HtmlDecode() });
                    blocks.Add(paragraph);
                }
            }
        }

        private static Paragraph CreateParagraph(HtmlNode node)
        {
            var paragraph = new Paragraph();
            ParseInlines(node, paragraph.Inlines);
            ApplyParagraphStyle(node, paragraph);
            return paragraph;
        }

        private static Paragraph CreateHeading(HtmlNode node)
        {
            var heading = new Paragraph
            {
                FontSize = GetHeadingFontSize(node.Name),
                FontWeight = FontWeights.Bold
            };
            ParseInlines(node, heading.Inlines);
            return heading;
        }

        private static double GetHeadingFontSize(string tagName) => tagName.ToLower() switch
        {
            "h1" => 24,
            "h2" => 22,
            "h3" => 20,
            "h4" => 18,
            "h5" => 16,
            "h6" => 14,
            _ => 14
        };

        private static Paragraph CreateList(HtmlNode node)
        {
            var listParagraph = new Paragraph { TextIndent = 24 };
            var index = 1;

            foreach (var li in node.SelectNodes(".//li"))
            {
                var listItem = new Span();
                listItem.Inlines.Add(new Run
                {
                    Text = node.Name == "ol" ? $"{index++}. " : "• "
                });
                ParseInlines(li, listItem.Inlines);
                listParagraph.Inlines.Add(listItem);
                listParagraph.Inlines.Add(new LineBreak());
            }

            return listParagraph;
        }

        private static Paragraph CreateHorizontalRule()
        {
            return new Paragraph
            {
                Inlines =
            {
                new Run
                {
                    Text = "\n",
                    FontSize = 8
                }
            },
                //BorderThickness = new Thickness(0, 1, 0, 0),
                //BorderBrush = new SolidColorBrush(Colors.Gray),
                
                Margin = new Thickness(0, 8, 0, 8)
            };
        }

        private static void ParseInlines(HtmlNode node, InlineCollection inlines)
        {
            foreach (var childNode in node.ChildNodes)
            {
                switch (childNode.NodeType)
                {
                    case HtmlNodeType.Text:
                        inlines.Add(CreateTextRun(childNode.InnerText.HtmlDecode()));
                        break;

                    case HtmlNodeType.Element:
                        var span = new Span();
                        ApplyInlineStyles(childNode, span);

                        switch (childNode.Name.ToLower())
                        {
                            case "br":
                                inlines.Add(new LineBreak());
                                break;
                            case "a":
                                var hyperlink = new Hyperlink();
                                ApplyHyperlinkStyle(childNode, hyperlink);
                                ParseInlines(childNode, hyperlink.Inlines);
                                inlines.Add(hyperlink);
                                break;
                            case "img":
                                inlines.Add(CreateImage(childNode));
                                break;
                            default:
                                ParseInlines(childNode, span.Inlines);
                                inlines.Add(span);
                                break;
                        }
                        break;
                }
            }
        }

        private static Run CreateTextRun(string text)
        {
            return new Run
            {
                Text = text.Replace("\n", "").Replace("  ", " ")
            };
        }

        private static InlineUIContainer CreateImage(HtmlNode node)
        {
            var image = new Image
            {
                Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(
                    new System.Uri(node.GetAttributeValue("src", ""))),
                //Width = node.GetAttributeValue("width", null),
                //Height = double.Parse(node.GetAttributeValue("height", null)),
                Stretch = Windows.UI.Xaml.Media.Stretch.Uniform
            };

            return new InlineUIContainer
            {
                Child = image
            };
        }

        private static void ApplyParagraphStyle(HtmlNode node, Paragraph paragraph)
        {
            var style = ParseStyle(node.GetAttributeValue("style", ""));
            if (style.TryGetValue("text-align", out var align))
            {
                paragraph.TextAlignment = align switch
                {
                    "center" => TextAlignment.Center,
                    "right" => TextAlignment.Right,
                    "justify" => TextAlignment.Justify,
                    _ => TextAlignment.Left
                };
            }
        }

        private static void ApplyInlineStyles(HtmlNode node, Span span)
        {
            var style = ParseStyle(node.GetAttributeValue("style", ""));

            // 处理字体样式
            span.FontWeight = node.Name == "b" || node.Name == "strong"
                ? FontWeights.Bold
                : style.TryGetValue("font-weight", out var weight)
                    ? FontWeightFromString(weight)
                    : FontWeights.Normal;

            span.FontStyle = node.Name == "i" || node.Name == "em"
                ? FontStyle.Italic
                : style.TryGetValue("font-style", out var styleStr) && styleStr == "italic"
                    ? FontStyle.Italic
                    : FontStyle.Normal;

            span.TextDecorations = node.Name == "u"
                ? TextDecorations.Underline
                : TextDecorations.None;

            // 处理颜色
            if (style.TryGetValue("color", out var color))
            {
                span.Foreground = ColorFromString(color);
            }

            // 处理字体大小
            if (style.TryGetValue("font-size", out var fontSize))
            {
                if (double.TryParse(fontSize.TrimEnd("px".ToCharArray()), out var size))
                {
                    span.FontSize = size;
                }
            }
        }

        private static void ApplyHyperlinkStyle(HtmlNode node, Hyperlink hyperlink)
        {
            hyperlink.NavigateUri = new System.Uri(node.GetAttributeValue("href", "#"));
            //hyperlink.Foreground = new SolidColorBrush(Colors.Blue);
            hyperlink.TextDecorations = TextDecorations.Underline;
        }

        private static Dictionary<string, string> ParseStyle(string style)
        {
            var result = new Dictionary<string, string>();
            var pairs = style.Split(';');
            foreach (var pair in pairs)
            {
                var parts = pair.Split(':');
                if (parts.Length == 2)
                {
                    result[parts[0].Trim().ToLower()] = parts[1].Trim();
                }
            }
            return result;
        }

        private static FontWeight FontWeightFromString(string weight) => weight switch
        {
            "bold" => FontWeights.Bold,
            "normal" => FontWeights.Normal,
            "100" => FontWeights.Thin,
            "200" => FontWeights.ExtraLight,
            "300" => FontWeights.Light,
            "400" => FontWeights.Normal,
            "500" => FontWeights.Medium,
            "600" => FontWeights.SemiBold,
            "700" => FontWeights.Bold,
            "800" => FontWeights.ExtraBold,
            "900" => FontWeights.Black,
            _ => FontWeights.Normal
        };

        private static Brush ColorFromString(string color)
        {
            try
            {
                return new SolidColorBrush((Color)XamlBindingHelper.ConvertValue(
                    typeof(Color), color.StartsWith("#") ? color : $"#{color}"));
            }
            catch
            {
                return new SolidColorBrush(Colors.Black);
            }
        }

        private static string HtmlDecode(this string text) =>
            System.Net.WebUtility.HtmlDecode(text);
    }
}