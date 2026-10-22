namespace MergeTool.Text.Diff;

/// <summary>Maps diff blocks from projected line space back to original document line ranges.</summary>
public static class ComparisonProjectionMapper
{
    public static IReadOnlyList<DiffBlock> MapToOriginal(
        IReadOnlyList<DiffBlock> projectedBlocks,
        ComparisonProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projectedBlocks);
        ArgumentNullException.ThrowIfNull(projection);

        var mapped = new List<DiffBlock>();
        foreach (DiffBlock block in projectedBlocks)
        {
            mapped.AddRange(SplitMappedBlock(block, projection));
        }

        for (int i = 0; i < mapped.Count; i++)
        {
            DiffBlock block = mapped[i];
            if (block.Ordinal != i)
            {
                mapped[i] = block with { Ordinal = i };
            }
        }

        return mapped;
    }

    private static IEnumerable<DiffBlock> SplitMappedBlock(DiffBlock block, ComparisonProjection projection)
    {
        return block.Kind switch
        {
            DiffBlockKind.Equal => SplitEqual(block, projection),
            DiffBlockKind.Inserted => SplitOneSided(
                block.RightRange,
                projection.RightProjectedToOriginalLine,
                projection.ProjectedRightLines.Count,
                DiffBlockKind.Inserted,
                isLeft: false),
            DiffBlockKind.Deleted => SplitOneSided(
                block.LeftRange,
                projection.LeftProjectedToOriginalLine,
                projection.ProjectedLeftLines.Count,
                DiffBlockKind.Deleted,
                isLeft: true),
            DiffBlockKind.Modified => SplitModified(block, projection),
            _ => throw new InvalidOperationException($"Unknown block kind '{block.Kind}'."),
        };
    }

    private static IEnumerable<DiffBlock> SplitEqual(DiffBlock block, ComparisonProjection projection)
    {
        int count = block.LeftRange.Length;
        int index = 0;
        while (index < count)
        {
            int leftStart = projection.LeftProjectedToOriginalLine[block.LeftRange.StartIndex + index];
            int rightStart = projection.RightProjectedToOriginalLine[block.RightRange.StartIndex + index];
            int end = index + 1;

            while (end < count
                && projection.LeftProjectedToOriginalLine[block.LeftRange.StartIndex + end]
                    == projection.LeftProjectedToOriginalLine[block.LeftRange.StartIndex + end - 1] + 1
                && projection.RightProjectedToOriginalLine[block.RightRange.StartIndex + end]
                    == projection.RightProjectedToOriginalLine[block.RightRange.StartIndex + end - 1] + 1)
            {
                end++;
            }

            yield return new DiffBlock(
                -1,
                DiffBlockKind.Equal,
                new LineRange(leftStart, end - index),
                new LineRange(rightStart, end - index));
            index = end;
        }
    }

    private static IEnumerable<DiffBlock> SplitOneSided(
        LineRange projectedRange,
        IReadOnlyList<int> projectedToOriginal,
        int projectedLineCount,
        DiffBlockKind kind,
        bool isLeft)
    {
        foreach (LineRange originalRange in ToContiguousOriginalRanges(projectedRange, projectedToOriginal))
        {
            LineRange left = isLeft ? originalRange : LineRange.Empty(originalRange.StartIndex);
            LineRange right = isLeft ? LineRange.Empty(originalRange.StartIndex) : originalRange;
            yield return new DiffBlock(-1, kind, left, right);
        }

        if (projectedRange.Length == 0)
        {
            int anchor = MapInsertionAnchor(projectedRange.StartIndex, projectedToOriginal, projectedLineCount);
            LineRange left = isLeft ? LineRange.Empty(anchor) : LineRange.Empty(anchor);
            LineRange right = isLeft ? LineRange.Empty(anchor) : LineRange.Empty(anchor);
            yield return new DiffBlock(-1, kind, left, right);
        }
    }

    private static IEnumerable<DiffBlock> SplitModified(DiffBlock block, ComparisonProjection projection)
    {
        foreach (LineRange leftRange in ToContiguousOriginalRanges(block.LeftRange, projection.LeftProjectedToOriginalLine))
        {
            foreach (LineRange rightRange in ToContiguousOriginalRanges(block.RightRange, projection.RightProjectedToOriginalLine))
            {
                yield return new DiffBlock(-1, DiffBlockKind.Modified, leftRange, rightRange);
                yield break;
            }
        }

        if (block.LeftRange.Length == 0 && block.RightRange.Length == 0)
        {
            yield break;
        }

        LineRange left = block.LeftRange.Length == 0
            ? LineRange.Empty(MapInsertionAnchor(
                block.RightRange.StartIndex,
                projection.RightProjectedToOriginalLine,
                projection.ProjectedRightLines.Count))
            : ToContiguousOriginalRanges(block.LeftRange, projection.LeftProjectedToOriginalLine).First();

        LineRange right = block.RightRange.Length == 0
            ? LineRange.Empty(MapInsertionAnchor(
                block.LeftRange.StartIndex,
                projection.LeftProjectedToOriginalLine,
                projection.ProjectedLeftLines.Count))
            : ToContiguousOriginalRanges(block.RightRange, projection.RightProjectedToOriginalLine).First();

        yield return new DiffBlock(-1, DiffBlockKind.Modified, left, right);
    }

    private static IEnumerable<LineRange> ToContiguousOriginalRanges(
        LineRange projectedRange,
        IReadOnlyList<int> projectedToOriginal)
    {
        if (projectedRange.Length == 0)
        {
            yield break;
        }

        int rangeStart = projectedToOriginal[projectedRange.StartIndex];
        int rangeEnd = rangeStart;
        for (int projectedIndex = projectedRange.StartIndex + 1; projectedIndex < projectedRange.EndExclusive; projectedIndex++)
        {
            int originalIndex = projectedToOriginal[projectedIndex];
            if (originalIndex == rangeEnd + 1)
            {
                rangeEnd = originalIndex;
            }
            else
            {
                yield return new LineRange(rangeStart, rangeEnd - rangeStart + 1);
                rangeStart = originalIndex;
                rangeEnd = originalIndex;
            }
        }

        yield return new LineRange(rangeStart, rangeEnd - rangeStart + 1);
    }

    private static int MapInsertionAnchor(
        int projectedStartIndex,
        IReadOnlyList<int> projectedToOriginal,
        int projectedLineCount)
    {
        if (projectedToOriginal.Count == 0)
        {
            return 0;
        }

        if (projectedStartIndex <= 0)
        {
            return projectedToOriginal[0];
        }

        if (projectedStartIndex >= projectedLineCount)
        {
            return projectedToOriginal[^1] + 1;
        }

        return projectedToOriginal[projectedStartIndex];
    }
}
