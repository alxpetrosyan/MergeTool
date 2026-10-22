using MergeTool.Core;

namespace MergeTool.Application.Operations;

/// <summary>
/// Holds the last successfully completed value of one operation slot. Stale completions are
/// rejected, and cancelled or failed completions never overwrite the last good value, so a
/// workspace always keeps showing its last completed result.
/// </summary>
public sealed class LastGoodResultSlot<T>
{
    private readonly OperationRequestCoordinator _coordinator;
    private readonly Lock _gate = new();

    private T? _lastGoodValue;
    private bool _hasValue;
    private OperationStatus _lastTerminalStatus = OperationStatus.Idle;

    public LastGoodResultSlot(OperationRequestCoordinator coordinator)
    {
        ArgumentNullException.ThrowIfNull(coordinator);
        _coordinator = coordinator;
    }

    /// <summary>Terminal status of the newest accepted completion (Idle before the first one).</summary>
    public OperationStatus LastTerminalStatus
    {
        get
        {
            lock (_gate)
            {
                return _lastTerminalStatus;
            }
        }
    }

    public bool TryGetLastGoodValue(out T value)
    {
        lock (_gate)
        {
            value = _lastGoodValue!;
            return _hasValue;
        }
    }

    /// <summary>
    /// Applies a terminal result for <paramref name="requestId"/>.
    /// Returns false when the completion is stale and was discarded entirely.
    /// Only a successful completion replaces the stored value.
    /// </summary>
    public bool TryApply(ComparisonRequestId requestId, OperationResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!_coordinator.IsCurrent(requestId))
        {
            return false;
        }

        lock (_gate)
        {
            _lastTerminalStatus = result.Status;
            if (result.IsCompleted)
            {
                _lastGoodValue = result.Value;
                _hasValue = true;
            }

            return true;
        }
    }
}
