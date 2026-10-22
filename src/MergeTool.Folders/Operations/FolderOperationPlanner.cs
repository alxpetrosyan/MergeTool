using MergeTool.Core;

namespace MergeTool.Folders.Operations;

public enum FolderOperationKind
{
    CopyLeftToRight = 0,
    CopyRightToLeft = 1,
    DeleteLeft = 2,
    DeleteRight = 3,
}

/// <summary>One immutable planned folder action with preconditions.</summary>
public sealed record FolderOperationItem(
    string RelativePath,
    FolderOperationKind Kind,
    string SourceFullPath,
    string? TargetFullPath,
    FolderOperationPrecondition? ExpectedSourcePrecondition,
    bool WillOverwriteTarget);

public sealed record FolderOperationPlan(IReadOnlyList<FolderOperationItem> Items)
{
    public static FolderOperationPlan Empty { get; } = new([]);

    public bool HasOverwriteTargets => Items.Any(item => item.WillOverwriteTarget);

    public bool HasDeleteOperations =>
        Items.Any(item => item.Kind is FolderOperationKind.DeleteLeft or FolderOperationKind.DeleteRight);
}

public static class FolderOperationPlanner
{
    public static FolderOperationPlan BuildCopyLeftToRight(
        IEnumerable<FolderComparisonRow> rows,
        string leftRoot,
        string rightRoot)
        => Build(rows, FolderOperationKind.CopyLeftToRight, leftRoot, rightRoot);

    public static FolderOperationPlan BuildCopyRightToLeft(
        IEnumerable<FolderComparisonRow> rows,
        string leftRoot,
        string rightRoot)
        => Build(rows, FolderOperationKind.CopyRightToLeft, leftRoot, rightRoot);

    public static FolderOperationPlan BuildDeleteLeft(
        IEnumerable<FolderComparisonRow> rows,
        string leftRoot,
        string rightRoot)
        => Build(rows, FolderOperationKind.DeleteLeft, leftRoot, rightRoot);

    public static FolderOperationPlan BuildDeleteRight(
        IEnumerable<FolderComparisonRow> rows,
        string leftRoot,
        string rightRoot)
        => Build(rows, FolderOperationKind.DeleteRight, leftRoot, rightRoot);

    private static FolderOperationPlan Build(
        IEnumerable<FolderComparisonRow> rows,
        FolderOperationKind kind,
        string leftRoot,
        string rightRoot)
    {
        string normalizedLeftRoot = PathNormalization.NormalizeFullPath(leftRoot);
        string normalizedRightRoot = PathNormalization.NormalizeFullPath(rightRoot);
        var items = new List<FolderOperationItem>();

        foreach (FolderComparisonRow row in rows)
        {
            if (row.IsDirectory || !FolderPathSafety.IsSafeRelativePath(row.RelativePath))
            {
                continue;
            }

            FolderOperationItem? item = kind switch
            {
                FolderOperationKind.CopyLeftToRight => BuildCopy(
                    row,
                    FolderOperationKind.CopyLeftToRight,
                    row.LeftFullPath,
                    row.RightFullPath,
                    normalizedRightRoot,
                    row.LeftSizeBytes,
                    row.LeftLastWriteTimeUtc),
                FolderOperationKind.CopyRightToLeft => BuildCopy(
                    row,
                    FolderOperationKind.CopyRightToLeft,
                    row.RightFullPath,
                    row.LeftFullPath,
                    normalizedLeftRoot,
                    row.RightSizeBytes,
                    row.RightLastWriteTimeUtc),
                FolderOperationKind.DeleteLeft => BuildDelete(
                    row,
                    FolderOperationKind.DeleteLeft,
                    row.LeftFullPath,
                    normalizedLeftRoot,
                    row.LeftSizeBytes,
                    row.LeftLastWriteTimeUtc),
                FolderOperationKind.DeleteRight => BuildDelete(
                    row,
                    FolderOperationKind.DeleteRight,
                    row.RightFullPath,
                    normalizedRightRoot,
                    row.RightSizeBytes,
                    row.RightLastWriteTimeUtc),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };

            if (item is not null)
            {
                items.Add(item);
            }
        }

        return new FolderOperationPlan(items);
    }

    private static FolderOperationItem? BuildCopy(
        FolderComparisonRow row,
        FolderOperationKind kind,
        string? sourceFullPath,
        string? targetFullPath,
        string normalizedTargetRoot,
        long? sourceSizeBytes,
        DateTime? sourceLastWriteTimeUtc)
    {
        if (string.IsNullOrWhiteSpace(sourceFullPath) || !File.Exists(sourceFullPath))
        {
            return null;
        }

        string resolvedTarget = string.IsNullOrWhiteSpace(targetFullPath)
            ? FolderPathSafety.ResolveUnderRoot(normalizedTargetRoot, row.RelativePath)
            : PathNormalization.NormalizeFullPath(targetFullPath);

        FolderOperationPrecondition? precondition =
            FolderOperationPrecondition.FromFile(sourceFullPath, sourceSizeBytes, sourceLastWriteTimeUtc);

        return new FolderOperationItem(
            row.RelativePath,
            kind,
            PathNormalization.NormalizeFullPath(sourceFullPath),
            resolvedTarget,
            precondition,
            File.Exists(resolvedTarget));
    }

    private static FolderOperationItem? BuildDelete(
        FolderComparisonRow row,
        FolderOperationKind kind,
        string? sourceFullPath,
        string normalizedRoot,
        long? sourceSizeBytes,
        DateTime? sourceLastWriteTimeUtc)
    {
        if (string.IsNullOrWhiteSpace(sourceFullPath) || !File.Exists(sourceFullPath))
        {
            return null;
        }

        string resolvedSource = PathNormalization.NormalizeFullPath(sourceFullPath);
        if (!FolderPathSafety.IsPathUnderRoot(resolvedSource, normalizedRoot))
        {
            return null;
        }

        FolderOperationPrecondition? precondition =
            FolderOperationPrecondition.FromFile(resolvedSource, sourceSizeBytes, sourceLastWriteTimeUtc);

        return new FolderOperationItem(
            row.RelativePath,
            kind,
            resolvedSource,
            null,
            precondition,
            WillOverwriteTarget: false);
    }
}
