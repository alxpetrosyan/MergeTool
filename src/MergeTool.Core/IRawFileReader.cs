namespace MergeTool.Core;

/// <summary>
/// Raw bytes of a file plus the facts needed to build identity tokens and metadata.
/// </summary>
/// <param name="NormalizedFullPath">Path normalized via <see cref="PathNormalization"/>.</param>
/// <param name="Bytes">The complete file content at read time.</param>
/// <param name="LastWriteTimeUtc">Last write timestamp in UTC.</param>
/// <param name="IsReadOnly">Whether the file carried the read-only attribute.</param>
public sealed record RawFileContent(
    string NormalizedFullPath,
    byte[] Bytes,
    DateTime LastWriteTimeUtc,
    bool IsReadOnly);

/// <summary>
/// Minimal abstraction over reading file bytes. Implemented by the FileSystem layer;
/// consumed by the Text layer, which may not reference FileSystem directly.
/// Implementations throw ordinary IO exceptions; callers convert them to diagnostics.
/// </summary>
public interface IRawFileReader
{
    Task<RawFileContent> ReadAsync(string path, CancellationToken cancellationToken);
}
