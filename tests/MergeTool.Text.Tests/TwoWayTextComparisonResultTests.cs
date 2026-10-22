using MergeTool.Core;
using MergeTool.Text.Diff;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class TwoWayTextComparisonResultTests
{
    [TestMethod]
    public void Create_CarriesIdentitiesRevisionsAndSummary()
    {
        DocumentSessionId leftId = DocumentSessionId.New();
        DocumentSessionId rightId = DocumentSessionId.New();
        IReadOnlyList<DiffBlock> blocks =
        [
            new DiffBlock(0, DiffBlockKind.Equal, new LineRange(0, 2), new LineRange(0, 2)),
            new DiffBlock(1, DiffBlockKind.Modified, new LineRange(2, 1), new LineRange(2, 2)),
            new DiffBlock(2, DiffBlockKind.Inserted, new LineRange(3, 0), new LineRange(4, 3)),
            new DiffBlock(3, DiffBlockKind.Deleted, new LineRange(3, 2), new LineRange(7, 0)),
        ];

        TwoWayTextComparisonResult result = TwoWayTextComparisonResult.Create(leftId, rightId, 5, 9, blocks);

        Assert.AreEqual(leftId, result.LeftDocumentId);
        Assert.AreEqual(rightId, result.RightDocumentId);
        Assert.AreEqual(5, result.LeftDocumentRevision);
        Assert.AreEqual(9, result.RightDocumentRevision);
        Assert.AreSame(blocks, result.Blocks);

        DiffSummary summary = result.Summary;
        Assert.AreEqual(1, summary.EqualBlockCount);
        Assert.AreEqual(1, summary.InsertedBlockCount);
        Assert.AreEqual(1, summary.DeletedBlockCount);
        Assert.AreEqual(1, summary.ModifiedBlockCount);
        Assert.AreEqual(3, summary.InsertedLineCount);
        Assert.AreEqual(2, summary.DeletedLineCount);
        Assert.AreEqual(1, summary.ModifiedLeftLineCount);
        Assert.AreEqual(2, summary.ModifiedRightLineCount);
        Assert.AreEqual(3, summary.DifferenceBlockCount);
        Assert.IsTrue(summary.HasDifferences);
    }

    [TestMethod]
    public void LineRange_FollowsZeroBasedStartLengthConvention()
    {
        LineRange range = new(3, 2);

        Assert.AreEqual(3, range.StartIndex);
        Assert.AreEqual(2, range.Length);
        Assert.AreEqual(5, range.EndExclusive);
        Assert.IsTrue(range.Contains(3));
        Assert.IsTrue(range.Contains(4));
        Assert.IsFalse(range.Contains(5));
        Assert.IsTrue(LineRange.Empty(7).IsEmpty);
        Assert.AreEqual(7, LineRange.Empty(7).StartIndex);
    }
}
