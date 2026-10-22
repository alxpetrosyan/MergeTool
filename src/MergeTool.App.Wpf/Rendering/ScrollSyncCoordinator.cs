using MergeTool.Application.Sessions;
using MergeTool.Core;

namespace MergeTool.App.Wpf.Rendering;

/// <summary>
/// Decides synchronized-scroll targets between the two panes. Only the pane the user is
/// interacting with (the active side) may drive the counterpart, and a follower movement that
/// is already close enough is suppressed — together these guarantee the two panes can never
/// recursively fight each other.
/// </summary>
public sealed class ScrollSyncCoordinator
{
    private ComparisonSide? _activeSide;

    /// <summary>The current presentation map; null disables synchronization entirely.</summary>
    public ComparisonPresentationMap? Map { get; set; }

    /// <summary>Marks which pane the user is interacting with (focus/pointer).</summary>
    public void SetActiveSide(ComparisonSide? side) => _activeSide = side;

    /// <summary>
    /// Computes the vertical offset the counterpart pane should scroll to, or null when no
    /// movement is required: no map, unknown geometry, an event from the non-active pane
    /// (feedback from a programmatic scroll), or a target already within half a line.
    /// </summary>
    public double? GetCounterpartOffset(
        ComparisonSide sourceSide,
        double sourceVerticalOffset,
        double lineHeight,
        double currentCounterpartOffset)
    {
        if (Map is null || lineHeight <= 0 || _activeSide != sourceSide)
        {
            return null;
        }

        int sourceLine = Math.Max(0, (int)Math.Round(sourceVerticalOffset / lineHeight));
        int targetLine = sourceSide == ComparisonSide.Left
            ? Map.MapLeftLineToRightAnchor(sourceLine)
            : Map.MapRightLineToLeftAnchor(sourceLine);

        double targetOffset = targetLine * lineHeight;
        return Math.Abs(targetOffset - currentCounterpartOffset) < lineHeight / 2
            ? null
            : targetOffset;
    }
}
