using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class MergePreconditionTests
{
    private static async Task<TwoWayComparisonSession> CreateComparedSessionAsync()
    {
        TwoWayComparisonSession session = new(
            SessionTestHelper.SessionFromText("same\r\nold\r\ntail\r\n", fileName: "left.txt"),
            SessionTestHelper.SessionFromText("same\r\nnew\r\ntail\r\n", fileName: "right.txt"),
            new PatienceLineDiffEngine());
        await session.RecompareAsync(CancellationToken.None);
        return session;
    }

    private static int FirstDifferenceOrdinal(TwoWayComparisonSession session)
        => session.PresentationMap!.DifferenceBlockOrdinals[0];

    [TestMethod]
    public async Task FreshPrecondition_Validates()
    {
        TwoWayComparisonSession session = await CreateComparedSessionAsync();

        MergePrecondition precondition = session.CreateMergePrecondition(FirstDifferenceOrdinal(session), ComparisonSide.Left);
        OperationResult validation = session.ValidateMergePrecondition(precondition);

        Assert.IsTrue(validation.IsCompleted);
    }

    [TestMethod]
    public async Task EditingSourceDocument_RejectsOldPrecondition()
    {
        TwoWayComparisonSession session = await CreateComparedSessionAsync();
        MergePrecondition precondition = session.CreateMergePrecondition(FirstDifferenceOrdinal(session), ComparisonSide.Left);

        session.NotifyDocumentEdited(ComparisonSide.Left, "same\r\nedited\r\ntail\r\n");
        OperationResult validation = session.ValidateMergePrecondition(precondition);

        Assert.IsTrue(validation.IsFailed);
        Assert.AreEqual(TwoWayComparisonSession.StaleComparisonMessage, validation.Diagnostics[0].Message);
    }

    [TestMethod]
    public async Task EditingTargetDocument_RejectsOldPrecondition()
    {
        TwoWayComparisonSession session = await CreateComparedSessionAsync();
        MergePrecondition precondition = session.CreateMergePrecondition(FirstDifferenceOrdinal(session), ComparisonSide.Left);

        session.NotifyDocumentEdited(ComparisonSide.Right, "same\r\nchanged\r\ntail\r\n");

        Assert.IsTrue(session.ValidateMergePrecondition(precondition).IsFailed);
    }

    [TestMethod]
    public async Task RecompareAfterEdit_RejectsPreconditionFromOldComparison()
    {
        TwoWayComparisonSession session = await CreateComparedSessionAsync();
        MergePrecondition precondition = session.CreateMergePrecondition(FirstDifferenceOrdinal(session), ComparisonSide.Left);

        session.NotifyDocumentEdited(ComparisonSide.Left, "same\r\nedited\r\ntail\r\n");
        await session.RecompareAsync(CancellationToken.None);

        Assert.IsTrue(session.CanMerge, "The session itself is mergeable again after recompare.");
        Assert.IsTrue(
            session.ValidateMergePrecondition(precondition).IsFailed,
            "A precondition from an older comparison revision must stay invalid.");
    }

    [TestMethod]
    public async Task TamperedFingerprint_IsRejected()
    {
        TwoWayComparisonSession session = await CreateComparedSessionAsync();
        MergePrecondition precondition = session.CreateMergePrecondition(FirstDifferenceOrdinal(session), ComparisonSide.Left);

        MergePrecondition tampered = precondition with { TargetTextFingerprint = new string('0', 64) };

        Assert.IsTrue(session.ValidateMergePrecondition(tampered).IsFailed);
    }

    [TestMethod]
    public async Task OutOfBoundsRange_IsRejected()
    {
        TwoWayComparisonSession session = await CreateComparedSessionAsync();
        MergePrecondition precondition = session.CreateMergePrecondition(FirstDifferenceOrdinal(session), ComparisonSide.Left);

        MergePrecondition outOfBounds = precondition with { TargetRange = new LineRange(0, 999) };

        Assert.IsTrue(session.ValidateMergePrecondition(outOfBounds).IsFailed);
    }

    [TestMethod]
    public async Task CreatingPreconditionOnStaleSession_Throws()
    {
        TwoWayComparisonSession session = await CreateComparedSessionAsync();
        session.NotifyDocumentEdited(ComparisonSide.Left, "different\r\n");

        InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(
            () => session.CreateMergePrecondition(0, ComparisonSide.Left));
        Assert.AreEqual(TwoWayComparisonSession.StaleComparisonMessage, error.Message);
    }
}
