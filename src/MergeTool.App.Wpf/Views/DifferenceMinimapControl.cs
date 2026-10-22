using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MergeTool.Application.Presentation;
using MergeTool.Text.Diff;

namespace MergeTool.App.Wpf.Views;

/// <summary>
/// Vertical difference minimap with colored markers and a viewport band. Clicking a marker
/// raises <see cref="BlockClicked"/> with the block ordinal.
/// </summary>
public sealed class DifferenceMinimapControl : FrameworkElement
{
    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model),
        typeof(MinimapModel),
        typeof(DifferenceMinimapControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ViewportProperty = DependencyProperty.Register(
        nameof(Viewport),
        typeof(MinimapViewport),
        typeof(DifferenceMinimapControl),
        new FrameworkPropertyMetadata(
            new MinimapViewport(0, 1),
            FrameworkPropertyMetadataOptions.AffectsRender));

    public MinimapModel? Model
    {
        get => (MinimapModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    public MinimapViewport Viewport
    {
        get => (MinimapViewport)GetValue(ViewportProperty);
        set => SetValue(ViewportProperty, value);
    }

    public event EventHandler<int>? BlockClicked;

    public DifferenceMinimapControl()
    {
        Width = 22;
        MinWidth = 18;
        Focusable = true;
        Cursor = Cursors.Hand;
        ToolTip = "Difference overview — click a marker to jump.";
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (Model is null || ActualHeight <= 0)
        {
            return;
        }

        double y = e.GetPosition(this).Y / ActualHeight;
        MinimapMarker? marker = Model.FindMarkerAtNormalizedY(y);
        if (marker is not null)
        {
            BlockClicked?.Invoke(this, marker.BlockOrdinal);
            e.Handled = true;
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        Rect bounds = new(0, 0, ActualWidth, ActualHeight);
        drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(245, 245, 245)), null, bounds);

        MinimapModel? model = Model;
        if (model is null || model.Markers.Count == 0)
        {
            DrawViewport(drawingContext, bounds);
            return;
        }

        foreach (MinimapMarker marker in model.Markers)
        {
            double top = marker.TopY * bounds.Height;
            double height = Math.Max(2, (marker.BottomY - marker.TopY) * bounds.Height);
            var brush = new SolidColorBrush(ColorForKind(marker.Kind));
            drawingContext.DrawRectangle(brush, null, new Rect(2, top, bounds.Width - 4, height));
        }

        DrawViewport(drawingContext, bounds);
    }

    private void DrawViewport(DrawingContext drawingContext, Rect bounds)
    {
        double top = Viewport.TopY * bounds.Height;
        double height = Math.Max(3, (Viewport.BottomY - Viewport.TopY) * bounds.Height);
        var fill = new SolidColorBrush(Color.FromArgb(48, 30, 120, 200));
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(30, 120, 200)), 1);
        drawingContext.DrawRectangle(fill, pen, new Rect(1, top, bounds.Width - 2, height));
    }

    private static Color ColorForKind(DiffBlockKind kind) => kind switch
    {
        DiffBlockKind.Inserted => Color.FromRgb(80, 160, 80),
        DiffBlockKind.Deleted => Color.FromRgb(200, 80, 80),
        DiffBlockKind.Modified => Color.FromRgb(210, 170, 40),
        _ => Color.FromRgb(160, 160, 160),
    };
}
