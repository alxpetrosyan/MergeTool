using MergeTool.Core;
using MergeTool.Folders;

namespace MergeTool.Folders.Tests;

[TestClass]
public sealed class QuickFolderComparisonEngineTests
{
    private static readonly DateTime BaseTime = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void MetadataMatch_SameLengthAndUtcTimestamp()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("same.txt", "hello", BaseTime, lengthOverride: 10);
        right.WriteFile("same.txt", "other-content-not-read", BaseTime, lengthOverride: 10);

        OperationResult<FolderComparisonResult> result = Compare(left.Root, right.Root);

        Assert.IsTrue(result.IsCompleted);
        FolderComparisonRow row = AssertSingleRow(result);
        Assert.AreEqual(FolderRowState.MetadataMatch, row.State);
        Assert.AreEqual("Metadata match", FolderRowStateLabels.GetDisplayName(row.State));
    }

    [TestMethod]
    public void MetadataDifferent_WhenLengthDiffers()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("changed.txt", "short", BaseTime);
        right.WriteFile("changed.txt", "much-longer-content", BaseTime);

        FolderComparisonRow row = AssertSingleRow(Compare(left.Root, right.Root));
        Assert.AreEqual(FolderRowState.MetadataDifferent, row.State);
    }

    [TestMethod]
    public void MetadataDifferent_WhenTimestampDiffers()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("time.txt", "abc", BaseTime);
        right.WriteFile("time.txt", "abc", BaseTime.AddHours(1));

        FolderComparisonRow row = AssertSingleRow(Compare(left.Root, right.Root));
        Assert.AreEqual(FolderRowState.MetadataDifferent, row.State);
    }

    [TestMethod]
    public void LeftOnly_AndRightOnly_AtNestedPaths()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile(Path.Combine("nested", "left-only.txt"), "x", BaseTime);
        right.WriteFile(Path.Combine("nested", "right-only.txt"), "y", BaseTime);

        OperationResult<FolderComparisonResult> result = Compare(left.Root, right.Root);
        Assert.IsTrue(result.IsCompleted);
        Assert.HasCount(2, result.Value.Rows);

        FolderComparisonRow leftOnly = result.Value.Rows.Single(r => r.State == FolderRowState.LeftOnly);
        FolderComparisonRow rightOnly = result.Value.Rows.Single(r => r.State == FolderRowState.RightOnly);
        Assert.AreEqual(Path.Combine("nested", "left-only.txt"), leftOnly.RelativePath);
        Assert.AreEqual(Path.Combine("nested", "right-only.txt"), rightOnly.RelativePath);
    }

    [TestMethod]
    public void CaseInsensitiveMatching_RetainsLeftDisplayCasing()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("Folder\\File.TXT", "a", BaseTime, lengthOverride: 5);
        right.WriteFile("folder\\file.txt", "b", BaseTime, lengthOverride: 5);

        FolderComparisonRow row = AssertSingleRow(Compare(left.Root, right.Root));
        Assert.AreEqual("Folder\\File.TXT", row.RelativePath);
        Assert.AreEqual(FolderRowState.MetadataMatch, row.State);
    }

    [TestMethod]
    public void InaccessiblePath_YieldsErrorRow_WhileOtherRowsRemain()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("good.txt", "ok", BaseTime);
        left.WriteFile(Path.Combine("broken", "inner.txt"), "bad", BaseTime);
        right.WriteFile("good.txt", "ok", BaseTime);

        FakeFolderScanAccess access = new();
        access.RegisterTree(left);
        access.RegisterTree(right);
        access.SetEnumerateFailure(
            Path.Combine(left.Root, "broken"),
            Diagnostic.Error("MT-TEST", "Simulated failure", path: "broken"));

        QuickFolderComparisonEngine engine = new(access);
        OperationResult<FolderComparisonResult> result = engine.Compare(new FolderComparisonRequest(left.Root, right.Root));

        Assert.IsTrue(result.IsCompleted);
        Assert.HasCount(2, result.Value.Rows);
        Assert.IsNotNull(result.Value.Rows.Single(r => r.RelativePath == "good.txt" && r.State == FolderRowState.MetadataMatch));
        FolderComparisonRow errorRow = result.Value.Rows.Single(r => r.State == FolderRowState.Error);
        Assert.Contains("broken", errorRow.RelativePath, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void Cancellation_ReturnsCancelledWithoutThrowing()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        for (int i = 0; i < 200; i++)
        {
            left.WriteFile($"file{i}.txt", "x", BaseTime);
            right.WriteFile($"file{i}.txt", "x", BaseTime);
        }

        using CancellationTokenSource cts = new();
        cts.Cancel();

        OperationResult<FolderComparisonResult> result = Compare(left.Root, right.Root, cancellationToken: cts.Token);
        Assert.IsTrue(result.IsCancelled);
    }

    [TestMethod]
    public void SameRoot_IsRejectedBeforeScan()
    {
        using TempTree left = TempTree.Create();
        OperationResult<FolderComparisonResult> result = Compare(left.Root, left.Root);

        Assert.IsTrue(result.IsFailed);
        Assert.IsTrue(result.Diagnostics.Any(d => d.Code == QuickFolderComparisonEngine.SameRootCode));
    }

    [TestMethod]
    public void MissingRoot_FailsWithDiagnostic()
    {
        using TempTree left = TempTree.Create();
        OperationResult<FolderComparisonResult> result = Compare(left.Root, Path.Combine(left.Root, "missing"));

        Assert.IsTrue(result.IsFailed);
        Assert.IsTrue(result.Diagnostics.Any(d => d.Code == QuickFolderComparisonEngine.RightRootMissingCode));
    }

    [TestMethod]
    public void Rows_AreSortedByOrdinalIgnoreCasePath()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("b.txt", "1", BaseTime);
        left.WriteFile("a.txt", "1", BaseTime);
        right.WriteFile("b.txt", "1", BaseTime);
        right.WriteFile("a.txt", "1", BaseTime);

        OperationResult<FolderComparisonResult> result = Compare(left.Root, right.Root);
        string[] paths = result.Value.Rows.Select(r => r.RelativePath).ToArray();
        CollectionAssert.AreEqual(new[] { "a.txt", "b.txt" }, paths);
    }

    [TestMethod]
    public void ReparsePoint_IsSkippedNotFollowed()
    {
        string leftRoot = CreateTempRoot();
        string rightRoot = CreateTempRoot();
        try
        {
            FakeFolderScanAccess access = new();
            access.SetDirectoryEntries(
                leftRoot,
                [new FolderScanEntry("outside.txt", false), new FolderScanEntry("link", true)]);
            access.SetFileMetadata(Path.Combine(leftRoot, "outside.txt"), 1, BaseTime);
            access.MarkReparsePoint(Path.Combine(leftRoot, "link"));
            access.SetDirectoryEntries(rightRoot, [new FolderScanEntry("outside.txt", false)]);
            access.SetFileMetadata(Path.Combine(rightRoot, "outside.txt"), 1, BaseTime);
            access.SetDirectoryEntries(
                Path.Combine(leftRoot, "link"),
                [new FolderScanEntry("hidden.txt", false)]);

            QuickFolderComparisonEngine engine = new(access);
            OperationResult<FolderComparisonResult> result =
                engine.Compare(new FolderComparisonRequest(leftRoot, rightRoot));

            Assert.IsTrue(result.IsCompleted);
            Assert.IsFalse(result.Value.Rows.Any(r => r.RelativePath.Contains("hidden", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(result.Value.Rows.Any(r => r.State == FolderRowState.SkippedReparsePoint));
        }
        finally
        {
            TryDelete(leftRoot);
            TryDelete(rightRoot);
        }
    }

    private static string CreateTempRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "MergeTool.Folders.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return PathNormalization.NormalizeFullPath(root);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
        }
    }

    private static OperationResult<FolderComparisonResult> Compare(
        string leftRoot,
        string rightRoot,
        CancellationToken cancellationToken = default)
    {
        QuickFolderComparisonEngine engine = new();
        return engine.Compare(new FolderComparisonRequest(leftRoot, rightRoot), cancellationToken: cancellationToken);
    }

    private static OperationResult<FolderComparisonResult> CompareThreeWay(
        string baseRoot,
        string leftRoot,
        string rightRoot,
        CancellationToken cancellationToken = default)
    {
        QuickFolderComparisonEngine engine = new();
        return engine.Compare(
            new FolderComparisonRequest(baseRoot, leftRoot, rightRoot),
            cancellationToken: cancellationToken);
    }

    [TestMethod]
    public void ThreeWay_AllPresenceCombinations_AreLabeledExplicitly()
    {
        using TempTree baseTree = TempTree.Create();
        using TempTree leftTree = TempTree.Create();
        using TempTree rightTree = TempTree.Create();

        baseTree.WriteFile("base-only.txt", "b", BaseTime);
        leftTree.WriteFile("left-only.txt", "l", BaseTime);
        rightTree.WriteFile("right-only.txt", "r", BaseTime);
        baseTree.WriteFile("base-left.txt", "bl", BaseTime);
        leftTree.WriteFile("base-left.txt", "bl", BaseTime);
        baseTree.WriteFile("base-right.txt", "br", BaseTime);
        rightTree.WriteFile("base-right.txt", "br", BaseTime);
        leftTree.WriteFile("left-right.txt", "lr", BaseTime);
        rightTree.WriteFile("left-right.txt", "lr", BaseTime);
        baseTree.WriteFile("all-three.txt", "same", BaseTime, lengthOverride: 4);
        leftTree.WriteFile("all-three.txt", "same", BaseTime, lengthOverride: 4);
        rightTree.WriteFile("all-three.txt", "same", BaseTime, lengthOverride: 4);
        baseTree.WriteFile("all-three-diff.txt", "a", BaseTime, lengthOverride: 1);
        leftTree.WriteFile("all-three-diff.txt", "b", BaseTime, lengthOverride: 2);
        rightTree.WriteFile("all-three-diff.txt", "c", BaseTime, lengthOverride: 3);

        OperationResult<FolderComparisonResult> result = CompareThreeWay(baseTree.Root, leftTree.Root, rightTree.Root);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual(FolderRowState.BaseOnly, RowState(result, "base-only.txt"));
        Assert.AreEqual(FolderRowState.LeftOnly, RowState(result, "left-only.txt"));
        Assert.AreEqual(FolderRowState.RightOnly, RowState(result, "right-only.txt"));
        Assert.AreEqual(FolderRowState.BaseLeftOnly, RowState(result, "base-left.txt"));
        Assert.AreEqual(FolderRowState.BaseRightOnly, RowState(result, "base-right.txt"));
        Assert.AreEqual(FolderRowState.LeftRightOnly, RowState(result, "left-right.txt"));
        Assert.AreEqual(FolderRowState.MetadataMatch, RowState(result, "all-three.txt"));
        Assert.AreEqual(FolderRowState.MetadataDifferent, RowState(result, "all-three-diff.txt"));
    }

    [TestMethod]
    public void ThreeWay_DuplicateRoots_AreRejectedBeforeScan()
    {
        using TempTree baseTree = TempTree.Create();
        using TempTree rightTree = TempTree.Create();

        OperationResult<FolderComparisonResult> result = CompareThreeWay(baseTree.Root, baseTree.Root, rightTree.Root);

        Assert.IsTrue(result.IsFailed);
        Assert.IsTrue(result.Diagnostics.Any(d => d.Code == QuickFolderComparisonEngine.DuplicateRootCode));
    }

    [TestMethod]
    public void ThreeWay_TripledFileRow_HasAllPaths()
    {
        using TempTree baseTree = TempTree.Create();
        using TempTree leftTree = TempTree.Create();
        using TempTree rightTree = TempTree.Create();
        baseTree.WriteFile("triple.txt", "a", BaseTime);
        leftTree.WriteFile("triple.txt", "b", BaseTime);
        rightTree.WriteFile("triple.txt", "c", BaseTime);

        FolderComparisonRow row = AssertSingleRow(CompareThreeWay(baseTree.Root, leftTree.Root, rightTree.Root));

        Assert.IsTrue(row.HasTripledFiles);
        Assert.AreEqual(Path.Combine(baseTree.Root, "triple.txt"), row.BaseFullPath);
        Assert.AreEqual(Path.Combine(leftTree.Root, "triple.txt"), row.LeftFullPath);
        Assert.AreEqual(Path.Combine(rightTree.Root, "triple.txt"), row.RightFullPath);
    }

    private static FolderRowState RowState(OperationResult<FolderComparisonResult> result, string relativePath)
        => result.Value.Rows.Single(r => r.RelativePath == relativePath).State;

    private static FolderComparisonRow AssertSingleRow(OperationResult<FolderComparisonResult> result)
    {
        Assert.IsTrue(result.IsCompleted);
        Assert.HasCount(1, result.Value.Rows);
        return result.Value.Rows[0];
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
                // Best-effort cleanup for temp tests.
            }
        }
    }

    private sealed class FakeFolderScanAccess : IFolderScanAccess
    {
        private readonly Dictionary<string, List<FolderScanEntry>> _directories = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _reparsePoints = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Diagnostic> _enumerateFailures = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, (long Length, DateTime LastWriteUtc)> _files = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _knownRoots = new(StringComparer.OrdinalIgnoreCase);

        public void RegisterTree(TempTree tree) => RegisterDirectory(tree.Root, tree.Root);

        public void SetDirectoryEntries(string directoryPath, IReadOnlyList<FolderScanEntry> entries)
        {
            directoryPath = Normalize(directoryPath);
            _knownRoots.Add(directoryPath);
            _directories[directoryPath] = [.. entries];
        }

        public void SetFileMetadata(string filePath, long length, DateTime lastWriteUtc)
            => _files[Normalize(filePath)] = (length, lastWriteUtc);

        public void MarkReparsePoint(string directoryPath)
            => _reparsePoints.Add(Normalize(directoryPath));

        public void SetEnumerateFailure(string directoryPath, Diagnostic diagnostic)
            => _enumerateFailures[Normalize(directoryPath)] = diagnostic;

        public bool DirectoryExists(string path)
        {
            path = Normalize(path);
            return _knownRoots.Contains(path) || _directories.ContainsKey(path) || Directory.Exists(path);
        }

        public bool IsReparsePointDirectory(string directoryPath) => _reparsePoints.Contains(Normalize(directoryPath));

        public bool TryEnumerateDirectory(
            string directoryPath,
            out IReadOnlyList<FolderScanEntry> entries,
            out Diagnostic? error)
        {
            directoryPath = Normalize(directoryPath);
            if (_enumerateFailures.TryGetValue(directoryPath, out Diagnostic? failure))
            {
                entries = [];
                error = failure;
                return false;
            }

            if (_directories.TryGetValue(directoryPath, out List<FolderScanEntry>? known))
            {
                entries = known;
                error = null;
                return true;
            }

            if (!Directory.Exists(directoryPath))
            {
                entries = [];
                error = Diagnostic.Error("MT-TEST", "Directory missing.", path: directoryPath);
                return false;
            }

            entries = [.. Directory.EnumerateFileSystemEntries(directoryPath).Select(entry =>
            {
                FileAttributes attributes = File.GetAttributes(entry);
                return new FolderScanEntry(Path.GetFileName(entry), attributes.HasFlag(FileAttributes.Directory));
            })];
            error = null;
            return true;
        }

        public bool TryGetFileMetadata(
            string filePath,
            out long lengthBytes,
            out DateTime lastWriteTimeUtc,
            out Diagnostic? error)
        {
            if (_files.TryGetValue(Normalize(filePath), out (long Length, DateTime LastWriteUtc) metadata))
            {
                lengthBytes = metadata.Length;
                lastWriteTimeUtc = metadata.LastWriteUtc;
                error = null;
                return true;
            }

            if (!File.Exists(filePath))
            {
                lengthBytes = 0;
                lastWriteTimeUtc = default;
                error = Diagnostic.Error("MT-TEST", "Missing file.", path: filePath);
                return false;
            }

            FileInfo info = new(filePath);
            lengthBytes = info.Length;
            lastWriteTimeUtc = info.LastWriteTimeUtc;
            error = null;
            return true;
        }

        private void RegisterDirectory(string root, string current)
        {
            current = Normalize(current);
            if (!_directories.TryGetValue(current, out List<FolderScanEntry>? entries))
            {
                entries = [];
                _directories[current] = entries;
            }

            foreach (string child in Directory.EnumerateFileSystemEntries(current))
            {
                string name = Path.GetFileName(child);
                FileAttributes attributes = File.GetAttributes(child);
                bool isDirectory = attributes.HasFlag(FileAttributes.Directory);
                entries.Add(new FolderScanEntry(name, isDirectory));
                if (isDirectory)
                {
                    RegisterDirectory(root, child);
                }
                else
                {
                    FileInfo info = new(child);
                    _files[Normalize(child)] = (info.Length, info.LastWriteTimeUtc);
                }
            }
        }

        private static string Normalize(string path) => PathNormalization.NormalizeFullPath(path);
    }
}
