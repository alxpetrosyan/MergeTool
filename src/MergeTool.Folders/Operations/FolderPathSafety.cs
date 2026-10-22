using MergeTool.Core;

namespace MergeTool.Folders.Operations;

/// <summary>Guards folder operations against traversal and root escape.</summary>
public static class FolderPathSafety
{
    public static bool IsSafeRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        if (Path.IsPathRooted(relativePath))
        {
            return false;
        }

        foreach (string segment in relativePath.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment is "." or "..")
            {
                return false;
            }
        }

        return true;
    }

    public static string ResolveUnderRoot(string normalizedRoot, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedRoot);
        if (!IsSafeRelativePath(relativePath))
        {
            throw new ArgumentException("Relative path is not safe.", nameof(relativePath));
        }

        string combined = Path.Combine(normalizedRoot, relativePath);
        return PathNormalization.NormalizeFullPath(combined);
    }

    public static bool IsPathUnderRoot(string fullPath, string normalizedRoot)
    {
        string normalizedPath = PathNormalization.NormalizeFullPath(fullPath);
        string rootWithSeparator = normalizedRoot.EndsWith(Path.DirectorySeparatorChar)
            ? normalizedRoot
            : normalizedRoot + Path.DirectorySeparatorChar;

        return normalizedPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                normalizedPath,
                normalizedRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
    }
}
