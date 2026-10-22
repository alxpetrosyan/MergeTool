using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Sessions;

/// <summary>
/// Applies one manual block copy between the two documents of a comparison session.
/// The copy direction decides source and target; the block's ranges decide the operation:
/// replace (modified), insert at the planned anchor (target range empty), or delete
/// (source range empty). Copied source lines are normalized to the target document's
/// line-ending convention. Only the target session mutates; the comparison becomes stale
/// and a recompare is required before the next merge.
/// </summary>
public sealed class ManualTwoWayMergeService
{
    public const string ReadOnlyTargetCode = "MT-MRG-001";

    public const string ReadOnlyTargetMessage = "The target document is read-only. The change cannot be applied.";

    /// <summary>
    /// Copies block <paramref name="blockOrdinal"/> of the latest result from
    /// <paramref name="sourceSide"/> onto the other document. Refuses with a safe
    /// explanation when the comparison is stale or the precondition no longer matches.
    /// </summary>
    public OperationResult CopyBlock(TwoWayComparisonSession session, int blockOrdinal, ComparisonSide sourceSide)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!session.CanMerge)
        {
            return OperationResult.Failed(Diagnostic.Error(
                TwoWayComparisonSession.MergePreconditionRejectedCode,
                TwoWayComparisonSession.StaleComparisonMessage));
        }

        (TextDocumentSession source, TextDocumentSession target, ComparisonSide targetSide) = sourceSide switch
        {
            ComparisonSide.Left => (session.Left, session.Right, ComparisonSide.Right),
            ComparisonSide.Right => (session.Right, session.Left, ComparisonSide.Left),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceSide), sourceSide, "A merge needs a concrete source side."),
        };

        if (target.IsReadOnly)
        {
            return OperationResult.Failed(Diagnostic.Error(
                ReadOnlyTargetCode,
                ReadOnlyTargetMessage,
                path: target.Metadata.NormalizedFullPath));
        }

        MergePrecondition precondition;
        try
        {
            precondition = session.CreateMergePrecondition(blockOrdinal, sourceSide);
        }
        catch (InvalidOperationException)
        {
            return OperationResult.Failed(Diagnostic.Error(
                TwoWayComparisonSession.MergePreconditionRejectedCode,
                TwoWayComparisonSession.StaleComparisonMessage));
        }

        // Defense in depth: the same validation gate a deferred merge would go through.
        OperationResult validation = session.ValidateMergePrecondition(precondition);
        if (!validation.IsCompleted)
        {
            return validation;
        }

        string mergedText = BuildMergedTargetText(
            source.CurrentLines,
            precondition.SourceRange,
            target.CurrentLines,
            precondition.TargetRange,
            target.Metadata.DominantLineEnding);

        session.NotifyDocumentEdited(targetSide, mergedText);
        return OperationResult.Completed();
    }

    /// <summary>
    /// Builds the target text with the source range copied over the target range.
    /// Copied lines adopt the target's line-ending convention; a copied line without a
    /// terminator (source end-of-file) stays unterminated only if it ends the new document.
    /// </summary>
    internal static string BuildMergedTargetText(
        IReadOnlyList<TextLine> sourceLines,
        LineRange sourceRange,
        IReadOnlyList<TextLine> targetLines,
        LineRange targetRange,
        LineEndingStyle targetConvention)
    {
        string conventionText = TextComposer.GetLineEndingText(targetConvention);
        List<(string Content, string Terminator)> merged = new(
            targetLines.Count - targetRange.Length + sourceRange.Length);

        for (int i = 0; i < targetRange.StartIndex; i++)
        {
            merged.Add((targetLines[i].Content, TextComposer.GetTerminatorText(targetLines[i].Terminator)));
        }

        for (int i = sourceRange.StartIndex; i < sourceRange.EndExclusive; i++)
        {
            string terminator = sourceLines[i].Terminator == LineTerminator.None
                ? string.Empty
                : conventionText;
            merged.Add((sourceLines[i].Content, terminator));
        }

        for (int i = targetRange.EndExclusive; i < targetLines.Count; i++)
        {
            merged.Add((targetLines[i].Content, TextComposer.GetTerminatorText(targetLines[i].Terminator)));
        }

        // Interior lines must always be terminated; only the final line may omit it.
        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < merged.Count; i++)
        {
            builder.Append(merged[i].Content);
            builder.Append(i < merged.Count - 1 && merged[i].Terminator.Length == 0
                ? conventionText
                : merged[i].Terminator);
        }

        return builder.ToString();
    }
}
