namespace MergeTool.Text.Diff;

/// <summary>
/// One immutable UTF-16 span inside a single source line. Offsets are zero-based within the
/// line text (AvalonEdit document coordinates).
/// </summary>
public sealed record InlineDiffSegment(int StartOffset, int Length, InlineDiffSegmentKind Kind)
{
    public int EndOffset => StartOffset + Length;
}
