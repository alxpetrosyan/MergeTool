using MergeTool.Text.Diff;
using MergeTool.Text.ThreeWay;

namespace MergeTool.App.Wpf.Rendering;

/// <summary>Per-line 3-way region classification for one inspection pane.</summary>
public sealed class ThreeWayPaneRenderModel
{
    private readonly ThreeWayRegionKind?[] _kindByLine;

    private ThreeWayPaneRenderModel(ThreeWayRegionKind?[] kindByLine) => _kindByLine = kindByLine;

    public static ThreeWayPaneRenderModel Empty { get; } = new([]);

    public int LineCount => _kindByLine.Length;

    public static ThreeWayPaneRenderModel Create(
        ThreeWayComparisonResult result,
        ThreeWayDocumentSide side,
        int lineCount)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfNegative(lineCount);

        var kinds = new ThreeWayRegionKind?[lineCount];
        foreach (ThreeWayRegion region in result.Regions)
        {
            if (!region.IsDifference)
            {
                continue;
            }

            if (!ShouldPaint(side, region.Kind))
            {
                continue;
            }

            LineRange range = side switch
            {
                ThreeWayDocumentSide.Base => region.BaseRange,
                ThreeWayDocumentSide.Left => region.LeftRange,
                ThreeWayDocumentSide.Right => region.RightRange,
                _ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
            };

            int end = Math.Min(range.EndExclusive, lineCount);
            for (int line = range.StartIndex; line < end; line++)
            {
                kinds[line] = region.Kind;
            }
        }

        return new ThreeWayPaneRenderModel(kinds);
    }

    public ThreeWayRegionKind? GetKindForLine(int line)
        => line >= 0 && line < _kindByLine.Length ? _kindByLine[line] : null;

    private static bool ShouldPaint(ThreeWayDocumentSide side, ThreeWayRegionKind kind) => kind switch
    {
        ThreeWayRegionKind.LeftOnlyChange => side is ThreeWayDocumentSide.Base or ThreeWayDocumentSide.Left,
        ThreeWayRegionKind.RightOnlyChange => side is ThreeWayDocumentSide.Base or ThreeWayDocumentSide.Right,
        ThreeWayRegionKind.SameChange or ThreeWayRegionKind.Divergent => true,
        _ => false,
    };
}
