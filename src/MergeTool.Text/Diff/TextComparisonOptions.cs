using MergeTool.Text.Formatted;

namespace MergeTool.Text.Diff;

/// <summary>
/// Options for two-way text comparison. MVP defaults are fixed and explicit:
/// line content is compared exactly (ordinal, case-sensitive); line-ending style (CRLF vs LF
/// vs CR) is NOT a content difference because it is preserved on save; the presence of a
/// final newline IS a content difference on the last line.
/// </summary>
public sealed record TextComparisonOptions
{
    public static TextComparisonOptions Default { get; } = new();

    public static TextComparisonOptions WithIntralineDiff { get; } = new(enableIntralineDiff: true);

    public TextComparisonOptions(
        bool enableIntralineDiff = false,
        IntralineDiffBounds? intralineBounds = null,
        WhitespaceComparisonMode whitespaceMode = WhitespaceComparisonMode.Exact,
        bool ignoreCase = false,
        bool ignoreEmptyLines = false,
        string? ignoredLinePattern = null,
        string? ignoredBlockStartPattern = null,
        string? ignoredBlockEndPattern = null,
        StructuredFormatKind structuredFormat = StructuredFormatKind.None,
        bool detectMovedBlocks = false)
    {
        EnableIntralineDiff = enableIntralineDiff;
        IntralineBounds = intralineBounds ?? IntralineDiffBounds.Default;
        WhitespaceMode = whitespaceMode;
        IgnoreCase = ignoreCase;
        IgnoreEmptyLines = ignoreEmptyLines;
        IgnoredLinePattern = NormalizePattern(ignoredLinePattern);
        IgnoredBlockStartPattern = NormalizePattern(ignoredBlockStartPattern);
        IgnoredBlockEndPattern = NormalizePattern(ignoredBlockEndPattern);
        StructuredFormat = structuredFormat;
        DetectMovedBlocks = detectMovedBlocks;
    }

    /// <summary>When false, comparison results carry no intraline segment data.</summary>
    public bool EnableIntralineDiff { get; init; }

    public IntralineDiffBounds IntralineBounds { get; init; }

    public WhitespaceComparisonMode WhitespaceMode { get; init; }

    public bool IgnoreCase { get; init; }

    public bool IgnoreEmptyLines { get; init; }

    public string? IgnoredLinePattern { get; init; }

    public string? IgnoredBlockStartPattern { get; init; }

    public string? IgnoredBlockEndPattern { get; init; }

    /// <summary>When set, JSON/XML may be pretty-printed for comparison display only.</summary>
    public StructuredFormatKind StructuredFormat { get; init; }

    /// <summary>When true, deleted/inserted regions with identical text may be marked as moved.</summary>
    public bool DetectMovedBlocks { get; init; }

    public bool HasActiveComparisonRules =>
        WhitespaceMode != WhitespaceComparisonMode.Exact
        || IgnoreCase
        || IgnoreEmptyLines
        || !string.IsNullOrWhiteSpace(IgnoredLinePattern)
        || !string.IsNullOrWhiteSpace(IgnoredBlockStartPattern);

    private static string? NormalizePattern(string? pattern)
        => string.IsNullOrWhiteSpace(pattern) ? null : pattern.Trim();
}
