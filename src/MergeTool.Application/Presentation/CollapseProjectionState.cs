using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Presentation;

/// <summary>
/// Mutable expand/collapse state layered on immutable projections. Navigation and merge
/// continue to use original line indices from the comparison session.
/// </summary>
public sealed class CollapseProjectionState
{
    private readonly CollapsedViewProjection _left;
    private readonly CollapsedViewProjection _right;
    private readonly HashSet<string> _expandedRegions = new(StringComparer.Ordinal);

    public CollapseProjectionState(CollapsedViewProjection left, CollapsedViewProjection right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        _left = left;
        _right = right;
    }

    public CollapsedViewProjection Left => _left;

    public CollapsedViewProjection Right => _right;

    public IReadOnlyCollection<string> ExpandedRegionIds => _expandedRegions;

    public bool IsRegionExpanded(string regionId) => _expandedRegions.Contains(regionId);

    public bool IsLineCurrentlyFolded(ComparisonSide side, int originalLine)
    {
        CollapsedViewProjection projection = side == ComparisonSide.Left ? _left : _right;
        CollapsedRegion? region = projection.FindRegionContaining(originalLine);
        return region is not null && !_expandedRegions.Contains(region.RegionId);
    }

    public void ExpandRegion(string regionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regionId);
        _expandedRegions.Add(regionId);
    }

    public void ExpandAll()
    {
        foreach (CollapsedRegion region in _left.Regions)
        {
            _expandedRegions.Add(region.RegionId);
        }

        foreach (CollapsedRegion region in _right.Regions)
        {
            _expandedRegions.Add(region.RegionId);
        }
    }

    public void EnsureLineVisible(ComparisonSide side, int originalLine)
    {
        CollapsedViewProjection projection = side == ComparisonSide.Left ? _left : _right;
        CollapsedRegion? region = projection.FindRegionContaining(originalLine);
        if (region is not null)
        {
            _expandedRegions.Add(region.RegionId);
        }
    }

    public void EnsureBlockVisible(int blockOrdinal, ComparisonPresentationMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (!map.TryGetBlock(blockOrdinal, out DiffBlock? block) || block is null)
        {
            return;
        }

        ExpandPrecedingCollapsedRegions(_left, blockOrdinal);
        ExpandPrecedingCollapsedRegions(_right, blockOrdinal);
        EnsureLineVisible(ComparisonSide.Left, block.LeftRange.StartIndex);
        EnsureLineVisible(ComparisonSide.Right, block.RightRange.StartIndex);
    }

    private void ExpandPrecedingCollapsedRegions(CollapsedViewProjection projection, int blockOrdinal)
    {
        foreach (CollapsedRegion region in projection.Regions)
        {
            if (region.BlockOrdinal < blockOrdinal)
            {
                _expandedRegions.Add(region.RegionId);
            }
        }
    }

    public IReadOnlyList<FoldSpecification> GetActiveFolds(ComparisonSide side)
    {
        CollapsedViewProjection projection = side == ComparisonSide.Left ? _left : _right;
        var folds = new List<FoldSpecification>();
        foreach (CollapsedRegion region in projection.Regions)
        {
            if (_expandedRegions.Contains(region.RegionId))
            {
                continue;
            }

            folds.Add(new FoldSpecification(
                region.HiddenStartLine,
                region.HiddenEndExclusive,
                region.RegionId,
                $"… {region.HiddenLineCount} unchanged lines …"));
        }

        return folds;
    }
}

public sealed record FoldSpecification(
    int StartLine,
    int EndLineExclusive,
    string RegionId,
    string Title);
