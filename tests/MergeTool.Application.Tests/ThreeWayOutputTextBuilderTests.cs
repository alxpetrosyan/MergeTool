using MergeTool.Application.Sessions;
using MergeTool.Text.Diff;
using MergeTool.Text.ThreeWay;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class ThreeWayOutputTextBuilderTests
{
    [TestMethod]
    public void Build_UnresolvedDifference_UsesBaseContent()
    {
        TextDocumentSession baseDoc = SessionTestHelper.SessionFromText("a\r\nold\r\n");
        TextDocumentSession leftDoc = SessionTestHelper.SessionFromText("a\r\nleft\r\n");
        TextDocumentSession rightDoc = SessionTestHelper.SessionFromText("a\r\nold\r\n");
        ThreeWayComparisonResult comparison = BuildComparison(baseDoc, leftDoc, rightDoc);
        ThreeWayRegion difference = comparison.Regions.Single(region => region.IsDifference);
        var resolutions = comparison.Regions.Select(region => region.IsDifference
            ? new ThreeWayRegionResolution(region.Ordinal, ThreeWayRegionResolutionState.Unresolved, null)
            : new ThreeWayRegionResolution(region.Ordinal, ThreeWayRegionResolutionState.Resolved, ThreeWayOutputSourceChoice.Base))
            .ToArray();

        string output = ThreeWayOutputTextBuilder.Build(comparison, resolutions, baseDoc, leftDoc, rightDoc);

        Assert.AreEqual("a\r\nold\r\n", output);
        Assert.AreEqual(ThreeWayRegionKind.LeftOnlyChange, difference.Kind);
    }

    [TestMethod]
    public void Build_ResolvedFromLeft_CopiesLeftText()
    {
        TextDocumentSession baseDoc = SessionTestHelper.SessionFromText("a\r\nold\r\n");
        TextDocumentSession leftDoc = SessionTestHelper.SessionFromText("a\r\nleft\r\n");
        TextDocumentSession rightDoc = SessionTestHelper.SessionFromText("a\r\nold\r\n");
        ThreeWayComparisonResult comparison = BuildComparison(baseDoc, leftDoc, rightDoc);
        ThreeWayRegion difference = comparison.Regions.Single(region => region.IsDifference);

        var resolutions = comparison.Regions.Select(region => region.Ordinal == difference.Ordinal
            ? new ThreeWayRegionResolution(region.Ordinal, ThreeWayRegionResolutionState.Resolved, ThreeWayOutputSourceChoice.Left)
            : new ThreeWayRegionResolution(region.Ordinal, ThreeWayRegionResolutionState.Resolved, ThreeWayOutputSourceChoice.Base))
            .ToArray();

        string output = ThreeWayOutputTextBuilder.Build(comparison, resolutions, baseDoc, leftDoc, rightDoc);

        Assert.AreEqual("a\r\nleft\r\n", output);
    }

    private static ThreeWayComparisonResult BuildComparison(
        TextDocumentSession baseDoc,
        TextDocumentSession leftDoc,
        TextDocumentSession rightDoc)
    {
        PatienceLineDiffEngine engine = new();
        IReadOnlyList<DiffBlock> baseLeft = engine.Compare(
            baseDoc.CurrentLines,
            leftDoc.CurrentLines,
            TextComparisonOptions.Default,
            CancellationToken.None);
        IReadOnlyList<DiffBlock> baseRight = engine.Compare(
            baseDoc.CurrentLines,
            rightDoc.CurrentLines,
            TextComparisonOptions.Default,
            CancellationToken.None);
        return ThreeWayComparisonBuilder.Build(
            baseDoc.CurrentLines,
            leftDoc.CurrentLines,
            rightDoc.CurrentLines,
            baseLeft,
            baseRight,
            baseDoc.DocumentRevision,
            leftDoc.DocumentRevision,
            rightDoc.DocumentRevision);
    }
}
