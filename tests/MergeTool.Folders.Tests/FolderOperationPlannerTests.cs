using MergeTool.Core;
using MergeTool.Folders;
using MergeTool.Folders.Operations;

namespace MergeTool.Folders.Tests;

[TestClass]
public sealed class FolderOperationPlannerTests
{
    private static readonly DateTime BaseTime = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void BuildCopyLeftToRight_CreatesItemsForFiles_WithPreconditionAndOverwrite()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("a.txt", "hello", BaseTime);
        right.WriteFile("a.txt", "other", BaseTime);

        var row = new FolderComparisonRow(
            "a.txt",
            FolderRowState.MetadataDifferent,
            Path.Combine(left.Root, "a.txt"),
            Path.Combine(right.Root, "a.txt"),
            5,
            5,
            BaseTime,
            BaseTime,
            isDirectory: false);

        FolderOperationPlan plan = FolderOperationPlanner.BuildCopyLeftToRight([row], left.Root, right.Root);

        Assert.HasCount(1, plan.Items);
        Assert.AreEqual(FolderOperationKind.CopyLeftToRight, plan.Items[0].Kind);
        Assert.IsTrue(plan.Items[0].WillOverwriteTarget);
        Assert.IsNotNull(plan.Items[0].ExpectedSourcePrecondition);
        Assert.AreEqual(5, plan.Items[0].ExpectedSourcePrecondition!.Length);
    }

    [TestMethod]
    public void BuildCopyLeftToRight_SkipsDirectoriesAndMissingSources()
    {
        var directoryRow = new FolderComparisonRow(
            "dir",
            FolderRowState.LeftOnly,
            @"C:\l\dir",
            null,
            null,
            null,
            null,
            null,
            isDirectory: true);
        var missingSource = new FolderComparisonRow(
            "missing.txt",
            FolderRowState.RightOnly,
            null,
            @"C:\r\missing.txt",
            null,
            1,
            null,
            BaseTime,
            isDirectory: false);

        FolderOperationPlan plan = FolderOperationPlanner.BuildCopyLeftToRight(
            [directoryRow, missingSource],
            @"C:\l",
            @"C:\r");

        Assert.HasCount(0, plan.Items);
    }

    [TestMethod]
    public void BuildDeleteLeft_RequiresExistingFileUnderRoot()
    {
        using TempTree left = TempTree.Create();
        left.WriteFile("remove.txt", "x", BaseTime);
        var row = new FolderComparisonRow(
            "remove.txt",
            FolderRowState.LeftOnly,
            Path.Combine(left.Root, "remove.txt"),
            null,
            1,
            null,
            BaseTime,
            null,
            isDirectory: false);

        FolderOperationPlan plan = FolderOperationPlanner.BuildDeleteLeft([row], left.Root, @"C:\r");

        Assert.HasCount(1, plan.Items);
        Assert.AreEqual(FolderOperationKind.DeleteLeft, plan.Items[0].Kind);
        Assert.IsFalse(plan.Items[0].WillOverwriteTarget);
    }

    private sealed class TempTree : IDisposable
    {
        public string Root { get; }

        private TempTree(string root) => Root = root;

        public static TempTree Create()
        {
            string root = Path.Combine(Path.GetTempPath(), "MergeTool.Folders.Tests", Guid.NewGuid().ToString("N"));
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
