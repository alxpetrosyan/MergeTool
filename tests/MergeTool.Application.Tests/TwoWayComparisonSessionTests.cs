using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.Diff;
using MergeTool.Text.Formatted;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class TwoWayComparisonSessionTests
{
    private static TwoWayComparisonSession CreateSession(string leftText, string rightText, ITextDiffEngine? engine = null)
        => new(
            SessionTestHelper.SessionFromText(leftText, fileName: "left.txt"),
            SessionTestHelper.SessionFromText(rightText, fileName: "right.txt"),
            engine ?? new PatienceLineDiffEngine());

    [TestMethod]
    public async Task Recompare_AppliesResultAndEnablesActions()
    {
        TwoWayComparisonSession session = CreateSession("a\r\nx\r\n", "a\r\ny\r\n");
        Assert.IsTrue(session.IsStale, "No result yet: session starts stale.");
        Assert.IsFalse(session.CanMerge);

        OperationStatus status = await session.RecompareAsync(CancellationToken.None);

        Assert.AreEqual(OperationStatus.Completed, status);
        Assert.IsNotNull(session.LatestResult);
        Assert.IsNotNull(session.PresentationMap);
        Assert.AreEqual(1, session.ComparisonRevision);
        Assert.IsFalse(session.IsStale);
        Assert.IsTrue(session.CanNavigate);
        Assert.IsTrue(session.CanMerge);
    }

    [TestMethod]
    public async Task Edit_InvalidatesResultAndDisablesActions_UntilRecompare()
    {
        TwoWayComparisonSession session = CreateSession("a\r\nx\r\n", "a\r\ny\r\n");
        await session.RecompareAsync(CancellationToken.None);
        session.SelectBlock(session.PresentationMap!.DifferenceBlockOrdinals[0]);
        Assert.IsNotNull(session.SelectedBlockOrdinal);

        session.NotifyDocumentEdited(ComparisonSide.Left, "a\r\nedited\r\n");

        Assert.IsTrue(session.IsStale);
        Assert.IsFalse(session.CanNavigate);
        Assert.IsFalse(session.CanMerge);
        Assert.IsNull(session.SelectedBlockOrdinal, "Selection must not survive invalidation.");
        Assert.IsNotNull(session.LatestResult, "The last result stays visible while stale.");

        await session.RecompareAsync(CancellationToken.None);

        Assert.IsFalse(session.IsStale);
        Assert.IsTrue(session.CanMerge, "Recompare restores action eligibility.");
        Assert.AreEqual(2, session.ComparisonRevision);
    }

    [TestMethod]
    public async Task SelectedBlock_IsClearedWhenNewResultApplies()
    {
        TwoWayComparisonSession session = CreateSession("a\r\nx\r\n", "a\r\ny\r\n");
        await session.RecompareAsync(CancellationToken.None);
        session.SelectBlock(1);

        await session.RecompareAsync(CancellationToken.None);

        Assert.IsNull(session.SelectedBlockOrdinal);
    }

    [TestMethod]
    public async Task SelectBlock_RejectsStaleStateAndInvalidOrdinals()
    {
        TwoWayComparisonSession session = CreateSession("a\r\n", "b\r\n");

        Assert.ThrowsExactly<InvalidOperationException>(() => session.SelectBlock(0));

        await session.RecompareAsync(CancellationToken.None);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.SelectBlock(99));

        session.NotifyDocumentEdited(ComparisonSide.Right, "edited\r\n");
        InvalidOperationException staleError = Assert.ThrowsExactly<InvalidOperationException>(() => session.SelectBlock(0));
        Assert.AreEqual(TwoWayComparisonSession.StaleComparisonMessage, staleError.Message);
    }

    private sealed class GatedEngine : ITextDiffEngine
    {
        private readonly List<(ManualResetEventSlim Gate, IReadOnlyList<DiffBlock> Result)> _calls = [];
        private readonly Lock _lock = new();
        private readonly SemaphoreSlim _callStarted = new(0);

        public int CallCount
        {
            get
            {
                lock (_lock)
                {
                    return _calls.Count;
                }
            }
        }

        public IReadOnlyList<DiffBlock> Compare(
            IReadOnlyList<TextLine> leftLines,
            IReadOnlyList<TextLine> rightLines,
            TextComparisonOptions options,
            CancellationToken cancellationToken)
        {
            ManualResetEventSlim gate = new(initialState: false);
            IReadOnlyList<DiffBlock> result;
            lock (_lock)
            {
                // Each call returns a fresh list instance (distinguishable by reference)
                // with ranges that fit the actual documents.
                result = [new DiffBlock(0, DiffBlockKind.Modified, new LineRange(0, leftLines.Count), new LineRange(0, rightLines.Count))];
                _calls.Add((gate, result));
            }

            _callStarted.Release();
            gate.Wait(cancellationToken);
            return result;
        }

        public async Task WaitForCallAsync(int index)
        {
            while (CallCount <= index)
            {
                await _callStarted.WaitAsync(TimeSpan.FromSeconds(10));
            }
        }

        public void ReleaseCall(int index)
        {
            lock (_lock)
            {
                _calls[index].Gate.Set();
            }
        }

        public IReadOnlyList<DiffBlock> ResultOfCall(int index)
        {
            lock (_lock)
            {
                return _calls[index].Result;
            }
        }
    }

    [TestMethod]
    public async Task StaleCompletion_NeverOverwritesNewerResult()
    {
        GatedEngine engine = new();
        TwoWayComparisonSession session = CreateSession("a\r\n", "b\r\n", engine);

        Task<OperationStatus> older = session.RecompareAsync(CancellationToken.None);
        await engine.WaitForCallAsync(0);
        Task<OperationStatus> newer = session.RecompareAsync(CancellationToken.None);
        await engine.WaitForCallAsync(1);

        // The newest request completes first and applies.
        engine.ReleaseCall(1);
        await newer;
        Assert.AreEqual(1, session.ComparisonRevision);
        IReadOnlyList<DiffBlock> appliedBlocks = session.LatestResult!.Blocks;
        Assert.HasCount(1, appliedBlocks);
        Assert.AreEqual(DiffBlockKind.Modified, appliedBlocks[0].Kind);

        // The older request completes afterwards and must be discarded entirely.
        engine.ReleaseCall(0);
        await older;
        Assert.AreEqual(1, session.ComparisonRevision, "A stale completion must not bump the revision.");
        Assert.AreSame(appliedBlocks, session.LatestResult!.Blocks, "A stale completion must not replace the result.");
    }

    [TestMethod]
    public async Task OlderCompletionFinishingFirst_IsAlsoDiscarded()
    {
        GatedEngine engine = new();
        TwoWayComparisonSession session = CreateSession("a\r\n", "b\r\n", engine);

        Task<OperationStatus> older = session.RecompareAsync(CancellationToken.None);
        await engine.WaitForCallAsync(0);
        Task<OperationStatus> newer = session.RecompareAsync(CancellationToken.None);
        await engine.WaitForCallAsync(1);

        engine.ReleaseCall(0);
        await older;
        Assert.AreEqual(0, session.ComparisonRevision, "The older request became stale when the newer one started.");
        Assert.IsNull(session.LatestResult);

        engine.ReleaseCall(1);
        await newer;
        Assert.AreEqual(1, session.ComparisonRevision);
        Assert.HasCount(1, session.LatestResult!.Blocks);
        Assert.AreEqual(DiffBlockKind.Modified, session.LatestResult.Blocks[0].Kind);
    }

    [TestMethod]
    public async Task Cancellation_KeepsLastAppliedResultVisible()
    {
        GatedEngine engine = new();
        TwoWayComparisonSession session = CreateSession("a\r\n", "b\r\n", engine);

        // Apply a first, successful result.
        Task<OperationStatus> first = session.RecompareAsync(CancellationToken.None);
        await engine.WaitForCallAsync(0);
        engine.ReleaseCall(0);
        Assert.AreEqual(OperationStatus.Completed, await first);
        IReadOnlyList<DiffBlock> appliedBlocks = session.LatestResult!.Blocks;

        // Cancel a second recompare mid-flight.
        using CancellationTokenSource cts = new();
        Task<OperationStatus> second = session.RecompareAsync(cts.Token);
        await engine.WaitForCallAsync(1);
        cts.Cancel();

        Assert.AreEqual(OperationStatus.Cancelled, await second);
        Assert.AreSame(appliedBlocks, session.LatestResult!.Blocks, "Cancelling must leave the last completed result visible.");
        Assert.AreEqual(1, session.ComparisonRevision);
        Assert.AreEqual(OperationStatus.Completed, session.ComparisonStatus, "Status reflects the still-visible completed result.");
    }

    [TestMethod]
    public async Task FormattedJson_DisablesMerge_ButAllowsNavigation()
    {
        const string left = "{\"x\":1}";
        const string right = "{\"x\":2}";
        var options = new TextComparisonOptions(enableIntralineDiff: true, structuredFormat: StructuredFormatKind.Json);
        TwoWayComparisonSession session = CreateSession(left, right);

        OperationStatus status = await session.RecompareAsync(options, CancellationToken.None);

        Assert.AreEqual(OperationStatus.Completed, status);
        Assert.IsTrue(session.IsFormattedProjectionActive);
        Assert.IsNotNull(session.FormattedDisplayLeftLines);
        Assert.IsNotNull(session.FormattedDisplayRightLines);
        Assert.IsTrue(session.CanNavigate);
        Assert.IsFalse(session.CanMerge);
        Assert.IsEmpty(session.Diagnostics);
    }

    [TestMethod]
    public async Task InvalidJson_FallsBackToRaw_WithDiagnostic()
    {
        TwoWayComparisonSession session = CreateSession("{not-json", "{not-json");
        var options = new TextComparisonOptions(structuredFormat: StructuredFormatKind.Json);

        await session.RecompareAsync(options, CancellationToken.None);

        Assert.IsFalse(session.IsFormattedProjectionActive);
        Assert.IsTrue(session.CanMerge);
        Assert.IsTrue(session.Diagnostics.Any(d => d.Code == "MT-FMT-001"));
        Assert.AreEqual("{not-json", session.Left.CurrentText, "Source documents must not be mutated.");
    }

    [TestMethod]
    public async Task FormattedXml_DisablesMerge()
    {
        const string left = "<root><a>1</a></root>";
        const string right = "<root><a>2</a></root>";
        var options = new TextComparisonOptions(structuredFormat: StructuredFormatKind.Xml);
        TwoWayComparisonSession session = CreateSession(left, right);

        await session.RecompareAsync(options, CancellationToken.None);

        Assert.IsTrue(session.IsFormattedProjectionActive);
        Assert.IsFalse(session.CanMerge);
    }

    [TestMethod]
    public async Task DetectMovedBlocks_MarksIdenticalDeletedInsertedPairs()
    {
        const string left = "keep\r\nmoved block\r\ntail\r\n";
        const string right = "keep\r\ntail\r\nmoved block\r\n";
        var options = new TextComparisonOptions(enableIntralineDiff: true, detectMovedBlocks: true);
        TwoWayComparisonSession session = CreateSession(left, right);

        await session.RecompareAsync(options, CancellationToken.None);

        Assert.IsNotEmpty(session.MovedBlockOrdinals);
        Assert.IsTrue(session.CanMerge);
    }
}
