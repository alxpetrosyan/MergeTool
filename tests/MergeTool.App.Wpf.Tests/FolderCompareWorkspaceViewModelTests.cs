using MergeTool.App.Wpf.Services;
using MergeTool.App.Wpf.ViewModels;
using MergeTool.Application.Requests;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.Folders;
using System.IO;

namespace MergeTool.App.Wpf.Tests;

[TestClass]
public sealed class FolderCompareWorkspaceViewModelTests
{
    private static readonly DateTime BaseTime = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void FilterRules_Differences_ExcludesMetadataMatch_IncludesOtherStates()
    {
        Assert.IsFalse(FolderCompareFilterRules.Includes(FolderCompareFilter.Differences, FolderRowState.MetadataMatch));
        Assert.IsTrue(FolderCompareFilterRules.Includes(FolderCompareFilter.Differences, FolderRowState.LeftOnly));
        Assert.IsTrue(FolderCompareFilterRules.Includes(FolderCompareFilter.Differences, FolderRowState.RightOnly));
        Assert.IsTrue(FolderCompareFilterRules.Includes(FolderCompareFilter.Differences, FolderRowState.MetadataDifferent));
        Assert.IsTrue(FolderCompareFilterRules.Includes(FolderCompareFilter.Differences, FolderRowState.ContentDifferent));
        Assert.IsFalse(FolderCompareFilterRules.Includes(FolderCompareFilter.Differences, FolderRowState.ContentMatch));
        Assert.IsTrue(FolderCompareFilterRules.Includes(FolderCompareFilter.Differences, FolderRowState.Error));
        Assert.IsTrue(FolderCompareFilterRules.Includes(FolderCompareFilter.Differences, FolderRowState.SkippedReparsePoint));
    }

    [TestMethod]
    public void FilterRules_ErrorsSkipped_IncludesOnlyErrorAndSkipped()
    {
        Assert.IsTrue(FolderCompareFilterRules.Includes(FolderCompareFilter.ErrorsSkipped, FolderRowState.Error));
        Assert.IsTrue(FolderCompareFilterRules.Includes(FolderCompareFilter.ErrorsSkipped, FolderRowState.SkippedReparsePoint));
        Assert.IsFalse(FolderCompareFilterRules.Includes(FolderCompareFilter.ErrorsSkipped, FolderRowState.MetadataDifferent));
    }

    [TestMethod]
    public void CompareCommand_RequiresDistinctValidRoots()
    {
        FolderCompareWorkspaceViewModel workspace = CreateWorkspace();
        Assert.IsFalse(workspace.CanCompare);

        workspace.LeftRoot = @"C:\left";
        workspace.RightRoot = @"C:\left";
        Assert.IsFalse(string.IsNullOrEmpty(workspace.RootValidationMessage));
        Assert.IsFalse(workspace.CanCompare);

        workspace.RightRoot = @"C:\right";
        Assert.IsNull(workspace.RootValidationMessage);
        Assert.IsTrue(workspace.CanCompare);
    }

    [TestMethod]
    public async Task Compare_PopulatesRows_AndAppliesActiveFilter()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("same.txt", "a", BaseTime, lengthOverride: 5);
        right.WriteFile("same.txt", "b", BaseTime, lengthOverride: 5);
        left.WriteFile("left-only.txt", "x", BaseTime);

        FolderCompareWorkspaceViewModel workspace = CreateWorkspace();
        workspace.LeftRoot = left.Root;
        workspace.RightRoot = right.Root;

        await workspace.CompareAsync();

        Assert.AreEqual(WorkspaceUiState.Completed, workspace.State);
        Assert.HasCount(2, workspace.FilteredRows);
        workspace.ActiveFilter = FolderCompareFilter.Differences;
        Assert.HasCount(1, workspace.FilteredRows);
        Assert.AreEqual(FolderRowState.LeftOnly, workspace.FilteredRows[0].State);
    }

    [TestMethod]
    public void OpenPairedTextComparison_IsEnabledOnlyForPairedFileRows()
    {
        FolderComparisonRow paired = new(
            "paired.txt",
            FolderRowState.MetadataDifferent,
            @"C:\left\paired.txt",
            @"C:\right\paired.txt",
            1,
            2,
            BaseTime,
            BaseTime,
            isDirectory: false);
        FolderComparisonRow oneSided = new(
            "left-only.txt",
            FolderRowState.LeftOnly,
            @"C:\left\left-only.txt",
            null,
            1,
            null,
            BaseTime,
            null,
            isDirectory: false);

        FolderCompareWorkspaceViewModel workspace = CreateWorkspace();
        workspace.GetType(); // keep instance

        FolderCompareRowViewModel pairedVm = new(paired);
        FolderCompareRowViewModel oneSidedVm = new(oneSided);
        Assert.IsTrue(pairedVm.CanOpenPairedTextComparison);
        Assert.IsFalse(oneSidedVm.CanOpenPairedTextComparison);

        workspace.SelectedRow = oneSidedVm;
        Assert.IsFalse(workspace.CanOpenPairedTextComparison);
        workspace.SelectedRow = pairedVm;
        Assert.IsTrue(workspace.CanOpenPairedTextComparison);
    }

    [TestMethod]
    public void BuildOpenPairedComparisonRequest_ReturnsNormalizedPaths()
    {
        FolderCompareWorkspaceViewModel workspace = CreateWorkspace();
        workspace.SelectedRow = new FolderCompareRowViewModel(new FolderComparisonRow(
            "file.txt",
            FolderRowState.MetadataDifferent,
            @"C:\left\file.txt",
            @"C:\right\file.txt",
            1,
            2,
            BaseTime,
            BaseTime,
            isDirectory: false));

        OpenPairedFileComparisonRequest? request = workspace.BuildOpenPairedComparisonRequest();

        Assert.IsNotNull(request);
        Assert.AreEqual(@"C:\left\file.txt", request.LeftPath);
        Assert.AreEqual(@"C:\right\file.txt", request.RightPath);
    }

    [TestMethod]
    public async Task OpenPairedTextComparison_UsesLauncher_WithBuiltRequest()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("pair.txt", "a\r\nx", BaseTime);
        right.WriteFile("pair.txt", "a\r\ny", BaseTime);

        RecordingLauncher launcher = new();
        FolderCompareWorkspaceViewModel workspace = CreateWorkspace(launcher);
        workspace.LeftRoot = left.Root;
        workspace.RightRoot = right.Root;
        await workspace.CompareAsync();

        FolderCompareRowViewModel row = workspace.FilteredRows.Single(r => r.RelativePath == "pair.txt");
        workspace.SelectedRow = row;
        await workspace.OpenPairedTextComparisonAsync();

        Assert.AreEqual(Path.Combine(left.Root, "pair.txt"), launcher.LastLeftPath);
        Assert.AreEqual(Path.Combine(right.Root, "pair.txt"), launcher.LastRightPath);
    }

    [TestMethod]
    public async Task FindRenameCandidates_RequiresCompletedScan()
    {
        FolderCompareWorkspaceViewModel workspace = CreateWorkspace();
        Assert.IsFalse(workspace.CanFindRenameCandidates);

        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("only-left.txt", "payload", BaseTime);
        right.WriteFile("only-right.txt", "payload", BaseTime);
        workspace.LeftRoot = left.Root;
        workspace.RightRoot = right.Root;
        await workspace.CompareAsync();

        Assert.IsTrue(workspace.CanFindRenameCandidates);
        await workspace.FindRenameCandidatesAsync();
        Assert.HasCount(1, workspace.RenameCandidates);
    }

    [TestMethod]
    public async Task AcceptRenameCandidate_OpensCrossPathComparison_WithoutFolderOps()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("old.txt", "same", BaseTime);
        right.WriteFile("new.txt", "same", BaseTime);

        RecordingLauncher launcher = new();
        FolderCompareWorkspaceViewModel workspace = CreateWorkspace(launcher);
        workspace.LeftRoot = left.Root;
        workspace.RightRoot = right.Root;
        await workspace.CompareAsync();
        await workspace.FindRenameCandidatesAsync();
        workspace.SelectedRenameCandidate = workspace.RenameCandidates.Single();

        await workspace.AcceptRenameCandidateAsync();

        Assert.AreEqual(Path.Combine(left.Root, "old.txt"), launcher.LastLeftPath);
        Assert.AreEqual(Path.Combine(right.Root, "new.txt"), launcher.LastRightPath);
    }

    private static FolderCompareWorkspaceViewModel CreateWorkspace(
        IFileComparisonLauncher? launcher = null)
        => new(
            new RunFolderComparisonUseCase(new QuickFolderComparisonEngine()),
            prompts: new RecordingPrompts(),
            fileComparisonLauncher: launcher);

    private sealed class RecordingLauncher : IFileComparisonLauncher
    {
        public string? LastLeftPath { get; private set; }

        public string? LastRightPath { get; private set; }

        public Task OpenPairedComparisonAsync(string leftPath, string rightPath)
        {
            LastLeftPath = leftPath;
            LastRightPath = rightPath;
            return Task.CompletedTask;
        }

        public Task OpenThreeWayComparisonAsync(string basePath, string leftPath, string rightPath)
            => Task.CompletedTask;
    }

    private sealed class RecordingPrompts : IUserPromptService
    {
        public int InformationCount { get; private set; }

        public SaveConflictDecision AskSaveConflict(string displayName) => SaveConflictDecision.Cancel;

        public DirtyCloseDecision AskCloseDirtyWorkspace(string workspaceTitle) => DirtyCloseDecision.Cancel;

        public bool ConfirmDiscardAllOnExit(int dirtyDocumentCount) => false;

        public void ShowInformation(string title, string message) => InformationCount++;
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

        public void WriteFile(string relativePath, string content, DateTime lastWriteUtc, long? lengthOverride = null)
        {
            string fullPath = Path.Combine(Root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
            if (lengthOverride is long targetLength && targetLength != new FileInfo(fullPath).Length)
            {
                using FileStream stream = new(fullPath, FileMode.Open, FileAccess.ReadWrite);
                stream.SetLength(targetLength);
            }

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
