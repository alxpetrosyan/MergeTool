using MergeTool.Core;

namespace MergeTool.Core.Settings;

public sealed record RecentComparisonEntry(
    string Kind,
    string PathA,
    string? PathB,
    string? PathBase,
    DateTime LastOpenedUtc,
    string? OptionsJson = null);

public sealed record PinnedComparisonEntry(
    string Label,
    string Kind,
    string PathA,
    string? PathB,
    string? PathBase,
    string? OptionsJson = null);

public sealed record AppSettingsDocument
{
    public int Version { get; set; } = AppSettingsMutations.CurrentVersion;

    public List<RecentComparisonEntry> Recents { get; init; } = [];

    public List<PinnedComparisonEntry> Pinned { get; init; } = [];
}

public static class AppSettingsStore
{
    public const string DefaultFileName = "mergetool.settings.json";

    private static readonly System.Text.Json.JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public static string GetDefaultPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MergeTool", DefaultFileName);

    public static AppSettingsDocument Load(string? path = null)
        => LoadWithDiagnostics(path).Document;

    public static AppSettingsLoadResult LoadWithDiagnostics(string? path = null)
    {
        string file = path ?? GetDefaultPath();
        if (!File.Exists(file))
        {
            return new AppSettingsLoadResult(new AppSettingsDocument(), [], LoadedFromDisk: false, UsedFallbackDefaults: false);
        }

        try
        {
            string json = File.ReadAllText(file);
            AppSettingsDocument? document = System.Text.Json.JsonSerializer.Deserialize<AppSettingsDocument>(json);
            if (document is null)
            {
                return CreateCorruptFallback(file, "Settings JSON deserialized to null.");
            }

            document = AppSettingsMutations.Migrate(document);
            return new AppSettingsLoadResult(document, [], LoadedFromDisk: true, UsedFallbackDefaults: false);
        }
        catch (Exception exception)
        {
            return CreateCorruptFallback(file, exception.Message);
        }
    }

    public static void Save(AppSettingsDocument document, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        document.Version = AppSettingsMutations.CurrentVersion;

        string file = path ?? GetDefaultPath();
        string? directory = Path.GetDirectoryName(file);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = System.Text.Json.JsonSerializer.Serialize(document, SerializerOptions);
        string temp = file + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, file, overwrite: true);
    }

    private static AppSettingsLoadResult CreateCorruptFallback(string file, string technicalDetail)
        => new(
            new AppSettingsDocument(),
            [
                Diagnostic.Warning(
                    AppSettingsMutations.CorruptSettingsCode,
                    "Settings could not be read. Defaults are in use until you save again.",
                    technicalDetail: technicalDetail,
                    path: file),
            ],
            LoadedFromDisk: false,
            UsedFallbackDefaults: true);
}

/// <summary>In-memory settings with explicit save and session list mutations.</summary>
public sealed class ComparisonSessionHistory
{
    private readonly string _path;

    public ComparisonSessionHistory(string? path = null)
    {
        _path = path ?? AppSettingsStore.GetDefaultPath();
        AppSettingsLoadResult load = AppSettingsStore.LoadWithDiagnostics(_path);
        Document = load.Document;
        LoadDiagnostics = load.Diagnostics;
        UsedFallbackDefaults = load.UsedFallbackDefaults;
    }

    public AppSettingsDocument Document { get; }

    public IReadOnlyList<Diagnostic> LoadDiagnostics { get; }

    public bool UsedFallbackDefaults { get; }

    public IReadOnlyList<RecentComparisonEntry> Recents => Document.Recents;

    public IReadOnlyList<PinnedComparisonEntry> Pinned => Document.Pinned;

    public void RecordRecent(RecentComparisonEntry entry)
    {
        AppSettingsMutations.AddRecent(Document, entry);
        Save();
    }

    public void AddPinned(PinnedComparisonEntry entry)
    {
        AppSettingsMutations.AddPinned(Document, entry);
        Save();
    }

    public void ResetRecents()
    {
        AppSettingsMutations.ResetRecents(Document);
        Save();
    }

    public void ResetAll()
    {
        AppSettingsMutations.ResetAll(Document);
        Save();
    }

    public void Save() => AppSettingsStore.Save(Document, _path);
}
