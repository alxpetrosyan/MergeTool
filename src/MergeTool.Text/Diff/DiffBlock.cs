namespace MergeTool.Text.Diff;

public enum DiffBlockKind
{
    /// <summary>Lines are identical on both sides (LeftRange.Length == RightRange.Length).</summary>
    Equal = 0,

    /// <summary>Lines exist only on the right (LeftRange is empty at the insertion anchor).</summary>
    Inserted = 1,

    /// <summary>Lines exist only on the left (RightRange is empty at the deletion anchor).</summary>
    Deleted = 2,

    /// <summary>
    /// A replacement region: all left lines pair with all right lines as one unit. Produced
    /// whenever a difference region has lines on BOTH sides, even with unequal line counts.
    /// Pure one-sided regions fall back to <see cref="Inserted"/>/<see cref="Deleted"/>.
    /// </summary>
    Modified = 3,
}

/// <summary>
/// One ordered block of a two-way comparison. <see cref="Ordinal"/> is the deterministic,
/// zero-based block ID within its result. Ranges use the zero-based [start, length]
/// convention; an empty range's StartIndex is the safe anchor (insertion/deletion point) in
/// that document.
/// </summary>
public sealed record DiffBlock(int Ordinal, DiffBlockKind Kind, LineRange LeftRange, LineRange RightRange)
{
    /// <summary>Safe navigation anchor on the left side (clamped start index).</summary>
    public int LeftAnchorLine => LeftRange.StartIndex;

    /// <summary>Safe navigation anchor on the right side (clamped start index).</summary>
    public int RightAnchorLine => RightRange.StartIndex;

    public bool IsDifference => Kind != DiffBlockKind.Equal;
}
