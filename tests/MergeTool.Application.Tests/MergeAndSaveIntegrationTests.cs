using MergeTool.Application.Sessions;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.FileSystem;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class MergeAndSaveIntegrationTests
{
    private string _directory = string.Empty;

    [TestInitialize]
    public void CreateTempDirectory()
    {
        _directory = Path.Combine(Path.GetTempPath(), "MergeToolMergeSave_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void DeleteTempDirectory()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { }
    }

    [TestMethod]
    public async Task CopyModifiedHunk_SaveRight_WritesMergedBytesToDisk()
    {
        string repoRoot = TestRepoRoot.Find();
        string leftFixture = Path.Combine(repoRoot, "tests", "TestData", "CompareFixtures", "mods_left.txt");
        string rightSource = Path.Combine(repoRoot, "tests", "TestData", "CompareFixtures", "mods_right.txt");
        string rightTarget = Path.Combine(_directory, "mods_right_saved.txt");
        File.Copy(rightSource, rightTarget, overwrite: true);

        PhysicalFileReader reader = new();
        OpenFileComparisonUseCase open = new(reader, new PatienceLineDiffEngine());
        DocumentPersistenceService persistence = new(reader);
        ManualTwoWayMergeService merge = new();

        OperationResult<TwoWayComparisonSession> opened =
            await open.OpenAsync(leftFixture, rightTarget, TextComparisonOptions.WithIntralineDiff, CancellationToken.None);
        Assert.IsTrue(opened.IsCompleted);
        TwoWayComparisonSession session = opened.Value;
        int ordinal = session.PresentationMap!.DifferenceBlockOrdinals[0];

        Assert.IsTrue(merge.CopyBlock(session, ordinal, ComparisonSide.Left).IsCompleted);
        Assert.IsTrue(session.Right.IsDirty);

        OperationResult save = await persistence.SaveAsync(session.Right, CancellationToken.None);
        Assert.IsTrue(save.IsCompleted, save.Diagnostics.FirstOrDefault()?.Message);

        string onDisk = await File.ReadAllTextAsync(rightTarget);
        StringAssert.Contains(onDisk, "line 5\r\n", "First hunk should match left after copy.");
        StringAssert.Contains(onDisk, "line 18 CHANGED", "Unmerged hunks must remain until copied.");
        Assert.IsFalse(session.Right.IsDirty, "Save clears dirty flag.");
    }
}
