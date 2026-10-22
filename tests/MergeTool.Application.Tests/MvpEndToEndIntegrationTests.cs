using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.Folders;
using MergeTool.FileSystem;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Tests;

/// <summary>MVP-11 gate: folder compare row → paired file compare opens real differences.</summary>
[TestClass]
public sealed class MvpEndToEndIntegrationTests
{
    [TestMethod]
    public async Task FolderCompare_ChangedRow_OpensPairedFileComparisonWithDifferences()
    {
        string repoRoot = TestRepoRoot.Find();
        string leftRoot = Path.Combine(repoRoot, "tests", "TestData", "FolderFixtures", "left");
        string rightRoot = Path.Combine(repoRoot, "tests", "TestData", "FolderFixtures", "right");

        QuickFolderComparisonEngine engine = new();
        OperationResult<FolderComparisonResult> folderResult =
            engine.Compare(new FolderComparisonRequest(leftRoot, rightRoot));
        Assert.IsTrue(folderResult.IsCompleted);

        FolderComparisonRow? changed = folderResult.Value.Rows
            .SingleOrDefault(r => r.RelativePath.Equals("changed.txt", StringComparison.OrdinalIgnoreCase));
        Assert.IsNotNull(changed);
        Assert.IsTrue(changed.HasPairedFiles);
        Assert.AreEqual(FolderRowState.MetadataDifferent, changed.State);

        PhysicalFileReader reader = new();
        OpenFileComparisonUseCase open = new(reader, new PatienceLineDiffEngine());
        OperationResult<Sessions.TwoWayComparisonSession> opened = await open.OpenAsync(
            changed.LeftFullPath!,
            changed.RightFullPath!,
            TextComparisonOptions.WithIntralineDiff,
            CancellationToken.None);

        Assert.IsTrue(opened.IsCompleted);
        Assert.IsTrue(opened.Value.LatestResult!.Summary.HasDifferences);
    }
}

internal static class TestRepoRoot
{
    public static string Find()
    {
        DirectoryInfo? dir = new(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "mergetool_task_pack")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
