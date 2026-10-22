using MergeTool.Application.Presentation;
using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class DifferenceListBuilderTests
{
    [TestMethod]
    public void Build_ListsDifferenceBlocksInOrder()
    {
        var blocks = new List<DiffBlock>
        {
            new(0, DiffBlockKind.Equal, new LineRange(0, 2), new LineRange(0, 2)),
            new(1, DiffBlockKind.Modified, new LineRange(2, 1), new LineRange(2, 1)),
            new(2, DiffBlockKind.Inserted, LineRange.Empty(3), new LineRange(3, 2)),
        };

        var map = ComparisonPresentationMap.Create(
            TwoWayTextComparisonResult.Create(DocumentSessionId.New(), DocumentSessionId.New(), 0, 0, blocks),
            5,
            5);

        IReadOnlyList<DifferenceListEntry> entries = DifferenceListBuilder.Build(map);
        Assert.HasCount(2, entries);
        Assert.AreEqual(1, entries[0].BlockOrdinal);
        Assert.AreEqual(2, entries[1].BlockOrdinal);
        Assert.AreEqual(1, entries[0].ListIndex);
        Assert.AreEqual(2, entries[1].ListIndex);
    }

    [TestMethod]
    public void Build_EntryAnchorsMatchPresentationMap()
    {
        var blocks = new List<DiffBlock>
        {
            new(0, DiffBlockKind.Deleted, new LineRange(4, 2), LineRange.Empty(4)),
        };

        var map = ComparisonPresentationMap.Create(
            TwoWayTextComparisonResult.Create(DocumentSessionId.New(), DocumentSessionId.New(), 0, 0, blocks),
            6,
            4);

        DifferenceListEntry entry = DifferenceListBuilder.Build(map).Single();
        Assert.AreEqual(map.GetLeftAnchorForBlock(0), entry.LeftAnchorLine);
        Assert.AreEqual(map.GetRightAnchorForBlock(0), entry.RightAnchorLine);
    }

    [TestMethod]
    public void Build_AppendsMovedSuffix_WhenOrdinalMarked()
    {
        var blocks = new List<DiffBlock>
        {
            new(0, DiffBlockKind.Deleted, new LineRange(0, 1), LineRange.Empty(0)),
            new(1, DiffBlockKind.Inserted, LineRange.Empty(0), new LineRange(0, 1)),
        };

        var map = ComparisonPresentationMap.Create(
            TwoWayTextComparisonResult.Create(DocumentSessionId.New(), DocumentSessionId.New(), 0, 0, blocks),
            1,
            1);

        IReadOnlyList<DifferenceListEntry> entries = DifferenceListBuilder.Build(map, new HashSet<int> { 0 });
        DifferenceListEntry entry = entries.Single(e => e.BlockOrdinal == 0);
        Assert.IsTrue(entry.IsMoved);
        Assert.Contains("moved", entry.Summary);
    }
}
