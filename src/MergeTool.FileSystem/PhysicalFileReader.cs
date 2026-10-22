using MergeTool.Core;

namespace MergeTool.FileSystem;

/// <summary>
/// Reads file bytes from the physical file system. Exceptions (missing path, sharing
/// violations, access denied) propagate to the caller, which owns diagnostics.
/// </summary>
public sealed class PhysicalFileReader : IRawFileReader
{
    public async Task<RawFileContent> ReadAsync(string path, CancellationToken cancellationToken)
    {
        string normalizedPath = PathNormalization.NormalizeFullPath(path);

        FileInfo info = new(normalizedPath);
        bool isReadOnly = info.Exists && info.IsReadOnly;

        byte[] bytes = await File.ReadAllBytesAsync(normalizedPath, cancellationToken).ConfigureAwait(false);
        DateTime lastWriteUtc = File.GetLastWriteTimeUtc(normalizedPath);

        return new RawFileContent(normalizedPath, bytes, lastWriteUtc, isReadOnly);
    }
}
