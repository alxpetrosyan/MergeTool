using MergeTool.Text;
using MergeTool.Text.Diff;

namespace MergeTool.Text.ThreeWay;

public enum ThreeWayRegionKind
{
    Unchanged = 0,
    LeftOnlyChange = 1,
    RightOnlyChange = 2,
    SameChange = 3,
    Divergent = 4,
}

public enum ThreeWayDocumentSide
{
    Base = 0,
    Left = 1,
    Right = 2,
}

/// <summary>One aligned region across base, left, and right documents.</summary>
public sealed record ThreeWayRegion(
    int Ordinal,
    ThreeWayRegionKind Kind,
    LineRange BaseRange,
    LineRange LeftRange,
    LineRange RightRange)
{
    public bool IsDifference => Kind != ThreeWayRegionKind.Unchanged;
}

public sealed record ThreeWayComparisonResult(
    IReadOnlyList<ThreeWayRegion> Regions,
    int BaseRevision,
    int LeftRevision,
    int RightRevision)
{
    public IReadOnlyList<int> DifferenceRegionOrdinals { get; } =
        [.. Regions.Where(region => region.IsDifference).Select(region => region.Ordinal)];
}

/// <summary>Builds a 3-way region model from two base-relative 2-way comparisons.</summary>
public static class ThreeWayComparisonBuilder
{
    public static ThreeWayComparisonResult Build(
        IReadOnlyList<TextLine> baseLines,
        IReadOnlyList<TextLine> leftLines,
        IReadOnlyList<TextLine> rightLines,
        IReadOnlyList<DiffBlock> baseLeftBlocks,
        IReadOnlyList<DiffBlock> baseRightBlocks,
        int baseRevision,
        int leftRevision,
        int rightRevision)
    {
        ArgumentNullException.ThrowIfNull(baseLines);
        ArgumentNullException.ThrowIfNull(leftLines);
        ArgumentNullException.ThrowIfNull(rightLines);
        ArgumentNullException.ThrowIfNull(baseLeftBlocks);
        ArgumentNullException.ThrowIfNull(baseRightBlocks);

        int baseLineCount = baseLines.Count;
        IReadOnlyList<BaseSegment> segments = CollectBaseSegments(baseLineCount, baseLeftBlocks, baseRightBlocks);
        var regions = new List<ThreeWayRegion>(segments.Count);
        int ordinal = 0;

        foreach (BaseSegment segment in segments)
        {
            LineRange baseRange = segment.ToBaseRange();
            bool leftChanged = HasChangeOnBase(baseLeftBlocks, segment);
            bool rightChanged = HasChangeOnBase(baseRightBlocks, segment);
            LineRange leftRange = MapToSideRange(baseLeftBlocks, segment);
            LineRange rightRange = MapToSideRange(baseRightBlocks, segment);
            ThreeWayRegionKind kind = Classify(
                leftChanged,
                rightChanged,
                leftLines,
                rightLines,
                leftRange,
                rightRange);

            regions.Add(new ThreeWayRegion(ordinal++, kind, baseRange, leftRange, rightRange));
        }

        return new ThreeWayComparisonResult(regions, baseRevision, leftRevision, rightRevision);
    }

    private readonly record struct BaseSegment(int Start, int EndExclusive)
    {
        public bool IsEmpty => EndExclusive == Start;

        public LineRange ToBaseRange()
            => IsEmpty ? LineRange.Empty(Start) : new LineRange(Start, EndExclusive - Start);
    }

    private static IReadOnlyList<BaseSegment> CollectBaseSegments(
        int baseLineCount,
        IReadOnlyList<DiffBlock> baseLeftBlocks,
        IReadOnlyList<DiffBlock> baseRightBlocks)
    {
        var cutPoints = new SortedSet<int> { 0, baseLineCount };
        var anchorPoints = new HashSet<int>();

        foreach (DiffBlock block in EnumerateDifferenceBlocks(baseLeftBlocks, baseRightBlocks))
        {
            cutPoints.Add(block.LeftRange.StartIndex);
            if (!block.LeftRange.IsEmpty)
            {
                cutPoints.Add(block.LeftRange.EndExclusive);
            }
            else
            {
                anchorPoints.Add(block.LeftRange.StartIndex);
            }
        }

        List<int> ordered = cutPoints.ToList();
        var segments = new List<BaseSegment>();
        for (int index = 0; index < ordered.Count - 1; index++)
        {
            int start = ordered[index];
            int end = ordered[index + 1];
            if (start < end)
            {
                segments.Add(new BaseSegment(start, end));
            }
        }

        foreach (int anchor in anchorPoints)
        {
            bool coveredBySpan = segments.Any(segment => segment.Start <= anchor && segment.EndExclusive > anchor);
            if (!coveredBySpan)
            {
                segments.Add(new BaseSegment(anchor, anchor));
            }
        }

        segments.Sort(static (left, right) =>
        {
            int byStart = left.Start.CompareTo(right.Start);
            return byStart != 0 ? byStart : left.EndExclusive.CompareTo(right.EndExclusive);
        });

        return segments;
    }

    private static IEnumerable<DiffBlock> EnumerateDifferenceBlocks(
        IReadOnlyList<DiffBlock> baseLeftBlocks,
        IReadOnlyList<DiffBlock> baseRightBlocks)
    {
        foreach (DiffBlock block in baseLeftBlocks)
        {
            if (block.IsDifference)
            {
                yield return block;
            }
        }

        foreach (DiffBlock block in baseRightBlocks)
        {
            if (block.IsDifference)
            {
                yield return block;
            }
        }
    }

    private static bool HasChangeOnBase(IReadOnlyList<DiffBlock> blocks, BaseSegment segment)
    {
        foreach (DiffBlock block in blocks)
        {
            if (!block.IsDifference)
            {
                continue;
            }

            if (Overlaps(block.LeftRange, segment))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Overlaps(LineRange baseRange, BaseSegment segment)
    {
        if (segment.IsEmpty)
        {
            return baseRange.IsEmpty && baseRange.StartIndex == segment.Start;
        }

        return baseRange.StartIndex < segment.EndExclusive && segment.Start < baseRange.EndExclusive;
    }

    private static LineRange MapToSideRange(IReadOnlyList<DiffBlock> baseRelativeBlocks, BaseSegment segment)
    {
        if (!HasChangeOnBase(baseRelativeBlocks, segment))
        {
            return segment.ToBaseRange();
        }

        LineRange? mapped = null;
        foreach (DiffBlock block in baseRelativeBlocks)
        {
            if (!block.IsDifference || !Overlaps(block.LeftRange, segment))
            {
                continue;
            }

            LineRange sideSlice = MapSubRange(block.LeftRange, block.RightRange, segment);
            mapped = mapped is null ? sideSlice : Union(mapped.Value, sideSlice);
        }

        return mapped ?? segment.ToBaseRange();
    }

    private static LineRange MapSubRange(LineRange baseRange, LineRange sideRange, BaseSegment segment)
    {
        if (segment.IsEmpty)
        {
            if (baseRange.IsEmpty && baseRange.StartIndex == segment.Start)
            {
                return sideRange;
            }

            return LineRange.Empty(sideRange.StartIndex);
        }

        if (baseRange.IsEmpty)
        {
            return LineRange.Empty(sideRange.StartIndex);
        }

        int overlapStart = Math.Max(baseRange.StartIndex, segment.Start);
        int overlapEnd = Math.Min(baseRange.EndExclusive, segment.EndExclusive);
        int offset = overlapStart - baseRange.StartIndex;
        int length = overlapEnd - overlapStart;

        if (sideRange.IsEmpty)
        {
            return LineRange.Empty(sideRange.StartIndex + offset);
        }

        return new LineRange(sideRange.StartIndex + offset, length);
    }

    private static LineRange Union(LineRange left, LineRange right)
    {
        if (left.IsEmpty)
        {
            return right;
        }

        if (right.IsEmpty)
        {
            return left;
        }

        int start = Math.Min(left.StartIndex, right.StartIndex);
        int end = Math.Max(left.EndExclusive, right.EndExclusive);
        return new LineRange(start, end - start);
    }

    private static ThreeWayRegionKind Classify(
        bool leftChanged,
        bool rightChanged,
        IReadOnlyList<TextLine> leftLines,
        IReadOnlyList<TextLine> rightLines,
        LineRange leftRange,
        LineRange rightRange)
    {
        if (!leftChanged && !rightChanged)
        {
            return ThreeWayRegionKind.Unchanged;
        }

        if (leftChanged && !rightChanged)
        {
            return ThreeWayRegionKind.LeftOnlyChange;
        }

        if (!leftChanged && rightChanged)
        {
            return ThreeWayRegionKind.RightOnlyChange;
        }

        return string.Equals(
            ExtractRegionText(leftLines, leftRange),
            ExtractRegionText(rightLines, rightRange),
            StringComparison.Ordinal)
            ? ThreeWayRegionKind.SameChange
            : ThreeWayRegionKind.Divergent;
    }

    private static string ExtractRegionText(IReadOnlyList<TextLine> lines, LineRange range)
    {
        if (range.IsEmpty)
        {
            return string.Empty;
        }

        var parts = new List<string>(range.Length);
        int end = Math.Min(range.EndExclusive, lines.Count);
        for (int line = range.StartIndex; line < end; line++)
        {
            parts.Add(lines[line].Content);
        }

        return string.Join('\n', parts);
    }
}

public static class ThreeWayRegionKindLabels
{
    public static string GetDisplayName(ThreeWayRegionKind kind) => kind switch
    {
        ThreeWayRegionKind.Unchanged => "Unchanged",
        ThreeWayRegionKind.LeftOnlyChange => "Left only",
        ThreeWayRegionKind.RightOnlyChange => "Right only",
        ThreeWayRegionKind.SameChange => "Same change",
        ThreeWayRegionKind.Divergent => "Divergent",
        _ => kind.ToString(),
    };
}
