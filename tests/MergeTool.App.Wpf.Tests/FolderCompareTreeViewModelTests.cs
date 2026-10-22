using MergeTool.App.Wpf.Services;
using MergeTool.App.Wpf.ViewModels;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.Folders;
using System.IO;

namespace MergeTool.App.Wpf.Tests;

/// <summary>
/// UI-REDESIGN-01: folder comparison tree-grid behavior — nested folders, per-state rows,
/// expansion, filters, and the double-click/open-paired-comparison path.
/// </summary>
[TestClass]
public sealed class FolderCompareTreeViewModelTests
{
    private static readonly DateTime BaseTime = new(2026, 7, 9, 8, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task Compare_BuildsTree_WithNestedFoldersAndFiles()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile(@"sub\inner.txt", "left content", BaseTime);
        right.WriteFile(@"sub\inner.txt", "right stuff!!", BaseTime);
        left.WriteFile("root-match.txt", "same", BaseTime);
        right.WriteFile("root-match.txt", "same", BaseTime);

        FolderCompareWorkspaceViewModel workspace = await CompareAsync(left, right);

        // Directories first, then files; children indented one level deeper.
        Assert.HasCount(3, workspace.TreeRows);
        FolderCompareTreeRowViewModel dir = workspace.TreeRows[0];
        Assert.AreEqual("sub", dir.Name);
        Assert.IsTrue(dir.IsDirectory);
        Assert.AreEqual(0, dir.Depth);
        Assert.IsTrue(dir.IsExpanded, "Directories start expanded.");
        FolderCompareTreeRowViewModel inner = workspace.TreeRows[1];
        Assert.AreEqual("inner.txt", inner.Name);
        Assert.AreEqual(1, inner.Depth);
        Assert.AreEqual(FolderRowState.MetadataDifferent, inner.State);
        Assert.AreEqual("root-match.txt", workspace.TreeRows[2].Name);
        Assert.AreEqual(FolderRowState.MetadataMatch, workspace.TreeRows[2].State);
    }

    [TestMethod]
    public async Task CollapseAndExpandDirectory_TogglesDescendantVisibility()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile(@"sub\a.txt", "x", BaseTime);
        right.WriteFile(@"sub\a.txt", "much longer content", BaseTime);

        FolderCompareWorkspaceViewModel workspace = await CompareAsync(left, right);
        FolderCompareTreeRowViewModel dir = workspace.TreeRows.Single(r => r.IsDirectory);

        dir.IsExpanded = false;
        Assert.HasCount(1, workspace.TreeRows, "Collapsed folder hides its children.");

        workspace.TreeRows.Single(r => r.IsDirectory).IsExpanded = true;
        Assert.HasCount(2, workspace.TreeRows);
    }

    [TestMethod]
    public async Task ExpandAllAndCollapseAllCommands_RebuildVisibleRows()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile(@"a\deep\file1.txt", "1", BaseTime);
        right.WriteFile(@"a\deep\file1.txt", "different!", BaseTime);

        FolderCompareWorkspaceViewModel workspace = await CompareAsync(left, right);
        Assert.HasCount(3, workspace.TreeRows, "a, a\\deep, file1.txt all visible when expanded.");

        workspace.CollapseAllFoldersCommand.Execute(null);
        Assert.HasCount(1, workspace.TreeRows);

        workspace.ExpandAllFoldersCommand.Execute(null);
        Assert.HasCount(3, workspace.TreeRows);
    }

    [TestMethod]
    public async Task Filter_Differences_HidesMatches_ButKeepsAncestorFolders()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile(@"changed\file.txt", "left", BaseTime);
        right.WriteFile(@"changed\file.txt", "right longer", BaseTime);
        left.WriteFile(@"clean\same.txt", "same", BaseTime, lengthOverride: 4);
        right.WriteFile(@"clean\same.txt", "same", BaseTime, lengthOverride: 4);

        FolderCompareWorkspaceViewModel workspace = await CompareAsync(left, right);
        workspace.ActiveFilter = FolderCompareFilter.Differences;

        Assert.IsTrue(workspace.TreeRows.Any(r => r.Name == "changed" && r.IsDirectory),
            "Folder containing a changed descendant stays visible.");
        Assert.IsTrue(workspace.TreeRows.Any(r => r.Name == "file.txt"));
        Assert.IsFalse(workspace.TreeRows.Any(r => r.Name == "same.txt"), "Matched file is filtered out.");
        Assert.IsFalse(workspace.TreeRows.Any(r => r.Name == "clean"), "Folder with only matches is filtered out.");
    }

    [TestMethod]
    public async Task Filter_LeftOnlyAndRightOnly_ShowOneSidedRows()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile(@"sub\left-only.txt", "L", BaseTime);
        right.WriteFile(@"sub\right-only.txt", "R", BaseTime);

        FolderCompareWorkspaceViewModel workspace = await CompareAsync(left, right);

        workspace.SetFilterLeftOnlyCommand.Execute(null);
        Assert.IsTrue(workspace.TreeRows.Any(r => r.Name == "left-only.txt"));
        Assert.IsFalse(workspace.TreeRows.Any(r => r.Name == "right-only.txt"));

        workspace.SetFilterRightOnlyCommand.Execute(null);
        Assert.IsTrue(workspace.TreeRows.Any(r => r.Name == "right-only.txt"));
        Assert.IsFalse(workspace.TreeRows.Any(r => r.Name == "left-only.txt"));
    }

    [TestMethod]
    public async Task TreeSelection_MapsToFlatSelection_AndOpensPairedComparison()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("pair.txt", "a", BaseTime);
        right.WriteFile("pair.txt", "bbbb", BaseTime);

        RecordingLauncher launcher = new();
        FolderCompareWorkspaceViewModel workspace = await CompareAsync(left, right, launcher);
        FolderCompareTreeRowViewModel fileRow = workspace.TreeRows.Single(r => !r.IsDirectory);

        workspace.SetSelectedTreeRows([fileRow]);
        Assert.IsNotNull(workspace.SelectedRow);
        Assert.AreEqual("pair.txt", workspace.SelectedRow!.RelativePath);
        Assert.IsTrue(workspace.CanOpenPairedTextComparison);

        await workspace.OpenPairedTextComparisonAsync();
        Assert.AreEqual(Path.Combine(left.Root, "pair.txt"), launcher.LastLeftPath);
        Assert.AreEqual(Path.Combine(right.Root, "pair.txt"), launcher.LastRightPath);
    }

    [TestMethod]
    public async Task OneSidedRow_Open_ShowsClearMessageInsteadOfComparison()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("only-left.txt", "L", BaseTime);
        right.WriteFile("anchor.txt", "x", BaseTime);
        left.WriteFile("anchor.txt", "x", BaseTime);

        RecordingLauncher launcher = new();
        RecordingPrompts prompts = new();
        FolderCompareWorkspaceViewModel workspace = await CompareAsync(left, right, launcher, prompts);
        FolderCompareTreeRowViewModel oneSided = workspace.TreeRows.Single(r => r.Name == "only-left.txt");

        workspace.SetSelectedTreeRows([oneSided]);
        Assert.IsFalse(workspace.CanOpenPairedTextComparison);
        await workspace.OpenPairedTextComparisonAsync();

        Assert.IsNull(launcher.LastLeftPath, "No comparison tab for a one-sided row.");
        Assert.AreEqual(1, prompts.InformationCount, "A clear message explains the missing pair.");
    }

    [TestMethod]
    public async Task SyntheticDirectoryRows_AreExcludedFromFolderOperations()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile(@"sub\file.txt", "L", BaseTime);

        FolderCompareWorkspaceViewModel workspace = await CompareAsync(left, right);
        FolderCompareTreeRowViewModel dir = workspace.TreeRows.Single(r => r.IsDirectory);
        Assert.IsNull(dir.RowViewModel, "Synthesized directory has no scan row.");

        workspace.SetSelectedTreeRows([dir]);
        Assert.IsNull(workspace.SelectedRow);
        Assert.IsFalse(workspace.CanOpenPairedTextComparison);
    }

    [TestMethod]
    public async Task RevealInExplorer_UsesRowFullPaths()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("pair.txt", "a", BaseTime);
        right.WriteFile("pair.txt", "bb", BaseTime);

        List<string> revealed = [];
        FolderCompareWorkspaceViewModel workspace = new(
            new RunFolderComparisonUseCase(new QuickFolderComparisonEngine()),
            prompts: new RecordingPrompts(),
            revealInExplorer: revealed.Add);
        workspace.LeftRoot = left.Root;
        workspace.RightRoot = right.Root;
        await workspace.CompareAsync();

        workspace.SetSelectedTreeRows([workspace.TreeRows.Single(r => !r.IsDirectory)]);
        Assert.IsTrue(workspace.RevealLeftInExplorerCommand.CanExecute(null));
        workspace.RevealLeftInExplorerCommand.Execute(null);
        workspace.RevealRightInExplorerCommand.Execute(null);

        Assert.HasCount(2, revealed);
        Assert.AreEqual(Path.Combine(left.Root, "pair.txt"), revealed[0]);
        Assert.AreEqual(Path.Combine(right.Root, "pair.txt"), revealed[1]);
    }

    private static async Task<FolderCompareWorkspaceViewModel> CompareAsync(
        TempTree left,
        TempTree right,
        IFileComparisonLauncher? launcher = null,
        IUserPromptService? prompts = null)
    {
        FolderCompareWorkspaceViewModel workspace = new(
            new RunFolderComparisonUseCase(new QuickFolderComparisonEngine()),
            prompts: prompts ?? new RecordingPrompts(),
            fileComparisonLauncher: launcher);
        workspace.LeftRoot = left.Root;
        workspace.RightRoot = right.Root;
        await workspace.CompareAsync();
        Assert.AreEqual(WorkspaceUiState.Completed, workspace.State);
        return workspace;
    }

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
            string root = Path.Combine(Path.GetTempPath(), "MergeTool.TreeTests", Guid.NewGuid().ToString("N"));
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
