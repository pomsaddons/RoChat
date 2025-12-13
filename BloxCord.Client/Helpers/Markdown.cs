using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace BloxCord.Client.Helpers;

public static class Markdown
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached(
            "Text",
            typeof(string),
            typeof(Markdown),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static string GetText(DependencyObject obj)
    {
        return (string)obj.GetValue(TextProperty);
    }

    public static void SetText(DependencyObject obj, string value)
    {
        obj.SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock textBlock)
        {
            textBlock.Inlines.Clear();
            var text = e.NewValue as string;
            if (string.IsNullOrEmpty(text))
                return;

            ParseMarkdown(textBlock.Inlines, text);
        }
    }

    private static void ParseMarkdown(InlineCollection inlines, string text)
    {
        // Simple parser for **bold**, *italic*, ~~strike~~, `code`
        // We will use a regex to find tokens
        
        // Regex to match markdown tokens
        // Groups: 1=bold, 2=italic, 3=strike, 4=code
        var regex = new Regex(@"(\*\*(.*?)\*\*)|(\*(.*?)\*)|(~~(.*?)~~)|(`(.*?)`)");

        int lastIndex = 0;

        foreach (Match match in regex.Matches(text))
        {
            // Add text before the match
            if (match.Index > lastIndex)
            {
                inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
            }

            if (match.Groups[1].Success) // Bold
            {
                var run = new Run(match.Groups[2].Value) { FontWeight = FontWeights.Bold };
                inlines.Add(run);
            }
            else if (match.Groups[3].Success) // Italic
            {
                var run = new Run(match.Groups[4].Value) { FontStyle = FontStyles.Italic };
                inlines.Add(run);
            }
            else if (match.Groups[5].Success) // Strike
            {
                var run = new Run(match.Groups[6].Value) { TextDecorations = TextDecorations.Strikethrough };
                inlines.Add(run);
            }
            else if (match.Groups[7].Success) // Code
            {
                var run = new Run(match.Groups[8].Value) 
                { 
                    FontFamily = new FontFamily("Consolas, Courier New, Monospace"),
                    Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
                };
                inlines.Add(run);
            }

            lastIndex = match.Index + match.Length;
        }

        // Add remaining text
        if (lastIndex < text.Length)
        {
            inlines.Add(new Run(text.Substring(lastIndex)));
        }
    }
}
