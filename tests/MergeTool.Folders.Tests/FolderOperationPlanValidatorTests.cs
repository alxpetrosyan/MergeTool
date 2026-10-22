using MergeTool.Core;
using MergeTool.Folders;
using MergeTool.Folders.Operations;

namespace MergeTool.Folders.Tests;

[TestClass]
public sealed class FolderOperationPlanValidatorTests
{
    private static readonly DateTime BaseTime = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Validate_RejectsMissingSourceAndIdentityMismatch()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("a.txt", "hello", BaseTime);

        var row = new FolderComparisonRow(
            "a.txt",
            FolderRowState.LeftOnly,
            Path.Combine(left.Root, "a.txt"),
            null,
            5,
            null,
            BaseTime,
            null,
            isDirectory: false);
        FolderOperationPlan plan = FolderOperationPlanner.BuildCopyLeftToRight([row], left.Root, right.Root);
        File.WriteAllText(Path.Combine(left.Root, "a.txt"), "changed");
        File.SetLastWriteTimeUtc(Path.Combine(left.Root, "a.txt"), BaseTime.AddMinutes(1));

        FolderOperationPlanValidationResult result = FolderOperationPlanValidator.Validate(plan, left.Root, right.Root);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Diagnostics.Any(d => d.Code == "MT-FOL-026"));
    }

    [TestMethod]
    public void Validate_RejectsUnsafeRelativePath()
    {
        FolderOperationPlan plan = new([
            new FolderOperationItem(
                "../escape.txt",
                FolderOperationKind.DeleteLeft,
                @"C:\left\escape.txt",
                null,
                null,
                false),
        ]);

        FolderOperationPlanValidationResult result = FolderOperationPlanValidator.Validate(plan, @"C:\left", @"C:\right");

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Diagnostics.Any(d => d.Code == "MT-FOL-022"));
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
