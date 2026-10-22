using MergeTool.Core.Settings;

namespace MergeTool.Core.Tests;

[TestClass]
public sealed class SettingsRegressionTests
{
    [TestMethod]
    public void SaveAndLoad_PreservesPinnedLabelAndRecentPaths()
    {
        string path = Path.Combine(Path.GetTempPath(), "MergeToolSettings_" + Path.GetRandomFileName() + ".json");
        try
        {
            AppSettingsDocument original = new()
            {
                Recents =
                [
                    new RecentComparisonEntry("file-pair", @"C:\a.txt", @"C:\b.txt", null, DateTime.UtcNow),
                ],
                Pinned =
                [
                    new PinnedComparisonEntry("Work trees", "folder-pair", @"C:\left", @"C:\right", null, "{}"),
                ],
            };

            AppSettingsStore.Save(original, path);
            AppSettingsDocument loaded = AppSettingsStore.Load(path);

            Assert.AreEqual(@"C:\a.txt", loaded.Recents[0].PathA);
            Assert.AreEqual("Work trees", loaded.Pinned[0].Label);
        }
        finally
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
        }
    }
}
