using System.Text;
using MergeTool.Core;
using MergeTool.FileSystem;
using MergeTool.Text;

namespace MergeTool.Application.Tests;

// FileSystem-layer tests live here per the architecture decision recorded in
// docs/architecture/README.md (no dedicated MergeTool.FileSystem.Tests project).
[TestClass]
public sealed class SafeTextFileWriterTests
{
    private string _testRoot = string.Empty;

    [TestInitialize]
    public void CreateTestRoot()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "MergeToolTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_testRoot);
    }

    [TestCleanup]
    public void DeleteTestRoot()
    {
        try
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, recursive: true);
            }
        }
        catch (IOException)
        {
            // Never fail a test in cleanup.
        }
    }

    private async Task<(TextDocumentSnapshot Snapshot, string Path)> WriteAndLoadAsync(string fileName, byte[] bytes)
    {
        string path = Path.Combine(_testRoot, fileName);
        await File.WriteAllBytesAsync(path, bytes);
        TextDocumentLoader loader = new(new PhysicalFileReader());
        OperationResult<TextDocumentSnapshot> result = await loader.LoadAsync(path, CancellationToken.None);
        Assert.IsTrue(result.IsCompleted, "Fixture load must succeed.");
        return (result.Value, path);
    }

    [TestMethod]
    public async Task Save_PreservesUtf8BomOnRoundTrip()
    {
        byte[] original = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("line1\r\nline2\r\n")];
        (TextDocumentSnapshot snapshot, string path) = await WriteAndLoadAsync("utf8bom.txt", original);

        SafeTextFileWriter writer = new();
        SaveResult save = await writer.SaveAsync(
            snapshot.Identity,
            snapshot.Text,
            snapshot.Metadata.ToWritePolicy(),
            CancellationToken.None);

        Assert.AreEqual(SaveStatus.Saved, save.Status);
        byte[] roundTripped = await File.ReadAllBytesAsync(path);
        CollectionAssert.AreEqual(original, roundTripped);
    }

    [TestMethod]
    public async Task Save_PreservesUtf16LittleEndianOnRoundTrip()
    {
        byte[] original = [0xFF, 0xFE, .. Encoding.Unicode.GetBytes("Ünïcode\r\ntext")];
        (TextDocumentSnapshot snapshot, string path) = await WriteAndLoadAsync("utf16le.txt", original);

        SafeTextFileWriter writer = new();
        SaveResult save = await writer.SaveAsync(snapshot.Identity, snapshot.Text, snapshot.Metadata.ToWritePolicy(), CancellationToken.None);

        Assert.AreEqual(SaveStatus.Saved, save.Status);
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(path));
    }

    [TestMethod]
    public async Task Save_PreservesUtf16BigEndianOnRoundTrip()
    {
        byte[] original = [0xFE, 0xFF, .. Encoding.BigEndianUnicode.GetBytes("big endian\n")];
        (TextDocumentSnapshot snapshot, string path) = await WriteAndLoadAsync("utf16be.txt", original);

        SafeTextFileWriter writer = new();
        SaveResult save = await writer.SaveAsync(snapshot.Identity, snapshot.Text, snapshot.Metadata.ToWritePolicy(), CancellationToken.None);

        Assert.AreEqual(SaveStatus.Saved, save.Status);
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(path));
    }

    [TestMethod]
    public async Task Save_PreservesAnsiFallbackBytesOnRoundTrip()
    {
        // Invalid UTF-8; loads via the documented windows-1252 fallback.
        byte[] original = [(byte)'q', 0x93, (byte)'w', 0x94, 0x0D, 0x0A, (byte)'e'];
        (TextDocumentSnapshot snapshot, string path) = await WriteAndLoadAsync("ansi.txt", original);

        Assert.AreEqual(TextEncodings.Windows1252, snapshot.Metadata.EncodingWebName);

        SafeTextFileWriter writer = new();
        SaveResult save = await writer.SaveAsync(snapshot.Identity, snapshot.Text, snapshot.Metadata.ToWritePolicy(), CancellationToken.None);

        Assert.AreEqual(SaveStatus.Saved, save.Status);
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(path));
    }

    [TestMethod]
    public async Task Save_PreservesLineEndingsAndMissingFinalNewline()
    {
        byte[] original = Encoding.UTF8.GetBytes("mixed\r\nendings\nhere\rlast-no-newline");
        (TextDocumentSnapshot snapshot, string path) = await WriteAndLoadAsync("mixed.txt", original);

        Assert.IsTrue(snapshot.Metadata.HasMixedLineEndings);
        Assert.IsFalse(snapshot.Metadata.HasFinalNewline);

        SafeTextFileWriter writer = new();
        SaveResult save = await writer.SaveAsync(snapshot.Identity, snapshot.Text, snapshot.Metadata.ToWritePolicy(), CancellationToken.None);

        Assert.AreEqual(SaveStatus.Saved, save.Status);
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(path));
    }

    [TestMethod]
    public async Task Save_EmptyFile_RoundTrips()
    {
        (TextDocumentSnapshot snapshot, string path) = await WriteAndLoadAsync("empty.txt", []);

        SafeTextFileWriter writer = new();
        SaveResult save = await writer.SaveAsync(snapshot.Identity, snapshot.Text, snapshot.Metadata.ToWritePolicy(), CancellationToken.None);

        Assert.AreEqual(SaveStatus.Saved, save.Status);
        Assert.AreEqual(0, new FileInfo(path).Length);
    }

    [TestMethod]
    public async Task Save_RefusesToOverwriteExternallyModifiedFile()
    {
        byte[] original = Encoding.UTF8.GetBytes("original content\r\n");
        (TextDocumentSnapshot snapshot, string path) = await WriteAndLoadAsync("conflict.txt", original);

        byte[] externalChange = Encoding.UTF8.GetBytes("changed by someone else\r\n");
        await File.WriteAllBytesAsync(path, externalChange);

        SafeTextFileWriter writer = new();
        SaveResult save = await writer.SaveAsync(snapshot.Identity, "my edited text\r\n", snapshot.Metadata.ToWritePolicy(), CancellationToken.None);

        Assert.AreEqual(SaveStatus.ExternalModificationConflict, save.Status);
        Assert.IsNull(save.NewIdentity);
        CollectionAssert.AreEqual(externalChange, await File.ReadAllBytesAsync(path), "The external change must not be overwritten.");
    }

    [TestMethod]
    public async Task Save_RefusesWhenTargetFileWasDeleted()
    {
        byte[] original = Encoding.UTF8.GetBytes("content\r\n");
        (TextDocumentSnapshot snapshot, string path) = await WriteAndLoadAsync("deleted.txt", original);
        File.Delete(path);

        SafeTextFileWriter writer = new();
        SaveResult save = await writer.SaveAsync(snapshot.Identity, "anything", snapshot.Metadata.ToWritePolicy(), CancellationToken.None);

        Assert.AreEqual(SaveStatus.ExternalModificationConflict, save.Status);
        Assert.IsFalse(File.Exists(path));
    }

    [TestMethod]
    public async Task Save_LeavesNoTempFileAfterSuccess()
    {
        byte[] original = Encoding.UTF8.GetBytes("a\r\n");
        (TextDocumentSnapshot snapshot, _) = await WriteAndLoadAsync("clean.txt", original);

        SafeTextFileWriter writer = new();
        await writer.SaveAsync(snapshot.Identity, snapshot.Text, snapshot.Metadata.ToWritePolicy(), CancellationToken.None);

        Assert.HasCount(1, Directory.GetFiles(_testRoot), "Only the target file may remain.");
    }

    [TestMethod]
    public async Task Save_LeavesNoTempFileAfterEncodingFailure()
    {
        byte[] original = [(byte)'a', 0x93];
        (TextDocumentSnapshot snapshot, _) = await WriteAndLoadAsync("encfail.txt", original);
        Assert.AreEqual(TextEncodings.Windows1252, snapshot.Metadata.EncodingWebName);

        SafeTextFileWriter writer = new();
        // U+4E2D cannot be encoded as windows-1252: the save must fail loudly, not silently
        // convert to another encoding, and must clean up its temp file.
        await Assert.ThrowsExactlyAsync<EncoderFallbackException>(
            () => writer.SaveAsync(snapshot.Identity, "中", snapshot.Metadata.ToWritePolicy(), CancellationToken.None));

        Assert.HasCount(1, Directory.GetFiles(_testRoot));
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(Path.Combine(_testRoot, "encfail.txt")));
    }

    [TestMethod]
    public async Task Save_ReturnsNewIdentityUsableForNextSave()
    {
        byte[] original = Encoding.UTF8.GetBytes("v1\r\n");
        (TextDocumentSnapshot snapshot, string path) = await WriteAndLoadAsync("resave.txt", original);

        SafeTextFileWriter writer = new();
        SaveResult first = await writer.SaveAsync(snapshot.Identity, "v2\r\n", snapshot.Metadata.ToWritePolicy(), CancellationToken.None);
        Assert.AreEqual(SaveStatus.Saved, first.Status);
        Assert.IsNotNull(first.NewIdentity);

        SaveResult second = await writer.SaveAsync(first.NewIdentity, "v3\r\n", snapshot.Metadata.ToWritePolicy(), CancellationToken.None);
        Assert.AreEqual(SaveStatus.Saved, second.Status);
        Assert.AreEqual("v3\r\n", await File.ReadAllTextAsync(path));
    }

    [TestMethod]
    public async Task PhysicalReader_MissingFile_BecomesFailedLoadResult()
    {
        TextDocumentLoader loader = new(new PhysicalFileReader());

        OperationResult<TextDocumentSnapshot> result = await loader.LoadAsync(
            Path.Combine(_testRoot, "does-not-exist.txt"),
            CancellationToken.None);

        Assert.IsTrue(result.IsFailed);
        Assert.AreEqual(TextDocumentLoader.ReadFailureCode, result.Diagnostics[0].Code);
    }

    [TestMethod]
    public async Task PhysicalReader_ReadOnlyFile_IsFlaggedInMetadata()
    {
        string path = Path.Combine(_testRoot, "readonly.txt");
        await File.WriteAllTextAsync(path, "locked\r\n");
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try
        {
            TextDocumentLoader loader = new(new PhysicalFileReader());
            OperationResult<TextDocumentSnapshot> result = await loader.LoadAsync(path, CancellationToken.None);

            Assert.IsTrue(result.IsCompleted);
            Assert.IsTrue(result.Value.Metadata.IsReadOnly);
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }
}
