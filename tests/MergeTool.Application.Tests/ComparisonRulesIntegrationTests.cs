using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class ComparisonRulesIntegrationTests
{
    private static TwoWayComparisonSession CreateSession(string leftText, string rightText)
        => new(
            SessionTestHelper.SessionFromText(leftText, fileName: "left.txt"),
            SessionTestHelper.SessionFromText(rightText, fileName: "right.txt"),
            new PatienceLineDiffEngine());

    [TestMethod]
    public async Task IgnoreCase_ProducesEqualBlockInOriginalSpace()
    {
        TwoWayComparisonSession session = CreateSession("Alpha\r\n", "alpha\r\n");
        var options = new TextComparisonOptions(enableIntralineDiff: true, ignoreCase: true);

        OperationStatus status = await session.RecompareAsync(options, CancellationToken.None);

        Assert.AreEqual(OperationStatus.Completed, status);
        Assert.IsFalse(session.LatestResult!.Summary.HasDifferences);
        Assert.AreEqual(options, session.AppliedComparisonOptions);
    }

    [TestMethod]
    public async Task InvalidRegex_FailsWithoutUpdatingAppliedRules()
    {
        TwoWayComparisonSession session = CreateSession("a\r\n", "b\r\n");
        await session.RecompareAsync(TextComparisonOptions.WithIntralineDiff, CancellationToken.None);
        Assert.IsTrue(session.LatestResult!.Summary.HasDifferences);

        OperationStatus status = await session.RecompareAsync(
            new TextComparisonOptions(enableIntralineDiff: true, ignoredLinePattern: "["),
            CancellationToken.None);

        Assert.AreEqual(OperationStatus.Failed, status);
        Assert.AreEqual(ComparisonProjector.InvalidRegexCode, session.Diagnostics.Single().Code);
        Assert.IsTrue(session.LatestResult.Summary.HasDifferences, "Previous result must remain visible.");
    }

    [TestMethod]
    public async Task Merge_WithIgnoreCaseRules_CopiesOriginalSourceText()
    {
        TwoWayComparisonSession session = CreateSession("A\r\n", "a\r\n");
        var options = new TextComparisonOptions(enableIntralineDiff: true, ignoreCase: true);
        Assert.AreEqual(OperationStatus.Completed, await session.RecompareAsync(options, CancellationToken.None));
        Assert.IsFalse(session.LatestResult!.Summary.HasDifferences);

        session.NotifyDocumentEdited(ComparisonSide.Left, "A\r\nB\r\n");
        Assert.AreEqual(OperationStatus.Completed, await session.RecompareAsync(options, CancellationToken.None));
        int block = session.PresentationMap!.DifferenceBlockOrdinals.Single();

        ManualTwoWayMergeService merge = new();
        OperationResult result = merge.CopyBlock(session, block, ComparisonSide.Left);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual("a\r\nB\r\n", session.Right.CurrentText, "Merge copies original source casing for changed lines only.");
        Assert.AreEqual("B", session.Right.CurrentLines[1].Content);
    }
}
