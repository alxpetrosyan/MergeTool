using MergeTool.Core;

namespace MergeTool.Folders;

/// <summary>Completed quick folder comparison output. Rows are sorted deterministically.</summary>
public sealed class FolderComparisonResult
{
    private static readonly IReadOnlyList<FolderComparisonRow> EmptyRows = [];
    private static readonly IReadOnlyList<Diagnostic> EmptyDiagnostics = [];

    public FolderComparisonResult(
        IReadOnlyList<FolderComparisonRow> rows,
        IEnumerable<Diagnostic>? diagnostics = null,
        int entriesScanned = 0)
    {
        Rows = rows ?? EmptyRows;
        Diagnostics = diagnostics is null ? EmptyDiagnostics : [.. diagnostics];
        EntriesScanned = entriesScanned;
    }

    public IReadOnlyList<FolderComparisonRow> Rows { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public int EntriesScanned { get; }
}
