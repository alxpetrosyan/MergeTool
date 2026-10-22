using MergeTool.Application.Operations;
using MergeTool.Core;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class OperationRequestCoordinatorTests
{
    [TestMethod]
    public void BeginRequest_IssuesStrictlyIncreasingIds()
    {
        OperationRequestCoordinator coordinator = new();

        ComparisonRequestId first = coordinator.BeginRequest();
        ComparisonRequestId second = coordinator.BeginRequest();
        ComparisonRequestId third = coordinator.BeginRequest();

        Assert.IsTrue(second.IsNewerThan(first));
        Assert.IsTrue(third.IsNewerThan(second));
    }

    [TestMethod]
    public void OlderRequest_BecomesStaleWhenNewerStarts()
    {
        OperationRequestCoordinator coordinator = new();

        ComparisonRequestId first = coordinator.BeginRequest();
        Assert.IsTrue(coordinator.IsCurrent(first));

        ComparisonRequestId second = coordinator.BeginRequest();
        Assert.IsFalse(coordinator.IsCurrent(first), "An older request must be identifiable as stale.");
        Assert.IsTrue(coordinator.IsCurrent(second));
    }

    [TestMethod]
    public void NoneRequest_IsNeverCurrent()
    {
        OperationRequestCoordinator coordinator = new();

        Assert.IsFalse(coordinator.IsCurrent(ComparisonRequestId.None));
    }

    [TestMethod]
    public void ConcurrentBeginRequest_NeverIssuesDuplicates()
    {
        OperationRequestCoordinator coordinator = new();
        long[] issued = new long[400];

        Parallel.For(0, issued.Length, i => issued[i] = coordinator.BeginRequest().Value);

        Assert.AreEqual(issued.Length, issued.Distinct().Count());
        Assert.AreEqual(issued.Length, coordinator.LatestIssued.Value);
    }
}
