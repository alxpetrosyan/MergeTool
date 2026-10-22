using MergeTool.Core;

namespace MergeTool.Folders;

/// <summary>Streams file bytes to compute SHA-256 without loading entire files into memory.</summary>
public interface IFileContentHashAccess
{
    bool TryComputeSha256(
        string filePath,
        CancellationToken cancellationToken,
        out string sha256Hex,
        out Diagnostic? error);
}
