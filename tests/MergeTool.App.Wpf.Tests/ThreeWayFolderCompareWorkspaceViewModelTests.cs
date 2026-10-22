using MergeTool.App.Wpf.Services;
using MergeTool.App.Wpf.ViewModels;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.Folders;
using System.IO;

namespace MergeTool.App.Wpf.Tests;

[TestClass]
public sealed class ThreeWayFolderCompareWorkspaceViewModelTests
{
    private static readonly DateTime BaseTime = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void CompareCommand_RequiresThreeDistinctValidRoots()
    {
        ThreeWayFolderCompareWorkspaceViewModel workspace = CreateWorkspace();
        Assert.IsFalse(workspace.CanCompare);

        workspace.BaseRoot = @"C:\base";
        workspace.LeftRoot = @"C:\left";
        workspace.RightRoot = @"C:\right";
        Assert.IsTrue(workspace.CanCompare);

        workspace.RightRoot = @"C:\left";
        Assert.IsFalse(workspace.CanCompare);
        Assert.AreEqual(QuickFolderComparisonEngine.DuplicateRootMessage, workspace.RootValidationMessage);
    }

    [TestMethod]
    public async Task Compare_PopulatesRows_WithThreeWayStates()
    {
        using TempTree baseTree = TempTree.Create();
        using TempTree leftTree = TempTree.Create();
        using TempTree rightTree = TempTree.Create();
        baseTree.WriteFile("shared.txt", "a", BaseTime, lengthOverride: 5);
        leftTree.WriteFile("shared.txt", "b", BaseTime, lengthOverride: 5);
        rightTree.WriteFile("shared.txt", "c", BaseTime, lengthOverride: 5);
        baseTree.WriteFile("base-only.txt", "x", BaseTime);

        ThreeWayFolderCompareWorkspaceViewModel workspace = CreateWorkspace();
        workspace.BaseRoot = baseTree.Root;
        workspace.LeftRoot = leftTree.Root;
        workspace.RightRoot = rightTree.Root;

        await workspace.CompareAsync();

        Assert.AreEqual(WorkspaceUiState.Completed, workspace.State);
        Assert.HasCount(2, workspace.FilteredRows);
        FolderCompareRowViewModel shared = workspace.FilteredRows.Single(r => r.RelativePath == "shared.txt");
        Assert.AreEqual(FolderRowState.MetadataMatch, shared.State);
        Assert.AreEqual("Metadata match", shared.StateDisplay);
        Assert.AreEqual(FolderRowState.BaseOnly, workspace.FilteredRows.Single(r => r.RelativePath == "base-only.txt").State);
    }

    [TestMethod]
    public void OpenThreeWayTextComparison_IsEnabledOnlyForTripledFileRows()
    {
        FolderComparisonRow triple = new(
            "triple.txt",
            FolderRowState.MetadataDifferent,
            @"C:\left\triple.txt",
            @"C:\right\triple.txt",
            1,
            2,
            BaseTime,
            BaseTime,
            isDirectory: false,
            baseFullPath: @"C:\base\triple.txt",
            baseSizeBytes: 3,
            baseLastWriteTimeUtc: BaseTime);
        FolderComparisonRow partial = new(
            "partial.txt",
            FolderRowState.BaseLeftOnly,
            @"C:\left\partial.txt",
            null,
            1,
            null,
            BaseTime,
            null,
            isDirectory: false,
            baseFullPath: @"C:\base\partial.txt");

        ThreeWayFolderCompareWorkspaceViewModel workspace = CreateWorkspace();
        workspace.SelectedRow = new FolderCompareRowViewModel(partial);
        Assert.IsFalse(workspace.CanOpenThreeWayTextComparison);

        workspace.SelectedRow = new FolderCompareRowViewModel(triple);
        Assert.IsTrue(workspace.CanOpenThreeWayTextComparison);
    }

    [TestMethod]
    public async Task OpenThreeWayTextComparison_UsesLauncher_WithBuiltRequest()
    {
        using TempTree baseTree = TempTree.Create();
        using TempTree leftTree = TempTree.Create();
        using TempTree rightTree = TempTree.Create();
        baseTree.WriteFile("triple.txt", "a", BaseTime);
        leftTree.WriteFile("triple.txt", "b", BaseTime);
        rightTree.WriteFile("triple.txt", "c", BaseTime);

        RecordingLauncher launcher = new();
        ThreeWayFolderCompareWorkspaceViewModel workspace = CreateWorkspace(launcher);
        workspace.BaseRoot = baseTree.Root;
        workspace.LeftRoot = leftTree.Root;
        workspace.RightRoot = rightTree.Root;
        await workspace.CompareAsync();

        workspace.SelectedRow = workspace.FilteredRows.Single(r => r.RelativePath == "triple.txt");
        await workspace.OpenThreeWayTextComparisonAsync();

        Assert.AreEqual(Path.Combine(baseTree.Root, "triple.txt"), launcher.LastBasePath);
        Assert.AreEqual(Path.Combine(leftTree.Root, "triple.txt"), launcher.LastLeftPath);
        Assert.AreEqual(Path.Combine(rightTree.Root, "triple.txt"), launcher.LastRightPath);
    }

    private static ThreeWayFolderCompareWorkspaceViewModel CreateWorkspace(
        IFileComparisonLauncher? launcher = null)
        => new(
            new RunFolderComparisonUseCase(new QuickFolderComparisonEngine()),
            fileComparisonLauncher: launcher);

    private sealed class RecordingLauncher : IFileComparisonLauncher
    {
        public string? LastBasePath { get; private set; }

        public string? LastLeftPath { get; private set; }

        public string? LastRightPath { get; private set; }

        public Task OpenPairedComparisonAsync(string leftPath, string rightPath) => Task.CompletedTask;

        public Task OpenThreeWayComparisonAsync(string basePath, string leftPath, string rightPath)
        {
            LastBasePath = basePath;
            LastLeftPath = leftPath;
            LastRightPath = rightPath;
            return Task.CompletedTask;
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
