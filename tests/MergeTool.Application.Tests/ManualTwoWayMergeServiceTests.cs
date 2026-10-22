using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class ManualTwoWayMergeServiceTests
{
    private static readonly ManualTwoWayMergeService Service = new();

    private static async Task<TwoWayComparisonSession> CompareAsync(
        string leftText, string rightText, bool leftReadOnly = false, bool rightReadOnly = false)
    {
        TwoWayComparisonSession session = new(
            SessionTestHelper.SessionFromText(leftText, isReadOnly: leftReadOnly, fileName: "left.txt"),
            SessionTestHelper.SessionFromText(rightText, isReadOnly: rightReadOnly, fileName: "right.txt"),
            new PatienceLineDiffEngine());
        Assert.AreEqual(OperationStatus.Completed, await session.RecompareAsync(CancellationToken.None));
        return session;
    }

    private static int SingleDifferenceOrdinal(TwoWayComparisonSession session)
    {
        var differences = session.PresentationMap!.DifferenceBlockOrdinals;
        Assert.HasCount(1, differences);
        return differences[0];
    }

    // --- Modified blocks ---

    [TestMethod]
    public async Task ModifiedBlock_LeftToRight_ReplacesTargetRangeExactly()
    {
        TwoWayComparisonSession session = await CompareAsync("one\r\nTWO\r\nthree\r\n", "one\r\ntwo\r\nthree\r\n");

        OperationResult result = Service.CopyBlock(session, SingleDifferenceOrdinal(session), ComparisonSide.Left);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual("one\r\nTWO\r\nthree\r\n", session.Right.CurrentText);
        Assert.AreEqual("one\r\nTWO\r\nthree\r\n", session.Left.CurrentText, "The source document never mutates.");
    }

    [TestMethod]
    public async Task ModifiedBlock_RightToLeft_ReplacesTargetRangeExactly()
    {
        TwoWayComparisonSession session = await CompareAsync("one\r\nTWO\r\nthree\r\n", "one\r\ntwo\r\nthree\r\n");

        OperationResult result = Service.CopyBlock(session, SingleDifferenceOrdinal(session), ComparisonSide.Right);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual("one\r\ntwo\r\nthree\r\n", session.Left.CurrentText);
        Assert.AreEqual("one\r\ntwo\r\nthree\r\n", session.Right.CurrentText, "The source document never mutates.");
    }

    [TestMethod]
    public async Task ModifiedBlock_WithUnequalLineCounts_ReplacesAsOneUnit()
    {
        TwoWayComparisonSession session = await CompareAsync(
            "head\r\nA1\r\nA2\r\nA3\r\ntail\r\n",
            "head\r\nB1\r\ntail\r\n");

        OperationResult result = Service.CopyBlock(session, SingleDifferenceOrdinal(session), ComparisonSide.Left);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual("head\r\nA1\r\nA2\r\nA3\r\ntail\r\n", session.Right.CurrentText);
    }

    // --- Inserted blocks (lines exist only on the right) ---

    [TestMethod]
    public async Task InsertedBlock_RightToLeft_InsertsAtThePlannedAnchor()
    {
        TwoWayComparisonSession session = await CompareAsync("a\r\nb\r\n", "a\r\nx\r\ny\r\nb\r\n");

        OperationResult result = Service.CopyBlock(session, SingleDifferenceOrdinal(session), ComparisonSide.Right);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual("a\r\nx\r\ny\r\nb\r\n", session.Left.CurrentText);
    }

    [TestMethod]
    public async Task InsertedBlock_LeftToRight_DeletesSoTargetMatchesAbsence()
    {
        TwoWayComparisonSession session = await CompareAsync("a\r\nb\r\n", "a\r\nx\r\ny\r\nb\r\n");

        OperationResult result = Service.CopyBlock(session, SingleDifferenceOrdinal(session), ComparisonSide.Left);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual("a\r\nb\r\n", session.Right.CurrentText);
    }

    // --- Deleted blocks (lines exist only on the left) ---

    [TestMethod]
    public async Task DeletedBlock_LeftToRight_InsertsTheMissingLines()
    {
        TwoWayComparisonSession session = await CompareAsync("a\r\ngone\r\nb\r\n", "a\r\nb\r\n");

        OperationResult result = Service.CopyBlock(session, SingleDifferenceOrdinal(session), ComparisonSide.Left);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual("a\r\ngone\r\nb\r\n", session.Right.CurrentText);
    }

    [TestMethod]
    public async Task DeletedBlock_RightToLeft_DeletesTheTargetRange()
    {
        TwoWayComparisonSession session = await CompareAsync("a\r\ngone\r\nb\r\n", "a\r\nb\r\n");

        OperationResult result = Service.CopyBlock(session, SingleDifferenceOrdinal(session), ComparisonSide.Right);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual("a\r\nb\r\n", session.Left.CurrentText);
    }

    // --- Line-ending normalization ---

    [TestMethod]
    public async Task CopiedLines_AdoptTheTargetLineEndingConvention()
    {
        // Left is an LF document, right is CRLF. The copied line must arrive with CRLF.
        TwoWayComparisonSession session = await CompareAsync("a\nNEW\nb\n", "a\r\nb\r\n");

        OperationResult result = Service.CopyBlock(session, SingleDifferenceOrdinal(session), ComparisonSide.Left);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual("a\r\nNEW\r\nb\r\n", session.Right.CurrentText);
    }

    [TestMethod]
    public async Task CopyIntoLfDocument_UsesLfTerminators()
    {
        TwoWayComparisonSession session = await CompareAsync("a\r\nNEW\r\nb\r\n", "a\nb\n");

        OperationResult result = Service.CopyBlock(session, SingleDifferenceOrdinal(session), ComparisonSide.Left);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual("a\r\nNEW\r\nb\r\n", session.Left.CurrentText, "Source stays untouched.");
        Assert.AreEqual("a\nNEW\nb\n", session.Right.CurrentText);
    }

    [TestMethod]
    public async Task CopyingAnUnterminatedFinalLine_KeepsTheTargetEndUnterminated()
    {
        // Left ends without a final newline; after the copy the right document must too.
        TwoWayComparisonSession session = await CompareAsync("a\r\nEND", "a\r\n");

        OperationResult result = Service.CopyBlock(session, SingleDifferenceOrdinal(session), ComparisonSide.Left);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual("a\r\nEND", session.Right.CurrentText);
    }

    // --- Session-state contract after a merge ---

    [TestMethod]
    public async Task SuccessfulMerge_MutatesOnlyTarget_MarksDirtyStale_AndRequiresRecompare()
    {
        TwoWayComparisonSession session = await CompareAsync("one\r\nTWO\r\nthree\r\n", "one\r\ntwo\r\nthree\r\n");
        int leftRevisionBefore = session.Left.DocumentRevision;
        int rightRevisionBefore = session.Right.DocumentRevision;
        int ordinal = SingleDifferenceOrdinal(session);

        Assert.IsTrue(Service.CopyBlock(session, ordinal, ComparisonSide.Left).IsCompleted);

        Assert.AreEqual(leftRevisionBefore, session.Left.DocumentRevision, "Source revision must not move.");
        Assert.AreEqual(rightRevisionBefore + 1, session.Right.DocumentRevision);
        Assert.IsFalse(session.Left.IsDirty);
        Assert.IsTrue(session.Right.IsDirty);
        Assert.IsTrue(session.IsStale);
        Assert.IsFalse(session.CanMerge, "Another merge requires a recompare first.");

        OperationResult second = Service.CopyBlock(session, ordinal, ComparisonSide.Left);
        Assert.IsTrue(second.IsFailed);
        Assert.AreEqual(TwoWayComparisonSession.StaleComparisonMessage, second.Diagnostics[0].Message);

        await session.RecompareAsync(CancellationToken.None);
        Assert.IsTrue(session.CanMerge, "After recompare merging is available again.");
        Assert.IsEmpty(session.PresentationMap!.DifferenceBlockOrdinals, "Documents now match.");
    }

    // --- Refusals ---

    [TestMethod]
    public async Task EditedTargetWithoutRecompare_RefusesWithTheStaleExplanation()
    {
        TwoWayComparisonSession session = await CompareAsync("one\r\nTWO\r\nthree\r\n", "one\r\ntwo\r\nthree\r\n");
        int ordinal = SingleDifferenceOrdinal(session);
        session.NotifyDocumentEdited(ComparisonSide.Right, "one\r\ntwo!\r\nthree\r\n");

        OperationResult result = Service.CopyBlock(session, ordinal, ComparisonSide.Left);

        Assert.IsTrue(result.IsFailed);
        Assert.AreEqual(TwoWayComparisonSession.MergePreconditionRejectedCode, result.Diagnostics[0].Code);
        Assert.AreEqual(TwoWayComparisonSession.StaleComparisonMessage, result.Diagnostics[0].Message);
        Assert.AreEqual("one\r\ntwo!\r\nthree\r\n", session.Right.CurrentText, "Nothing may change on refusal.");
    }

    [TestMethod]
    public async Task ReadOnlyTarget_RefusesTheCopy()
    {
        TwoWayComparisonSession session = await CompareAsync(
            "one\r\nTWO\r\nthree\r\n", "one\r\ntwo\r\nthree\r\n", rightReadOnly: true);

        OperationResult result = Service.CopyBlock(session, SingleDifferenceOrdinal(session), ComparisonSide.Left);

        Assert.IsTrue(result.IsFailed);
        Assert.AreEqual(ManualTwoWayMergeService.ReadOnlyTargetCode, result.Diagnostics[0].Code);
        Assert.AreEqual("one\r\ntwo\r\nthree\r\n", session.Right.CurrentText);
    }
}
