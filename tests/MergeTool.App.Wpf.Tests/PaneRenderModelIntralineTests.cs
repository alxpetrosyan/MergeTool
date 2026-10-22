using MergeTool.App.Wpf.Rendering;
using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.Diff;

namespace MergeTool.App.Wpf.Tests;

internal static class IntralineRenderTestHelper
{
    public static (ComparisonPresentationMap Map, TwoWayTextComparisonResult Result, int LeftCount, int RightCount) MapWithIntraline(
        string leftText,
        string rightText)
    {
        var leftLines = TextLineSplitter.Split(leftText).Lines;
        var rightLines = TextLineSplitter.Split(rightText).Lines;
        var blocks = new PatienceLineDiffEngine().Compare(
            leftLines, rightLines, TextComparisonOptions.WithIntralineDiff, CancellationToken.None);
        TwoWayTextComparisonResult result = TwoWayTextComparisonResult.Create(
            DocumentSessionId.New(), DocumentSessionId.New(), 0, 0, blocks);
        result = IntralineDiffEnricher.Enrich(result, leftLines, rightLines, TextComparisonOptions.WithIntralineDiff);
        return (
            ComparisonPresentationMap.Create(result, leftLines.Count, rightLines.Count),
            result,
            leftLines.Count,
            rightLines.Count);
    }
}

[TestClass]
public sealed class PaneRenderModelIntralineTests
{
    [TestMethod]
    public void WholeLineFallback_PreservesModifiedLineClassification()
    {
        string longToken = new('z', IntralineDiffBounds.Default.MaxLineLength + 1);
        var (map, result, leftCount, rightCount) = IntralineRenderTestHelper.MapWithIntraline(
            $"same\r\n{longToken}\r\n",
            $"same\r\n{longToken}x\r\n");

        PaneRenderModel left = PaneRenderModel.Create(map, ComparisonSide.Left, leftCount, result.InlineDiffs);
        PaneRenderModel right = PaneRenderModel.Create(map, ComparisonSide.Right, rightCount, result.InlineDiffs);

        Assert.AreEqual(DiffBlockKind.Modified, left.GetKindForLine(1));
        Assert.AreEqual(DiffBlockKind.Modified, right.GetKindForLine(1));

        PaneInlineLineModel? inlineLeft = left.GetInlineForLine(1);
        PaneInlineLineModel? inlineRight = right.GetInlineForLine(1);
        Assert.IsNotNull(inlineLeft);
        Assert.IsNotNull(inlineRight);
        Assert.IsTrue(inlineLeft.IsWholeLineFallback);
        Assert.IsTrue(inlineRight.IsWholeLineFallback);
        Assert.IsEmpty(inlineLeft.HighlightSegments);
        Assert.IsEmpty(inlineRight.HighlightSegments);
    }

    [TestMethod]
    public void InlineSegments_MapToCorrectLineOnEachSide()
    {
        var (map, result, leftCount, rightCount) = IntralineRenderTestHelper.MapWithIntraline(
            "same\r\nhello\r\n",
            "same\r\nhallo\r\n");

        PaneRenderModel left = PaneRenderModel.Create(map, ComparisonSide.Left, leftCount, result.InlineDiffs);
        PaneRenderModel right = PaneRenderModel.Create(map, ComparisonSide.Right, rightCount, result.InlineDiffs);

        Assert.IsNull(left.GetInlineForLine(0));
        PaneInlineLineModel? inlineLeft = left.GetInlineForLine(1);
        PaneInlineLineModel? inlineRight = right.GetInlineForLine(1);
        Assert.IsNotNull(inlineLeft);
        Assert.IsNotNull(inlineRight);
        Assert.IsFalse(inlineLeft.IsWholeLineFallback);
        Assert.HasCount(1, inlineLeft.HighlightSegments);
        Assert.HasCount(1, inlineRight.HighlightSegments);
    }
}
