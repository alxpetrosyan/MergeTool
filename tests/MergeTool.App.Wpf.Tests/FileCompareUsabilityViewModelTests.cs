using MergeTool.App.Wpf.ViewModels;

namespace MergeTool.App.Wpf.Tests;

[TestClass]
public sealed class FileCompareUsabilityViewModelTests
{
    [TestMethod]
    public void ShowFindPanelCommand_ExpandsFindPanel()
    {
        FileCompareWorkspaceViewModel workspace = WorkspaceTestFactory.CreateFileCompare(new MergeTool.FileSystem.PhysicalFileReader());

        Assert.IsFalse(workspace.IsFindPanelExpanded);
        workspace.ShowFindPanelCommand.Execute(null);
        Assert.IsTrue(workspace.IsFindPanelExpanded);
    }

    [TestMethod]
    public void ToggleDifferenceRailCommand_TogglesRailVisibility()
    {
        FileCompareWorkspaceViewModel workspace = WorkspaceTestFactory.CreateFileCompare(new MergeTool.FileSystem.PhysicalFileReader());

        Assert.IsTrue(workspace.IsDifferenceRailVisible);
        workspace.ToggleDifferenceRailCommand.Execute(null);
        Assert.IsFalse(workspace.IsDifferenceRailVisible);
    }
}
