using System.IO;
using MergeTool.Core;

namespace MergeTool.Folders;

/// <summary>Production SHA-256 access using a buffered file stream.</summary>
public sealed class PhysicalFileContentHashAccess : IFileContentHashAccess
{
    public bool TryComputeSha256(
        string filePath,
        CancellationToken cancellationToken,
        out string sha256Hex,
        out Diagnostic? error)
    {
        try
        {
            using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            sha256Hex = StreamingSha256Hasher.ComputeHex(stream, cancellationToken);
            error = null;
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            sha256Hex = string.Empty;
            error = Diagnostic.Error(
                "MT-FLD-012",
                "Could not read file content for hashing.",
                technicalDetail: exception.Message,
                path: filePath);
            return false;
        }
    }
}
