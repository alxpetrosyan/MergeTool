using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text.Diff;
using MergeTool.Text.ThreeWay;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class ThreeWayOutputMergeSessionTests
{
    private static readonly PatienceLineDiffEngine Engine = new();

    [TestMethod]
    public void Initialize_StartsFromBase_AndMarksDifferenceUnresolved()
    {
        ThreeWayComparisonSession comparison = CreateComparisonSession();
        ThreeWayOutputMergeSession outputMerge = new();
        outputMerge.Initialize(comparison);

        Assert.AreEqual("a\r\nold\r\n", outputMerge.Output!.CurrentText);
        ThreeWayRegion difference = comparison.LatestResult!.Regions.Single(region => region.IsDifference);
        Assert.AreEqual(ThreeWayRegionResolutionState.Unresolved, outputMerge.Resolutions[difference.Ordinal].State);
    }

    [TestMethod]
    public void ApplyRegionChoice_CopiesLeftTextIntoOutput()
    {
        ThreeWayComparisonSession comparison = CreateComparisonSession();
        ThreeWayOutputMergeSession outputMerge = new();
        outputMerge.Initialize(comparison);
        int differenceOrdinal = comparison.LatestResult!.Regions.Single(region => region.IsDifference).Ordinal;

        OperationResult result = outputMerge.ApplyRegionChoice(
            comparison,
            differenceOrdinal,
            ThreeWayOutputSourceChoice.Left);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual("a\r\nleft\r\n", outputMerge.Output!.CurrentText);
        Assert.AreEqual(ThreeWayOutputSourceChoice.Left, outputMerge.Resolutions[differenceOrdinal].ChosenSource);
    }

    [TestMethod]
    public void ManualOutputEdit_MarksDifferenceRegionsStale()
    {
        ThreeWayComparisonSession comparison = CreateComparisonSession();
        ThreeWayOutputMergeSession outputMerge = new();
        outputMerge.Initialize(comparison);
        int differenceOrdinal = comparison.LatestResult!.Regions.Single(region => region.IsDifference).Ordinal;
        outputMerge.ApplyRegionChoice(comparison, differenceOrdinal, ThreeWayOutputSourceChoice.Left);

        outputMerge.NotifyManualOutputEdit(comparison, "manual\r\n");

        Assert.IsTrue(outputMerge.HasManualOutputEdit);
        Assert.AreEqual(ThreeWayRegionResolutionState.Stale, outputMerge.Resolutions[differenceOrdinal].State);
        Assert.HasCount(1, outputMerge.UnresolvedDifferenceRegionOrdinals(comparison.LatestResult));
    }

    [TestMethod]
    public void ApplyRegionChoice_DoesNotMutateSourceDocuments()
    {
        ThreeWayComparisonSession comparison = CreateComparisonSession();
        string baseText = comparison.Base.CurrentText;
        string leftText = comparison.Left.CurrentText;
        string rightText = comparison.Right.CurrentText;
        ThreeWayOutputMergeSession outputMerge = new();
        outputMerge.Initialize(comparison);
        int differenceOrdinal = comparison.LatestResult!.Regions.Single(region => region.IsDifference).Ordinal;

        outputMerge.ApplyRegionChoice(comparison, differenceOrdinal, ThreeWayOutputSourceChoice.Left);

        Assert.AreEqual(baseText, comparison.Base.CurrentText);
        Assert.AreEqual(leftText, comparison.Left.CurrentText);
        Assert.AreEqual(rightText, comparison.Right.CurrentText);
    }

    private static ThreeWayComparisonSession CreateComparisonSession()
    {
        ThreeWayComparisonSession session = new(
            SessionTestHelper.SessionFromText("a\r\nold\r\n", fileName: "base.txt"),
            SessionTestHelper.SessionFromText("a\r\nleft\r\n", fileName: "left.txt"),
            SessionTestHelper.SessionFromText("a\r\nold\r\n", fileName: "right.txt"),
            Engine);
        session.RecompareAsync(TextComparisonOptions.Default, CancellationToken.None).GetAwaiter().GetResult();
        return session;
    }
}
