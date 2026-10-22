namespace MergeTool.Text.Diff;

/// <summary>
/// Computes the ordered block model of a two-way line comparison. Implementations must be
/// deterministic: the same inputs always produce the same block sequence and ranges.
/// </summary>
public interface ITextDiffEngine
{
    IReadOnlyList<DiffBlock> Compare(
        IReadOnlyList<TextLine> leftLines,
        IReadOnlyList<TextLine> rightLines,
        TextComparisonOptions options,
        CancellationToken cancellationToken);
}
