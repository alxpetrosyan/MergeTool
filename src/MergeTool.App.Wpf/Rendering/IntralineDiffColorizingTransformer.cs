using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using MergeTool.Text.Diff;

namespace MergeTool.App.Wpf.Rendering;

/// <summary>
/// AvalonEdit colorizer that paints intraline diff spans from <see cref="PaneRenderModel"/>
/// without mutating document text.
/// </summary>
public sealed class IntralineDiffColorizingTransformer : DocumentColorizingTransformer
{
    private PaneRenderModel _model = PaneRenderModel.Empty;

    public void SetModel(PaneRenderModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        int lineIndex = line.LineNumber - 1;
        PaneInlineLineModel? inline = _model.GetInlineForLine(lineIndex);
        if (inline is null || inline.IsWholeLineFallback || inline.HighlightSegments.Count == 0)
        {
            return;
        }

        foreach (InlineDiffSegment segment in inline.HighlightSegments)
        {
            if (segment.Length <= 0)
            {
                continue;
            }

            int start = line.Offset + segment.StartOffset;
            int end = start + segment.Length;
            ChangeLinePart(start, end, element => element.BackgroundBrush = DiffBlockPalette.IntralineChangedBackground);
        }
    }
}
