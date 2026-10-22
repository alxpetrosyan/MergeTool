using MergeTool.Core;

namespace MergeTool.Folders;

/// <summary>
/// Abstraction over directory enumeration and metadata reads so the comparison engine can be
/// tested without touching the real file system.
/// </summary>
public interface IFolderScanAccess
{
    bool DirectoryExists(string path);

    bool TryEnumerateDirectory(
        string directoryPath,
        out IReadOnlyList<FolderScanEntry> entries,
        out Diagnostic? error);

    bool IsReparsePointDirectory(string directoryPath);

    bool TryGetFileMetadata(
        string filePath,
        out long lengthBytes,
        out DateTime lastWriteTimeUtc,
        out Diagnostic? error);
}

/// <summary>One child name returned from a directory enumeration.</summary>
public sealed record FolderScanEntry(string Name, bool IsDirectory);
