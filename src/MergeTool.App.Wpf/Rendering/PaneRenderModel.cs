using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text.Diff;

namespace MergeTool.App.Wpf.Rendering;

/// <summary>
/// Per-line inline highlight data for one pane line. When <see cref="IsWholeLineFallback"/>
/// is true, only whole-line modified styling applies.
/// </summary>
public sealed record PaneInlineLineModel(
    IReadOnlyList<InlineDiffSegment> HighlightSegments,
    bool IsWholeLineFallback)
{
    public static PaneInlineLineModel None { get; } = new([], IsWholeLineFallback: false);
}

/// <summary>
/// Semantic per-line render classification for one editor pane, derived exclusively from the
/// comparison result (via <see cref="ComparisonPresentationMap"/>). The renderer never
/// rescans editor text to infer differences; this model is the only source of block colors.
/// </summary>
public sealed class PaneRenderModel
{
    private readonly DiffBlockKind?[] _kindByLine;
    private readonly PaneInlineLineModel?[] _inlineByLine;
    private readonly bool[] _movedByLine;

    private PaneRenderModel(DiffBlockKind?[] kindByLine, PaneInlineLineModel?[] inlineByLine, bool[] movedByLine)
    {
        _kindByLine = kindByLine;
        _inlineByLine = inlineByLine;
        _movedByLine = movedByLine;
    }

    public static PaneRenderModel Empty { get; } = new([], [], []);

    public int LineCount => _kindByLine.Length;

    /// <summary>
    /// Builds the per-line classification for <paramref name="side"/> of the given map.
    /// Equal lines have no classification (default editor background).
    /// </summary>
    public static PaneRenderModel Create(
        ComparisonPresentationMap map,
        ComparisonSide side,
        int lineCount,
        IReadOnlyList<ModifiedLineInlineDiff>? inlineDiffs = null,
        IReadOnlySet<int>? movedBlockOrdinals = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentOutOfRangeException.ThrowIfNegative(lineCount);
        if (side is not (ComparisonSide.Left or ComparisonSide.Right))
        {
            throw new ArgumentOutOfRangeException(nameof(side), side, "A render model needs a concrete side.");
        }

        var kinds = new DiffBlockKind?[lineCount];
        var inline = new PaneInlineLineModel?[lineCount];
        var moved = new bool[lineCount];

        foreach (DiffBlock block in map.Blocks)
        {
            if (!block.IsDifference)
            {
                continue;
            }

            bool isMoved = movedBlockOrdinals?.Contains(block.Ordinal) == true;
            LineRange range = side == ComparisonSide.Left ? block.LeftRange : block.RightRange;
            int end = Math.Min(range.EndExclusive, lineCount);
            for (int line = range.StartIndex; line < end; line++)
            {
                kinds[line] = block.Kind;
                if (isMoved)
                {
                    moved[line] = true;
                }
            }
        }

        if (inlineDiffs is { Count: > 0 })
        {
            foreach (ModifiedLineInlineDiff entry in inlineDiffs)
            {
                InlineDiffLinePairResult pair = entry.LinePair;
                int lineIndex = side == ComparisonSide.Left ? pair.LeftLineIndex : pair.RightLineIndex;
                if (lineIndex < 0 || lineIndex >= lineCount)
                {
                    continue;
                }

                IReadOnlyList<InlineDiffSegment> segments = side == ComparisonSide.Left
                    ? pair.LeftHighlightSegments.ToArray()
                    : pair.RightHighlightSegments.ToArray();

                inline[lineIndex] = new PaneInlineLineModel(segments, pair.IsWholeLineFallback);
            }
        }

        return new PaneRenderModel(kinds, inline, moved);
    }

    /// <summary>The difference kind covering this zero-based line, or null for equal lines.</summary>
    public DiffBlockKind? GetKindForLine(int line)
        => line >= 0 && line < _kindByLine.Length ? _kindByLine[line] : null;

    /// <summary>True when this line belongs to a moved deleted/inserted pair.</summary>
    public bool IsMovedLine(int line)
        => line >= 0 && line < _movedByLine.Length && _movedByLine[line];

    /// <summary>Inline highlight model for the line, or null when none applies.</summary>
    public PaneInlineLineModel? GetInlineForLine(int line)
        => line >= 0 && line < _inlineByLine.Length ? _inlineByLine[line] : null;
}
