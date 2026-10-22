using MergeTool.Text.Diff;

namespace MergeTool.Application.Presentation;

/// <summary>Builds minimap models and markers from comparison blocks.</summary>
public static class MinimapModelBuilder
{
    public static MinimapModel Build(
        IReadOnlyList<DiffBlock> blocks,
        int leftLineCount,
        int rightLineCount)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        int totalLines = Math.Max(1, Math.Max(leftLineCount, rightLineCount));

        if (blocks.Count == 0)
        {
            return new MinimapModel([], totalLines);
        }

        var markers = new List<MinimapMarker>();
        foreach (DiffBlock block in blocks)
        {
            if (!block.IsDifference)
            {
                continue;
            }

            int start = int.MaxValue;
            int end = 0;
            if (!block.LeftRange.IsEmpty)
            {
                start = Math.Min(start, block.LeftRange.StartIndex);
                end = Math.Max(end, block.LeftRange.EndExclusive);
            }

            if (!block.RightRange.IsEmpty)
            {
                start = Math.Min(start, block.RightRange.StartIndex);
                end = Math.Max(end, block.RightRange.EndExclusive);
            }

            if (start == int.MaxValue)
            {
                start = 0;
                end = 1;
            }

            if (end <= start)
            {
                end = start + 1;
            }

            double top = start / (double)totalLines;
            double bottom = end / (double)totalLines;
            if (bottom < top)
            {
                (top, bottom) = (bottom, top);
            }

            bottom = Math.Min(1.0, Math.Max(bottom, top + (1.0 / totalLines)));
            markers.Add(new MinimapMarker(block.Ordinal, top, bottom, block.Kind));
        }

        markers.Sort(static (a, b) =>
        {
            int byY = a.TopY.CompareTo(b.TopY);
            return byY != 0 ? byY : a.BlockOrdinal.CompareTo(b.BlockOrdinal);
        });

        return new MinimapModel(markers.ToList(), totalLines);
    }
}
