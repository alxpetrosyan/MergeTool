using MergeTool.Text.Diff;

namespace MergeTool.Text.Diff;

/// <summary>Bounded optional moved-block detection between deleted and inserted regions.</summary>
public static class MovedBlockDetector
{
    public const int DefaultMaxBlockLines = 64;

    public const int DefaultMaxMovedPairs = 32;

    public static IReadOnlyList<int> DetectMovedBlockOrdinals(
        IReadOnlyList<DiffBlock> blocks,
        IReadOnlyList<TextLine> leftLines,
        IReadOnlyList<TextLine> rightLines,
        int maxBlockLines = DefaultMaxBlockLines,
        int maxMovedPairs = DefaultMaxMovedPairs)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        var moved = new List<int>();
        int pairCount = 0;
        var deletions = blocks.Where(b => b.Kind == DiffBlockKind.Deleted).ToList();
        var insertions = blocks.Where(b => b.Kind == DiffBlockKind.Inserted).ToList();

        foreach (DiffBlock deleted in deletions)
        {
            if (pairCount >= maxMovedPairs)
            {
                break;
            }

            if (deleted.LeftRange.Length > maxBlockLines)
            {
                continue;
            }

            string leftText = Join(leftLines, deleted.LeftRange);
            foreach (DiffBlock inserted in insertions)
            {
                if (inserted.RightRange.Length > maxBlockLines)
                {
                    continue;
                }

                string rightText = Join(rightLines, inserted.RightRange);
                if (leftText == rightText)
                {
                    moved.Add(deleted.Ordinal);
                    moved.Add(inserted.Ordinal);
                    pairCount++;
                    break;
                }
            }
        }

        return moved.Distinct().OrderBy(i => i).ToList();
    }

    private static string Join(IReadOnlyList<TextLine> lines, LineRange range)
    {
        var parts = new List<string>();
        for (int i = range.StartIndex; i < range.EndExclusive; i++)
        {
            parts.Add(lines[i].Content);
        }

        return string.Join('\n', parts);
    }
}
