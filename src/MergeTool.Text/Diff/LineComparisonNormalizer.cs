using System.Text;

namespace MergeTool.Text.Diff;

/// <summary>Normalizes one line's content for comparison keys only; never mutates source documents.</summary>
public static class LineComparisonNormalizer
{
    public static string Normalize(string content, TextComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(options);

        string normalized = options.WhitespaceMode switch
        {
            WhitespaceComparisonMode.Exact => content,
            WhitespaceComparisonMode.TrimEnd => content.TrimEnd(),
            WhitespaceComparisonMode.TrimAll => content.Trim(),
            WhitespaceComparisonMode.IgnoreAll => RemoveAllWhitespace(content),
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.WhitespaceMode, "Unknown whitespace mode."),
        };

        return options.IgnoreCase ? normalized.ToUpperInvariant() : normalized;
    }

    private static string RemoveAllWhitespace(string content)
    {
        if (content.Length == 0)
        {
            return content;
        }

        var builder = new StringBuilder(content.Length);
        foreach (char ch in content)
        {
            if (!char.IsWhiteSpace(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }
}
