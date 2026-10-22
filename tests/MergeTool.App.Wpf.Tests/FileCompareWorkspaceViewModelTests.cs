using MergeTool.App.Wpf.ViewModels;
using MergeTool.Core;

namespace MergeTool.App.Wpf.Tests;

[TestClass]
public sealed class FileCompareWorkspaceViewModelTests
{
    private static FileCompareWorkspaceViewModel CreateReadyWorkspace(GatedReader reader)
    {
        FileCompareWorkspaceViewModel workspace = WorkspaceTestFactory.CreateFileCompare(reader);
        workspace.LeftPath = @"C:\fixtures\left.txt";
        workspace.RightPath = @"C:\fixtures\right.txt";
        return workspace;
    }

    [TestMethod]
    public void CompareCommand_RequiresBothPaths()
    {
        FileCompareWorkspaceViewModel workspace = WorkspaceTestFactory.CreateFileCompare(new GatedReader());

        Assert.IsFalse(workspace.CanCompare);
        workspace.LeftPath = @"C:\fixtures\left.txt";
        Assert.IsFalse(workspace.CanCompare);
        workspace.RightPath = @"C:\fixtures\right.txt";
        Assert.IsTrue(workspace.CanCompare);
    }

    [TestMethod]
    public async Task Compare_TransitionsIdleWorkingCompleted_AndShowsSummary()
    {
        GatedReader reader = new();
        FileCompareWorkspaceViewModel workspace = CreateReadyWorkspace(reader);
        Assert.AreEqual(WorkspaceUiState.Idle, workspace.State);

        Task compare = workspace.CompareAsync();
        Assert.AreEqual(WorkspaceUiState.Working, workspace.State);
        Assert.IsTrue(workspace.IsBusy);
        Assert.IsNotNull(workspace.BusyText);

        reader.Open();
        await compare;

        Assert.AreEqual(WorkspaceUiState.Completed, workspace.State);
        Assert.IsFalse(workspace.IsBusy);
        Assert.IsNull(workspace.BusyText);
        Assert.IsNotNull(workspace.Session);
        Assert.IsNotNull(workspace.SummaryText);
        Assert.Contains("difference", workspace.SummaryText);
        Assert.AreEqual("left.txt ↔ right.txt", workspace.Title);
    }

    [TestMethod]
    public async Task CancelCommand_IsEnabledWhileBusy_AndCancelsToSafeState()
    {
        GatedReader reader = new();
        FileCompareWorkspaceViewModel workspace = CreateReadyWorkspace(reader);

        Task compare = workspace.CompareAsync();
        Assert.IsTrue(workspace.CancelCommand.CanExecute(null), "Cancel must be available while busy.");

        workspace.CancelCommand.Execute(null);
        await compare;

        Assert.AreEqual(WorkspaceUiState.Cancelled, workspace.State);
        Assert.IsFalse(workspace.IsBusy);
        Assert.IsFalse(workspace.CancelCommand.CanExecute(null), "Cancel must disable once idle again.");
        Assert.IsNull(workspace.Session, "No session may appear from a cancelled open.");
    }

    [TestMethod]
    public async Task NewerCompare_SupersedesOlderOne()
    {
        GatedReader reader = new();
        FileCompareWorkspaceViewModel workspace = CreateReadyWorkspace(reader);

        Task older = workspace.CompareAsync();
        Task newer = workspace.CompareAsync();

        reader.Open();
        await older;
        await newer;

        Assert.AreEqual(WorkspaceUiState.Completed, workspace.State, "The newest operation's outcome wins.");
        Assert.IsNotNull(workspace.Session);
    }

    [TestMethod]
    public async Task FailedCompare_ShowsSafeDiagnostics_WithTechnicalDetailOnDemand()
    {
        // Real file system, missing files: expected user-data error, not a crash.
        FileCompareWorkspaceViewModel workspace =
            WorkspaceTestFactory.CreateFileCompare(new MergeTool.FileSystem.PhysicalFileReader());
        workspace.LeftPath = @"C:\definitely\missing\left.txt";
        workspace.RightPath = @"C:\definitely\missing\right.txt";

        await workspace.CompareAsync();

        Assert.AreEqual(WorkspaceUiState.Failed, workspace.State);
        Assert.HasCount(1, workspace.Diagnostics);
        DiagnosticViewModel diagnostic = workspace.Diagnostics[0];
        Assert.AreEqual("The file could not be read.", diagnostic.Message);
        Assert.IsTrue(diagnostic.HasTechnicalDetail);
        Assert.IsNull(diagnostic.VisibleTechnicalDetail, "Technical detail is hidden by default.");

        diagnostic.ShowTechnicalDetail = true;
        Assert.IsNotNull(diagnostic.VisibleTechnicalDetail, "Technical detail appears only on demand.");
    }

    [TestMethod]
    public async Task CompletedWorkspace_WithCleanDocuments_CanClose()
    {
        GatedReader reader = new();
        FileCompareWorkspaceViewModel workspace = CreateReadyWorkspace(reader);
        reader.Open();
        await workspace.CompareAsync();

        Assert.IsFalse(workspace.HasUnsavedChanges);
        Assert.IsTrue(workspace.CanClose);
    }
}
