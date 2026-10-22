using System.Security.Cryptography;

namespace MergeTool.Folders;

/// <summary>Incremental SHA-256 over a readable stream using a fixed-size buffer.</summary>
public static class StreamingSha256Hasher
{
    public const int DefaultBufferSize = 8192;

    public static string ComputeHex(Stream stream, CancellationToken cancellationToken, int bufferSize = DefaultBufferSize)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(bufferSize, 0);

        using IncrementalHash incremental = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[bufferSize];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            incremental.AppendData(buffer, 0, read);
        }

        return Convert.ToHexStringLower(incremental.GetHashAndReset());
    }
}
