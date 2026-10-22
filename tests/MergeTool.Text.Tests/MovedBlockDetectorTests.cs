using MergeTool.Text;
using MergeTool.Text.Diff;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class MovedBlockDetectorTests
{
    [TestMethod]
    public void DetectMovedBlockOrdinals_MarksDeletedAndInsertedPair_WithIdenticalText()
    {
        var blocks = new List<DiffBlock>
        {
            new(0, DiffBlockKind.Equal, new LineRange(0, 1), new LineRange(0, 1)),
            new(1, DiffBlockKind.Deleted, new LineRange(1, 3), new LineRange(1, 1)),
            new(2, DiffBlockKind.Inserted, new LineRange(3, 3), new LineRange(1, 3)),
            new(3, DiffBlockKind.Equal, new LineRange(3, 4), new LineRange(3, 4)),
        };
        IReadOnlyList<TextLine> left = Lines("keep", "moved", "block", "tail");
        IReadOnlyList<TextLine> right = Lines("keep", "moved", "block", "tail");

        IReadOnlyList<int> moved = MovedBlockDetector.DetectMovedBlockOrdinals(blocks, left, right);

        Assert.HasCount(2, moved);
        Assert.IsTrue(moved.Contains(1));
        Assert.IsTrue(moved.Contains(2));
    }

    [TestMethod]
    public void DetectMovedBlockOrdinals_IgnoresModifiedBlocks()
    {
        var blocks = new List<DiffBlock>
        {
            new(0, DiffBlockKind.Modified, new LineRange(0, 1), new LineRange(0, 1)),
        };
        IReadOnlyList<TextLine> left = Lines("a");
        IReadOnlyList<TextLine> right = Lines("b");

        IReadOnlyList<int> moved = MovedBlockDetector.DetectMovedBlockOrdinals(blocks, left, right);

        Assert.IsEmpty(moved);
    }

    [TestMethod]
    public void DetectMovedBlockOrdinals_SkipsOversizedBlocks()
    {
        var blocks = new List<DiffBlock>
        {
            new(0, DiffBlockKind.Deleted, new LineRange(0, 5), new LineRange(0, 0)),
            new(1, DiffBlockKind.Inserted, new LineRange(0, 0), new LineRange(0, 5)),
        };
        IReadOnlyList<TextLine> left = Lines("a", "b", "c", "d", "e");
        IReadOnlyList<TextLine> right = Lines("a", "b", "c", "d", "e");

        IReadOnlyList<int> moved = MovedBlockDetector.DetectMovedBlockOrdinals(
            blocks,
            left,
            right,
            maxBlockLines: 2);

        Assert.IsEmpty(moved);
    }

    private static IReadOnlyList<TextLine> Lines(params string[] contents)
    {
        List<TextLine> lines = [];
        for (int i = 0; i < contents.Length; i++)
        {
            lines.Add(new TextLine(i, contents[i], LineTerminator.Lf));
        }

        return lines;
    }
}
