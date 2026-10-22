namespace MergeTool.Core;

/// <summary>
/// Version token of a file's content at a known point in time. Two tokens are equal only if
/// length, last write time (UTC), and content hash all match. This type carries no IO; the
/// FileSystem layer produces it.
/// </summary>
/// <param name="Length">File length in bytes.</param>
/// <param name="LastWriteTimeUtc">Last write timestamp; must be <see cref="DateTimeKind.Utc"/>.</param>
/// <param name="ContentSha256">Lowercase hex SHA-256 of the loaded bytes.</param>
public sealed record FileVersionToken
{
    public FileVersionToken(long length, DateTime lastWriteTimeUtc, string contentSha256)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentSha256);
        if (lastWriteTimeUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Last write time must be UTC.", nameof(lastWriteTimeUtc));
        }

        Length = length;
        LastWriteTimeUtc = lastWriteTimeUtc;
        ContentSha256 = contentSha256.ToLowerInvariant();
    }

    public long Length { get; }

    public DateTime LastWriteTimeUtc { get; }

    public string ContentSha256 { get; }

    /// <summary>Computes a token from raw content bytes and a UTC last-write time.</summary>
    public static FileVersionToken FromBytes(ReadOnlySpan<byte> bytes, DateTime lastWriteTimeUtc)
    {
        byte[] hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return new FileVersionToken(bytes.Length, lastWriteTimeUtc, Convert.ToHexStringLower(hash));
    }
}

/// <summary>
/// A normalized full path plus the version token captured when the file was loaded.
/// Used to detect external modification before overwriting a file.
/// </summary>
public sealed record FileIdentity
{
    public FileIdentity(string fullPath, FileVersionToken version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);
        ArgumentNullException.ThrowIfNull(version);

        FullPath = fullPath;
        Version = version;
    }

    /// <summary>Normalized full path. Normalization is owned by the FileSystem layer.</summary>
    public string FullPath { get; }

    public FileVersionToken Version { get; }
}
