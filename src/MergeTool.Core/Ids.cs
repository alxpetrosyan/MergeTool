namespace MergeTool.Core;

/// <summary>
/// Opaque identity of one in-memory document session. Never derived from a file path.
/// </summary>
public readonly record struct DocumentSessionId(Guid Value)
{
    public static DocumentSessionId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}

/// <summary>
/// Opaque identity of one comparison session (a pair of document sessions under comparison).
/// </summary>
public readonly record struct ComparisonSessionId(Guid Value)
{
    public static ComparisonSessionId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}
