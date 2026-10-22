using MergeTool.App.Wpf.ViewModels;
using MergeTool.Application.UseCases;
using MergeTool.Core;

namespace MergeTool.App.Wpf.Tests;

[TestClass]
public sealed class FileCompareStaleFlowTests
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
    public async Task EditingAPane_MarksComparisonStale_AndDisablesMergeNavigation()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        int invalidatedEvents = 0;
        workspace.ComparisonInvalidated += (_, _) => invalidatedEvents++;

        Assert.IsFalse(workspace.IsComparisonStale);
        Assert.IsFalse(workspace.RecompareCommand.CanExecute(null), "Recompare is offered only once stale.");

        workspace.NotifyPaneEdited(ComparisonSide.Left, "a\r\nedited\r\n");

        Assert.AreEqual(WorkspaceUiState.Stale, workspace.State);
        Assert.IsTrue(workspace.IsComparisonStale);
        Assert.IsTrue(workspace.RecompareCommand.CanExecute(null));
        Assert.IsFalse(workspace.Session!.CanNavigate, "Navigation must be disabled while stale.");
        Assert.IsFalse(workspace.Session.CanMerge, "Merge must be disabled while stale.");
        Assert.IsTrue(workspace.HasUnsavedChanges);
        Assert.AreEqual(1, invalidatedEvents, "The view clears semantic overlays exactly once per staleness transition.");
        Assert.Contains("stale", workspace.SummaryText!, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task Recompare_RestoresACurrentComparison()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        int updatedEvents = 0;
        workspace.ComparisonUpdated += (_, _) => updatedEvents++;

        workspace.NotifyPaneEdited(ComparisonSide.Left, "a\r\nedited\r\n");
        await workspace.RecompareAsync();

        Assert.AreEqual(WorkspaceUiState.Completed, workspace.State);
        Assert.IsFalse(workspace.IsComparisonStale);
        Assert.IsFalse(workspace.RecompareCommand.CanExecute(null));
        Assert.IsTrue(workspace.Session!.CanNavigate);
        Assert.IsTrue(workspace.Session.CanMerge);
        Assert.AreEqual(1, updatedEvents, "The view refreshes overlays from the applied result.");
        Assert.Contains("difference", workspace.SummaryText!);
    }

    [TestMethod]
    public async Task IdenticalTextEdit_DoesNotGoStale()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();

        workspace.NotifyPaneEdited(ComparisonSide.Left, workspace.Session!.Left.CurrentText);

        Assert.AreEqual(WorkspaceUiState.Completed, workspace.State);
        Assert.IsFalse(workspace.IsComparisonStale);
    }

    [TestMethod]
    public async Task SamePathOnBothSides_IsRejectedWithAClearMessage()
    {
        GatedReader reader = new();
        reader.Open();
        FileCompareWorkspaceViewModel workspace = WorkspaceTestFactory.CreateFileCompare(reader);
        workspace.LeftPath = @"C:\fixtures\same.txt";
        workspace.RightPath = @"C:\FIXTURES\same.txt";

        await workspace.CompareAsync();

        Assert.AreEqual(WorkspaceUiState.Failed, workspace.State);
        Assert.IsNull(workspace.Session);
        Assert.HasCount(1, workspace.Diagnostics);
        Assert.AreEqual(OpenFileComparisonUseCase.SamePathMessage, workspace.Diagnostics[0].Message);
    }

    [TestMethod]
    public async Task DirtyWorkspace_RefusesToClose()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        Assert.IsTrue(workspace.CanClose);

        workspace.NotifyPaneEdited(ComparisonSide.Right, "changed\r\n");

        Assert.IsFalse(workspace.CanClose, "Unsaved edits must block closing until saved or discarded.");
    }
}
