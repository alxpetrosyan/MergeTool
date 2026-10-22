using MergeTool.Core;

namespace MergeTool.Folders;

/// <summary>
/// One row in a flat recursive folder comparison result.
/// </summary>
public sealed record FolderComparisonRow
{
    public FolderComparisonRow(
        string relativePath,
        FolderRowState state,
        string? leftFullPath,
        string? rightFullPath,
        long? leftSizeBytes,
        long? rightSizeBytes,
        DateTime? leftLastWriteTimeUtc,
        DateTime? rightLastWriteTimeUtc,
        bool isDirectory,
        IReadOnlyList<Diagnostic>? rowDiagnostics = null,
        string? baseFullPath = null,
        long? baseSizeBytes = null,
        DateTime? baseLastWriteTimeUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        RelativePath = relativePath;
        State = state;
        LeftFullPath = leftFullPath;
        RightFullPath = rightFullPath;
        LeftSizeBytes = leftSizeBytes;
        RightSizeBytes = rightSizeBytes;
        LeftLastWriteTimeUtc = leftLastWriteTimeUtc;
        RightLastWriteTimeUtc = rightLastWriteTimeUtc;
        IsDirectory = isDirectory;
        RowDiagnostics = rowDiagnostics ?? [];
        BaseFullPath = baseFullPath;
        BaseSizeBytes = baseSizeBytes;
        BaseLastWriteTimeUtc = baseLastWriteTimeUtc;
    }

    public string RelativePath { get; }

    public FolderRowState State { get; }

    public string? BaseFullPath { get; }

    public string? LeftFullPath { get; }

    public string? RightFullPath { get; }

    public long? BaseSizeBytes { get; }

    public long? LeftSizeBytes { get; }

    public long? RightSizeBytes { get; }

    public DateTime? BaseLastWriteTimeUtc { get; }

    public DateTime? LeftLastWriteTimeUtc { get; }

    public DateTime? RightLastWriteTimeUtc { get; }

    public bool IsDirectory { get; }

    public IReadOnlyList<Diagnostic> RowDiagnostics { get; }

    /// <summary>True when both sides reference an existing file (not a directory row).</summary>
    public bool HasPairedFiles =>
        !IsDirectory
        && LeftFullPath is not null
        && RightFullPath is not null;

    /// <summary>True when base, left, and right all reference an existing file.</summary>
    public bool HasTripledFiles =>
        !IsDirectory
        && BaseFullPath is not null
        && LeftFullPath is not null
        && RightFullPath is not null;
}
