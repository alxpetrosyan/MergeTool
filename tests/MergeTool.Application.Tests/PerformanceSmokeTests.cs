using System.Diagnostics;
using MergeTool.Application.Sessions;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.FileSystem;
using MergeTool.Folders;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Tests;

/// <summary>
/// Smoke-level performance guards for large inputs. Budgets are generous for CI variability;
/// see <c>docs/performance/POL-03_budgets.md</c> for measurement environment notes.
/// </summary>
[TestClass]
public sealed class PerformanceSmokeTests
{
    private static readonly PhysicalFileReader Reader = new();
    private static readonly OpenFileComparisonUseCase OpenUseCase = new(Reader, new PatienceLineDiffEngine());
    private static readonly QuickFolderComparisonEngine FolderEngine = new();

    private string _directory = string.Empty;

    [TestInitialize]
    public void CreateTempDirectory()
    {
        _directory = Path.Combine(Path.GetTempPath(), "MergeToolPerf_" + Path.GetRandomFileName());
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

    [TestMethod]
    public async Task OneMegabyteText_OpenAndCompare_CompletesWithinBudget()
    {
        string leftPath = Path.Combine(_directory, "left.txt");
        string rightPath = Path.Combine(_directory, "right.txt");
        await File.WriteAllTextAsync(leftPath, CreateRepeatedLineFile(1_000_000));
        await File.WriteAllTextAsync(rightPath, CreateRepeatedLineFile(1_000_000, mutateEvery: 500));

        Stopwatch stopwatch = Stopwatch.StartNew();
        OperationResult<TwoWayComparisonSession> result = await OpenUseCase.OpenAsync(
            leftPath,
            rightPath,
            TextComparisonOptions.WithIntralineDiff,
            CancellationToken.None);
        stopwatch.Stop();

        Assert.IsTrue(result.IsCompleted, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.IsNotNull(result.Value.LatestResult);
        Assert.IsLessThanOrEqualTo(PerformanceBudgets.OneMegabyteOpenAndCompare, stopwatch.Elapsed);
    }

    [TestMethod]
    public void ThousandFolderEntries_QuickMetadataScan_CompletesWithinBudget()
    {
        (string leftRoot, string rightRoot) = CreateMirroredFolderPair(1_000);

        Stopwatch stopwatch = Stopwatch.StartNew();
        OperationResult<FolderComparisonResult> result = FolderEngine.Compare(
            new FolderComparisonRequest(leftRoot, rightRoot));
        stopwatch.Stop();

        Assert.IsTrue(result.IsCompleted);
        Assert.HasCount(1_000, result.Value.Rows);
        Assert.IsLessThanOrEqualTo(PerformanceBudgets.ThousandFolderEntries, stopwatch.Elapsed);
    }

    [TestMethod]
    public void TenThousandFolderEntries_QuickMetadataScan_CompletesWithinBudget()
    {
        (string leftRoot, string rightRoot) = CreateMirroredFolderPair(10_000);

        Stopwatch stopwatch = Stopwatch.StartNew();
        OperationResult<FolderComparisonResult> result = FolderEngine.Compare(
            new FolderComparisonRequest(leftRoot, rightRoot));
        stopwatch.Stop();

        Assert.IsTrue(result.IsCompleted);
        Assert.HasCount(10_000, result.Value.Rows);
        Assert.IsLessThanOrEqualTo(PerformanceBudgets.TenThousandFolderEntries, stopwatch.Elapsed);
    }

    [TestMethod]
    public void ContentHashMode_SmallTree_CompletesWithinBudget()
    {
        (string leftRoot, string rightRoot) = CreateMirroredFolderPair(200, payload: new string('a', 4_096));

        Stopwatch stopwatch = Stopwatch.StartNew();
        OperationResult<FolderComparisonResult> result = FolderEngine.Compare(
            new FolderComparisonRequest(
                leftRoot,
                rightRoot,
                new FolderComparisonOptions(FolderComparisonMode.ContentHash)));
        stopwatch.Stop();

        Assert.IsTrue(result.IsCompleted);
        Assert.IsLessThanOrEqualTo(PerformanceBudgets.TwoHundredFileContentHash, stopwatch.Elapsed);
    }

    private (string LeftRoot, string RightRoot) CreateMirroredFolderPair(int fileCount, string payload = "x")
    {
        string leftRoot = Path.Combine(_directory, $"left_{fileCount}");
        string rightRoot = Path.Combine(_directory, $"right_{fileCount}");
        Directory.CreateDirectory(leftRoot);
        Directory.CreateDirectory(rightRoot);

        for (int index = 0; index < fileCount; index++)
        {
            string relative = Path.Combine($"dir{index / 100}", $"file{index}.txt");
            string leftPath = Path.Combine(leftRoot, relative);
            string rightPath = Path.Combine(rightRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(leftPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(rightPath)!);
            File.WriteAllText(leftPath, payload);
            File.WriteAllText(rightPath, payload);
        }

        return (leftRoot, rightRoot);
    }

    private static string CreateRepeatedLineFile(int targetBytes, int mutateEvery = -1)
    {
        const string line = "line payload for performance smoke\r\n";
        var builder = new System.Text.StringBuilder(targetBytes + line.Length);
        int lineIndex = 0;
        while (builder.Length < targetBytes)
        {
            if (mutateEvery > 0 && lineIndex % mutateEvery == 0)
            {
                builder.Append("mutated line for diff detection\r\n");
            }
            else
            {
                builder.Append(line);
            }

            lineIndex++;
        }

        return builder.ToString();
    }

    private static class PerformanceBudgets
    {
        public static readonly TimeSpan OneMegabyteOpenAndCompare = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan ThousandFolderEntries = TimeSpan.FromSeconds(8);
        public static readonly TimeSpan TenThousandFolderEntries = TimeSpan.FromSeconds(45);
        public static readonly TimeSpan TwoHundredFileContentHash = TimeSpan.FromSeconds(30);
    }
}
