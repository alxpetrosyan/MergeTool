namespace MergeTool.Folders;

/// <summary>In-memory SHA-256 cache scoped to one folder comparison run.</summary>
internal sealed class FolderContentHashSessionCache
{
    private readonly Dictionary<string, CacheEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string filePath, long lengthBytes, DateTime lastWriteTimeUtc, out string sha256Hex)
    {
        if (_entries.TryGetValue(filePath, out CacheEntry? entry)
            && entry.LengthBytes == lengthBytes
            && entry.LastWriteTimeUtc == lastWriteTimeUtc)
        {
            sha256Hex = entry.Sha256Hex;
            return true;
        }

        sha256Hex = string.Empty;
        return false;
    }

    public void Store(string filePath, long lengthBytes, DateTime lastWriteTimeUtc, string sha256Hex)
        => _entries[filePath] = new CacheEntry(lengthBytes, lastWriteTimeUtc, sha256Hex);

    private sealed record CacheEntry(long LengthBytes, DateTime LastWriteTimeUtc, string Sha256Hex);
}
