using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class DifferenceNavigatorTests
{
    /// <summary>Left/right with three separated modified blocks (lines 1, 4, 7).</summary>
    private static async Task<TwoWayComparisonSession> CreateSessionWithThreeDifferencesAsync()
    {
        TwoWayComparisonSession session = new(
            SessionTestHelper.SessionFromText("e0\r\nL1\r\ne2\r\ne3\r\nL4\r\ne5\r\ne6\r\nL7\r\n"),
            SessionTestHelper.SessionFromText("e0\r\nR1\r\ne2\r\ne3\r\nR4\r\ne5\r\ne6\r\nR7\r\n"),
            new PatienceLineDiffEngine());
        Assert.AreEqual(OperationStatus.Completed, await session.RecompareAsync(CancellationToken.None));
        return session;
    }

    [TestMethod]
    public async Task MoveNext_WalksAllDifferencesInOrder_AndDoesNotWrap()
    {
        TwoWayComparisonSession session = await CreateSessionWithThreeDifferencesAsync();
        DifferenceNavigator navigator = new(session);
        Assert.AreEqual(3, navigator.DifferenceCount);

        NavigationTarget first = navigator.MoveNext()!;
        Assert.AreEqual(1, first.DifferenceNumber);
        Assert.AreEqual(1, first.LeftAnchorLine);
        Assert.AreEqual(1, first.RightAnchorLine);

        NavigationTarget second = navigator.MoveNext()!;
        Assert.AreEqual(2, second.DifferenceNumber);
        Assert.AreEqual(4, second.LeftAnchorLine);

        NavigationTarget third = navigator.MoveNext()!;
        Assert.AreEqual(3, third.DifferenceNumber);
        Assert.AreEqual(7, third.LeftAnchorLine);
        Assert.IsFalse(navigator.CanMoveNext, "At the last difference there is no next.");

        Assert.IsNull(navigator.MoveNext(), "Navigation must not wrap.");
        Assert.AreEqual(third.BlockOrdinal, session.SelectedBlockOrdinal, "A refused move keeps the selection.");
    }

    [TestMethod]
    public async Task MovePrevious_DoesNotWrapAtTheFirstDifference()
    {
        TwoWayComparisonSession session = await CreateSessionWithThreeDifferencesAsync();
        DifferenceNavigator navigator = new(session);

        NavigationTarget last = navigator.MovePrevious()!;
        Assert.AreEqual(3, last.DifferenceNumber, "With no selection, MovePrevious targets the last difference.");

        Assert.AreEqual(2, navigator.MovePrevious()!.DifferenceNumber);
        Assert.AreEqual(1, navigator.MovePrevious()!.DifferenceNumber);
        Assert.IsFalse(navigator.CanMovePrevious);
        Assert.IsNull(navigator.MovePrevious(), "Navigation must not wrap backwards.");
        Assert.IsNotNull(session.SelectedBlockOrdinal);
    }

    [TestMethod]
    public async Task AnchorsComeFromThePresentationMap()
    {
        TwoWayComparisonSession session = await CreateSessionWithThreeDifferencesAsync();
        DifferenceNavigator navigator = new(session);

        NavigationTarget target = navigator.MoveNext()!;
        ComparisonPresentationMap map = session.PresentationMap!;
        Assert.AreEqual(map.GetLeftAnchorForBlock(target.BlockOrdinal), target.LeftAnchorLine);
        Assert.AreEqual(map.GetRightAnchorForBlock(target.BlockOrdinal), target.RightAnchorLine);
    }

    [TestMethod]
    public async Task StaleComparison_DisablesNavigation()
    {
        TwoWayComparisonSession session = await CreateSessionWithThreeDifferencesAsync();
        DifferenceNavigator navigator = new(session);
        navigator.MoveNext();

        session.NotifyDocumentEdited(ComparisonSide.Left, "changed\r\n");

        Assert.IsFalse(navigator.CanMoveNext);
        Assert.IsFalse(navigator.CanMovePrevious);
        Assert.IsNull(navigator.MoveNext());
        Assert.IsNull(navigator.MovePrevious());
        Assert.AreEqual(0, navigator.DifferenceCount, "A stale result exposes no navigable differences.");
    }

    [TestMethod]
    public async Task IdenticalDocuments_HaveNothingToNavigate()
    {
        TwoWayComparisonSession session = new(
            SessionTestHelper.SessionFromText("same\r\n"),
            SessionTestHelper.SessionFromText("same\r\n"),
            new PatienceLineDiffEngine());
        await session.RecompareAsync(CancellationToken.None);
        DifferenceNavigator navigator = new(session);

        Assert.AreEqual(0, navigator.DifferenceCount);
        Assert.IsFalse(navigator.CanMoveNext);
        Assert.IsFalse(navigator.CanMovePrevious);
        Assert.IsNull(navigator.MoveNext());
    }
}
