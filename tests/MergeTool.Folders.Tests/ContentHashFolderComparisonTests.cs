using System.Security.Cryptography;
using MergeTool.Core;
using MergeTool.Folders;

namespace MergeTool.Folders.Tests;

[TestClass]
public sealed class ContentHashFolderComparisonTests
{
    private static readonly DateTime BaseTime = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LaterTime = BaseTime.AddHours(2);

    [TestMethod]
    public void ContentHash_IdenticalBytesDifferentTimestamps_ContentMatch()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("same-bytes.txt", "identical-payload", BaseTime);
        right.WriteFile("same-bytes.txt", "identical-payload", LaterTime);

        FolderComparisonRow row = AssertSingleRow(
            Compare(left.Root, right.Root, new FolderComparisonOptions(FolderComparisonMode.ContentHash)));

        Assert.AreEqual(FolderRowState.ContentMatch, row.State);
        Assert.AreEqual("Content match", FolderRowStateLabels.GetDisplayName(row.State));
    }

    [TestMethod]
    public void ContentHash_SameMetadataDifferentBytes_ContentDifferent()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("meta-same.txt", "alpha", BaseTime, lengthOverride: 10);
        right.WriteFile("meta-same.txt", "beta-beta", BaseTime, lengthOverride: 10);

        FolderComparisonRow row = AssertSingleRow(
            Compare(left.Root, right.Root, new FolderComparisonOptions(FolderComparisonMode.ContentHash)));

        Assert.AreEqual(FolderRowState.ContentDifferent, row.State);
    }

    [TestMethod]
    public void QuickMetadata_DifferentTimestamps_StayMetadataDifferent_WithZeroTolerance()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("same-bytes.txt", "identical-payload", BaseTime);
        right.WriteFile("same-bytes.txt", "identical-payload", LaterTime);

        FolderComparisonRow row = AssertSingleRow(Compare(left.Root, right.Root));

        Assert.AreEqual(FolderRowState.MetadataDifferent, row.State);
    }

    [TestMethod]
    public void QuickMetadata_TimestampTolerance_CanTreatNearTimesAsMetadataMatch()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("tolerant.txt", "payload", BaseTime, lengthOverride: 7);
        right.WriteFile("tolerant.txt", "payload", BaseTime.AddSeconds(30), lengthOverride: 7);

        FolderComparisonRow row = AssertSingleRow(
            Compare(
                left.Root,
                right.Root,
                new FolderComparisonOptions(
                    FolderComparisonMode.QuickMetadata,
                    TimeSpan.FromMinutes(1))));

        Assert.AreEqual(FolderRowState.MetadataMatch, row.State);
    }

    [TestMethod]
    public void ContentHash_Cancellation_ReturnsCancelled()
    {
        using TempTree left = TempTree.Create();
        using TempTree right = TempTree.Create();
        left.WriteFile("file.txt", new string('x', 32_000), BaseTime);
        right.WriteFile("file.txt", new string('x', 32_000), BaseTime);

        using CancellationTokenSource cts = new();
        cts.Cancel();

        OperationResult<FolderComparisonResult> result = Compare(
            left.Root,
            right.Root,
            new FolderComparisonOptions(FolderComparisonMode.ContentHash),
            cts.Token);

        Assert.IsTrue(result.IsCancelled);
    }

    [TestMethod]
    public void StreamingHasher_ReadsLargePayloadInMultipleChunks()
    {
        byte[] payload = new byte[StreamingSha256Hasher.DefaultBufferSize * 3 + 17];
        Random.Shared.NextBytes(payload);
        string expected = Convert.ToHexStringLower(SHA256.HashData(payload));

        using MemoryStream stream = new(payload);
        string actual = StreamingSha256Hasher.ComputeHex(stream, CancellationToken.None, bufferSize: 1024);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void PhysicalHashAccess_StreamsLargeFileWithoutLoadingAllAtOnce()
    {
        string path = Path.Combine(Path.GetTempPath(), "MergeTool.Folders.Tests", Guid.NewGuid().ToString("N") + ".bin");
        try
        {
            byte[] payload = new byte[StreamingSha256Hasher.DefaultBufferSize * 3 + 17];
            Random.Shared.NextBytes(payload);
            File.WriteAllBytes(path, payload);
            string expected = Convert.ToHexStringLower(SHA256.HashData(payload));

            PhysicalFileContentHashAccess access = new();
            Assert.IsTrue(access.TryComputeSha256(path, CancellationToken.None, out string actual, out Diagnostic? error));
            Assert.IsNull(error);
            Assert.AreEqual(expected, actual);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static OperationResult<FolderComparisonResult> Compare(
        string leftRoot,
        string rightRoot,
        FolderComparisonOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        QuickFolderComparisonEngine engine = new();
        return engine.Compare(
            new FolderComparisonRequest(leftRoot, rightRoot, options ?? FolderComparisonOptions.QuickMetadata),
            cancellationToken: cancellationToken);
    }

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
            }
        }
    }
}
