namespace MergeTool.Text.Diff;

/// <summary>
/// A projection of original document lines into normalized comparison lines with a stable
/// index map back to original zero-based line numbers.
/// </summary>
public sealed class ComparisonProjection
{
    public ComparisonProjection(
        IReadOnlyList<TextLine> projectedLeftLines,
        IReadOnlyList<TextLine> projectedRightLines,
        IReadOnlyList<int> leftProjectedToOriginalLine,
        IReadOnlyList<int> rightProjectedToOriginalLine)
    {
        ArgumentNullException.ThrowIfNull(projectedLeftLines);
        ArgumentNullException.ThrowIfNull(projectedRightLines);
        ArgumentNullException.ThrowIfNull(leftProjectedToOriginalLine);
        ArgumentNullException.ThrowIfNull(rightProjectedToOriginalLine);

        if (projectedLeftLines.Count != leftProjectedToOriginalLine.Count
            || projectedRightLines.Count != rightProjectedToOriginalLine.Count)
        {
            throw new ArgumentException("Projection line counts must match their index maps.");
        }

        ProjectedLeftLines = projectedLeftLines;
        ProjectedRightLines = projectedRightLines;
        LeftProjectedToOriginalLine = leftProjectedToOriginalLine;
        RightProjectedToOriginalLine = rightProjectedToOriginalLine;
    }

    public IReadOnlyList<TextLine> ProjectedLeftLines { get; }

    public IReadOnlyList<TextLine> ProjectedRightLines { get; }

    public IReadOnlyList<int> LeftProjectedToOriginalLine { get; }

    public IReadOnlyList<int> RightProjectedToOriginalLine { get; }
}
