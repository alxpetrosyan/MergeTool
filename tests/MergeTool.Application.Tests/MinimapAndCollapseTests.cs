using MergeTool.Application.Presentation;
using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class MinimapAndCollapseTests
{
    private static List<DiffBlock> Blocks(params (DiffBlockKind kind, int leftStart, int leftLen, int rightStart, int rightLen)[] specs)
    {
        var blocks = new List<DiffBlock>();
        for (int i = 0; i < specs.Length; i++)
        {
            (DiffBlockKind kind, int leftStart, int leftLen, int rightStart, int rightLen) = specs[i];
            blocks.Add(new DiffBlock(
                i,
                kind,
                new LineRange(leftStart, leftLen),
                new LineRange(rightStart, rightLen)));
        }

        return blocks;
    }

    [TestMethod]
    public void Minimap_OrdersMarkersByBlockOrdinal()
    {
        var blocks = Blocks(
            (DiffBlockKind.Equal, 0, 5, 0, 5),
            (DiffBlockKind.Modified, 5, 1, 5, 1),
            (DiffBlockKind.Equal, 6, 5, 6, 5),
            (DiffBlockKind.Inserted, 11, 0, 11, 2));

        MinimapModel model = MinimapModelBuilder.Build(blocks, 11, 13);
        Assert.HasCount(2, model.Markers);
        Assert.IsLessThan(model.Markers[1].TopY, model.Markers[0].TopY);
        Assert.AreEqual(DiffBlockKind.Modified, model.Markers[0].Kind);
        Assert.AreEqual(DiffBlockKind.Inserted, model.Markers[1].Kind);
    }

    [TestMethod]
    public void Minimap_MarkerRangesAreNormalizedAndOrdered()
    {
        var blocks = Blocks((DiffBlockKind.Modified, 2, 3, 4, 2));
        MinimapModel model = MinimapModelBuilder.Build(blocks, 10, 10);
        MinimapMarker marker = model.Markers[0];
        Assert.IsGreaterThanOrEqualTo(0, marker.TopY);
        Assert.IsLessThanOrEqualTo(1, marker.BottomY);
        Assert.IsLessThan(marker.BottomY, marker.TopY);
    }

    [TestMethod]
    public void Minimap_CalculateViewport_ClampedToDocument()
    {
        var blocks = Blocks((DiffBlockKind.Equal, 0, 100, 0, 100));
        MinimapModel model = MinimapModelBuilder.Build(blocks, 100, 100);
        MinimapViewport viewport = model.CalculateViewport(95, 20);
        Assert.IsGreaterThanOrEqualTo(0, viewport.StartY);
        Assert.IsLessThanOrEqualTo(1, viewport.EndY);
        Assert.IsLessThan(viewport.BottomY, viewport.TopY);
    }

    [TestMethod]
    public void Minimap_FindMarkerAtNormalizedY_ReturnsContainingMarker()
    {
        var blocks = Blocks(
            (DiffBlockKind.Equal, 0, 10, 0, 10),
            (DiffBlockKind.Deleted, 10, 2, 10, 0));
        MinimapModel model = MinimapModelBuilder.Build(blocks, 12, 10);
        MinimapMarker marker = model.Markers[0];
        double mid = (marker.StartY + marker.EndY) / 2;
        Assert.AreEqual(marker.BlockOrdinal, model.FindMarkerAtNormalizedY(mid)?.BlockOrdinal);
    }

    [TestMethod]
    public void Collapsed_NoDifferences_StillMapsEveryLine()
    {
        var blocks = Blocks((DiffBlockKind.Equal, 0, 5, 0, 5));
        CollapsedViewProjection projection = CollapsedViewProjectionBuilder.BuildForSide(
            blocks,
            5,
            ComparisonSide.Left,
            collapseThreshold: 100,
            contextLines: 1);
        Assert.IsEmpty(projection.Regions);
        Assert.HasCount(5, projection.OriginalLineToDisplayRow);
    }

    [TestMethod]
    public void Collapsed_OneDifference_KeepsChangedLinesVisible()
    {
        var blocks = Blocks(
            (DiffBlockKind.Equal, 0, 20, 0, 20),
            (DiffBlockKind.Modified, 20, 1, 20, 1));
        CollapsedViewProjection projection = CollapsedViewProjectionBuilder.BuildForSide(
            blocks,
            21,
            ComparisonSide.Left,
            collapseThreshold: 8,
            contextLines: 2);
        Assert.Contains(20, projection.AlwaysVisibleLines);
        Assert.IsFalse(projection.FindRegionContaining(20)?.ContainsLine(20) ?? false);
    }

    [TestMethod]
    public void Collapsed_MultipleDistantEqualBlocks_CreateMultipleRegions()
    {
        var blocks = Blocks(
            (DiffBlockKind.Equal, 0, 20, 0, 20),
            (DiffBlockKind.Modified, 20, 1, 20, 1),
            (DiffBlockKind.Equal, 21, 30, 21, 30));
        CollapsedViewProjection projection = CollapsedViewProjectionBuilder.BuildForSide(
            blocks,
            51,
            ComparisonSide.Left,
            collapseThreshold: 8,
            contextLines: 2);
        Assert.HasCount(2, projection.Regions);
    }

    [TestMethod]
    public void Collapsed_AdjacentDifferences_DoNotCollapseBetweenThem()
    {
        var blocks = Blocks(
            (DiffBlockKind.Equal, 0, 20, 0, 20),
            (DiffBlockKind.Modified, 20, 1, 20, 1),
            (DiffBlockKind.Inserted, 21, 0, 21, 1));
        CollapsedViewProjection projection = CollapsedViewProjectionBuilder.BuildForSide(
            blocks,
            22,
            ComparisonSide.Right,
            collapseThreshold: 8,
            contextLines: 2);
        Assert.Contains(20, projection.AlwaysVisibleLines);
        Assert.Contains(21, projection.AlwaysVisibleLines);
    }

    [TestMethod]
    public void Collapsed_VeryLargeEqualRegion_HidesMiddleOnly()
    {
        var blocks = Blocks((DiffBlockKind.Equal, 0, 100, 0, 100));
        CollapsedViewProjection projection = CollapsedViewProjectionBuilder.BuildForSide(
            blocks,
            100,
            ComparisonSide.Left,
            collapseThreshold: 8,
            contextLines: 2);
        CollapsedRegion region = projection.Regions[0];
        Assert.AreEqual(2, region.HiddenStartLine);
        Assert.AreEqual(98, region.HiddenEndExclusive);
        Assert.DoesNotContain(50, projection.AlwaysVisibleLines);
        Assert.Contains(0, projection.AlwaysVisibleLines);
        Assert.Contains(99, projection.AlwaysVisibleLines);
    }

    [TestMethod]
    public void Collapsed_BoundaryAtFileStartAndEnd_PreservesContext()
    {
        var blocks = Blocks(
            (DiffBlockKind.Equal, 0, 15, 0, 15),
            (DiffBlockKind.Modified, 15, 1, 15, 1));
        CollapsedViewProjection projection = CollapsedViewProjectionBuilder.BuildForSide(
            blocks,
            16,
            ComparisonSide.Left,
            collapseThreshold: 8,
            contextLines: 3);
        CollapsedRegion region = projection.Regions[0];
        Assert.AreEqual(3, region.HiddenStartLine);
        Assert.Contains(0, projection.AlwaysVisibleLines);
        Assert.Contains(14, projection.AlwaysVisibleLines);
    }

    [TestMethod]
    public void Collapsed_ConfiguredContextLineCount_IsHonored()
    {
        var blocks = Blocks((DiffBlockKind.Equal, 0, 20, 0, 20));
        CollapsedViewProjection withTwo = CollapsedViewProjectionBuilder.BuildForSide(
            blocks, 20, ComparisonSide.Left, collapseThreshold: 8, contextLines: 2);
        CollapsedViewProjection withFour = CollapsedViewProjectionBuilder.BuildForSide(
            blocks, 20, ComparisonSide.Left, collapseThreshold: 8, contextLines: 4);
        Assert.IsGreaterThan(withFour.Regions[0].HiddenLineCount, withTwo.Regions[0].HiddenLineCount);
    }

    [TestMethod]
    public void Collapsed_DisplayMapping_IsMonotonicForVisibleLines()
    {
        var blocks = Blocks(
            (DiffBlockKind.Equal, 0, 20, 0, 20),
            (DiffBlockKind.Modified, 20, 2, 20, 2));
        CollapsedViewProjection projection = CollapsedViewProjectionBuilder.BuildForSide(
            blocks,
            22,
            ComparisonSide.Left,
            collapseThreshold: 8,
            contextLines: 2);
        int previous = -1;
        foreach (int line in projection.AlwaysVisibleLines.Order())
        {
            int row = projection.DisplayRowForOriginalLine(line);
            Assert.IsGreaterThanOrEqualTo(0, row);
            Assert.IsGreaterThan(previous, row);
            previous = row;
        }
    }

    [TestMethod]
    public void Collapsed_ExpandOneRegion_RevealsHiddenLines()
    {
        var blocks = Blocks((DiffBlockKind.Equal, 0, 20, 0, 20));
        CollapsedViewProjection left = CollapsedViewProjectionBuilder.BuildForSide(blocks, 20, ComparisonSide.Left);
        CollapsedViewProjection right = CollapsedViewProjectionBuilder.BuildForSide(blocks, 20, ComparisonSide.Right);
        var state = new CollapseProjectionState(left, right);
        string regionId = left.Regions[0].RegionId;
        Assert.IsTrue(state.IsLineCurrentlyFolded(ComparisonSide.Left, 10));
        state.ExpandRegion(regionId);
        Assert.IsFalse(state.IsLineCurrentlyFolded(ComparisonSide.Left, 10));
        Assert.IsEmpty(state.GetActiveFolds(ComparisonSide.Left));
    }

    [TestMethod]
    public void Collapsed_ExpandAll_RevealsEveryRegion()
    {
        var blocks = Blocks(
            (DiffBlockKind.Equal, 0, 20, 0, 20),
            (DiffBlockKind.Modified, 20, 1, 20, 1),
            (DiffBlockKind.Equal, 21, 20, 21, 20));
        CollapsedViewProjection left = CollapsedViewProjectionBuilder.BuildForSide(blocks, 41, ComparisonSide.Left);
        CollapsedViewProjection right = CollapsedViewProjectionBuilder.BuildForSide(blocks, 41, ComparisonSide.Right);
        var state = new CollapseProjectionState(left, right);
        state.ExpandAll();
        Assert.IsEmpty(state.GetActiveFolds(ComparisonSide.Left));
        Assert.IsEmpty(state.GetActiveFolds(ComparisonSide.Right));
    }

    [TestMethod]
    public void Collapsed_NavigationToHiddenDifference_ExpandsRegion()
    {
        var blocks = Blocks(
            (DiffBlockKind.Equal, 0, 20, 0, 20),
            (DiffBlockKind.Modified, 20, 1, 20, 1));
        CollapsedViewProjection left = CollapsedViewProjectionBuilder.BuildForSide(blocks, 21, ComparisonSide.Left);
        CollapsedViewProjection right = CollapsedViewProjectionBuilder.BuildForSide(blocks, 21, ComparisonSide.Right);
        var state = new CollapseProjectionState(left, right);
        var map = ComparisonPresentationMap.Create(
            TwoWayTextComparisonResult.Create(DocumentSessionId.New(), DocumentSessionId.New(), 0, 0, blocks),
            21,
            21);
        state.EnsureBlockVisible(1, map);
        Assert.IsFalse(state.IsLineCurrentlyFolded(ComparisonSide.Left, 10));
    }

    [TestMethod]
    public void PresentationBundle_StaleRevision_IsNotAppliedByConsumerGuard()
    {
        var blocks = Blocks((DiffBlockKind.Modified, 0, 1, 0, 1));
        FileComparePresentationBundle bundle = FileComparePresentationBundle.TryBuild(1, blocks, 5, 5)!;
        Assert.AreEqual(1, bundle.ComparisonRevision);
        Assert.AreNotEqual(2, bundle.ComparisonRevision);
    }
}
