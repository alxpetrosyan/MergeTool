using MergeTool.Core;
using MergeTool.Core.Settings;
using MergeTool.Text;
using MergeTool.Text.Diff;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class RegressionGoldenTests
{
    private static readonly PatienceLineDiffEngine Engine = new();

    [TestMethod]
    public void Golden_ModifiedMiddleBlock_ProducesExpectedOrdinalSummary()
    {
        IReadOnlyList<TextLine> left = TextLineSplitter.Split("a\r\nold\r\nz\r\n").Lines;
        IReadOnlyList<TextLine> right = TextLineSplitter.Split("a\r\nnew\r\nz\r\n").Lines;

        IReadOnlyList<DiffBlock> blocks = Engine.Compare(left, right, TextComparisonOptions.Default, CancellationToken.None);
        IReadOnlyList<DiffBlock> differences = blocks.Where(block => block.IsDifference).ToList();

        Assert.HasCount(1, differences);
        Assert.AreEqual(DiffBlockKind.Modified, differences[0].Kind);
        Assert.AreEqual(1, differences[0].LeftAnchorLine);
        Assert.AreEqual(1, differences[0].RightAnchorLine);
    }

    [TestMethod]
    public void Golden_IgnoredLinePattern_ExcludesMatchingLinesFromProjection()
    {
        IReadOnlyList<TextLine> left = TextLineSplitter.Split("keep\r\n# skip\r\nend\r\n").Lines;
        IReadOnlyList<TextLine> right = TextLineSplitter.Split("keep\r\n# skip\r\nchanged\r\n").Lines;
        var options = new TextComparisonOptions(ignoredLinePattern: "^#");

        ComparisonProjectionBuildResult projection = ComparisonProjector.Build(left, right, options);

        Assert.IsTrue(projection.IsSuccess);
        IReadOnlyList<DiffBlock> blocks = Engine.Compare(
            projection.Projection!.ProjectedLeftLines,
            projection.Projection.ProjectedRightLines,
            options,
            CancellationToken.None);
        IReadOnlyList<DiffBlock> differences = blocks.Where(block => block.IsDifference).ToList();
        Assert.HasCount(1, differences);
        Assert.AreEqual(DiffBlockKind.Modified, differences[0].Kind);
    }

    [TestMethod]
    public void Property_SameInputs_ProduceEqualDiagnosticsAndBlocks()
    {
        const string left = "alpha\r\nbeta\r\n";
        const string right = "alpha\r\ngamma\r\n";
        IReadOnlyList<TextLine> leftLines = TextLineSplitter.Split(left).Lines;
        IReadOnlyList<TextLine> rightLines = TextLineSplitter.Split(right).Lines;

        IReadOnlyList<DiffBlock> first = Engine.Compare(leftLines, rightLines, TextComparisonOptions.Default, CancellationToken.None);
        IReadOnlyList<DiffBlock> second = Engine.Compare(leftLines, rightLines, TextComparisonOptions.Default, CancellationToken.None);

        Assert.HasCount(first.Count, second);
        for (int index = 0; index < first.Count; index++)
        {
            Assert.AreEqual(first[index], second[index]);
        }
    }
}
