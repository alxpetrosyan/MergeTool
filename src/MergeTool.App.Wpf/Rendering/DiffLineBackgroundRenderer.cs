using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;
using MergeTool.Text.Diff;

namespace MergeTool.App.Wpf.Rendering;

/// <summary>
/// AvalonEdit background renderer painting the fixed semantic block styles. It draws purely
/// from a <see cref="PaneRenderModel"/> (derived from the comparison result); it never
/// inspects the editor text.
/// </summary>
public sealed class DiffLineBackgroundRenderer : IBackgroundRenderer
{
    private PaneRenderModel _model = PaneRenderModel.Empty;

    public KnownLayer Layer => KnownLayer.Background;

    /// <summary>Replaces the semantic model (new comparison applied or cleared on stale edits).</summary>
    public void SetModel(PaneRenderModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(textView);
        ArgumentNullException.ThrowIfNull(drawingContext);
        if (_model.LineCount == 0 || !textView.VisualLinesValid)
        {
            return;
        }

        foreach (VisualLine visualLine in textView.VisualLines)
        {
            int lineIndex = visualLine.FirstDocumentLine.LineNumber - 1;
            Brush? brush = DiffBlockPalette.GetBackground(_model.GetKindForLine(lineIndex));
            if (brush is not null)
            {
                // Cover the full horizontal extent even when scrolled sideways.
                var rect = new Rect(
                    0,
                    visualLine.VisualTop - textView.VerticalOffset,
                    Math.Max(textView.ActualWidth, textView.HorizontalOffset + textView.ActualWidth),
                    visualLine.Height);
                drawingContext.DrawRectangle(brush, pen: null, rect);
            }

            if (_model.IsMovedLine(lineIndex))
            {
                var marker = new Rect(
                    0,
                    visualLine.VisualTop - textView.VerticalOffset,
                    4,
                    visualLine.Height);
                drawingContext.DrawRectangle(DiffBlockPalette.MovedMarker, pen: null, marker);
            }
        }
    }
}

/// <summary>
/// Fixed built-in semantic styles for diff blocks. Deliberately not configurable in MVP:
/// one palette, chosen for distinguishability on the default light editor background.
/// </summary>
public static class DiffBlockPalette
{
    public static readonly Brush InsertedBackground = Freeze(new SolidColorBrush(Color.FromRgb(0xDD, 0xF4, 0xDD)));
    public static readonly Brush DeletedBackground = Freeze(new SolidColorBrush(Color.FromRgb(0xF8, 0xDE, 0xDE)));
    public static readonly Brush ModifiedBackground = Freeze(new SolidColorBrush(Color.FromRgb(0xFC, 0xF0, 0xCE)));
    public static readonly Brush IntralineChangedBackground = Freeze(new SolidColorBrush(Color.FromRgb(0xF5, 0xD9, 0x76)));
    public static readonly Brush MovedMarker = Freeze(new SolidColorBrush(Color.FromRgb(0x7B, 0x5E, 0xC8)));

    /// <summary>Background for a block kind; null for equal (unclassified) lines.</summary>
    public static Brush? GetBackground(DiffBlockKind? kind) => kind switch
    {
        DiffBlockKind.Inserted => InsertedBackground,
        DiffBlockKind.Deleted => DeletedBackground,
        DiffBlockKind.Modified => ModifiedBackground,
        _ => null,
    };

    private static Brush Freeze(Brush brush)
    {
        brush.Freeze();
        return brush;
    }
}
