namespace MergeTool.Core.Diagnostics;

/// <summary>Shared diagnostics for operation lifecycle outcomes that are not tied to one feature area.</summary>
public static class OperationDiagnostics
{
    public const string CancelledCode = "MT-OP-001";
    public const string UnexpectedFailureCode = "MT-UNX-001";

    public const string CancelledMessage = "The operation was cancelled. The last completed result is unchanged.";

    public const string UnexpectedFailureMessage = "An unexpected error occurred. No changes were applied.";

    public static Diagnostic OperationCancelled()
        => Diagnostic.Info(CancelledCode, CancelledMessage);

    public static Diagnostic UnexpectedFailure(Exception exception, string? context = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        string technicalDetail = context is null
            ? exception.Message
            : $"{context}: {exception.Message}";

        return Diagnostic.Error(UnexpectedFailureCode, UnexpectedFailureMessage, technicalDetail: technicalDetail);
    }
}
