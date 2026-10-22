using System.Collections.ObjectModel;

namespace MergeTool.Core;

/// <summary>
/// Terminal outcome of an operation that produces no value: diagnostics plus a final status.
/// </summary>
public class OperationResult
{
    private static readonly IReadOnlyList<Diagnostic> EmptyDiagnostics = [];

    private protected OperationResult(OperationStatus status, IEnumerable<Diagnostic>? diagnostics)
    {
        if (status is not (OperationStatus.Completed or OperationStatus.Cancelled or OperationStatus.Failed))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "An operation result must represent a terminal status.");
        }

        Status = status;
        Diagnostics = diagnostics is null
            ? EmptyDiagnostics
            : new ReadOnlyCollection<Diagnostic>([.. diagnostics]);
    }

    public OperationStatus Status { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public bool IsCompleted => Status == OperationStatus.Completed;

    public bool IsCancelled => Status == OperationStatus.Cancelled;

    public bool IsFailed => Status == OperationStatus.Failed;

    public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    public static OperationResult Completed(IEnumerable<Diagnostic>? diagnostics = null)
        => new(OperationStatus.Completed, diagnostics);

    public static OperationResult Cancelled(IEnumerable<Diagnostic>? diagnostics = null)
        => new(OperationStatus.Cancelled, diagnostics);

    public static OperationResult Failed(params Diagnostic[] diagnostics)
        => new(OperationStatus.Failed, diagnostics);

    public static OperationResult Failed(IEnumerable<Diagnostic> diagnostics)
        => new(OperationStatus.Failed, diagnostics);
}

/// <summary>
/// Terminal outcome of an operation that produces a value. The value is available only when
/// <see cref="OperationResult.Status"/> is <see cref="OperationStatus.Completed"/>.
/// </summary>
public sealed class OperationResult<T> : OperationResult
{
    private readonly T? _value;

    private OperationResult(OperationStatus status, T? value, IEnumerable<Diagnostic>? diagnostics)
        : base(status, diagnostics)
    {
        _value = value;
    }

    /// <summary>
    /// The produced value. Throws when the operation did not complete successfully.
    /// </summary>
    public T Value => IsCompleted
        ? _value!
        : throw new InvalidOperationException($"An operation with status '{Status}' carries no value.");

    public bool TryGetValue(out T value)
    {
        value = _value!;
        return IsCompleted;
    }

    public static OperationResult<T> Completed(T value, IEnumerable<Diagnostic>? diagnostics = null)
        => new(OperationStatus.Completed, value, diagnostics);

    public static new OperationResult<T> Cancelled(IEnumerable<Diagnostic>? diagnostics = null)
        => new(OperationStatus.Cancelled, default, diagnostics);

    public static new OperationResult<T> Failed(params Diagnostic[] diagnostics)
        => new(OperationStatus.Failed, default, diagnostics);

    public static new OperationResult<T> Failed(IEnumerable<Diagnostic> diagnostics)
        => new(OperationStatus.Failed, default, diagnostics);
}
