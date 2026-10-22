namespace MergeTool.Folders.Operations;

/// <summary>
/// Metadata captured when a folder operation is planned. Used to detect source changes
/// before copy or delete runs.
/// </summary>
public sealed record FolderOperationPrecondition(long Length, DateTime LastWriteTimeUtc)
{
    public static FolderOperationPrecondition? FromFile(string? fullPath, long? sizeBytes, DateTime? lastWriteTimeUtc)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
        {
            return null;
        }

        if (sizeBytes is long length && lastWriteTimeUtc is DateTime utc && utc.Kind == DateTimeKind.Utc)
        {
            return new FolderOperationPrecondition(length, utc);
        }

        FileInfo info = new(fullPath);
        return new FolderOperationPrecondition(info.Length, info.LastWriteTimeUtc);
    }

    public static bool MatchesFile(string fullPath, FolderOperationPrecondition expected)
    {
        if (!File.Exists(fullPath))
        {
            return false;
        }

        FileInfo info = new(fullPath);
        return info.Length == expected.Length && info.LastWriteTimeUtc == expected.LastWriteTimeUtc;
    }
}
