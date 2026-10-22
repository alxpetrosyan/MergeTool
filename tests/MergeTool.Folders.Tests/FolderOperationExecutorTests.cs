using MergeTool.Core;
using MergeTool.Folders;
using MergeTool.Folders.Operations;

namespace MergeTool.Folders.Tests;

[TestClass]
public sealed class FolderOperationExecutorTests
{
    private static readonly DateTime BaseTime = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Execute_CreatesTargetDirectory_AndContinuesAfterFailure()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("ok.txt", "ok", BaseTime);
        left.WriteFile("bad.txt", "bad", BaseTime);

        string okTarget = Path.Combine(right.Root, "ok.txt");
        string badTarget = Path.Combine(right.Root, "nested", "bad.txt");
        FolderOperationPlan plan = new([
            new FolderOperationItem(
                "ok.txt",
                FolderOperationKind.CopyLeftToRight,
                Path.Combine(left.Root, "ok.txt"),
                okTarget,
                FolderOperationPrecondition.FromFile(Path.Combine(left.Root, "ok.txt"), 2, BaseTime),
                false),
            new FolderOperationItem(
                "bad.txt",
                FolderOperationKind.CopyLeftToRight,
                Path.Combine(left.Root, "bad.txt"),
                badTarget,
                FolderOperationPrecondition.FromFile(Path.Combine(left.Root, "bad.txt"), 3, BaseTime),
                false),
        ]);

        RecordingFileOperations fileOperations = new()
        {
            CopyFailurePath = Path.Combine(left.Root, "bad.txt"),
        };
        FolderOperationExecutor executor = new(fileOperations);

        FolderOperationResult result = executor.Execute(plan, left.Root, right.Root, preferRecycleBin: true, CancellationToken.None);

        Assert.AreEqual(1, result.SucceededCount);
        Assert.AreEqual(1, result.FailedCount);
        Assert.IsTrue(Directory.Exists(Path.GetDirectoryName(badTarget)!));
        Assert.Contains(okTarget, fileOperations.CopiedPaths);
    }

    [TestMethod]
    public void Execute_RespectsCancellation()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("a.txt", "a", BaseTime);
        left.WriteFile("b.txt", "b", BaseTime);

        FolderOperationPlan plan = FolderOperationPlanner.BuildCopyLeftToRight(
            [
                new(
                    "a.txt",
                    FolderRowState.LeftOnly,
                    Path.Combine(left.Root, "a.txt"),
                    null,
                    1,
                    null,
                    BaseTime,
                    null,
                    isDirectory: false),
                new(
                    "b.txt",
                    FolderRowState.LeftOnly,
                    Path.Combine(left.Root, "b.txt"),
                    null,
                    1,
                    null,
                    BaseTime,
                    null,
                    isDirectory: false),
            ],
            left.Root,
            right.Root);

        using CancellationTokenSource cts = new();
        cts.Cancel();
        FolderOperationExecutor executor = new(new RecordingFileOperations());

        Assert.ThrowsExactly<OperationCanceledException>(() =>
            executor.Execute(plan, left.Root, right.Root, preferRecycleBin: true, cts.Token));
    }

    private sealed class RecordingFileOperations : IFolderFileOperations
    {
        public string? CopyFailurePath { get; init; }

        public List<string> CopiedPaths { get; } = [];

        public void CopyFilePreservingSourceBytes(string sourceFullPath, string targetFullPath, bool overwrite)
        {
            string? directory = Path.GetDirectoryName(targetFullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (string.Equals(sourceFullPath, CopyFailurePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("Access denied.");
            }

            File.Copy(sourceFullPath, targetFullPath, overwrite);
            CopiedPaths.Add(targetFullPath);
        }

        public FileDeleteOutcome DeleteFile(string fullPath, bool preferRecycleBin)
        {
            File.Delete(fullPath);
            return new FileDeleteOutcome(true, !preferRecycleBin, null);
        }
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
