using MergeTool.Core;

namespace MergeTool.Core.Tests;

[TestClass]
public sealed class OperationResultTests
{
    [TestMethod]
    public void Completed_CarriesValueAndDiagnostics()
    {
        Diagnostic warning = Diagnostic.Warning("MT-TST-010", "Partial data");
        OperationResult<int> result = OperationResult<int>.Completed(42, [warning]);

        Assert.AreEqual(OperationStatus.Completed, result.Status);
        Assert.AreEqual(42, result.Value);
        Assert.HasCount(1, result.Diagnostics);
        Assert.IsFalse(result.HasErrors);
    }

    [TestMethod]
    public void Cancelled_IsSafeStateWithoutValue()
    {
        OperationResult<string> result = OperationResult<string>.Cancelled();

        Assert.AreEqual(OperationStatus.Cancelled, result.Status);
        Assert.IsTrue(result.IsCancelled);
        Assert.IsFalse(result.TryGetValue(out _));
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = result.Value);
    }

    [TestMethod]
    public void Failed_ExposesErrorDiagnostics()
    {
        OperationResult<string> result = OperationResult<string>.Failed(
            Diagnostic.Error("MT-TST-011", "The operation failed."));

        Assert.AreEqual(OperationStatus.Failed, result.Status);
        Assert.IsTrue(result.HasErrors);
        Assert.IsFalse(result.TryGetValue(out _));
    }

    [TestMethod]
    public void ValuelessResult_SupportsAllTerminalStates()
    {
        Assert.IsTrue(OperationResult.Completed().IsCompleted);
        Assert.IsTrue(OperationResult.Cancelled().IsCancelled);
        Assert.IsTrue(OperationResult.Failed(Diagnostic.Error("MT-TST-012", "Failed.")).IsFailed);
    }

    [TestMethod]
    public void Diagnostics_AreSnapshottedAtCreation()
    {
        List<Diagnostic> source = [Diagnostic.Info("MT-TST-013", "One")];
        OperationResult result = OperationResult.Completed(source);
        source.Add(Diagnostic.Info("MT-TST-014", "Two"));

        Assert.HasCount(1, result.Diagnostics);
    }
}
