namespace MergeTool.Text.Diff;

/// <summary>Formats active comparison rules for UI badges.</summary>
public static class ComparisonRulesSummary
{
    public static string FormatActiveRules(TextComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.HasActiveComparisonRules)
        {
            return "Exact compare";
        }

        var parts = new List<string>();
        if (options.WhitespaceMode != WhitespaceComparisonMode.Exact)
        {
            parts.Add(options.WhitespaceMode switch
            {
                WhitespaceComparisonMode.TrimEnd => "Trim end",
                WhitespaceComparisonMode.TrimAll => "Trim all",
                WhitespaceComparisonMode.IgnoreAll => "Ignore whitespace",
                _ => options.WhitespaceMode.ToString(),
            });
        }

        if (options.IgnoreCase)
        {
            parts.Add("Ignore case");
        }

        if (options.IgnoreEmptyLines)
        {
            parts.Add("Ignore empty lines");
        }

        if (!string.IsNullOrWhiteSpace(options.IgnoredLinePattern))
        {
            parts.Add("Ignored lines");
        }

        if (!string.IsNullOrWhiteSpace(options.IgnoredBlockStartPattern))
        {
            parts.Add("Ignored blocks");
        }

        return string.Join(", ", parts);
    }
}
