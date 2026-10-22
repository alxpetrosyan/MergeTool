using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;
using MergeTool.Text.ThreeWay;

namespace MergeTool.App.Wpf.Rendering;

public sealed class ThreeWayLineBackgroundRenderer : IBackgroundRenderer
{
    private ThreeWayPaneRenderModel _model = ThreeWayPaneRenderModel.Empty;

    public KnownLayer Layer => KnownLayer.Background;

    public void SetModel(ThreeWayPaneRenderModel model)
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
            Brush? brush = ThreeWayRegionPalette.GetBackground(_model.GetKindForLine(lineIndex));
            if (brush is null)
            {
                continue;
            }

            var rect = new Rect(
                0,
                visualLine.VisualTop - textView.VerticalOffset,
                Math.Max(textView.ActualWidth, textView.HorizontalOffset + textView.ActualWidth),
                visualLine.Height);
            drawingContext.DrawRectangle(brush, pen: null, rect);
        }
    }
}

public static class ThreeWayRegionPalette
{
    public static readonly Brush LeftOnlyBackground = Freeze(new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xD6)));
    public static readonly Brush RightOnlyBackground = Freeze(new SolidColorBrush(Color.FromRgb(0xE8, 0xF4, 0xFF)));
    public static readonly Brush SameChangeBackground = Freeze(new SolidColorBrush(Color.FromRgb(0xEE, 0xF6, 0xEE)));
    public static readonly Brush DivergentBackground = Freeze(new SolidColorBrush(Color.FromRgb(0xF8, 0xDE, 0xDE)));

    public static Brush? GetBackground(ThreeWayRegionKind? kind) => kind switch
    {
        ThreeWayRegionKind.LeftOnlyChange => LeftOnlyBackground,
        ThreeWayRegionKind.RightOnlyChange => RightOnlyBackground,
        ThreeWayRegionKind.SameChange => SameChangeBackground,
        ThreeWayRegionKind.Divergent => DivergentBackground,
        _ => null,
    };

    private static Brush Freeze(Brush brush)
    {
        brush.Freeze();
        return brush;
    }
}
