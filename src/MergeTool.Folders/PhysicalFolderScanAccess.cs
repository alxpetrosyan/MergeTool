using System.IO;
using MergeTool.Core;

namespace MergeTool.Folders;

/// <summary>Production folder scan access using System.IO. Does not read file bodies.</summary>
public sealed class PhysicalFolderScanAccess : IFolderScanAccess
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool TryEnumerateDirectory(
        string directoryPath,
        out IReadOnlyList<FolderScanEntry> entries,
        out Diagnostic? error)
    {
        try
        {
            entries = [.. Directory.EnumerateFileSystemEntries(directoryPath).Select(entry =>
            {
                FileAttributes attributes = File.GetAttributes(entry);
                return new FolderScanEntry(Path.GetFileName(entry), attributes.HasFlag(FileAttributes.Directory));
            })];
            error = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            entries = [];
            error = Diagnostic.Error(
                "MT-FLD-010",
                "Could not read this directory.",
                technicalDetail: exception.Message,
                path: directoryPath);
            return false;
        }
    }

    public bool IsReparsePointDirectory(string directoryPath)
    {
        try
        {
            return Directory.Exists(directoryPath)
                && File.GetAttributes(directoryPath).HasFlag(FileAttributes.ReparsePoint);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public bool TryGetFileMetadata(
        string filePath,
        out long lengthBytes,
        out DateTime lastWriteTimeUtc,
        out Diagnostic? error)
    {
        try
        {
            FileInfo info = new(filePath);
            lengthBytes = info.Length;
            lastWriteTimeUtc = info.LastWriteTimeUtc;
            error = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            lengthBytes = 0;
            lastWriteTimeUtc = default;
            error = Diagnostic.Error(
                "MT-FLD-011",
                "Could not read file metadata.",
                technicalDetail: exception.Message,
                path: filePath);
            return false;
        }
    }
}
