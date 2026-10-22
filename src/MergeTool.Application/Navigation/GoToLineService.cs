using MergeTool.Core;

namespace MergeTool.Application.Navigation;

public sealed record GoToLineTarget(int ZeroBasedLineIndex, ComparisonSide Side);

public static class GoToLineService
{
    public const string OutOfRangeCode = "MT-GTL-001";

    public const string InvalidLineNumberCode = "MT-GTL-002";

    /// <summary>
    /// Resolves a 1-based source line number for the given pane. Line numbers match editor
    /// line numbers (first line is 1).
    /// </summary>
    public static OperationResult<GoToLineTarget> TryResolve(
        int oneBasedLineNumber,
        int lineCount,
        ComparisonSide side)
    {
        if (oneBasedLineNumber < 1)
        {
            return OperationResult<GoToLineTarget>.Failed(
                Diagnostic.Error(InvalidLineNumberCode, "Line number must be at least 1."));
        }

        if (lineCount == 0)
        {
            return OperationResult<GoToLineTarget>.Failed(
                Diagnostic.Error(OutOfRangeCode, "The document is empty."));
        }

        if (oneBasedLineNumber > lineCount)
        {
            return OperationResult<GoToLineTarget>.Failed(
                Diagnostic.Error(
                    OutOfRangeCode,
                    $"Line {oneBasedLineNumber} is out of range (document has {lineCount} line(s)).",
                    technicalDetail: $"Valid range: 1..{lineCount}"));
        }

        return OperationResult<GoToLineTarget>.Completed(
            new GoToLineTarget(oneBasedLineNumber - 1, side));
    }
}
