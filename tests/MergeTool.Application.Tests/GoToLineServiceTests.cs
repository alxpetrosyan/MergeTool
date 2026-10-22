using MergeTool.Application.Navigation;
using MergeTool.Core;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class GoToLineServiceTests
{
    [TestMethod]
    public void TryResolve_ValidLine_ReturnsZeroBasedIndex()
    {
        OperationResult<GoToLineTarget> result = GoToLineService.TryResolve(3, 10, ComparisonSide.Right);
        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual(2, result.Value.ZeroBasedLineIndex);
        Assert.AreEqual(ComparisonSide.Right, result.Value.Side);
    }

    [TestMethod]
    public void TryResolve_AboveRange_ReturnsDiagnostic()
    {
        OperationResult<GoToLineTarget> result = GoToLineService.TryResolve(11, 10, ComparisonSide.Left);
        Assert.IsTrue(result.IsFailed);
        Assert.AreEqual(GoToLineService.OutOfRangeCode, result.Diagnostics.Single().Code);
    }

    [TestMethod]
    public void TryResolve_BelowOne_ReturnsDiagnostic()
    {
        OperationResult<GoToLineTarget> result = GoToLineService.TryResolve(0, 10, ComparisonSide.Left);
        Assert.IsTrue(result.IsFailed);
        Assert.AreEqual(GoToLineService.InvalidLineNumberCode, result.Diagnostics.Single().Code);
    }

    [TestMethod]
    public void TryResolve_EmptyDocument_ReturnsDiagnostic()
    {
        OperationResult<GoToLineTarget> result = GoToLineService.TryResolve(1, 0, ComparisonSide.Left);
        Assert.IsTrue(result.IsFailed);
        Assert.AreEqual(GoToLineService.OutOfRangeCode, result.Diagnostics.Single().Code);
    }
}
