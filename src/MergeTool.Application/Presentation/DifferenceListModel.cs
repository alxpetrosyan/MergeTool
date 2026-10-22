using MergeTool.Application.Sessions;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Presentation;

public sealed record DifferenceListEntry(
    int BlockOrdinal,
    int ListIndex,
    DiffBlockKind Kind,
    string Summary,
    int LeftAnchorLine,
    int RightAnchorLine,
    bool IsMoved = false);

public static class DifferenceListBuilder
{
    public static IReadOnlyList<DifferenceListEntry> Build(
        ComparisonPresentationMap map,
        IReadOnlySet<int>? movedBlockOrdinals = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        var entries = new List<DifferenceListEntry>();
        IReadOnlyList<int> ordinals = map.DifferenceBlockOrdinals;

        for (int i = 0; i < ordinals.Count; i++)
        {
            int ordinal = ordinals[i];
            if (!map.TryGetBlock(ordinal, out DiffBlock? block) || block is null)
            {
                continue;
            }

            entries.Add(new DifferenceListEntry(
                ordinal,
                i + 1,
                block.Kind,
                FormatSummary(block, movedBlockOrdinals?.Contains(ordinal) == true),
                map.GetLeftAnchorForBlock(ordinal),
                map.GetRightAnchorForBlock(ordinal),
                movedBlockOrdinals?.Contains(ordinal) == true));
        }

        return entries;
    }

    private static string FormatSummary(DiffBlock block, bool isMoved)
    {
        string summary = block.Kind switch
        {
            DiffBlockKind.Inserted => $"Inserted ({block.RightRange.Length} line(s))",
            DiffBlockKind.Deleted => $"Deleted ({block.LeftRange.Length} line(s))",
            DiffBlockKind.Modified => $"Modified ({block.LeftRange.Length}/{block.RightRange.Length} line(s))",
            _ => block.Kind.ToString(),
        };

        return isMoved ? $"{summary} — moved" : summary;
    }
}
