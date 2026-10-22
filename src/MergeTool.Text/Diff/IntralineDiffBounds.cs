namespace MergeTool.Text.Diff;

/// <summary>Documented limits for bounded intraline work.</summary>
public sealed record IntralineDiffBounds
{
    public const int DefaultMaxLineLength = 4096;

    public const int DefaultMaxWorkBudget = 262_144;

    public static IntralineDiffBounds Default { get; } = new();

    public IntralineDiffBounds(int maxLineLength = DefaultMaxLineLength, int maxWorkBudget = DefaultMaxWorkBudget)
    {
        if (maxLineLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLineLength));
        }

        if (maxWorkBudget <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxWorkBudget));
        }

        MaxLineLength = maxLineLength;
        MaxWorkBudget = maxWorkBudget;
    }

    public int MaxLineLength { get; }

    public int MaxWorkBudget { get; }
}
