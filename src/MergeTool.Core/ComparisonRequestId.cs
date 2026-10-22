namespace MergeTool.Core;

/// <summary>
/// Monotonic identity of one comparison (or other long-running) request. A larger value is
/// always a newer request. The application layer owns issuing these values.
/// </summary>
public readonly record struct ComparisonRequestId(long Value) : IComparable<ComparisonRequestId>
{
    public static readonly ComparisonRequestId None = new(0);

    public int CompareTo(ComparisonRequestId other) => Value.CompareTo(other.Value);

    public bool IsNewerThan(ComparisonRequestId other) => Value > other.Value;

    public static bool operator <(ComparisonRequestId left, ComparisonRequestId right) => left.Value < right.Value;

    public static bool operator >(ComparisonRequestId left, ComparisonRequestId right) => left.Value > right.Value;

    public static bool operator <=(ComparisonRequestId left, ComparisonRequestId right) => left.Value <= right.Value;

    public static bool operator >=(ComparisonRequestId left, ComparisonRequestId right) => left.Value >= right.Value;

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
