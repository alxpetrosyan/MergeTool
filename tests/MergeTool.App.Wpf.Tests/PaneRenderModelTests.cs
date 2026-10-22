using MergeTool.App.Wpf.Rendering;
using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.Diff;

namespace MergeTool.App.Wpf.Tests;

internal static class RenderTestHelper
{
    /// <summary>Runs the real diff engine and builds the presentation map for two texts.</summary>
    public static (ComparisonPresentationMap Map, int LeftCount, int RightCount) MapFor(string leftText, string rightText)
    {
        var leftLines = TextLineSplitter.Split(leftText).Lines;
        var rightLines = TextLineSplitter.Split(rightText).Lines;
        var blocks = new PatienceLineDiffEngine().Compare(
            leftLines, rightLines, TextComparisonOptions.Default, CancellationToken.None);
        var result = TwoWayTextComparisonResult.Create(
            DocumentSessionId.New(), DocumentSessionId.New(), 0, 0, blocks);
        return (ComparisonPresentationMap.Create(result, leftLines.Count, rightLines.Count), leftLines.Count, rightLines.Count);
    }
}

[TestClass]
public sealed class PaneRenderModelTests
{
    [TestMethod]
    public void ModifiedLines_AreClassifiedOnBothSides()
    {
        var (map, leftCount, rightCount) = RenderTestHelper.MapFor("same\r\nold\r\ntail\r\n", "same\r\nnew\r\ntail\r\n");
        PaneRenderModel left = PaneRenderModel.Create(map, ComparisonSide.Left, leftCount);
        PaneRenderModel right = PaneRenderModel.Create(map, ComparisonSide.Right, rightCount);

        Assert.IsNull(left.GetKindForLine(0), "Equal lines carry no classification.");
        Assert.AreEqual(DiffBlockKind.Modified, left.GetKindForLine(1));
        Assert.AreEqual(DiffBlockKind.Modified, right.GetKindForLine(1));
        Assert.IsNull(left.GetKindForLine(2));
    }

    [TestMethod]
    public void InsertedLines_AreClassifiedOnlyOnTheRight()
    {
        var (map, leftCount, rightCount) = RenderTestHelper.MapFor("a\r\nb\r\n", "a\r\nextra\r\nb\r\n");
        PaneRenderModel left = PaneRenderModel.Create(map, ComparisonSide.Left, leftCount);
        PaneRenderModel right = PaneRenderModel.Create(map, ComparisonSide.Right, rightCount);

        Assert.AreEqual(DiffBlockKind.Inserted, right.GetKindForLine(1));
        for (int line = 0; line < leftCount; line++)
        {
            Assert.IsNull(left.GetKindForLine(line), $"Left line {line} must stay unclassified for a pure insertion.");
        }
    }

    [TestMethod]
    public void DeletedLines_AreClassifiedOnlyOnTheLeft()
    {
        var (map, leftCount, rightCount) = RenderTestHelper.MapFor("a\r\ngone\r\nb\r\n", "a\r\nb\r\n");
        PaneRenderModel left = PaneRenderModel.Create(map, ComparisonSide.Left, leftCount);
        PaneRenderModel right = PaneRenderModel.Create(map, ComparisonSide.Right, rightCount);

        Assert.AreEqual(DiffBlockKind.Deleted, left.GetKindForLine(1));
        for (int line = 0; line < rightCount; line++)
        {
            Assert.IsNull(right.GetKindForLine(line), $"Right line {line} must stay unclassified for a pure deletion.");
        }
    }

    [TestMethod]
    public void ClassificationComesFromBlocks_NotFromLineContent()
    {
        // The same textual line ("dup") is equal in one place and inserted in another.
        var (map, _, rightCount) = RenderTestHelper.MapFor("dup\r\nx\r\n", "dup\r\nx\r\ndup\r\n");
        PaneRenderModel right = PaneRenderModel.Create(map, ComparisonSide.Right, rightCount);

        Assert.IsNull(right.GetKindForLine(0), "First 'dup' is part of an equal block.");
        Assert.AreEqual(DiffBlockKind.Inserted, right.GetKindForLine(2), "Second 'dup' is an insertion.");
    }

    [TestMethod]
    public void OutOfRangeLines_AreUnclassified()
    {
        var (map, leftCount, _) = RenderTestHelper.MapFor("a\r\n", "b\r\n");
        PaneRenderModel left = PaneRenderModel.Create(map, ComparisonSide.Left, leftCount);

        Assert.IsNull(left.GetKindForLine(-1));
        Assert.IsNull(left.GetKindForLine(leftCount));
        Assert.IsNull(PaneRenderModel.Empty.GetKindForLine(0));
    }

    [TestMethod]
    public void Palette_IsFixedAndDistinctPerDifferenceKind()
    {
        var brushes = new[]
        {
            DiffBlockPalette.GetBackground(DiffBlockKind.Inserted),
            DiffBlockPalette.GetBackground(DiffBlockKind.Deleted),
            DiffBlockPalette.GetBackground(DiffBlockKind.Modified),
        };

        Assert.IsNull(DiffBlockPalette.GetBackground(DiffBlockKind.Equal), "Equal lines get no overlay.");
        Assert.IsNull(DiffBlockPalette.GetBackground(null));
        Assert.HasCount(3, brushes.Where(b => b is not null).Distinct().ToList());
        Assert.IsTrue(brushes.All(b => b!.IsFrozen), "Brushes must be frozen for cross-thread safety.");
    }

    [TestMethod]
    public void Create_MarksMovedLines_WhenOrdinalSetProvided()
    {
        var blocks = new List<DiffBlock>
        {
            new(0, DiffBlockKind.Deleted, new LineRange(1, 1), LineRange.Empty(1)),
        };

        var map = ComparisonPresentationMap.Create(
            TwoWayTextComparisonResult.Create(DocumentSessionId.New(), DocumentSessionId.New(), 0, 0, blocks),
            3,
            2);

        PaneRenderModel left = PaneRenderModel.Create(map, ComparisonSide.Left, 3, movedBlockOrdinals: new HashSet<int> { 0 });

        Assert.IsTrue(left.IsMovedLine(1));
        Assert.IsFalse(left.IsMovedLine(0));
    }
}
