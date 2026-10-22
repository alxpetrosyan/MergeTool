using MergeTool.App.Wpf.Services;
using MergeTool.App.Wpf.ViewModels;
using MergeTool.Application.UseCases;
using MergeTool.Folders;
using MergeTool.Folders.Operations;
using System.IO;

namespace MergeTool.App.Wpf.Tests;

[TestClass]
public sealed class FolderCompareOperationTests
{
    private static readonly DateTime BaseTime = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task CanRunFolderOperations_RequiresPromptServiceAndSelection()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("a.txt", "a", BaseTime);

        FolderCompareWorkspaceViewModel workspace = CreateWorkspace(includePrompts: false);
        workspace.LeftRoot = left.Root;
        workspace.RightRoot = right.Root;
        await workspace.CompareAsync();
        Assert.IsFalse(workspace.CanRunFolderOperations);

        workspace = CreateWorkspace(includePrompts: true);
        workspace.LeftRoot = left.Root;
        workspace.RightRoot = right.Root;
        await workspace.CompareAsync();
        workspace.SetSelectedRows([]);
        Assert.IsFalse(workspace.CanRunFolderOperations);

        workspace.SetSelectedRows([workspace.FilteredRows.Single()]);
        Assert.IsTrue(workspace.CanRunFolderOperations);
    }

    [TestMethod]
    public async Task RunFolderOperation_DoesNotExecute_WhenPreviewCancelled()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("only-left.txt", "source", BaseTime);

        RecordingFileOperations fileOperations = new();
        RecordingOperationPrompts prompts = new() { ConfirmPlanResult = FolderOperationConfirmation.Cancel };
        FolderCompareWorkspaceViewModel workspace = CreateWorkspace(
            fileOperations: fileOperations,
            operationPrompts: prompts);
        workspace.LeftRoot = left.Root;
        workspace.RightRoot = right.Root;
        await workspace.CompareAsync();

        workspace.SelectedRow = workspace.FilteredRows.Single();
        await workspace.RunFolderOperationAsync(FolderOperationKind.CopyLeftToRight);

        Assert.AreEqual(0, fileOperations.CopyCount);
        Assert.AreEqual(1, prompts.PlanPreviewCount);
    }

    [TestMethod]
    public async Task RunFolderOperation_CopiesAndRecompares_WhenConfirmed()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("only-left.txt", "source", BaseTime);

        RecordingFileOperations fileOperations = new();
        RecordingOperationPrompts prompts = new() { ConfirmPlanResult = FolderOperationConfirmation.Confirm };
        FolderCompareWorkspaceViewModel workspace = CreateWorkspace(
            fileOperations: fileOperations,
            operationPrompts: prompts);
        workspace.LeftRoot = left.Root;
        workspace.RightRoot = right.Root;
        await workspace.CompareAsync();

        workspace.SelectedRow = workspace.FilteredRows.Single(r => r.RelativePath == "only-left.txt");
        await workspace.RunFolderOperationAsync(FolderOperationKind.CopyLeftToRight);

        Assert.AreEqual(1, fileOperations.CopyCount);
        Assert.IsTrue(File.Exists(Path.Combine(right.Root, "only-left.txt")));
        Assert.AreEqual(1, prompts.PlanPreviewCount);
        Assert.AreEqual(WorkspaceUiState.Completed, workspace.State);
    }

    private static FolderCompareWorkspaceViewModel CreateWorkspace(
        bool includePrompts = true,
        IFolderFileOperations? fileOperations = null,
        IFolderOperationPromptService? operationPrompts = null)
        => new(
            new RunFolderComparisonUseCase(new QuickFolderComparisonEngine()),
            prompts: new RecordingPrompts(),
            operationPrompts: includePrompts
                ? operationPrompts ?? new RecordingOperationPrompts { ConfirmPlanResult = FolderOperationConfirmation.Confirm }
                : null,
            folderFileOperations: fileOperations ?? new RecordingFileOperations());

    private sealed class RecordingOperationPrompts : IFolderOperationPromptService
    {
        public FolderOperationConfirmation ConfirmPlanResult { get; init; } = FolderOperationConfirmation.Cancel;

        public bool ConfirmPermanentDeleteResult { get; init; } = true;

        public int PlanPreviewCount { get; private set; }

        public FolderOperationConfirmation ConfirmPlan(FolderOperationPlan plan)
        {
            PlanPreviewCount++;
            return ConfirmPlanResult;
        }

        public bool ConfirmPermanentDelete(int fileCount) => ConfirmPermanentDeleteResult;
    }

    private sealed class RecordingFileOperations : IFolderFileOperations
    {
        public int CopyCount { get; private set; }

        public void CopyFilePreservingSourceBytes(string sourceFullPath, string targetFullPath, bool overwrite)
        {
            CopyCount++;
            string? directory = Path.GetDirectoryName(targetFullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(sourceFullPath, targetFullPath, overwrite);
        }

        public FileDeleteOutcome DeleteFile(string fullPath, bool preferRecycleBin)
        {
            File.Delete(fullPath);
            return new FileDeleteOutcome(true, false, null);
        }
    }

    private sealed class RecordingPrompts : IUserPromptService
    {
        public SaveConflictDecision AskSaveConflict(string displayName) => SaveConflictDecision.Cancel;

        public DirtyCloseDecision AskCloseDirtyWorkspace(string workspaceTitle) => DirtyCloseDecision.Cancel;

        public bool ConfirmDiscardAllOnExit(int dirtyDocumentCount) => false;

        public void ShowInformation(string title, string message)
        {
        }
    }

    private sealed class TempTree : IDisposable
    {
        public string Root { get; }

        private TempTree(string root) => Root = root;

        public static TempTree Create()
        {
            string root = Path.Combine(Path.GetTempPath(), "MergeTool.App.Wpf.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TempTree(root);
        }

        public void WriteFile(string relativePath, string content, DateTime lastWriteUtc)
        {
            string fullPath = Path.Combine(Root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
            File.SetLastWriteTimeUtc(fullPath, lastWriteUtc);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
