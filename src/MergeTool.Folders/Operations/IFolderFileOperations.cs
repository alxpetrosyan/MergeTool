namespace MergeTool.Folders.Operations;

public sealed record FileDeleteOutcome(bool Succeeded, bool UsedRecycleBin, string? FailureReason);

/// <summary>Physical file copy/delete port used by <see cref="FolderOperationExecutor"/>.</summary>
public interface IFolderFileOperations
{
    void CopyFilePreservingSourceBytes(string sourceFullPath, string targetFullPath, bool overwrite);

    FileDeleteOutcome DeleteFile(string fullPath, bool preferRecycleBin);
}
