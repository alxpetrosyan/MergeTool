using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text.Diff;
using MergeTool.Text.ThreeWay;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class ThreeWayComparisonSessionTests
{
    private static readonly PatienceLineDiffEngine Engine = new();

    [TestMethod]
    public async Task Recompare_BuildsDifferenceRegionOrdinals()
    {
        ThreeWayComparisonSession session = new(
            SessionTestHelper.SessionFromText("a\r\nold\r\nc\r\n", fileName: "base.txt"),
            SessionTestHelper.SessionFromText("a\r\nleft\r\nc\r\n", fileName: "left.txt"),
            SessionTestHelper.SessionFromText("a\r\nright\r\nc\r\n", fileName: "right.txt"),
            Engine);

        OperationStatus status = await session.RecompareAsync(TextComparisonOptions.Default, CancellationToken.None);

        Assert.AreEqual(OperationStatus.Completed, status);
        Assert.IsFalse(session.IsStale);
        Assert.IsNotNull(session.LatestResult);
        Assert.IsNotEmpty(session.LatestResult.DifferenceRegionOrdinals);
        Assert.IsTrue(session.LatestResult.Regions.Any(region => region.Kind == ThreeWayRegionKind.Divergent));
    }
}
