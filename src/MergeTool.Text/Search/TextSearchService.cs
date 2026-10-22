using MergeTool.Core;
using MergeTool.Text;

namespace MergeTool.Text.Search;

public sealed record TextSearchOptions(
    bool UseRegex = false,
    bool IgnoreCase = false,
    bool WholeWord = false,
    bool ChangedLinesOnly = false,
    ComparisonSide? Pane = null);

public sealed record TextSearchMatch(int LineIndex, int StartOffset, int Length, ComparisonSide Side);

public static class TextSearchService
{
    public const string InvalidRegexCode = "MT-SRC-001";

    public static IReadOnlyList<Diagnostic> ValidatePattern(TextSearchOptions options, string? pattern)
    {
        if (!options.UseRegex || string.IsNullOrEmpty(pattern))
        {
            return [];
        }

        try
        {
            _ = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            return [];
        }
        catch (Exception exception)
        {
            return [Diagnostic.Error(InvalidRegexCode, "Search pattern is not a valid regular expression.", technicalDetail: exception.Message)];
        }
    }

    public static IReadOnlyList<TextSearchMatch> Search(
        IReadOnlyList<TextLine> leftLines,
        IReadOnlyList<TextLine> rightLines,
        string pattern,
        TextSearchOptions options,
        IReadOnlySet<int>? changedLeftLines,
        IReadOnlySet<int>? changedRightLines,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(leftLines);
        ArgumentNullException.ThrowIfNull(rightLines);
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        var matches = new List<TextSearchMatch>();
        if (options.Pane is null or ComparisonSide.Left)
        {
            SearchSide(leftLines, ComparisonSide.Left, changedLeftLines);
        }

        if (options.Pane is null or ComparisonSide.Right)
        {
            SearchSide(rightLines, ComparisonSide.Right, changedRightLines);
        }

        return matches;

        void SearchSide(IReadOnlyList<TextLine> lines, ComparisonSide side, IReadOnlySet<int>? changed)
        {
            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (options.ChangedLinesOnly && changed is not null && !changed.Contains(lineIndex))
                {
                    continue;
                }

                string content = lines[lineIndex].Content;
                foreach ((int start, int length) in FindMatches(content, pattern, options))
                {
                    matches.Add(new TextSearchMatch(lineIndex, start, length, side));
                }
            }
        }
    }

    private static IEnumerable<(int Start, int Length)> FindMatches(string content, string pattern, TextSearchOptions options)
    {
        if (options.UseRegex)
        {
            var regexOptions = System.Text.RegularExpressions.RegexOptions.CultureInvariant;
            if (options.IgnoreCase)
            {
                regexOptions |= System.Text.RegularExpressions.RegexOptions.IgnoreCase;
            }

            var regex = new System.Text.RegularExpressions.Regex(pattern, regexOptions);
            foreach (System.Text.RegularExpressions.Match match in regex.Matches(content))
            {
                if (!options.WholeWord || IsWholeWord(content, match.Index, match.Length))
                {
                    yield return (match.Index, match.Length);
                }
            }

            yield break;
        }

        StringComparison comparison = options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        int start = 0;
        while (start <= content.Length - pattern.Length)
        {
            int index = content.IndexOf(pattern, start, comparison);
            if (index < 0)
            {
                yield break;
            }

            if (!options.WholeWord || IsWholeWord(content, index, pattern.Length))
            {
                yield return (index, pattern.Length);
            }

            start = index + Math.Max(1, pattern.Length);
        }
    }

    private static bool IsWholeWord(string content, int index, int length)
    {
        bool left = index == 0 || !char.IsLetterOrDigit(content[index - 1]);
        int end = index + length;
        bool right = end >= content.Length || !char.IsLetterOrDigit(content[end]);
        return left && right;
    }
}
