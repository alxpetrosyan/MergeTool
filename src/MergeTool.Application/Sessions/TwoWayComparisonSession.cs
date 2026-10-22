using MergeTool.Application.Operations;
using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.Diff;
using MergeTool.Text.Formatted;

namespace MergeTool.Application.Sessions;

/// <summary>
/// The single owner of one two-way comparison's lifecycle: it schedules recompares, rejects
/// stale completions, tracks staleness against document revisions, and gates navigation and
/// merge eligibility. WPF code renders this state and forwards intent; nothing else manages
/// comparison state.
/// Public members are called from the UI thread; the diff computation itself runs on the
/// thread pool and publishes results back through the current-request check.
/// </summary>
public sealed class TwoWayComparisonSession
{
    /// <summary>Exact user-facing message required by the architecture guardrails.</summary>
    public const string StaleComparisonMessage = "Comparison is stale. Recompare before merging.";

    public const string MergePreconditionRejectedCode = "MT-CMP-001";

    private readonly ITextDiffEngine _diffEngine;
    private readonly OperationRequestCoordinator _coordinator = new();

    public TwoWayComparisonSession(TextDocumentSession left, TextDocumentSession right, ITextDiffEngine diffEngine)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(diffEngine);

        Left = left;
        Right = right;
        _diffEngine = diffEngine;
    }

    public ComparisonSessionId Id { get; } = ComparisonSessionId.New();

    public TextDocumentSession Left { get; }

    public TextDocumentSession Right { get; }

    /// <summary>The newest applied comparison result; never replaced by older completions.</summary>
    public TwoWayTextComparisonResult? LatestResult { get; private set; }

    public ComparisonPresentationMap? PresentationMap { get; private set; }

    /// <summary>Increments each time a new result is applied.</summary>
    public int ComparisonRevision { get; private set; }

    public OperationStatus ComparisonStatus { get; private set; } = OperationStatus.Idle;

    public IReadOnlyList<Diagnostic> Diagnostics { get; private set; } = [];

    /// <summary>Comparison rules used to produce <see cref="LatestResult"/>, if any.</summary>
    public TextComparisonOptions? AppliedComparisonOptions { get; private set; }

    /// <summary>True when the latest comparison diffed pretty-printed JSON/XML display lines.</summary>
    public bool IsFormattedProjectionActive { get; private set; }

    /// <summary>Formatted left lines shown in the editor when <see cref="IsFormattedProjectionActive"/>.</summary>
    public IReadOnlyList<TextLine>? FormattedDisplayLeftLines { get; private set; }

    /// <summary>Formatted right lines shown in the editor when <see cref="IsFormattedProjectionActive"/>.</summary>
    public IReadOnlyList<TextLine>? FormattedDisplayRightLines { get; private set; }

    /// <summary>Block ordinals classified as moved (deleted/inserted pairs with identical text).</summary>
    public IReadOnlySet<int> MovedBlockOrdinals { get; private set; } = new HashSet<int>();

    /// <summary>Ordinal of the currently selected block within <see cref="LatestResult"/>, if any.</summary>
    public int? SelectedBlockOrdinal { get; private set; }

    /// <summary>
    /// True until a result exists that was produced from both documents' current revisions.
    /// </summary>
    public bool IsStale =>
        LatestResult is null
        || LatestResult.LeftDocumentRevision != Left.DocumentRevision
        || LatestResult.RightDocumentRevision != Right.DocumentRevision;

    /// <summary>Block navigation is allowed only on a current, completed result.</summary>
    public bool CanNavigate => !IsStale && ComparisonStatus != OperationStatus.Running;

    /// <summary>Merge actions are allowed only on a current, completed result in raw text mode.</summary>
    public bool CanMerge => CanNavigate && !IsFormattedProjectionActive;

    /// <summary>
    /// Runs a comparison of the current document contents. Only the newest request may apply
    /// its completion; older completions are discarded entirely. Cancellation keeps the last
    /// applied result visible.
    /// </summary>
    public async Task<OperationStatus> RecompareAsync(CancellationToken cancellationToken)
        => await RecompareAsync(TextComparisonOptions.WithIntralineDiff, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Runs a comparison of the current document contents using <paramref name="options"/>.
    /// Only the newest request may apply its completion; older completions are discarded entirely.
    /// </summary>
    public async Task<OperationStatus> RecompareAsync(TextComparisonOptions options, CancellationToken cancellationToken)
    {
        ComparisonRequestId requestId = _coordinator.BeginRequest();
        ComparisonStatus = OperationStatus.Running;

        // Capture immutable inputs at request time; later edits replace these references.
        IReadOnlyList<TextLine> leftLines = Left.CurrentLines;
        IReadOnlyList<TextLine> rightLines = Right.CurrentLines;
        int leftRevision = Left.DocumentRevision;
        int rightRevision = Right.DocumentRevision;
        bool isFormattedActive = false;
        IReadOnlyList<TextLine>? formattedLeftLines = null;
        IReadOnlyList<TextLine>? formattedRightLines = null;
        List<Diagnostic> formatDiagnostics = [];

        if (options.StructuredFormat != StructuredFormatKind.None)
        {
            FormattedProjectionResult formatted = StructuredTextProjection.TryProject(
                Left.CurrentText,
                Right.CurrentText,
                options.StructuredFormat);
            formatDiagnostics.AddRange(formatted.Diagnostics);
            if (formatted.IsSuccess
                && formatted.FormattedLeftLines is not null
                && formatted.FormattedRightLines is not null)
            {
                formattedLeftLines = StructuredTextProjection.ToTextLines(formatted.FormattedLeftLines);
                formattedRightLines = StructuredTextProjection.ToTextLines(formatted.FormattedRightLines);
                leftLines = formattedLeftLines;
                rightLines = formattedRightLines;
                isFormattedActive = true;
            }
        }

        try
        {
            ComparisonProjectionBuildResult projectionResult = await Task.Run(
                () => ComparisonProjector.Build(leftLines, rightLines, options),
                cancellationToken).ConfigureAwait(false);

            if (!projectionResult.IsSuccess)
            {
                if (_coordinator.IsCurrent(requestId))
                {
                    Diagnostics = projectionResult.Diagnostics;
                    ComparisonStatus = OperationStatus.Failed;
                }

                return OperationStatus.Failed;
            }

            ComparisonProjection projection = projectionResult.Projection!;
            IReadOnlyList<DiffBlock> projectedBlocks = await Task.Run(
                () => _diffEngine.Compare(
                    projection.ProjectedLeftLines,
                    projection.ProjectedRightLines,
                    options,
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!_coordinator.IsCurrent(requestId))
            {
                return ComparisonStatus; // Stale completion: discard entirely.
            }

            IReadOnlyList<DiffBlock> blocks = isFormattedActive
                ? projectedBlocks
                : ComparisonProjectionMapper.MapToOriginal(projectedBlocks, projection);

            TwoWayTextComparisonResult result = TwoWayTextComparisonResult.Create(
                Left.Id, Right.Id, leftRevision, rightRevision, blocks);

            result = IntralineDiffEnricher.Enrich(result, leftLines, rightLines, options);

            LatestResult = result;
            AppliedComparisonOptions = options;
            IsFormattedProjectionActive = isFormattedActive;
            FormattedDisplayLeftLines = formattedLeftLines;
            FormattedDisplayRightLines = formattedRightLines;
            MovedBlockOrdinals = options.DetectMovedBlocks && !isFormattedActive
                ? MovedBlockDetector
                    .DetectMovedBlockOrdinals(blocks, Left.CurrentLines, Right.CurrentLines)
                    .ToHashSet()
                : new HashSet<int>();
            PresentationMap = ComparisonPresentationMap.Create(result, leftLines.Count, rightLines.Count);
            ComparisonRevision++;
            SelectedBlockOrdinal = null; // Selection rebinding is the navigator's job (MVP-08).
            Diagnostics = formatDiagnostics;
            ComparisonStatus = OperationStatus.Completed;
            return ComparisonStatus;
        }
        catch (OperationCanceledException)
        {
            if (_coordinator.IsCurrent(requestId))
            {
                // Controlled terminal state; last applied result stays visible.
                ComparisonStatus = LatestResult is null ? OperationStatus.Idle : OperationStatus.Completed;
            }

            return OperationStatus.Cancelled;
        }
    }

    /// <summary>
    /// Notifies the session that a pane's text changed: bumps that document's revision,
    /// marks it dirty, and invalidates navigation/merge until the next recompare completes.
    /// </summary>
    public void NotifyDocumentEdited(ComparisonSide side, string newText)
    {
        TextDocumentSession target = side switch
        {
            ComparisonSide.Left => Left,
            ComparisonSide.Right => Right,
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Edit notifications need a concrete side."),
        };

        target.ReplaceText(newText);
        if (IsStale)
        {
            SelectedBlockOrdinal = null;
        }
    }

    /// <summary>Selects a difference block of the latest result (navigation).</summary>
    public void SelectBlock(int blockOrdinal)
    {
        if (!CanNavigate || LatestResult is null)
        {
            throw new InvalidOperationException(StaleComparisonMessage);
        }

        ArgumentOutOfRangeException.ThrowIfNegative(blockOrdinal);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(blockOrdinal, LatestResult.Blocks.Count);
        SelectedBlockOrdinal = blockOrdinal;
    }

    /// <summary>
    /// Captures the precondition proving a copy of <paramref name="blockOrdinal"/> from
    /// <paramref name="sourceSide"/> is safe right now.
    /// </summary>
    public MergePrecondition CreateMergePrecondition(int blockOrdinal, ComparisonSide sourceSide)
    {
        if (!CanMerge || LatestResult is null)
        {
            throw new InvalidOperationException(StaleComparisonMessage);
        }

        DiffBlock block = LatestResult.Blocks[blockOrdinal];
        (TextDocumentSession source, TextDocumentSession target, LineRange sourceRange, LineRange targetRange) = sourceSide switch
        {
            ComparisonSide.Left => (Left, Right, block.LeftRange, block.RightRange),
            ComparisonSide.Right => (Right, Left, block.RightRange, block.LeftRange),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceSide), sourceSide, "A merge needs a concrete source side."),
        };

        return new MergePrecondition(
            ComparisonRevision,
            sourceSide,
            source.DocumentRevision,
            target.DocumentRevision,
            sourceRange,
            targetRange,
            MergePrecondition.FingerprintRange(source.CurrentLines, sourceRange),
            MergePrecondition.FingerprintRange(target.CurrentLines, targetRange));
    }

    /// <summary>
    /// Re-validates a previously captured precondition against the session's current state.
    /// Any mismatch rejects the operation with a safe diagnostic.
    /// </summary>
    public OperationResult ValidateMergePrecondition(MergePrecondition precondition)
    {
        ArgumentNullException.ThrowIfNull(precondition);

        if (IsStale
            || precondition.ComparisonRevision != ComparisonRevision)
        {
            return Rejected();
        }

        (TextDocumentSession source, TextDocumentSession target) = precondition.SourceSide == ComparisonSide.Left
            ? (Left, Right)
            : (Right, Left);

        if (precondition.SourceDocumentRevision != source.DocumentRevision
            || precondition.TargetDocumentRevision != target.DocumentRevision)
        {
            return Rejected();
        }

        if (precondition.SourceRange.EndExclusive > source.CurrentLines.Count
            || precondition.TargetRange.EndExclusive > target.CurrentLines.Count)
        {
            return Rejected();
        }

        if (MergePrecondition.FingerprintRange(source.CurrentLines, precondition.SourceRange) != precondition.SourceTextFingerprint
            || MergePrecondition.FingerprintRange(target.CurrentLines, precondition.TargetRange) != precondition.TargetTextFingerprint)
        {
            return Rejected();
        }

        return OperationResult.Completed();

        static OperationResult Rejected() => OperationResult.Failed(
            Diagnostic.Error(MergePreconditionRejectedCode, StaleComparisonMessage));
    }
}
