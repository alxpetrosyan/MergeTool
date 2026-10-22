namespace MergeTool.Text.Diff;

/// <summary>How line content is normalized before comparison keys are built.</summary>
public enum WhitespaceComparisonMode
{
    Exact = 0,
    TrimEnd = 1,
    TrimAll = 2,
    IgnoreAll = 3,
}
