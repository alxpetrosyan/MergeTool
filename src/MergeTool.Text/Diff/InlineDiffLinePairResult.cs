namespace MergeTool.Text.Diff;

/// <summary>
/// Intraline diff for one paired line inside a <see cref="DiffBlockKind.Modified"/> block.
/// When <see cref="IsWholeLineFallback"/> is true, inline work was skipped and only whole-line
/// modified styling applies.
/// </summary>
public sealed record InlineDiffLinePairResult(
    int LeftLineIndex,
    int RightLineIndex,
    IReadOnlyList<InlineDiffSegment> LeftSegments,
    IReadOnlyList<InlineDiffSegment> RightSegments,
    bool IsWholeLineFallback)
{
    public static InlineDiffLinePairResult WholeLineFallback(int leftLineIndex, int rightLineIndex)
        => new(leftLineIndex, rightLineIndex, [], [], IsWholeLineFallback: true);

    /// <summary>Segments that should receive inline highlight on the left pane.</summary>
    public IEnumerable<InlineDiffSegment> LeftHighlightSegments
        => IsWholeLineFallback
            ? []
            : LeftSegments.Where(s => s.Kind is InlineDiffSegmentKind.Replacement or InlineDiffSegmentKind.Deletion);

    /// <summary>Segments that should receive inline highlight on the right pane.</summary>
    public IEnumerable<InlineDiffSegment> RightHighlightSegments
        => IsWholeLineFallback
            ? []
            : RightSegments.Where(s => s.Kind is InlineDiffSegmentKind.Replacement or InlineDiffSegmentKind.Insertion);
}
