namespace MergeTool.Core;

/// <summary>
/// Lifecycle state of a long-running operation as observed by the caller.
/// </summary>
public enum OperationStatus
{
    Idle = 0,
    Running = 1,
    Completed = 2,
    Cancelled = 3,
    Failed = 4,
}
