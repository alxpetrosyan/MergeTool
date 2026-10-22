using MergeTool.Core;

namespace MergeTool.Folders.Operations;

public sealed record FolderOperationResult(
    int SucceededCount,
    int FailedCount,
    IReadOnlyList<Diagnostic> Diagnostics);

public sealed class FolderOperationExecutor
{
    private readonly IFolderFileOperations _fileOperations;

    public FolderOperationExecutor(IFolderFileOperations fileOperations)
    {
        ArgumentNullException.ThrowIfNull(fileOperations);
        _fileOperations = fileOperations;
    }

    public FolderOperationResult Execute(
        FolderOperationPlan plan,
        string leftRoot,
        string rightRoot,
        bool preferRecycleBin,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);

        FolderOperationPlanValidationResult validation = FolderOperationPlanValidator.Validate(
            plan,
            leftRoot,
            rightRoot,
            verifySourcePreconditions: true);
        if (!validation.IsValid)
        {
            return new FolderOperationResult(0, plan.Items.Count, validation.Diagnostics);
        }

        int succeeded = 0;
        int failed = 0;
        var diagnostics = new List<Diagnostic>();

        foreach (FolderOperationItem item in plan.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                ExecuteItem(item, preferRecycleBin);
                succeeded++;
            }
            catch (Exception exception)
            {
                failed++;
                diagnostics.Add(Diagnostic.Error(
                    "MT-FOL-010",
                    $"Folder operation failed for '{item.RelativePath}'.",
                    technicalDetail: exception.Message,
                    path: item.SourceFullPath ?? item.TargetFullPath));
            }
        }

        return new FolderOperationResult(succeeded, failed, diagnostics);
    }

    private void ExecuteItem(FolderOperationItem item, bool preferRecycleBin)
    {
        if (item.ExpectedSourcePrecondition is FolderOperationPrecondition expected
            && !FolderOperationPrecondition.MatchesFile(item.SourceFullPath, expected))
        {
            throw new InvalidOperationException("Source file changed since the operation was confirmed.");
        }

        switch (item.Kind)
        {
            case FolderOperationKind.CopyLeftToRight:
            case FolderOperationKind.CopyRightToLeft:
                CopyFile(item);
                break;
            case FolderOperationKind.DeleteLeft:
            case FolderOperationKind.DeleteRight:
                DeleteFile(item, preferRecycleBin);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(item), item.Kind, null);
        }
    }

    private void CopyFile(FolderOperationItem item)
    {
        ArgumentException.ThrowIfNullOrEmpty(item.TargetFullPath);
        _fileOperations.CopyFilePreservingSourceBytes(
            item.SourceFullPath,
            item.TargetFullPath,
            overwrite: true);
    }

    private void DeleteFile(FolderOperationItem item, bool preferRecycleBin)
    {
        FileDeleteOutcome outcome = _fileOperations.DeleteFile(item.SourceFullPath, preferRecycleBin);
        if (!outcome.Succeeded)
        {
            string detail = outcome.FailureReason ?? "Delete failed.";
            if (preferRecycleBin && RecycleBinSupport.IsAvailable)
            {
                detail += " Recycle Bin delete failed; permanent delete was not attempted.";
            }

            throw new IOException(detail);
        }
    }
}
