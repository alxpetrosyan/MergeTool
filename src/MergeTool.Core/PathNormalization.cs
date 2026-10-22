namespace MergeTool.Core;

/// <summary>
/// Shared path-normalization primitive. All persistent paths in the product are normalized
/// full paths produced by this helper; no layer concatenates path strings manually.
/// </summary>
public static class PathNormalization
{
    /// <summary>
    /// Returns the normalized full form of <paramref name="path"/>: absolute, with segments
    /// resolved and any trailing directory separator removed (except for a drive root).
    /// Original character casing is preserved for display.
    /// </summary>
    public static string NormalizeFullPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string full = Path.GetFullPath(path);
        string root = Path.GetPathRoot(full) ?? string.Empty;
        if (full.Length > root.Length)
        {
            full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return full;
    }
}
