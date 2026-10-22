using System.Text;
using MergeTool.Core;
using MergeTool.Text;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class TextDocumentLoaderTests
{
    private static readonly DateTime SampleUtc = new(2026, 7, 2, 8, 30, 0, DateTimeKind.Utc);

    private sealed class FakeReader : IRawFileReader
    {
        public byte[] Bytes { get; init; } = [];
        public bool IsReadOnly { get; init; }
        public Exception? ThrowOnRead { get; init; }

        public Task<RawFileContent> ReadAsync(string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ThrowOnRead is not null)
            {
                throw ThrowOnRead;
            }

            return Task.FromResult(new RawFileContent(@"C:\fixtures\sample.txt", Bytes, SampleUtc, IsReadOnly));
        }
    }

    [TestMethod]
    public async Task Load_BuildsSnapshotWithMetadataAndIdentity()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("alpha\r\nbeta");
        TextDocumentLoader loader = new(new FakeReader { Bytes = bytes, IsReadOnly = true });

        OperationResult<TextDocumentSnapshot> result = await loader.LoadAsync(@"C:\fixtures\sample.txt", CancellationToken.None);

        Assert.IsTrue(result.IsCompleted);
        TextDocumentSnapshot snapshot = result.Value;
        Assert.AreEqual(@"C:\fixtures\sample.txt", snapshot.Metadata.NormalizedFullPath);
        Assert.AreEqual("sample.txt", snapshot.Metadata.DisplayName);
        Assert.AreEqual(TextEncodings.Utf8, snapshot.Metadata.EncodingWebName);
        Assert.IsFalse(snapshot.Metadata.HasByteOrderMark);
        Assert.AreEqual(LineEndingStyle.CrLf, snapshot.Metadata.DominantLineEnding);
        Assert.IsFalse(snapshot.Metadata.HasFinalNewline);
        Assert.IsTrue(snapshot.Metadata.IsReadOnly);
        Assert.AreEqual(bytes.LongLength, snapshot.Metadata.Length);
        Assert.AreEqual(SampleUtc, snapshot.Metadata.LastWriteTimeUtc);
        Assert.AreEqual(TextClassification.Text, snapshot.Metadata.Classification);
        Assert.HasCount(2, snapshot.Lines);
        Assert.AreEqual("alpha\r\nbeta", snapshot.Text);
        Assert.AreEqual(FileVersionToken.FromBytes(bytes, SampleUtc), snapshot.Identity.Version);
    }

    [TestMethod]
    public async Task Load_ReadFailure_ReturnsFailedResultWithSafeDiagnostic()
    {
        TextDocumentLoader loader = new(new FakeReader
        {
            ThrowOnRead = new FileNotFoundException("Could not find file 'C:\\fixtures\\missing.txt'."),
        });

        OperationResult<TextDocumentSnapshot> result = await loader.LoadAsync(@"C:\fixtures\missing.txt", CancellationToken.None);

        Assert.IsTrue(result.IsFailed);
        Assert.HasCount(1, result.Diagnostics);
        Diagnostic diagnostic = result.Diagnostics[0];
        Assert.AreEqual(TextDocumentLoader.ReadFailureCode, diagnostic.Code);
        Assert.AreEqual("The file could not be read.", diagnostic.Message);
        Assert.IsNotNull(diagnostic.TechnicalDetail);
    }

    [TestMethod]
    public async Task Load_NotText_CompletesWithClassificationAndDiagnostic()
    {
        TextDocumentLoader loader = new(new FakeReader { Bytes = [0x00, 0x01, 0x02] });

        OperationResult<TextDocumentSnapshot> result = await loader.LoadAsync(@"C:\fixtures\sample.txt", CancellationToken.None);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual(TextClassification.NotTextForThisProduct, result.Value.Metadata.Classification);
        Assert.IsEmpty(result.Value.Lines);
        Assert.HasCount(1, result.Value.Metadata.LoadDiagnostics);
    }

    [TestMethod]
    public async Task Load_Cancellation_PropagatesForApplicationLayer()
    {
        TextDocumentLoader loader = new(new FakeReader { Bytes = [] });
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => loader.LoadAsync(@"C:\fixtures\sample.txt", cts.Token));
    }
}
