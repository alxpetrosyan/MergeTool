using MergeTool.App.Wpf.ViewModels;
using MergeTool.Core.Settings;
using System.IO;

namespace MergeTool.App.Wpf.Tests;

[TestClass]
public sealed class MainWindowSessionHistoryTests
{
    [TestMethod]
    public void RecordRecent_UpdatesRecentsCollection()
    {
        string path = Path.Combine(Path.GetTempPath(), "MergeToolSettingsTest_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            ComparisonSessionHistory history = new(path);
            MainWindowViewModel shell = WorkspaceTestFactory.CreateShell(new GatedReader(), sessionHistory: history);

            shell.RecordRecent(new RecentComparisonEntry(
                ComparisonSessionKinds.FilePair,
                @"C:\left.txt",
                @"C:\right.txt",
                null,
                DateTime.UtcNow));

            Assert.HasCount(1, shell.Recents);
            Assert.AreEqual(@"C:\left.txt", shell.Recents[0].PathA);
        }
        finally
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

    [TestMethod]
    public void OpenRecent_WithMissingPaths_IsDisabled()
    {
        MainWindowViewModel shell = WorkspaceTestFactory.CreateShell(new GatedReader());
        var missing = new SessionListItemViewModel(
            "missing",
            ComparisonSessionKinds.FilePair,
            @"C:\missing-left.txt",
            @"C:\missing-right.txt",
            null,
            pathsExist: false,
            optionsJson: null);
        shell.Recents.Add(missing);
        shell.SelectedRecent = missing;

        Assert.IsFalse(shell.CanOpenSelectedRecent);
    }

    [TestMethod]
    public void PinActiveSession_AddsPinnedEntry()
    {
        string path = Path.Combine(Path.GetTempPath(), "MergeToolSettingsTest_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            ComparisonSessionHistory history = new(path);
            MainWindowViewModel shell = WorkspaceTestFactory.CreateShell(new GatedReader(), sessionHistory: history);
            FileCompareWorkspaceViewModel workspace = shell.Workspaces.OfType<FileCompareWorkspaceViewModel>().First();
            shell.ActiveWorkspace = workspace;
            workspace.LeftPath = @"C:\left.txt";
            workspace.RightPath = @"C:\right.txt";

            shell.PinActiveSession();

            Assert.HasCount(1, shell.Pinned);
            Assert.AreEqual(@"C:\left.txt", shell.Pinned[0].PathA);
        }
        finally
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

    [TestMethod]
    public void ResetSettings_ClearsRecentsAndPinned()
    {
        string path = Path.Combine(Path.GetTempPath(), "MergeToolSettingsTest_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            ComparisonSessionHistory history = new(path);
            MainWindowViewModel shell = WorkspaceTestFactory.CreateShell(new GatedReader(), sessionHistory: history);
            FileCompareWorkspaceViewModel workspace = shell.Workspaces.OfType<FileCompareWorkspaceViewModel>().First();
            shell.ActiveWorkspace = workspace;
            workspace.LeftPath = @"C:\a.txt";
            workspace.RightPath = @"C:\b.txt";
            shell.RecordRecent(new RecentComparisonEntry(
                ComparisonSessionKinds.FilePair,
                @"C:\a.txt",
                @"C:\b.txt",
                null,
                DateTime.UtcNow));
            shell.PinActiveSession();

            shell.ResetSettings();

            Assert.IsEmpty(shell.Recents);
            Assert.IsEmpty(shell.Pinned);
        }
        finally
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
}
