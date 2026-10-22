using System.IO;

namespace MergeTool.App.Wpf.Controls;

/// <summary>What kind of filesystem entry a <see cref="PathComboSelector"/> expects.</summary>
public enum PathSelectorKind
{
    Any = 0,
    File = 1,
    Folder = 2,
}

/// <summary>
/// Pure suggestion/validation logic for path selectors: recent-path matching plus child
/// folder/file completion. All filesystem errors are swallowed; suggestions are best-effort.
/// </summary>
public static class PathSuggestionProvider
{
    public const int DefaultMaxResults = 25;

    /// <summary>
    /// Builds dropdown suggestions for the given typed text. Empty text returns recent paths.
    /// Text ending in a separator lists children of that directory; otherwise children of the
    /// parent directory whose names start with the last typed segment.
    /// </summary>
    public static IReadOnlyList<string> GetSuggestions(
        string? text,
        IEnumerable<string>? recentPaths,
        int maxResults = DefaultMaxResults)
    {
        List<string> results = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        string typed = text?.Trim() ?? string.Empty;

        foreach (string recent in recentPaths ?? [])
        {
            if (string.IsNullOrWhiteSpace(recent))
            {
                continue;
            }

            if (typed.Length == 0 || recent.StartsWith(typed, StringComparison.OrdinalIgnoreCase))
            {
                if (seen.Add(recent) && results.Count < maxResults)
                {
                    results.Add(recent);
                }
            }
        }

        if (typed.Length > 0)
        {
            foreach (string child in EnumerateFileSystemCompletions(typed, maxResults))
            {
                if (seen.Add(child) && results.Count < maxResults)
                {
                    results.Add(child);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// True when non-empty text does not resolve to an existing entry of the expected kind.
    /// Empty text is neutral (not marked invalid).
    /// </summary>
    public static bool IsInvalid(string? text, PathSelectorKind kind)
    {
        string typed = text?.Trim() ?? string.Empty;
        if (typed.Length == 0)
        {
            return false;
        }

        try
        {
            return kind switch
            {
                PathSelectorKind.File => !File.Exists(typed),
                PathSelectorKind.Folder => !Directory.Exists(typed),
                _ => !File.Exists(typed) && !Directory.Exists(typed),
            };
        }
        catch (Exception)
        {
            return true;
        }
    }

    /// <summary>True when the text ends with a path separator (the moment to offer children).</summary>
    public static bool EndsWithSeparator(string? text)
        => !string.IsNullOrEmpty(text) && (text[^1] == '\\' || text[^1] == '/');

    private static IEnumerable<string> EnumerateFileSystemCompletions(string typed, int maxResults)
    {
        string directory;
        string prefix;

        try
        {
            if (EndsWithSeparator(typed))
            {
                directory = typed;
                prefix = string.Empty;
            }
            else
            {
                directory = Path.GetDirectoryName(typed) ?? string.Empty;
                prefix = Path.GetFileName(typed);
            }

            if (directory.Length == 0 || !Directory.Exists(directory))
            {
                return [];
            }

            List<string> children = [];
            foreach (string entry in Directory.EnumerateDirectories(directory))
            {
                if (MatchesPrefix(entry, prefix))
                {
                    children.Add(entry);
                    if (children.Count >= maxResults)
                    {
                        return children;
                    }
                }
            }

            foreach (string entry in Directory.EnumerateFiles(directory))
            {
                if (MatchesPrefix(entry, prefix))
                {
                    children.Add(entry);
                    if (children.Count >= maxResults)
                    {
                        return children;
                    }
                }
            }

            return children;
        }
        catch (Exception)
        {
            // Invalid characters, access denied, unavailable drives: no completions.
            return [];
        }
    }

    private static bool MatchesPrefix(string fullPath, string prefix)
        => prefix.Length == 0
           || Path.GetFileName(fullPath).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
}
