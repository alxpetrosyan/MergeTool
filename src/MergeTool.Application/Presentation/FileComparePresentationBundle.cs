using MergeTool.Core;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Presentation;

/// <summary>
/// Minimap and collapse projections tied to a comparison revision. Views discard updates
/// when the session revision has moved on.
/// </summary>
public sealed class FileComparePresentationBundle
{
    public FileComparePresentationBundle(
        int comparisonRevision,
        MinimapModel minimap,
        CollapseProjectionState collapse)
    {
        if (comparisonRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(comparisonRevision));
        }

        ArgumentNullException.ThrowIfNull(minimap);
        ArgumentNullException.ThrowIfNull(collapse);

        ComparisonRevision = comparisonRevision;
        Minimap = minimap;
        Collapse = collapse;
    }

    public int ComparisonRevision { get; }

    public MinimapModel Minimap { get; }

    public CollapseProjectionState Collapse { get; }

    public static FileComparePresentationBundle? TryBuild(
        int comparisonRevision,
        IReadOnlyList<DiffBlock> blocks,
        int leftLineCount,
        int rightLineCount,
        int collapseThreshold = CollapsedViewProjectionBuilder.DefaultCollapseThreshold,
        int contextLines = CollapsedViewProjectionBuilder.DefaultContextLines)
    {
        if (comparisonRevision < 0 || blocks.Count == 0)
        {
            return null;
        }

        MinimapModel minimap = MinimapModelBuilder.Build(blocks, leftLineCount, rightLineCount);
        CollapsedViewProjection left = CollapsedViewProjectionBuilder.BuildForSide(
            blocks,
            leftLineCount,
            ComparisonSide.Left,
            collapseThreshold,
            contextLines);
        CollapsedViewProjection right = CollapsedViewProjectionBuilder.BuildForSide(
            blocks,
            rightLineCount,
            ComparisonSide.Right,
            collapseThreshold,
            contextLines);
        var collapse = new CollapseProjectionState(left, right);
        return new FileComparePresentationBundle(comparisonRevision, minimap, collapse);
    }
}
