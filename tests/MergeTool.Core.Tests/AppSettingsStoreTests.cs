using MergeTool.Core;
using MergeTool.Core.Settings;

namespace MergeTool.Core.Tests;

[TestClass]
public sealed class AppSettingsStoreTests
{
    [TestMethod]
    public void SaveAndLoad_RoundTrips()
    {
        string path = CreateTempPath();
        try
        {
            AppSettingsDocument doc = new()
            {
                Recents = [new RecentComparisonEntry("file-pair", @"C:\a.txt", @"C:\b.txt", null, DateTime.UtcNow)],
                Pinned = [new PinnedComparisonEntry("Pair", "file-pair", @"C:\a.txt", @"C:\b.txt", null)],
            };
            AppSettingsStore.Save(doc, path);
            AppSettingsDocument loaded = AppSettingsStore.Load(path);
            Assert.HasCount(1, loaded.Recents);
            Assert.HasCount(1, loaded.Pinned);
            Assert.AreEqual(AppSettingsMutations.CurrentVersion, loaded.Version);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [TestMethod]
    public void LoadWithDiagnostics_CorruptJson_UsesFallbackAndDiagnostic()
    {
        string path = CreateTempPath();
        try
        {
            File.WriteAllText(path, "{ not valid json");

            AppSettingsLoadResult result = AppSettingsStore.LoadWithDiagnostics(path);

            Assert.IsTrue(result.UsedFallbackDefaults);
            Assert.IsEmpty(result.Document.Recents);
            Assert.IsTrue(result.Diagnostics.Any(d => d.Code == AppSettingsMutations.CorruptSettingsCode));
        }
        finally
        {
            TryDelete(path);
        }
    }

    [TestMethod]
    public void Migrate_OlderVersion_PreservesLists()
    {
        AppSettingsDocument migrated = AppSettingsMutations.Migrate(new AppSettingsDocument
        {
            Version = 0,
            Recents = [new RecentComparisonEntry("file-pair", @"C:\x.txt", @"C:\y.txt", null, DateTime.UtcNow)],
        });

        Assert.AreEqual(AppSettingsMutations.CurrentVersion, migrated.Version);
        Assert.HasCount(1, migrated.Recents);
    }

    [TestMethod]
    public void AddRecent_DeduplicatesAndMovesToFront()
    {
        AppSettingsDocument document = new();
        DateTime first = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime second = new(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc);

        AppSettingsMutations.AddRecent(document, new RecentComparisonEntry("file-pair", @"C:\a.txt", @"C:\b.txt", null, first));
        AppSettingsMutations.AddRecent(document, new RecentComparisonEntry("file-pair", @"C:\a.txt", @"C:\b.txt", null, second));

        Assert.HasCount(1, document.Recents);
        Assert.IsTrue(document.Recents[0].LastOpenedUtc >= second);
    }

    [TestMethod]
    public void AddRecent_CapsListAtMax()
    {
        AppSettingsDocument document = new();
        for (int i = 0; i < AppSettingsMutations.MaxRecents + 5; i++)
        {
            AppSettingsMutations.AddRecent(
                document,
                new RecentComparisonEntry("file-pair", $@"C:\a{i}.txt", $@"C:\b{i}.txt", null, DateTime.UtcNow));
        }

        Assert.HasCount(AppSettingsMutations.MaxRecents, document.Recents);
    }

    [TestMethod]
    public void ResetAll_ClearsRecentsAndPinned()
    {
        AppSettingsDocument document = new()
        {
            Recents = [new RecentComparisonEntry("file-pair", @"C:\a.txt", @"C:\b.txt", null, DateTime.UtcNow)],
            Pinned = [new PinnedComparisonEntry("Pair", "file-pair", @"C:\a.txt", @"C:\b.txt", null)],
        };

        AppSettingsMutations.ResetAll(document);

        Assert.IsEmpty(document.Recents);
        Assert.IsEmpty(document.Pinned);
    }

    [TestMethod]
    public void Save_DoesNotPersistFileContents()
    {
        string path = CreateTempPath();
        try
        {
            AppSettingsStore.Save(
                new AppSettingsDocument
                {
                    Recents =
                    [
                        new RecentComparisonEntry(
                            ComparisonSessionKinds.FilePair,
                            @"C:\secret\content.txt",
                            @"C:\secret\other.txt",
                            null,
                            DateTime.UtcNow),
                    ],
                },
                path);

            string json = File.ReadAllText(path);
            Assert.DoesNotContain("file contents", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("secret", json, StringComparison.Ordinal);
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static string CreateTempPath()
        => Path.Combine(Path.GetTempPath(), "MergeToolSettingsTest_" + Guid.NewGuid().ToString("N") + ".json");

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
        }
    }
}
