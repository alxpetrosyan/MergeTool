using MergeTool.Text.Diff;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class BoundedIntralineDiffCalculatorTests
{
    private static readonly BoundedIntralineDiffCalculator Calculator = new();

    private static InlineDiffLinePairResult Compute(
        string left,
        string right,
        IntralineDiffBounds? bounds = null)
        => Calculator.Compute(left, right, 0, 0, bounds ?? IntralineDiffBounds.Default);

    [TestMethod]
    public void ReplacementSpans_AreDetectedOnBothSides()
    {
        InlineDiffLinePairResult result = Compute("hello", "hallo");

        Assert.IsFalse(result.IsWholeLineFallback);
        Assert.HasCount(1, result.LeftHighlightSegments.ToList());
        Assert.HasCount(1, result.RightHighlightSegments.ToList());

        InlineDiffSegment left = result.LeftHighlightSegments.Single();
        InlineDiffSegment right = result.RightHighlightSegments.Single();
        Assert.AreEqual(InlineDiffSegmentKind.Replacement, left.Kind);
        Assert.AreEqual(InlineDiffSegmentKind.Replacement, right.Kind);
        Assert.AreEqual(1, left.StartOffset);
        Assert.AreEqual(1, left.Length);
        Assert.AreEqual(1, right.StartOffset);
        Assert.AreEqual(1, right.Length);
    }

    [TestMethod]
    public void InsertionSpans_AreDetectedOnRightSideOnly()
    {
        InlineDiffLinePairResult result = Compute("abc", "abXc");

        Assert.IsFalse(result.IsWholeLineFallback);
        Assert.IsEmpty(result.LeftHighlightSegments.ToList());
        InlineDiffSegment insertion = result.RightHighlightSegments.Single();
        Assert.AreEqual(InlineDiffSegmentKind.Insertion, insertion.Kind);
        Assert.AreEqual(2, insertion.StartOffset);
        Assert.AreEqual(1, insertion.Length);
    }

    [TestMethod]
    public void DeletionSpans_AreDetectedOnLeftSideOnly()
    {
        InlineDiffLinePairResult result = Compute("abXc", "abc");

        Assert.IsFalse(result.IsWholeLineFallback);
        Assert.IsEmpty(result.RightHighlightSegments.ToList());
        InlineDiffSegment deletion = result.LeftHighlightSegments.Single();
        Assert.AreEqual(InlineDiffSegmentKind.Deletion, deletion.Kind);
        Assert.AreEqual(2, deletion.StartOffset);
        Assert.AreEqual(1, deletion.Length);
    }

    [TestMethod]
    public void Whitespace_IsSignificant()
    {
        InlineDiffLinePairResult result = Compute("a b", "a  b");

        Assert.IsFalse(result.IsWholeLineFallback);
        InlineDiffSegment right = result.RightHighlightSegments.Single();
        Assert.AreEqual(InlineDiffSegmentKind.Insertion, right.Kind);
        Assert.AreEqual(1, right.Length, "An extra space must produce a one-code-unit highlight.");
    }

    [TestMethod]
    public void SurrogatePairs_AreNotSplit()
    {
        const string left = "a\uD83D\uDE00b";
        const string right = "a\uD83D\uDE01b";
        InlineDiffLinePairResult result = Compute(left, right);

        Assert.IsFalse(result.IsWholeLineFallback);
        InlineDiffSegment leftSeg = result.LeftHighlightSegments.Single();
        InlineDiffSegment rightSeg = result.RightHighlightSegments.Single();
        Assert.AreEqual(1, leftSeg.StartOffset);
        Assert.AreEqual(2, leftSeg.Length, "Surrogate pair must stay one UTF-16 span.");
        Assert.AreEqual(1, rightSeg.StartOffset);
        Assert.AreEqual(2, rightSeg.Length);
    }

    [TestMethod]
    public void LongLine_ExceedingMaxLength_ReturnsWholeLineFallback()
    {
        string longLine = new('x', IntralineDiffBounds.Default.MaxLineLength + 1);
        InlineDiffLinePairResult result = Compute(longLine, longLine);

        Assert.IsTrue(result.IsWholeLineFallback);
        Assert.IsEmpty(result.LeftSegments);
        Assert.IsEmpty(result.RightSegments);
    }

    [TestMethod]
    public void WorkBudgetExceeded_ReturnsWholeLineFallback()
    {
        var bounds = new IntralineDiffBounds(maxLineLength: 10_000, maxWorkBudget: 4);
        InlineDiffLinePairResult result = Compute("abcd", "wxyz", bounds);

        Assert.IsTrue(result.IsWholeLineFallback);
    }
}
