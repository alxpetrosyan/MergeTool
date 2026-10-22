namespace MergeTool.Core;

/// <summary>
/// Structured progress for a long-running operation.
/// </summary>
/// <param name="Phase">User-safe name of the current phase, e.g. "Scanning folders".</param>
/// <param name="CurrentItem">Optional user-safe description of the item being processed.</param>
/// <param name="CompletedCount">Items completed so far.</param>
/// <param name="TotalCount">Total item count if known; otherwise null.</param>
public sealed record OperationProgress(
    string Phase,
    string? CurrentItem,
    long CompletedCount,
    long? TotalCount)
{
    public static OperationProgress ForPhase(string phase) => new(phase, null, 0, null);
}
