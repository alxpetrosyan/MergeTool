using MergeTool.Text;
using MergeTool.Text.Diff;
using MergeTool.Text.ThreeWay;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class ThreeWayComparisonBuilderTests
{
    private static readonly PatienceLineDiffEngine Engine = new();

    private static IReadOnlyList<TextLine> Lines(params string[] contents)
        => [.. contents.Select((content, index) => new TextLine(index, content, LineTerminator.CrLf))];

    private static ThreeWayComparisonResult Build(
        string[] baseLines,
        string[] leftLines,
        string[] rightLines)
    {
        IReadOnlyList<TextLine> baseText = Lines(baseLines);
        IReadOnlyList<TextLine> leftText = Lines(leftLines);
        IReadOnlyList<TextLine> rightText = Lines(rightLines);
        IReadOnlyList<DiffBlock> baseLeft = Engine.Compare(baseText, leftText, TextComparisonOptions.Default, CancellationToken.None);
        IReadOnlyList<DiffBlock> baseRight = Engine.Compare(baseText, rightText, TextComparisonOptions.Default, CancellationToken.None);
        return ThreeWayComparisonBuilder.Build(baseText, leftText, rightText, baseLeft, baseRight, 1, 2, 3);
    }

    [TestMethod]
    public void IdenticalDocuments_ProduceSingleUnchangedRegion()
    {
        ThreeWayComparisonResult result = Build(["a", "b"], ["a", "b"], ["a", "b"]);

        Assert.HasCount(1, result.Regions);
        Assert.AreEqual(ThreeWayRegionKind.Unchanged, result.Regions[0].Kind);
        Assert.AreEqual(new LineRange(0, 2), result.Regions[0].BaseRange);
    }

    [TestMethod]
    public void LeftOnlyChange_ClassifiesAffectedBaseLines()
    {
        ThreeWayComparisonResult result = Build(["a", "old", "c"], ["a", "left", "c"], ["a", "old", "c"]);

        ThreeWayRegion change = result.Regions.Single(region => region.Kind == ThreeWayRegionKind.LeftOnlyChange);
        Assert.AreEqual(new LineRange(1, 1), change.BaseRange);
        Assert.AreEqual(new LineRange(1, 1), change.LeftRange);
        Assert.AreEqual(new LineRange(1, 1), change.RightRange);
    }

    [TestMethod]
    public void RightOnlyChange_ClassifiesAffectedBaseLines()
    {
        ThreeWayComparisonResult result = Build(["a", "old", "c"], ["a", "old", "c"], ["a", "right", "c"]);

        ThreeWayRegion change = result.Regions.Single(region => region.Kind == ThreeWayRegionKind.RightOnlyChange);
        Assert.AreEqual(new LineRange(1, 1), change.BaseRange);
        Assert.AreEqual(new LineRange(1, 1), change.LeftRange);
        Assert.AreEqual(new LineRange(1, 1), change.RightRange);
    }

    [TestMethod]
    public void SameChangeOnBothSides_ClassifiesAsSameChange()
    {
        ThreeWayComparisonResult result = Build(["a", "old", "c"], ["a", "new", "c"], ["a", "new", "c"]);

        Assert.IsTrue(result.Regions.Any(region => region.Kind == ThreeWayRegionKind.SameChange));
    }

    [TestMethod]
    public void DivergentChangesOnSameBaseLine_ClassifiesAsDivergent()
    {
        ThreeWayComparisonResult result = Build(["a", "old", "c"], ["a", "left", "c"], ["a", "right", "c"]);

        ThreeWayRegion change = result.Regions.Single(region => region.Kind == ThreeWayRegionKind.Divergent);
        Assert.AreEqual(new LineRange(1, 1), change.BaseRange);
    }

    [TestMethod]
    public void EmptyBase_LeftInsertionOnly_ClassifiesLeftOnly()
    {
        ThreeWayComparisonResult result = Build([], ["left-only"], []);

        Assert.IsTrue(result.Regions.Any(region => region.Kind == ThreeWayRegionKind.LeftOnlyChange));
    }

    [TestMethod]
    public void Insertion_OnlyOnLeftSide_ClassifiesLeftOnly()
    {
        ThreeWayComparisonResult result = Build(
            ["keep"],
            ["keep", "left-insert"],
            ["keep"]);

        Assert.IsTrue(result.Regions.Any(region => region.Kind == ThreeWayRegionKind.LeftOnlyChange));
    }

    [TestMethod]
    public void Build_PreservesDocumentRevisions()
    {
        ThreeWayComparisonResult result = Build(["x"], ["x"], ["y"]);
        Assert.AreEqual(1, result.BaseRevision);
        Assert.AreEqual(2, result.LeftRevision);
        Assert.AreEqual(3, result.RightRevision);
    }
}
