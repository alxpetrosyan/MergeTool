using MergeTool.Text.Diff;

namespace MergeTool.Application.Presentation;

/// <summary>One navigable marker on the difference minimap (normalized 0..1 vertical coordinates).</summary>
public sealed record MinimapMarker(int BlockOrdinal, double StartY, double EndY, DiffBlockKind Kind)
{
    public double TopY => Math.Min(StartY, EndY);

    public double BottomY => Math.Max(StartY, EndY);

    public bool ContainsNormalizedY(double y) => y >= TopY && y <= BottomY;
}

/// <summary>Viewport band on the minimap (normalized 0..1).</summary>
public sealed record MinimapViewport(double StartY, double EndY)
{
    public double TopY => Math.Min(StartY, EndY);

    public double BottomY => Math.Max(StartY, EndY);
}

/// <summary>Minimap overview derived from comparison blocks; immutable for one comparison revision.</summary>
public sealed class MinimapModel
{
    public MinimapModel(IReadOnlyList<MinimapMarker> markers, int totalLines)
    {
        ArgumentNullException.ThrowIfNull(markers);
        if (totalLines <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalLines));
        }

        Markers = markers;
        TotalLines = totalLines;
    }

    public IReadOnlyList<MinimapMarker> Markers { get; }

    public int TotalLines { get; }

    public MinimapMarker? FindMarkerAtNormalizedY(double y)
    {
        foreach (MinimapMarker marker in Markers)
        {
            if (marker.ContainsNormalizedY(y))
            {
                return marker;
            }
        }

        return null;
    }

    public MinimapViewport CalculateViewport(int firstVisibleLine, int visibleLineCount)
    {
        int start = Math.Clamp(firstVisibleLine, 0, Math.Max(0, TotalLines - 1));
        int end = Math.Clamp(start + Math.Max(1, visibleLineCount), start + 1, TotalLines);
        double startY = start / (double)TotalLines;
        double endY = end / (double)TotalLines;
        if (endY <= startY)
        {
            endY = Math.Min(1.0, startY + (1.0 / TotalLines));
        }

        return new MinimapViewport(Math.Min(startY, endY), Math.Max(startY, endY));
    }
}
