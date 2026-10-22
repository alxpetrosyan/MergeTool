using MergeTool.Core;

namespace MergeTool.Core.Settings;

/// <summary>Result of loading application settings from disk.</summary>
public sealed record AppSettingsLoadResult(
    AppSettingsDocument Document,
    IReadOnlyList<Diagnostic> Diagnostics,
    bool LoadedFromDisk,
    bool UsedFallbackDefaults);

/// <summary>Mutations and validation for versioned application settings.</summary>
public static class AppSettingsMutations
{
    public const int CurrentVersion = 1;
    public const int MaxRecents = 20;
    public const string CorruptSettingsCode = "MT-SET-001";

    public static AppSettingsDocument Migrate(AppSettingsDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Version >= CurrentVersion)
        {
            return document with { Version = CurrentVersion };
        }

        return new AppSettingsDocument
        {
            Version = CurrentVersion,
            Recents = document.Recents,
            Pinned = document.Pinned,
        };
    }

    public static void AddRecent(AppSettingsDocument document, RecentComparisonEntry entry, int maxRecents = MaxRecents)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(entry);

        string key = BuildSessionKey(entry.Kind, entry.PathA, entry.PathB, entry.PathBase);
        document.Recents.RemoveAll(existing => BuildSessionKey(existing.Kind, existing.PathA, existing.PathB, existing.PathBase) == key);

        RecentComparisonEntry normalized = entry with { LastOpenedUtc = DateTime.UtcNow };
        document.Recents.Insert(0, normalized);

        if (document.Recents.Count > maxRecents)
        {
            document.Recents.RemoveRange(maxRecents, document.Recents.Count - maxRecents);
        }
    }

    public static void AddPinned(AppSettingsDocument document, PinnedComparisonEntry entry)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(entry);

        string key = BuildSessionKey(entry.Kind, entry.PathA, entry.PathB, entry.PathBase);
        document.Pinned.RemoveAll(existing => BuildSessionKey(existing.Kind, existing.PathA, existing.PathB, existing.PathBase) == key);
        document.Pinned.Insert(0, entry);
    }

    public static void ResetRecents(AppSettingsDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        document.Recents.Clear();
    }

    public static void ResetAll(AppSettingsDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        document.Recents.Clear();
        document.Pinned.Clear();
        document.Version = CurrentVersion;
    }

    public static bool PathsExist(RecentComparisonEntry entry)
        => PathsExist(entry.Kind, entry.PathA, entry.PathB, entry.PathBase);

    public static bool PathsExist(PinnedComparisonEntry entry)
        => PathsExist(entry.Kind, entry.PathA, entry.PathB, entry.PathBase);

    public static bool PathsExist(string kind, string pathA, string? pathB, string? pathBase)
    {
        return kind switch
        {
            ComparisonSessionKinds.FilePair or ComparisonSessionKinds.FolderPair =>
                FileOrDirectoryExists(pathA, isFolder: IsFolderKind(kind))
                && pathB is not null
                && FileOrDirectoryExists(pathB, isFolder: IsFolderKind(kind)),
            ComparisonSessionKinds.FileThreeWay or ComparisonSessionKinds.FolderThreeWay =>
                pathBase is not null
                && FileOrDirectoryExists(pathBase, isFolder: IsFolderKind(kind))
                && FileOrDirectoryExists(pathA, isFolder: IsFolderKind(kind))
                && pathB is not null
                && FileOrDirectoryExists(pathB, isFolder: IsFolderKind(kind)),
            _ => false,
        };
    }

    public static string BuildDisplayLabel(string kind, string pathA, string? pathB, string? pathBase)
    {
        string nameA = Path.GetFileName(pathA.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(nameA))
        {
            nameA = pathA;
        }

        return kind switch
        {
            ComparisonSessionKinds.FilePair or ComparisonSessionKinds.FolderPair when pathB is not null =>
                $"{nameA} ↔ {Path.GetFileName(pathB)}",
            ComparisonSessionKinds.FileThreeWay or ComparisonSessionKinds.FolderThreeWay =>
                $"Base: {Path.GetFileName(pathBase ?? pathA)}",
            _ => nameA,
        };
    }

    public static string BuildSessionKey(string kind, string pathA, string? pathB, string? pathBase)
    {
        static string Normalize(string path) => PathNormalization.NormalizeFullPath(path);
        return string.Join(
            '|',
            kind,
            Normalize(pathA),
            pathB is null ? string.Empty : Normalize(pathB),
            pathBase is null ? string.Empty : Normalize(pathBase));
    }

    private static bool IsFolderKind(string kind)
        => kind is ComparisonSessionKinds.FolderPair or ComparisonSessionKinds.FolderThreeWay;

    private static bool FileOrDirectoryExists(string path, bool isFolder)
        => isFolder ? Directory.Exists(path) : File.Exists(path);

    public static string? SerializeOptions(StoredComparisonOptions? options)
    {
        if (options is null)
        {
            return null;
        }

        return System.Text.Json.JsonSerializer.Serialize(options);
    }

    public static StoredComparisonOptions? DeserializeOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return null;
        }

        return System.Text.Json.JsonSerializer.Deserialize<StoredComparisonOptions>(optionsJson);
    }
}
