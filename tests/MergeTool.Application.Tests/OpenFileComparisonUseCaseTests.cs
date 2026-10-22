using System.Text;
using MergeTool.Application.Sessions;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class OpenFileComparisonUseCaseTests
{
    /// <summary>Returns fixed bytes per path; records whether it was called at all.</summary>
    private sealed class FixedBytesReader : IRawFileReader
    {
        private readonly Func<string, byte[]> _bytesForPath;

        public FixedBytesReader(Func<string, byte[]> bytesForPath) => _bytesForPath = bytesForPath;

        public bool WasCalled { get; private set; }

        public Task<RawFileContent> ReadAsync(string path, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(new RawFileContent(
                PathNormalization.NormalizeFullPath(path),
                _bytesForPath(path),
                new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc),
                IsReadOnly: false));
        }
    }

    private static OpenFileComparisonUseCase CreateUseCase(FixedBytesReader reader)
        => new(reader, new PatienceLineDiffEngine());

    [TestMethod]
    public async Task SamePath_IsRejectedWithoutTouchingTheFileSystem()
    {
        FixedBytesReader reader = new(_ => Encoding.UTF8.GetBytes("a"));
        OpenFileComparisonUseCase useCase = CreateUseCase(reader);

        OperationResult<TwoWayComparisonSession> result =
            await useCase.OpenAsync(@"C:\fixtures\a.txt", @"C:\fixtures\a.txt", TextComparisonOptions.WithIntralineDiff, CancellationToken.None);

        Assert.IsTrue(result.IsFailed);
        Assert.IsFalse(reader.WasCalled, "Rejection must happen before any file IO.");
        Assert.HasCount(1, result.Diagnostics);
        Assert.AreEqual(OpenFileComparisonUseCase.SamePathCode, result.Diagnostics[0].Code);
        Assert.AreEqual(OpenFileComparisonUseCase.SamePathMessage, result.Diagnostics[0].Message);
    }

    [TestMethod]
    public async Task SamePath_IsComparedInNormalizedForm()
    {
        FixedBytesReader reader = new(_ => Encoding.UTF8.GetBytes("a"));
        OpenFileComparisonUseCase useCase = CreateUseCase(reader);

        // Different spellings, same normalized file: casing and a redundant path segment.
        OperationResult<TwoWayComparisonSession> result = await useCase.OpenAsync(
            @"C:\fixtures\A.TXT",
            @"C:\fixtures\sub\..\a.txt",
            TextComparisonOptions.WithIntralineDiff,
            CancellationToken.None);

        Assert.IsTrue(result.IsFailed);
        Assert.AreEqual(OpenFileComparisonUseCase.SamePathCode, result.Diagnostics[0].Code);
    }

    [TestMethod]
    public async Task NotTextFile_IsRejectedAsUnsupported_NotOpenedAsBinaryComparison()
    {
        // NUL bytes classify the file as NotTextForThisProduct.
        FixedBytesReader reader = new(path => path.Contains("binary", StringComparison.OrdinalIgnoreCase)
            ? [0x4D, 0x5A, 0x00, 0x00, 0x01]
            : Encoding.UTF8.GetBytes("plain text\r\n"));
        OpenFileComparisonUseCase useCase = CreateUseCase(reader);

        OperationResult<TwoWayComparisonSession> result = await useCase.OpenAsync(
            @"C:\fixtures\left.txt",
            @"C:\fixtures\binary.dat",
            TextComparisonOptions.WithIntralineDiff,
            CancellationToken.None);

        Assert.IsTrue(result.IsFailed, "A non-text file must produce a failed open, not a session.");
        Assert.IsTrue(
            result.Diagnostics.Any(d => d.Code == OpenFileComparisonUseCase.NotTextCode),
            "The not-text diagnostic must be present.");
    }

    [TestMethod]
    public async Task TwoTextFiles_OpenIntoASessionWithAnAppliedComparison()
    {
        FixedBytesReader reader = new(path => Encoding.UTF8.GetBytes(
            path.Contains("left", StringComparison.OrdinalIgnoreCase) ? "a\r\nb\r\n" : "a\r\nc\r\n"));
        OpenFileComparisonUseCase useCase = CreateUseCase(reader);

        OperationResult<TwoWayComparisonSession> result = await useCase.OpenAsync(
            @"C:\fixtures\left.txt",
            @"C:\fixtures\right.txt",
            TextComparisonOptions.WithIntralineDiff,
            CancellationToken.None);

        Assert.IsTrue(result.IsCompleted);
        Assert.IsNotNull(result.Value.LatestResult);
        Assert.IsNotNull(result.Value.PresentationMap);
        Assert.IsFalse(result.Value.IsStale);
    }
}
