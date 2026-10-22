using System.Text;
using MergeTool.Application.Sessions;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.FileSystem;
using MergeTool.Text;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class DocumentPersistenceServiceTests
{
    private static readonly DocumentPersistenceService Service = new(new PhysicalFileReader());
    private static readonly TextDocumentLoader Loader = new(new PhysicalFileReader());

    private string _directory = string.Empty;

    [TestInitialize]
    public void CreateTempDirectory()
    {
        _directory = Path.Combine(Path.GetTempPath(), "MergeToolTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void DeleteTempDirectory()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private async Task<TextDocumentSession> LoadDocumentAsync(string fileName, byte[] bytes)
    {
        string path = Path.Combine(_directory, fileName);
        await File.WriteAllBytesAsync(path, bytes);
        var load = await Loader.LoadAsync(path, CancellationToken.None);
        Assert.IsTrue(load.IsCompleted);
        return TextDocumentSession.FromSnapshot(load.Value);
    }

    [TestMethod]
    public async Task Save_WritesEditedText_PreservingEncodingBomAndLineEndings()
    {
        byte[] original = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("alpha\r\nbeta\r\n")];
        TextDocumentSession document = await LoadDocumentAsync("bom.txt", original);

        document.ReplaceText("alpha\r\nBETA\r\n");
        OperationResult result = await Service.SaveAsync(document, CancellationToken.None);

        Assert.IsTrue(result.IsCompleted);
        Assert.IsFalse(document.IsDirty);
        byte[] onDisk = await File.ReadAllBytesAsync(Path.Combine(_directory, "bom.txt"));
        byte[] expected = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("alpha\r\nBETA\r\n")];
        CollectionAssert.AreEqual(expected, onDisk, "BOM, encoding, and CRLF endings must round-trip exactly.");
    }

    [TestMethod]
    public async Task Save_PreservesLfOnlyConvention_WithoutBom()
    {
        TextDocumentSession document = await LoadDocumentAsync("lf.txt", Encoding.UTF8.GetBytes("a\nb\n"));

        document.ReplaceText("a\nB\n");
        OperationResult result = await Service.SaveAsync(document, CancellationToken.None);

        Assert.IsTrue(result.IsCompleted);
        byte[] onDisk = await File.ReadAllBytesAsync(Path.Combine(_directory, "lf.txt"));
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("a\nB\n"), onDisk, "No BOM, no CRLF conversion.");
    }

    [TestMethod]
    public async Task Save_AfterExternalModification_RefusesAndOverwritesNothing()
    {
        TextDocumentSession document = await LoadDocumentAsync("conflict.txt", Encoding.UTF8.GetBytes("loaded\r\n"));
        document.ReplaceText("my edit\r\n");

        string path = Path.Combine(_directory, "conflict.txt");
        await File.WriteAllBytesAsync(path, Encoding.UTF8.GetBytes("EXTERNAL CHANGE\r\n"));

        OperationResult result = await Service.SaveAsync(document, CancellationToken.None);

        Assert.IsTrue(result.IsFailed);
        Assert.AreEqual(DocumentPersistenceService.SaveConflictCode, result.Diagnostics[0].Code);
        Assert.AreEqual(DocumentPersistenceService.SaveConflictMessage, result.Diagnostics[0].Message);
        Assert.IsTrue(document.IsDirty, "The document stays dirty; nothing was saved.");
        Assert.AreEqual(
            "EXTERNAL CHANGE\r\n",
            await File.ReadAllTextAsync(path),
            "The external content must survive untouched (no silent overwrite).");
    }

    [TestMethod]
    public async Task SecondSave_UsesTheIdentityFromTheFirstSave()
    {
        TextDocumentSession document = await LoadDocumentAsync("twice.txt", Encoding.UTF8.GetBytes("v1\r\n"));

        document.ReplaceText("v2\r\n");
        Assert.IsTrue((await Service.SaveAsync(document, CancellationToken.None)).IsCompleted);
        document.ReplaceText("v3\r\n");
        Assert.IsTrue((await Service.SaveAsync(document, CancellationToken.None)).IsCompleted);

        Assert.AreEqual("v3\r\n", await File.ReadAllTextAsync(Path.Combine(_directory, "twice.txt")));
    }

    [TestMethod]
    public async Task Save_CleanDocument_IsANoOp()
    {
        TextDocumentSession document = await LoadDocumentAsync("clean.txt", Encoding.UTF8.GetBytes("same\r\n"));
        DateTime lastWrite = File.GetLastWriteTimeUtc(Path.Combine(_directory, "clean.txt"));

        OperationResult result = await Service.SaveAsync(document, CancellationToken.None);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual(lastWrite, File.GetLastWriteTimeUtc(Path.Combine(_directory, "clean.txt")));
    }

    [TestMethod]
    public async Task Save_ReadOnlyDocument_IsRefusedSafely()
    {
        string path = Path.Combine(_directory, "readonly.txt");
        await File.WriteAllBytesAsync(path, Encoding.UTF8.GetBytes("locked\r\n"));
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try
        {
            var load = await Loader.LoadAsync(path, CancellationToken.None);
            TextDocumentSession document = TextDocumentSession.FromSnapshot(load.Value);

            OperationResult result = await Service.SaveAsync(document, CancellationToken.None);

            Assert.IsTrue(result.IsFailed);
            Assert.AreEqual(DocumentPersistenceService.ReadOnlyDocumentCode, result.Diagnostics[0].Code);
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }

    [TestMethod]
    public async Task Reload_ReplacesContentAndClearsDirty()
    {
        TextDocumentSession document = await LoadDocumentAsync("reload.txt", Encoding.UTF8.GetBytes("v1\r\n"));
        document.ReplaceText("local edit\r\n");
        await File.WriteAllBytesAsync(Path.Combine(_directory, "reload.txt"), Encoding.UTF8.GetBytes("v2 from disk\r\n"));

        OperationResult result = await Service.ReloadAsync(document, CancellationToken.None);

        Assert.IsTrue(result.IsCompleted);
        Assert.IsFalse(document.IsDirty);
        Assert.AreEqual("v2 from disk\r\n", document.CurrentText);

        // The refreshed identity allows a subsequent save.
        document.ReplaceText("v3\r\n");
        Assert.IsTrue((await Service.SaveAsync(document, CancellationToken.None)).IsCompleted);
    }
}
