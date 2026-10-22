using MergeTool.Application.Operations;
using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.Diff;
using MergeTool.Text.ThreeWay;

namespace MergeTool.Application.Sessions;

/// <summary>
/// Owns one 3-way comparison lifecycle: base, left, and right document sessions plus the
/// derived region model from two base-relative 2-way comparisons.
/// </summary>
public sealed class ThreeWayComparisonSession
{
    public const string StaleComparisonMessage = "Comparison is stale. Recompare before continuing.";

    private readonly ITextDiffEngine _diffEngine;
    private readonly OperationRequestCoordinator _coordinator = new();

    public ThreeWayComparisonSession(
        TextDocumentSession baseDocument,
        TextDocumentSession left,
        TextDocumentSession right,
        ITextDiffEngine diffEngine)
    {
        ArgumentNullException.ThrowIfNull(baseDocument);
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(diffEngine);

        Base = baseDocument;
        Left = left;
        Right = right;
        _diffEngine = diffEngine;
    }

    public ComparisonSessionId Id { get; } = ComparisonSessionId.New();

    public TextDocumentSession Base { get; }

    public TextDocumentSession Left { get; }

    public TextDocumentSession Right { get; }

    public ThreeWayComparisonResult? LatestResult { get; private set; }

    public int ComparisonRevision { get; private set; }

    public OperationStatus ComparisonStatus { get; private set; } = OperationStatus.Idle;

    public IReadOnlyList<Diagnostic> Diagnostics { get; private set; } = [];

    public int? SelectedRegionOrdinal { get; private set; }

    public bool IsStale =>
        LatestResult is null
        || LatestResult.BaseRevision != Base.DocumentRevision
        || LatestResult.LeftRevision != Left.DocumentRevision
        || LatestResult.RightRevision != Right.DocumentRevision;

    public bool CanNavigate => !IsStale && ComparisonStatus != OperationStatus.Running;

    public async Task<OperationStatus> RecompareAsync(
        TextComparisonOptions options,
        CancellationToken cancellationToken)
    {
        ComparisonRequestId requestId = _coordinator.BeginRequest();
        ComparisonStatus = OperationStatus.Running;

        IReadOnlyList<TextLine> baseLines = Base.CurrentLines;
        IReadOnlyList<TextLine> leftLines = Left.CurrentLines;
        IReadOnlyList<TextLine> rightLines = Right.CurrentLines;
        int baseRevision = Base.DocumentRevision;
        int leftRevision = Left.DocumentRevision;
        int rightRevision = Right.DocumentRevision;

        try
        {
            IReadOnlyList<DiffBlock> baseLeftBlocks = await ComparePairAsync(
                baseLines,
                leftLines,
                options,
                cancellationToken).ConfigureAwait(false);
            IReadOnlyList<DiffBlock> baseRightBlocks = await ComparePairAsync(
                baseLines,
                rightLines,
                options,
                cancellationToken).ConfigureAwait(false);

            if (!_coordinator.IsCurrent(requestId))
            {
                return ComparisonStatus;
            }

            ThreeWayComparisonResult result = ThreeWayComparisonBuilder.Build(
                baseLines,
                leftLines,
                rightLines,
                baseLeftBlocks,
                baseRightBlocks,
                baseRevision,
                leftRevision,
                rightRevision);

            LatestResult = result;
            ComparisonRevision++;
            SelectedRegionOrdinal = null;
            Diagnostics = [];
            ComparisonStatus = OperationStatus.Completed;
            return OperationStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            if (_coordinator.IsCurrent(requestId))
            {
                ComparisonStatus = LatestResult is null ? OperationStatus.Idle : OperationStatus.Completed;
            }

            return OperationStatus.Cancelled;
        }
    }

    public void SelectRegion(int regionOrdinal)
    {
        if (!CanNavigate || LatestResult is null)
        {
            throw new InvalidOperationException(StaleComparisonMessage);
        }

        ArgumentOutOfRangeException.ThrowIfNegative(regionOrdinal);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(regionOrdinal, LatestResult.Regions.Count);
        SelectedRegionOrdinal = regionOrdinal;
    }

    private Task<IReadOnlyList<DiffBlock>> ComparePairAsync(
        IReadOnlyList<TextLine> leftLines,
        IReadOnlyList<TextLine> rightLines,
        TextComparisonOptions options,
        CancellationToken cancellationToken)
        => Task.Run(
            () => _diffEngine.Compare(leftLines, rightLines, options, cancellationToken),
            cancellationToken);
}
