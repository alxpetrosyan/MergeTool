using MergeTool.Folders;

namespace MergeTool.Folders.Tests;

[TestClass]
public sealed class FolderRenameCandidateFinderTests
{
    private static readonly DateTime BaseTime = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Find_ContentHashMatch_AcrossDifferentPaths()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("old/name.txt", "same-payload", BaseTime);
        right.WriteFile("new/name.txt", "same-payload", BaseTime);

        IReadOnlyList<FolderComparisonRow> rows =
        [
            Row("old/name.txt", FolderRowState.LeftOnly, left.FullPathFor("old/name.txt"), null),
            Row("new/name.txt", FolderRowState.RightOnly, null, right.FullPathFor("new/name.txt")),
        ];

        FolderRenameCandidateFinder finder = new();
        IReadOnlyList<FolderRenameCandidate> candidates = finder.Find(rows).Value!;

        Assert.HasCount(1, candidates);
        Assert.AreEqual(FolderRenameCandidateMatchKind.ContentHash, candidates[0].MatchKind);
        Assert.AreEqual("old/name.txt", candidates[0].LeftRelativePath);
        Assert.AreEqual("new/name.txt", candidates[0].RightRelativePath);
    }

    [TestMethod]
    public void Find_NameSimilarity_WhenHashesDiffer()
    {
        FolderComparisonRow left = Row(
            "src/report_v1.txt",
            FolderRowState.LeftOnly,
            @"C:\left\src\report_v1.txt",
            null,
            10,
            null);
        FolderComparisonRow right = Row(
            "src/report_v2.txt",
            FolderRowState.RightOnly,
            null,
            @"C:\right\src\report_v2.txt",
            null,
            11);

        FolderRenameCandidateFinder finder = new(new NeverHashAccess());
        IReadOnlyList<FolderRenameCandidate> candidates = finder.Find([left, right]).Value!;

        Assert.HasCount(1, candidates);
        Assert.AreEqual(FolderRenameCandidateMatchKind.NameSimilarity, candidates[0].MatchKind);
    }

    [TestMethod]
    public void Find_DoesNotPairDirectories()
    {
        IReadOnlyList<FolderComparisonRow> rows =
        [
            new("dir", FolderRowState.LeftOnly, @"C:\left\dir", null, null, null, BaseTime, null, isDirectory: true),
            new("dir2", FolderRowState.RightOnly, null, @"C:\right\dir2", null, null, null, BaseTime, isDirectory: true),
        ];

        FolderRenameCandidateFinder finder = new();
        IReadOnlyList<FolderRenameCandidate> candidates = finder.Find(rows).Value!;

        Assert.IsEmpty(candidates);
    }

    [TestMethod]
    public void AreNamesSimilar_RequiresMatchingExtension()
    {
        Assert.IsFalse(FolderRenameCandidateFinder.AreNamesSimilar(@"C:\a\file.txt", @"C:\b\file.log", 2));
        Assert.IsTrue(FolderRenameCandidateFinder.AreNamesSimilar(@"C:\a\report_v1.txt", @"C:\b\report_v2.txt", 2));
    }

    private static FolderComparisonRow Row(
        string relativePath,
        FolderRowState state,
        string? leftPath,
        string? rightPath,
        long? leftSize = 13,
        long? rightSize = 13)
        => new(
            relativePath,
            state,
            leftPath,
            rightPath,
            leftSize,
            rightSize,
            BaseTime,
            BaseTime,
            isDirectory: false);

    private sealed class NeverHashAccess : IFileContentHashAccess
    {
        public bool TryComputeSha256(string filePath, CancellationToken cancellationToken, out string sha256Hex, out Core.Diagnostic? error)
        {
            sha256Hex = string.Empty;
            error = null;
            return false;
        }
    }

    private sealed class TempTree : IDisposable
    {
        public string Root { get; }

        private TempTree(string root) => Root = root;

        public static TempTree Create()
        {
            string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MergeTool.Folders.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TempTree(root);
        }

        public string FullPathFor(string relativePath) => System.IO.Path.Combine(Root, relativePath);

        public void WriteFile(string relativePath, string content, DateTime lastWriteUtc)
        {
            string fullPath = FullPathFor(relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
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
