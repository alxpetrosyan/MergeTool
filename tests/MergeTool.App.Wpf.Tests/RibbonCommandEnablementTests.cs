using MergeTool.App.Wpf.ViewModels;
using MergeTool.Core;

namespace MergeTool.App.Wpf.Tests;

/// <summary>
/// UI-REDESIGN-01: ribbon quick actions bind straight to workspace commands, so command
/// enablement is the single source of truth for button state. These tests pin the contract:
/// merge copies disabled without a selected difference or when stale, saves gated on dirty
/// writable documents.
/// </summary>
[TestClass]
public sealed class RibbonCommandEnablementTests
{
    private static async Task<FileCompareWorkspaceViewModel> CreateComparedWorkspaceAsync()
    {
        GatedReader reader = new();
        reader.Open();
        FileCompareWorkspaceViewModel workspace = WorkspaceTestFactory.CreateFileCompare(reader);
        workspace.LeftPath = @"C:\fixtures\left.txt";
        workspace.RightPath = @"C:\fixtures\right.txt";
        await workspace.CompareAsync();
        Assert.AreEqual(WorkspaceUiState.Completed, workspace.State);
        return workspace;
    }

    [TestMethod]
    public async Task CopyCommands_DisabledWithoutSelectedDifference_EnabledAfterNavigation()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();

        Assert.IsFalse(workspace.CopyToRightCommand.CanExecute(null), "No difference selected yet.");
        Assert.IsFalse(workspace.CopyToLeftCommand.CanExecute(null));

        workspace.NextDifferenceCommand.Execute(null);

        Assert.IsTrue(workspace.CopyToRightCommand.CanExecute(null));
        Assert.IsTrue(workspace.CopyToLeftCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task CopyAndNavigation_DisabledWhileStale()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        workspace.NextDifferenceCommand.Execute(null);
        Assert.IsTrue(workspace.CopyToRightCommand.CanExecute(null));

        workspace.NotifyPaneEdited(ComparisonSide.Left, "a\r\nedited by hand\r\n");

        Assert.IsTrue(workspace.IsComparisonStale);
        Assert.IsFalse(workspace.CopyToRightCommand.CanExecute(null), "Merge must be blocked while stale.");
        Assert.IsFalse(workspace.CopyToLeftCommand.CanExecute(null));
        Assert.IsFalse(workspace.NextDifferenceCommand.CanExecute(null), "Navigation must be blocked while stale.");
        Assert.IsTrue(workspace.RecompareCommand.CanExecute(null), "Recompare is the way out of stale.");
    }

    [TestMethod]
    public async Task SaveCommands_EnabledOnlyForDirtyDocuments()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();

        Assert.IsFalse(workspace.SaveLeftCommand.CanExecute(null), "Nothing dirty right after compare.");
        Assert.IsFalse(workspace.SaveRightCommand.CanExecute(null));
        Assert.IsFalse(workspace.SaveAllCommand.CanExecute(null));

        workspace.NextDifferenceCommand.Execute(null);
        workspace.CopyToRightCommand.Execute(null); // Right document becomes dirty.

        Assert.IsFalse(workspace.SaveLeftCommand.CanExecute(null));
        Assert.IsTrue(workspace.SaveRightCommand.CanExecute(null));
        Assert.IsTrue(workspace.SaveAllCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ShowFindPanelCommand_ExpandsFindPanel()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        Assert.IsFalse(workspace.IsFindPanelExpanded);

        workspace.ShowFindPanelCommand.Execute(null);

        Assert.IsTrue(workspace.IsFindPanelExpanded);
    }

    [TestMethod]
    public async Task ShowComparisonSettingsCommand_RaisesDialogRequest()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        int requests = 0;
        workspace.ComparisonSettingsRequested += (_, _) => requests++;

        workspace.ShowComparisonSettingsCommand.Execute(null);

        Assert.AreEqual(1, requests);
    }
}
