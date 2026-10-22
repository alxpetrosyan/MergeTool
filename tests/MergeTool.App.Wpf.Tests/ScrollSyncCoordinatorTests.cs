using MergeTool.App.Wpf.Rendering;
using MergeTool.Core;

namespace MergeTool.App.Wpf.Tests;

[TestClass]
public sealed class ScrollSyncCoordinatorTests
{
    private const double LineHeight = 16.0;

    private static ScrollSyncCoordinator CreateCoordinator(out int rightCount)
    {
        // Left has 3 extra lines at the top; below that the documents match.
        string left = "x1\r\nx2\r\nx3\r\na\r\nb\r\nc\r\nd\r\n";
        string right = "a\r\nb\r\nc\r\nd\r\n";
        var (map, _, rc) = RenderTestHelper.MapFor(left, right);
        rightCount = rc;
        return new ScrollSyncCoordinator { Map = map };
    }

    [TestMethod]
    public void WithoutMapOrGeometry_NoSyncHappens()
    {
        ScrollSyncCoordinator coordinator = new();
        coordinator.SetActiveSide(ComparisonSide.Left);
        Assert.IsNull(coordinator.GetCounterpartOffset(ComparisonSide.Left, 100, LineHeight, 0), "No map: no sync.");

        coordinator = CreateCoordinator(out _);
        coordinator.SetActiveSide(ComparisonSide.Left);
        Assert.IsNull(coordinator.GetCounterpartOffset(ComparisonSide.Left, 100, 0, 0), "Unknown line height: no sync.");
    }

    [TestMethod]
    public void OnlyTheActivePane_DrivesTheCounterpart()
    {
        ScrollSyncCoordinator coordinator = CreateCoordinator(out _);

        coordinator.SetActiveSide(null);
        Assert.IsNull(coordinator.GetCounterpartOffset(ComparisonSide.Left, 5 * LineHeight, LineHeight, 0));

        coordinator.SetActiveSide(ComparisonSide.Left);
        Assert.IsNotNull(coordinator.GetCounterpartOffset(ComparisonSide.Left, 5 * LineHeight, LineHeight, 0));
        Assert.IsNull(
            coordinator.GetCounterpartOffset(ComparisonSide.Right, 5 * LineHeight, LineHeight, 0),
            "A scroll event echoing back from the follower pane must never re-drive the active pane.");
    }

    [TestMethod]
    public void ActiveLeftScroll_MapsThroughThePresentationMap()
    {
        ScrollSyncCoordinator coordinator = CreateCoordinator(out _);
        coordinator.SetActiveSide(ComparisonSide.Left);

        // Left line 4 ("b") corresponds to right line 1.
        double? target = coordinator.GetCounterpartOffset(ComparisonSide.Left, 4 * LineHeight, LineHeight, 0);
        Assert.AreEqual(1 * LineHeight, target!.Value, 0.001);

        // Left line 0 is inside the deleted-only region; it anchors to the right boundary (line 0).
        Assert.IsNull(
            coordinator.GetCounterpartOffset(ComparisonSide.Left, 0, LineHeight, 0),
            "Counterpart already at the anchor: no movement.");
    }

    [TestMethod]
    public void TargetsWithinHalfALine_AreSuppressed()
    {
        ScrollSyncCoordinator coordinator = CreateCoordinator(out _);
        coordinator.SetActiveSide(ComparisonSide.Left);

        double? target = coordinator.GetCounterpartOffset(
            ComparisonSide.Left, 4 * LineHeight, LineHeight, currentCounterpartOffset: 1 * LineHeight + 3);
        Assert.IsNull(target, "A follower already within half a line must not be nudged (prevents jitter loops).");
    }

    [TestMethod]
    public void DifferentLengthDocuments_ClampToValidLines()
    {
        ScrollSyncCoordinator coordinator = CreateCoordinator(out int rightCount);
        coordinator.SetActiveSide(ComparisonSide.Left);

        // Scrolling far past the left document still yields a valid right anchor.
        double? target = coordinator.GetCounterpartOffset(ComparisonSide.Left, 500 * LineHeight, LineHeight, 0);
        Assert.IsNotNull(target);
        int targetLine = (int)Math.Round(target.Value / LineHeight);
        Assert.IsTrue(targetLine >= 0 && targetLine < rightCount, "Anchor must be a valid right line.");
    }
}
