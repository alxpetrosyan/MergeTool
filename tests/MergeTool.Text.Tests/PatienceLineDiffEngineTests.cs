using MergeTool.Text;
using MergeTool.Text.Diff;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class PatienceLineDiffEngineTests
{
    private static readonly PatienceLineDiffEngine Engine = new();

    private static IReadOnlyList<TextLine> Lines(params string[] contents)
        => [.. contents.Select((content, index) => new TextLine(index, content, LineTerminator.CrLf))];

    private static IReadOnlyList<DiffBlock> Compare(IReadOnlyList<TextLine> left, IReadOnlyList<TextLine> right)
        => Engine.Compare(left, right, TextComparisonOptions.Default, CancellationToken.None);

    private static void AssertBlock(DiffBlock block, int ordinal, DiffBlockKind kind, int leftStart, int leftLength, int rightStart, int rightLength)
    {
        Assert.AreEqual(ordinal, block.Ordinal, "ordinal");
        Assert.AreEqual(kind, block.Kind, $"kind of block {ordinal}");
        Assert.AreEqual(new LineRange(leftStart, leftLength), block.LeftRange, $"left range of block {ordinal}");
        Assert.AreEqual(new LineRange(rightStart, rightLength), block.RightRange, $"right range of block {ordinal}");
    }

    [TestMethod]
    public void IdenticalInputs_ProduceSingleEqualBlock()
    {
        IReadOnlyList<TextLine> lines = Lines("a", "b", "c");

        IReadOnlyList<DiffBlock> blocks = Compare(lines, Lines("a", "b", "c"));

        Assert.HasCount(1, blocks);
        AssertBlock(blocks[0], 0, DiffBlockKind.Equal, 0, 3, 0, 3);
    }

    [TestMethod]
    public void OneChangedLine_IsModifiedBlockWithExactRanges()
    {
        IReadOnlyList<DiffBlock> blocks = Compare(
            Lines("a", "old", "c"),
            Lines("a", "new", "c"));

        Assert.HasCount(3, blocks);
        AssertBlock(blocks[0], 0, DiffBlockKind.Equal, 0, 1, 0, 1);
        AssertBlock(blocks[1], 1, DiffBlockKind.Modified, 1, 1, 1, 1);
        AssertBlock(blocks[2], 2, DiffBlockKind.Equal, 2, 1, 2, 1);
    }

    [TestMethod]
    public void InsertionAtStart()
    {
        IReadOnlyList<DiffBlock> blocks = Compare(
            Lines("a", "b"),
            Lines("new", "a", "b"));

        Assert.HasCount(2, blocks);
        AssertBlock(blocks[0], 0, DiffBlockKind.Inserted, 0, 0, 0, 1);
        AssertBlock(blocks[1], 1, DiffBlockKind.Equal, 0, 2, 1, 2);
    }

    [TestMethod]
    public void InsertionInMiddle()
    {
        IReadOnlyList<DiffBlock> blocks = Compare(
            Lines("a", "b"),
            Lines("a", "new", "b"));

        Assert.HasCount(3, blocks);
        AssertBlock(blocks[0], 0, DiffBlockKind.Equal, 0, 1, 0, 1);
        AssertBlock(blocks[1], 1, DiffBlockKind.Inserted, 1, 0, 1, 1);
        AssertBlock(blocks[2], 2, DiffBlockKind.Equal, 1, 1, 2, 1);
    }

    [TestMethod]
    public void InsertionAtEnd()
    {
        IReadOnlyList<DiffBlock> blocks = Compare(
            Lines("a", "b"),
            Lines("a", "b", "new"));

        Assert.HasCount(2, blocks);
        AssertBlock(blocks[0], 0, DiffBlockKind.Equal, 0, 2, 0, 2);
        AssertBlock(blocks[1], 1, DiffBlockKind.Inserted, 2, 0, 2, 1);
    }

    [TestMethod]
    public void DeletionAtStart()
    {
        IReadOnlyList<DiffBlock> blocks = Compare(
            Lines("gone", "a", "b"),
            Lines("a", "b"));

        Assert.HasCount(2, blocks);
        AssertBlock(blocks[0], 0, DiffBlockKind.Deleted, 0, 1, 0, 0);
        AssertBlock(blocks[1], 1, DiffBlockKind.Equal, 1, 2, 0, 2);
    }

    [TestMethod]
    public void DeletionInMiddle()
    {
        IReadOnlyList<DiffBlock> blocks = Compare(
            Lines("a", "gone", "b"),
            Lines("a", "b"));

        Assert.HasCount(3, blocks);
        AssertBlock(blocks[0], 0, DiffBlockKind.Equal, 0, 1, 0, 1);
        AssertBlock(blocks[1], 1, DiffBlockKind.Deleted, 1, 1, 1, 0);
        AssertBlock(blocks[2], 2, DiffBlockKind.Equal, 2, 1, 1, 1);
    }

    [TestMethod]
    public void DeletionAtEnd()
    {
        IReadOnlyList<DiffBlock> blocks = Compare(
            Lines("a", "b", "gone"),
            Lines("a", "b"));

        Assert.HasCount(2, blocks);
        AssertBlock(blocks[0], 0, DiffBlockKind.Equal, 0, 2, 0, 2);
        AssertBlock(blocks[1], 1, DiffBlockKind.Deleted, 2, 1, 2, 0);
    }

    [TestMethod]
    public void EmptyLeft_IsOneInsertedBlock()
    {
        IReadOnlyList<DiffBlock> blocks = Compare(Lines(), Lines("a", "b"));

        Assert.HasCount(1, blocks);
        AssertBlock(blocks[0], 0, DiffBlockKind.Inserted, 0, 0, 0, 2);
    }

    [TestMethod]
    public void EmptyRight_IsOneDeletedBlock()
    {
        IReadOnlyList<DiffBlock> blocks = Compare(Lines("a", "b"), Lines());

        Assert.HasCount(1, blocks);
        AssertBlock(blocks[0], 0, DiffBlockKind.Deleted, 0, 2, 0, 0);
    }

    [TestMethod]
    public void BothEmpty_ProducesNoBlocks()
    {
        Assert.IsEmpty(Compare(Lines(), Lines()));
    }

    [TestMethod]
    public void AdjacentModifications_MergeIntoOneModifiedBlock()
    {
        IReadOnlyList<DiffBlock> blocks = Compare(
            Lines("keep", "old1", "old2", "keep2"),
            Lines("keep", "new1", "new2", "keep2"));

        Assert.HasCount(3, blocks);
        AssertBlock(blocks[1], 1, DiffBlockKind.Modified, 1, 2, 1, 2);
    }

    [TestMethod]
    public void UnbalancedReplacement_IsOneModifiedBlock()
    {
        // 2 left lines replaced by 3 right lines: pairing is ambiguous, so the whole
        // region is one Modified block (documented fallback).
        IReadOnlyList<DiffBlock> blocks = Compare(
            Lines("keep", "old1", "old2", "keep2"),
            Lines("keep", "new1", "new2", "new3", "keep2"));

        Assert.HasCount(3, blocks);
        AssertBlock(blocks[1], 1, DiffBlockKind.Modified, 1, 2, 1, 3);
    }

    [TestMethod]
    public void RepeatedEqualLines_DoNotMisalign()
    {
        // Braces repeat; unique lines must anchor the alignment.
        IReadOnlyList<DiffBlock> blocks = Compare(
            Lines("{", "alpha();", "}", "{", "beta();", "}"),
            Lines("{", "alpha();", "}", "{", "gamma();", "}"));

        Assert.HasCount(3, blocks);
        AssertBlock(blocks[0], 0, DiffBlockKind.Equal, 0, 4, 0, 4);
        AssertBlock(blocks[1], 1, DiffBlockKind.Modified, 4, 1, 4, 1);
        AssertBlock(blocks[2], 2, DiffBlockKind.Equal, 5, 1, 5, 1);
    }

    [TestMethod]
    public void RepeatedLinesOnly_TrimToMinimalDifference()
    {
        // "a a a" vs "a a": prefix trimming pairs the first two; the third is deleted.
        IReadOnlyList<DiffBlock> blocks = Compare(
            Lines("a", "a", "a"),
            Lines("a", "a"));

        Assert.HasCount(2, blocks);
        AssertBlock(blocks[0], 0, DiffBlockKind.Equal, 0, 2, 0, 2);
        AssertBlock(blocks[1], 1, DiffBlockKind.Deleted, 2, 1, 2, 0);
    }

    [TestMethod]
    public void SourceCodeLikeText_WithSeveralSeparatedBlocks()
    {
        IReadOnlyList<DiffBlock> blocks = Compare(
            Lines("using A;", "", "void M()", "{", "    one();", "    two();", "}", "", "int F() => 1;"),
            Lines("using A;", "using B;", "", "void M()", "{", "    one();", "    two2();", "}", "", "int F() => 2;"));

        Assert.HasCount(6, blocks);
        AssertBlock(blocks[0], 0, DiffBlockKind.Equal, 0, 1, 0, 1);
        AssertBlock(blocks[1], 1, DiffBlockKind.Inserted, 1, 0, 1, 1);
        AssertBlock(blocks[2], 2, DiffBlockKind.Equal, 1, 4, 2, 4);
        AssertBlock(blocks[3], 3, DiffBlockKind.Modified, 5, 1, 6, 1);
        AssertBlock(blocks[4], 4, DiffBlockKind.Equal, 6, 2, 7, 2);
        AssertBlock(blocks[5], 5, DiffBlockKind.Modified, 8, 1, 9, 1);
    }

    [TestMethod]
    public void FinalNewlineDifference_MakesLastLineDiffer()
    {
        IReadOnlyList<TextLine> terminated = [new TextLine(0, "a", LineTerminator.CrLf), new TextLine(1, "b", LineTerminator.CrLf)];
        IReadOnlyList<TextLine> unterminated = [new TextLine(0, "a", LineTerminator.CrLf), new TextLine(1, "b", LineTerminator.None)];

        IReadOnlyList<DiffBlock> blocks = Engine.Compare(terminated, unterminated, TextComparisonOptions.Default, CancellationToken.None);

        Assert.HasCount(2, blocks);
        AssertBlock(blocks[0], 0, DiffBlockKind.Equal, 0, 1, 0, 1);
        AssertBlock(blocks[1], 1, DiffBlockKind.Modified, 1, 1, 1, 1);
    }

    [TestMethod]
    public void TerminatorStyleDifference_IsNotAContentDifference()
    {
        IReadOnlyList<TextLine> crlf = [new TextLine(0, "a", LineTerminator.CrLf), new TextLine(1, "b", LineTerminator.CrLf)];
        IReadOnlyList<TextLine> lf = [new TextLine(0, "a", LineTerminator.Lf), new TextLine(1, "b", LineTerminator.Lf)];

        IReadOnlyList<DiffBlock> blocks = Engine.Compare(crlf, lf, TextComparisonOptions.Default, CancellationToken.None);

        Assert.HasCount(1, blocks);
        Assert.AreEqual(DiffBlockKind.Equal, blocks[0].Kind);
    }

    [TestMethod]
    public void SameInputs_RepeatedRuns_ProduceIdenticalBlockSequences()
    {
        IReadOnlyList<TextLine> left = Lines("a", "x", "b", "x", "c", "d", "x", "e");
        IReadOnlyList<TextLine> right = Lines("a", "b", "x", "q", "c", "x", "e", "f");

        IReadOnlyList<DiffBlock> first = Compare(left, right);
        for (int run = 0; run < 5; run++)
        {
            IReadOnlyList<DiffBlock> again = Compare(left, right);
            Assert.HasCount(first.Count, again);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i], again[i], $"Block {i} differs on run {run}.");
            }
        }
    }

    [TestMethod]
    public void LargeInput_CompletesWithoutRecursionOverflow()
    {
        // 60,000 lines with periodic unique content and a sprinkling of edits.
        List<TextLine> left = [];
        List<TextLine> right = [];
        int rightIndex = 0;
        for (int i = 0; i < 60_000; i++)
        {
            string content = i % 7 == 0 ? $"unique-{i}" : "filler";
            left.Add(new TextLine(i, content, LineTerminator.Lf));
            if (i % 997 == 0)
            {
                right.Add(new TextLine(rightIndex++, $"edited-{i}", LineTerminator.Lf));
            }
            else
            {
                right.Add(new TextLine(rightIndex++, content, LineTerminator.Lf));
            }
        }

        IReadOnlyList<DiffBlock> blocks = Engine.Compare(left, right, TextComparisonOptions.Default, CancellationToken.None);

        Assert.IsGreaterThan(0, blocks.Count);
        // Block chain must exactly cover both documents in order.
        int leftCursor = 0, rightCursor = 0;
        foreach (DiffBlock block in blocks)
        {
            Assert.AreEqual(leftCursor, block.LeftRange.StartIndex);
            Assert.AreEqual(rightCursor, block.RightRange.StartIndex);
            leftCursor = block.LeftRange.EndExclusive;
            rightCursor = block.RightRange.EndExclusive;
        }

        Assert.AreEqual(left.Count, leftCursor);
        Assert.AreEqual(right.Count, rightCursor);
    }

    [TestMethod]
    public void Cancellation_ThrowsOperationCanceled()
    {
        using CancellationTokenSource cts = new();
        cts.Cancel();

        Assert.ThrowsExactly<OperationCanceledException>(
            () => _ = Engine.Compare(Lines("a"), Lines("b"), TextComparisonOptions.Default, cts.Token));
    }
}
