namespace MergeTool.Text.Diff;

/// <summary>
/// A zero-based, [StartIndex, Length] range of lines. This is the product-wide range
/// convention; inclusive/exclusive end conventions are never used.
/// An empty range (Length == 0) still has a meaningful StartIndex: the insertion point.
/// </summary>
public readonly record struct LineRange(int StartIndex, int Length)
{
    public static LineRange Empty(int startIndex) => new(startIndex, 0);

    public bool IsEmpty => Length == 0;

    /// <summary>Exclusive end index (StartIndex + Length).</summary>
    public int EndExclusive => StartIndex + Length;

    public bool Contains(int lineIndex) => lineIndex >= StartIndex && lineIndex < EndExclusive;
}
