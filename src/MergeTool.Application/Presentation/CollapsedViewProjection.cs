using MergeTool.Core;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Presentation;

/// <summary>One collapsible equal run inside a single pane (original line coordinates).</summary>
public sealed record CollapsedRegion(
    string RegionId,
    int BlockOrdinal,
    int HiddenStartLine,
    int HiddenEndExclusive,
    int HiddenLineCount)
{
    public bool ContainsLine(int originalLine)
        => originalLine >= HiddenStartLine && originalLine < HiddenEndExclusive;
}

/// <summary>
/// Immutable collapse layout for one pane. Fold ranges are advisory for the view adapter;
/// merge/navigation always use original line indices from the comparison result.
/// </summary>
public sealed class CollapsedViewProjection
{
    public CollapsedViewProjection(
        int lineCount,
        IReadOnlyList<CollapsedRegion> regions,
        IReadOnlyDictionary<int, int> originalLineToDisplayRow,
        IReadOnlySet<int> alwaysVisibleLines)
    {
        LineCount = lineCount;
        Regions = regions;
        OriginalLineToDisplayRow = originalLineToDisplayRow;
        AlwaysVisibleLines = alwaysVisibleLines;
    }

    public int LineCount { get; }

    public IReadOnlyList<CollapsedRegion> Regions { get; }

    /// <summary>Maps each original line to a monotonic display row index (context lines only).</summary>
    public IReadOnlyDictionary<int, int> OriginalLineToDisplayRow { get; }

    public IReadOnlySet<int> AlwaysVisibleLines { get; }

    public int DisplayRowForOriginalLine(int originalLine)
        => OriginalLineToDisplayRow.TryGetValue(originalLine, out int row) ? row : -1;

    public CollapsedRegion? FindRegionContaining(int originalLine)
    {
        foreach (CollapsedRegion region in Regions)
        {
            if (region.ContainsLine(originalLine))
            {
                return region;
            }
        }

        return null;
    }
}

public static class CollapsedViewProjectionBuilder
{
    public const int DefaultCollapseThreshold = 8;

    public const int DefaultContextLines = 2;

    public static CollapsedViewProjection BuildForSide(
        IReadOnlyList<DiffBlock> blocks,
        int lineCount,
        ComparisonSide side,
        int collapseThreshold = DefaultCollapseThreshold,
        int contextLines = DefaultContextLines)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        ArgumentOutOfRangeException.ThrowIfNegative(lineCount);

        var regions = new List<CollapsedRegion>();
        var displayMap = new Dictionary<int, int>();
        var alwaysVisible = new HashSet<int>();
        int displayRow = 0;

        foreach (DiffBlock block in blocks)
        {
            LineRange range = side == ComparisonSide.Left ? block.LeftRange : block.RightRange;
            int rangeEnd = Math.Min(range.EndExclusive, lineCount);
            if (range.StartIndex >= lineCount)
            {
                continue;
            }

            if (block.Kind == DiffBlockKind.Equal && range.Length > collapseThreshold)
            {
                int ctx = Math.Min(contextLines, range.Length / 2);
                int hiddenStart = range.StartIndex + ctx;
                int hiddenEnd = rangeEnd - ctx;
                if (hiddenEnd > hiddenStart)
                {
                    regions.Add(new CollapsedRegion(
                        RegionKey(block.Ordinal, side),
                        block.Ordinal,
                        hiddenStart,
                        hiddenEnd,
                        hiddenEnd - hiddenStart));
                }

                for (int line = range.StartIndex; line < rangeEnd; line++)
                {
                    if (line >= hiddenStart && line < hiddenEnd)
                    {
                        continue;
                    }

                    displayMap[line] = displayRow++;
                    alwaysVisible.Add(line);
                }
            }
            else
            {
                for (int line = range.StartIndex; line < rangeEnd; line++)
                {
                    displayMap[line] = displayRow++;
                    alwaysVisible.Add(line);
                }
            }
        }

        return new CollapsedViewProjection(lineCount, regions, displayMap, alwaysVisible);
    }

    public static string RegionKey(int blockOrdinal, ComparisonSide side) => $"eq-{side}-{blockOrdinal}";
}
