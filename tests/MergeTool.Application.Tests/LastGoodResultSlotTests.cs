using MergeTool.Application.Operations;
using MergeTool.Core;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class LastGoodResultSlotTests
{
    [TestMethod]
    public void SuccessfulCurrentCompletion_IsStored()
    {
        OperationRequestCoordinator coordinator = new();
        LastGoodResultSlot<string> slot = new(coordinator);

        ComparisonRequestId request = coordinator.BeginRequest();
        bool applied = slot.TryApply(request, OperationResult<string>.Completed("result-1"));

        Assert.IsTrue(applied);
        Assert.IsTrue(slot.TryGetLastGoodValue(out string value));
        Assert.AreEqual("result-1", value);
        Assert.AreEqual(OperationStatus.Completed, slot.LastTerminalStatus);
    }

    [TestMethod]
    public void StaleCompletion_IsDiscardedEntirely()
    {
        OperationRequestCoordinator coordinator = new();
        LastGoodResultSlot<string> slot = new(coordinator);

        ComparisonRequestId stale = coordinator.BeginRequest();
        ComparisonRequestId current = coordinator.BeginRequest();

        Assert.IsFalse(slot.TryApply(stale, OperationResult<string>.Completed("stale value")));
        Assert.IsFalse(slot.TryGetLastGoodValue(out _), "A stale completion must not update state.");
        Assert.AreEqual(OperationStatus.Idle, slot.LastTerminalStatus);

        Assert.IsTrue(slot.TryApply(current, OperationResult<string>.Completed("current value")));
        Assert.IsTrue(slot.TryGetLastGoodValue(out string value));
        Assert.AreEqual("current value", value);
    }

    [TestMethod]
    public void Cancellation_DoesNotOverwriteLastGoodValue()
    {
        OperationRequestCoordinator coordinator = new();
        LastGoodResultSlot<string> slot = new(coordinator);

        ComparisonRequestId first = coordinator.BeginRequest();
        slot.TryApply(first, OperationResult<string>.Completed("good value"));

        ComparisonRequestId second = coordinator.BeginRequest();
        bool applied = slot.TryApply(second, OperationResult<string>.Cancelled());

        Assert.IsTrue(applied, "A current cancellation is a valid terminal state.");
        Assert.AreEqual(OperationStatus.Cancelled, slot.LastTerminalStatus);
        Assert.IsTrue(slot.TryGetLastGoodValue(out string value));
        Assert.AreEqual("good value", value, "Cancelling must leave the last completed result visible.");
    }

    [TestMethod]
    public void Failure_DoesNotOverwriteLastGoodValue()
    {
        OperationRequestCoordinator coordinator = new();
        LastGoodResultSlot<string> slot = new(coordinator);

        ComparisonRequestId first = coordinator.BeginRequest();
        slot.TryApply(first, OperationResult<string>.Completed("good value"));

        ComparisonRequestId second = coordinator.BeginRequest();
        slot.TryApply(second, OperationResult<string>.Failed(Diagnostic.Error("MT-TST-020", "The comparison failed.")));

        Assert.AreEqual(OperationStatus.Failed, slot.LastTerminalStatus);
        Assert.IsTrue(slot.TryGetLastGoodValue(out string value));
        Assert.AreEqual("good value", value);
    }

    [TestMethod]
    public void CancellationConvention_ProducesResultNotException()
    {
        // Documents the boundary convention: use cases catch OperationCanceledException
        // and convert it to a Cancelled result.
        static OperationResult<int> RunUseCase(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                return OperationResult<int>.Completed(1);
            }
            catch (OperationCanceledException)
            {
                return OperationResult<int>.Cancelled();
            }
        }

        using CancellationTokenSource cts = new();
        cts.Cancel();

        OperationResult<int> result = RunUseCase(cts.Token);

        Assert.AreEqual(OperationStatus.Cancelled, result.Status);
    }
}
