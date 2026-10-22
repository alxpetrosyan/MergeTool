using MergeTool.Application.Sessions;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Presentation;

/// <summary>Source-line indices that belong to non-equal comparison blocks.</summary>
public sealed record ChangedLinesIndex(IReadOnlySet<int> Left, IReadOnlySet<int> Right)
{
    public static ChangedLinesIndex Build(ComparisonPresentationMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        var left = new HashSet<int>();
        var right = new HashSet<int>();

        foreach (DiffBlock block in map.Blocks)
        {
            if (!block.IsDifference)
            {
                continue;
            }

            for (int line = block.LeftRange.StartIndex; line < block.LeftRange.EndExclusive; line++)
            {
                left.Add(line);
            }

            for (int line = block.RightRange.StartIndex; line < block.RightRange.EndExclusive; line++)
            {
                right.Add(line);
            }
        }

        return new ChangedLinesIndex(left, right);
    }
}
