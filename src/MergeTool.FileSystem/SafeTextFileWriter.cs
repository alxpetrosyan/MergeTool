using System.Text;
using MergeTool.Core;

namespace MergeTool.FileSystem;

public enum SaveStatus
{
    Saved = 0,

    /// <summary>The file on disk no longer matches the expected identity; nothing was written.</summary>
    ExternalModificationConflict = 1,
}

/// <summary>
/// Outcome of a save attempt. <see cref="NewIdentity"/> is present only after a successful
/// save and becomes the caller's new expected identity.
/// </summary>
public sealed record SaveResult(SaveStatus Status, FileIdentity? NewIdentity)
{
    public static SaveResult Conflict() => new(SaveStatus.ExternalModificationConflict, null);
}

/// <summary>
/// Saves text files safely:
/// 1. Re-reads the current file identity and refuses to overwrite when it differs from the
///    identity captured at load/reload (external modification).
/// 2. Writes to a temporary file in the target directory, flushes it to disk, then replaces
///    the target atomically (File.Replace, falling back to File.Move for new files).
/// 3. Encodes exactly per the supplied <see cref="TextWritePolicy"/> (original encoding and
///    BOM preserved by the caller passing the loaded document's policy). Text content is
///    written as supplied; line endings are never converted here.
/// </summary>
public sealed class SafeTextFileWriter
{
    public async Task<SaveResult> SaveAsync(
        FileIdentity expectedIdentity,
        string text,
        TextWritePolicy writePolicy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(expectedIdentity);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(writePolicy);

        string targetPath = PathNormalization.NormalizeFullPath(expectedIdentity.FullPath);
        string directory = Path.GetDirectoryName(targetPath)
            ?? throw new IOException($"The path '{targetPath}' has no parent directory.");

        cancellationToken.ThrowIfCancellationRequested();

        // Refuse to overwrite external changes: compare the identity of the bytes on disk
        // right now against the identity captured when the document was loaded.
        if (File.Exists(targetPath))
        {
            byte[] currentBytes = await File.ReadAllBytesAsync(targetPath, cancellationToken).ConfigureAwait(false);
            FileVersionToken currentVersion = FileVersionToken.FromBytes(currentBytes, File.GetLastWriteTimeUtc(targetPath));
            if (currentVersion != expectedIdentity.Version)
            {
                return SaveResult.Conflict();
            }
        }
        else
        {
            // The loaded file vanished; that is an external modification as well.
            return SaveResult.Conflict();
        }

        Encoding encoding = TextEncodings.GetEncoding(writePolicy.EncodingWebName);
        byte[] bom = writePolicy.IncludeByteOrderMark
            ? TextEncodings.GetByteOrderMark(writePolicy.EncodingWebName)
            : [];
        byte[] payload = encoding.GetBytes(text);

        string tempPath = Path.Combine(directory, $".{Path.GetFileName(targetPath)}.{Path.GetRandomFileName()}.tmp");
        try
        {
            await using (FileStream stream = new(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                if (bom.Length > 0)
                {
                    await stream.WriteAsync(bom, cancellationToken).ConfigureAwait(false);
                }

                await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Replace(tempPath, targetPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
        }
        catch
        {
            TryDeleteTempFile(tempPath);
            throw;
        }

        byte[] writtenBytes = new byte[bom.Length + payload.Length];
        bom.CopyTo(writtenBytes, 0);
        payload.CopyTo(writtenBytes, bom.Length);
        FileVersionToken newVersion = FileVersionToken.FromBytes(writtenBytes, File.GetLastWriteTimeUtc(targetPath));
        return new SaveResult(SaveStatus.Saved, new FileIdentity(targetPath, newVersion));
    }

    /// <summary>Creates a new file at <paramref name="targetPath"/>; fails if the file already exists.</summary>
    public async Task<SaveResult> SaveNewFileAsync(
        string targetPath,
        string text,
        TextWritePolicy writePolicy,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(writePolicy);

        string normalizedPath = PathNormalization.NormalizeFullPath(targetPath);
        if (File.Exists(normalizedPath))
        {
            throw new IOException($"The file '{normalizedPath}' already exists.");
        }

        string directory = Path.GetDirectoryName(normalizedPath)
            ?? throw new IOException($"The path '{normalizedPath}' has no parent directory.");

        cancellationToken.ThrowIfCancellationRequested();

        Encoding encoding = TextEncodings.GetEncoding(writePolicy.EncodingWebName);
        byte[] bom = writePolicy.IncludeByteOrderMark
            ? TextEncodings.GetByteOrderMark(writePolicy.EncodingWebName)
            : [];
        byte[] payload = encoding.GetBytes(text);

        string tempPath = Path.Combine(directory, $".{Path.GetFileName(normalizedPath)}.{Path.GetRandomFileName()}.tmp");
        try
        {
            await using (FileStream stream = new(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                if (bom.Length > 0)
                {
                    await stream.WriteAsync(bom, cancellationToken).ConfigureAwait(false);
                }

                await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, normalizedPath);
        }
        catch
        {
            TryDeleteTempFile(tempPath);
            throw;
        }

        byte[] writtenBytes = new byte[bom.Length + payload.Length];
        bom.CopyTo(writtenBytes, 0);
        payload.CopyTo(writtenBytes, bom.Length);
        FileVersionToken newVersion = FileVersionToken.FromBytes(writtenBytes, File.GetLastWriteTimeUtc(normalizedPath));
        return new SaveResult(SaveStatus.Saved, new FileIdentity(normalizedPath, newVersion));
    }

    private static void TryDeleteTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (IOException)
        {
            // Best effort; an orphaned temp file must never mask the original failure.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
