using MergeTool.Core;

namespace MergeTool.Application.Operations;

/// <summary>
/// Issues monotonic request IDs for one logical operation slot (for example, "the comparison
/// of workspace X") and decides whether a completion is still current. The application layer
/// owns one coordinator per slot; domain services never see request IDs.
/// </summary>
public sealed class OperationRequestCoordinator
{
    private long _latestIssued;

    /// <summary>
    /// Starts a new request. Any request issued earlier immediately becomes stale.
    /// </summary>
    public ComparisonRequestId BeginRequest() => new(Interlocked.Increment(ref _latestIssued));

    /// <summary>The most recently issued request, or <see cref="ComparisonRequestId.None"/>.</summary>
    public ComparisonRequestId LatestIssued => new(Interlocked.Read(ref _latestIssued));

    /// <summary>
    /// True when <paramref name="requestId"/> is the most recently issued request and its
    /// completion may therefore be applied to workspace state.
    /// </summary>
    public bool IsCurrent(ComparisonRequestId requestId)
        => requestId.Value != 0 && requestId.Value == Interlocked.Read(ref _latestIssued);
}
