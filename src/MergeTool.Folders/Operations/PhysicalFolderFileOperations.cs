namespace MergeTool.Folders.Operations;

/// <summary>Default copy/delete implementation using <see cref="File"/> APIs.</summary>
public sealed class PhysicalFolderFileOperations : IFolderFileOperations
{
    public void CopyFilePreservingSourceBytes(string sourceFullPath, string targetFullPath, bool overwrite)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFullPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFullPath);

        string? directory = Path.GetDirectoryName(targetFullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Copy(sourceFullPath, targetFullPath, overwrite);
    }

    public FileDeleteOutcome DeleteFile(string fullPath, bool preferRecycleBin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);
        if (!File.Exists(fullPath))
        {
            return new FileDeleteOutcome(true, false, null);
        }

        if (preferRecycleBin && RecycleBinSupport.IsAvailable)
        {
            try
            {
                RecycleBinSupport.SendToRecycleBin(fullPath);
                return new FileDeleteOutcome(true, true, null);
            }
            catch (Exception exception)
            {
                return new FileDeleteOutcome(false, false, exception.Message);
            }
        }

        File.Delete(fullPath);
        return new FileDeleteOutcome(true, false, null);
    }
}
