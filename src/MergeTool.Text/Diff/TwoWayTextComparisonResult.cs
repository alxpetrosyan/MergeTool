using MergeTool.Core;

namespace MergeTool.Text.Diff;

/// <summary>Aggregate counts over a comparison's blocks.</summary>
public sealed record DiffSummary(
    int EqualBlockCount,
    int InsertedBlockCount,
    int DeletedBlockCount,
    int ModifiedBlockCount,
    int InsertedLineCount,
    int DeletedLineCount,
    int ModifiedLeftLineCount,
    int ModifiedRightLineCount)
{
    public int DifferenceBlockCount => InsertedBlockCount + DeletedBlockCount + ModifiedBlockCount;

    public bool HasDifferences => DifferenceBlockCount > 0;
}

/// <summary>
/// The deterministic result of one two-way text comparison: which document sessions and
/// revisions were compared, the ordered block model, and summary counts. A merge action is
/// valid only while both document revisions still match this result.
/// </summary>
public sealed record TwoWayTextComparisonResult
{
    private TwoWayTextComparisonResult(
        DocumentSessionId leftDocumentId,
        DocumentSessionId rightDocumentId,
        int leftDocumentRevision,
        int rightDocumentRevision,
        IReadOnlyList<DiffBlock> blocks,
        DiffSummary summary,
        IReadOnlyList<ModifiedLineInlineDiff> inlineDiffs)
    {
        LeftDocumentId = leftDocumentId;
        RightDocumentId = rightDocumentId;
        LeftDocumentRevision = leftDocumentRevision;
        RightDocumentRevision = rightDocumentRevision;
        Blocks = blocks;
        Summary = summary;
        InlineDiffs = inlineDiffs;
    }

    public DocumentSessionId LeftDocumentId { get; }

    public DocumentSessionId RightDocumentId { get; }

    public int LeftDocumentRevision { get; }

    public int RightDocumentRevision { get; }

    /// <summary>Blocks ordered by source position; ordinals are 0..Count-1.</summary>
    public IReadOnlyList<DiffBlock> Blocks { get; }

    public DiffSummary Summary { get; }

    /// <summary>
    /// Optional intraline data for modified line pairs. Empty when intraline diff was disabled
    /// or not computed.
    /// </summary>
    public IReadOnlyList<ModifiedLineInlineDiff> InlineDiffs { get; }

    public static TwoWayTextComparisonResult Create(
        DocumentSessionId leftDocumentId,
        DocumentSessionId rightDocumentId,
        int leftDocumentRevision,
        int rightDocumentRevision,
        IReadOnlyList<DiffBlock> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        return new TwoWayTextComparisonResult(
            leftDocumentId,
            rightDocumentId,
            leftDocumentRevision,
            rightDocumentRevision,
            blocks,
            Summarize(blocks),
            []);
    }

    public TwoWayTextComparisonResult WithInlineDiffs(IReadOnlyList<ModifiedLineInlineDiff> inlineDiffs)
    {
        ArgumentNullException.ThrowIfNull(inlineDiffs);
        return new TwoWayTextComparisonResult(
            LeftDocumentId,
            RightDocumentId,
            LeftDocumentRevision,
            RightDocumentRevision,
            Blocks,
            Summary,
            inlineDiffs);
    }

    private static DiffSummary Summarize(IReadOnlyList<DiffBlock> blocks)
    {
        int equal = 0, inserted = 0, deleted = 0, modified = 0;
        int insertedLines = 0, deletedLines = 0, modifiedLeft = 0, modifiedRight = 0;

        foreach (DiffBlock block in blocks)
        {
            switch (block.Kind)
            {
                case DiffBlockKind.Equal:
                    equal++;
                    break;
                case DiffBlockKind.Inserted:
                    inserted++;
                    insertedLines += block.RightRange.Length;
                    break;
                case DiffBlockKind.Deleted:
                    deleted++;
                    deletedLines += block.LeftRange.Length;
                    break;
                case DiffBlockKind.Modified:
                    modified++;
                    modifiedLeft += block.LeftRange.Length;
                    modifiedRight += block.RightRange.Length;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown block kind '{block.Kind}'.");
            }
        }

        return new DiffSummary(equal, inserted, deleted, modified, insertedLines, deletedLines, modifiedLeft, modifiedRight);
    }
}
