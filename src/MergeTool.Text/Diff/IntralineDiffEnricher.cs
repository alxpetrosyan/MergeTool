using MergeTool.Text;

namespace MergeTool.Text.Diff;

/// <summary>Enriches a two-way comparison result with optional intraline data for modified line pairs.</summary>
public static class IntralineDiffEnricher
{
    private static readonly BoundedIntralineDiffCalculator Calculator = new();

    public static TwoWayTextComparisonResult Enrich(
        TwoWayTextComparisonResult result,
        IReadOnlyList<TextLine> leftLines,
        IReadOnlyList<TextLine> rightLines,
        TextComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(leftLines);
        ArgumentNullException.ThrowIfNull(rightLines);
        ArgumentNullException.ThrowIfNull(options);

        if (!options.EnableIntralineDiff)
        {
            return result;
        }

        IntralineDiffBounds bounds = options.IntralineBounds;
        var inlineDiffs = new List<ModifiedLineInlineDiff>();

        foreach (DiffBlock block in result.Blocks)
        {
            if (block.Kind != DiffBlockKind.Modified)
            {
                continue;
            }

            int pairCount = Math.Min(block.LeftRange.Length, block.RightRange.Length);
            for (int offset = 0; offset < pairCount; offset++)
            {
                int leftIndex = block.LeftRange.StartIndex + offset;
                int rightIndex = block.RightRange.StartIndex + offset;

                InlineDiffLinePairResult pairResult = Calculator.Compute(
                    leftLines[leftIndex].Content,
                    rightLines[rightIndex].Content,
                    leftIndex,
                    rightIndex,
                    bounds);

                inlineDiffs.Add(new ModifiedLineInlineDiff(block.Ordinal, pairResult));
            }
        }

        return result.WithInlineDiffs(inlineDiffs);
    }
}
