using MergeTool.Text;
using MergeTool.Text.Diff;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class IntralineDiffEnricherTests
{
    [TestMethod]
    public void Enrich_WhenDisabled_LeavesInlineDiffsEmpty()
    {
        var leftLines = TextLineSplitter.Split("a\r\nb\r\n").Lines;
        var rightLines = TextLineSplitter.Split("a\r\nc\r\n").Lines;
        var blocks = new PatienceLineDiffEngine().Compare(
            leftLines, rightLines, TextComparisonOptions.Default, CancellationToken.None);
        TwoWayTextComparisonResult result = TwoWayTextComparisonResult.Create(
            Core.DocumentSessionId.New(), Core.DocumentSessionId.New(), 0, 0, blocks);

        TwoWayTextComparisonResult enriched = IntralineDiffEnricher.Enrich(
            result, leftLines, rightLines, TextComparisonOptions.Default);

        Assert.IsEmpty(enriched.InlineDiffs);
        Assert.AreSame(result, enriched);
    }

    [TestMethod]
    public void Enrich_ModifiedBlockOnly_ProducesInlinePairs()
    {
        var leftLines = TextLineSplitter.Split("same\r\nold\r\n").Lines;
        var rightLines = TextLineSplitter.Split("same\r\nnew\r\n").Lines;
        var blocks = new PatienceLineDiffEngine().Compare(
            leftLines, rightLines, TextComparisonOptions.WithIntralineDiff, CancellationToken.None);
        TwoWayTextComparisonResult result = TwoWayTextComparisonResult.Create(
            Core.DocumentSessionId.New(), Core.DocumentSessionId.New(), 0, 0, blocks);

        TwoWayTextComparisonResult enriched = IntralineDiffEnricher.Enrich(
            result, leftLines, rightLines, TextComparisonOptions.WithIntralineDiff);

        Assert.HasCount(1, enriched.InlineDiffs);
        Assert.AreEqual(1, enriched.InlineDiffs[0].LinePair.LeftLineIndex);
        Assert.AreEqual(1, enriched.InlineDiffs[0].LinePair.RightLineIndex);
        Assert.IsFalse(enriched.InlineDiffs[0].LinePair.IsWholeLineFallback);
    }
}
