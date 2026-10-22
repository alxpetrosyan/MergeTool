using MergeTool.Application.Navigation;
using MergeTool.Application.Sessions;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.Core.Diagnostics;
using MergeTool.Core.Settings;
using MergeTool.Folders;
using MergeTool.Text;
using MergeTool.Text.Diff;
using MergeTool.Text.Search;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class SafeFailureModesTests
{
    [TestMethod]
    public async Task InvalidRegex_Recompare_PreservesLastCompletedResult()
    {
        TwoWayComparisonSession session = new(
            SessionTestHelper.SessionFromText("a\r\n", fileName: "left.txt"),
            SessionTestHelper.SessionFromText("b\r\n", fileName: "right.txt"),
            new PatienceLineDiffEngine());

        await session.RecompareAsync(CancellationToken.None);
        IReadOnlyList<DiffBlock> appliedBlocks = session.LatestResult!.Blocks;

        var invalidOptions = new TextComparisonOptions(enableIntralineDiff: true, ignoredLinePattern: "[");
        await session.RecompareAsync(invalidOptions, CancellationToken.None);

        Assert.AreSame(appliedBlocks, session.LatestResult!.Blocks);
        Assert.IsTrue(session.Diagnostics.Any(d => d.Code == ComparisonProjector.InvalidRegexCode));
    }

    [TestMethod]
    public void OperationDiagnostics_UnexpectedFailure_KeepsExceptionInTechnicalDetailOnly()
    {
        Diagnostic diagnostic = OperationDiagnostics.UnexpectedFailure(new InvalidOperationException("inner detail"), "Save output");

        Assert.AreEqual(OperationDiagnostics.UnexpectedFailureCode, diagnostic.Code);
        Assert.AreEqual(OperationDiagnostics.UnexpectedFailureMessage, diagnostic.Message);
        Assert.IsTrue(diagnostic.TechnicalDetail!.Contains("inner detail", StringComparison.Ordinal));
        Assert.IsFalse(diagnostic.Message.Contains("inner detail", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DiagnosticRecovery_KnownCodes_ReturnActionableHints()
    {
        Assert.IsNotNull(DiagnosticRecovery.TryGetRecoveryHint(DocumentPersistenceService.SaveConflictCode));
        Assert.IsNotNull(DiagnosticRecovery.TryGetRecoveryHint(QuickFolderComparisonEngine.LeftRootMissingCode));
        Assert.IsNotNull(DiagnosticRecovery.TryGetRecoveryHint(TextSearchService.InvalidRegexCode));
        Assert.IsNotNull(DiagnosticRecovery.TryGetRecoveryHint(AppSettingsMutations.CorruptSettingsCode));
        Assert.IsNotNull(DiagnosticRecovery.TryGetRecoveryHint(OperationDiagnostics.CancelledCode));
    }

    [TestMethod]
    public void DiagnosticRecovery_CoversAllProductionCodes()
    {
        string[] productionCodes =
        [
            AppSettingsMutations.CorruptSettingsCode,
            OperationDiagnostics.CancelledCode,
            OperationDiagnostics.UnexpectedFailureCode,
            TwoWayComparisonSession.MergePreconditionRejectedCode,
            OpenFileComparisonUseCase.NotTextCode,
            OpenFileComparisonUseCase.SamePathCode,
            ComparisonProjector.InvalidRegexCode,
            ComparisonProjector.OverlappingIgnoredBlocksCode,
            ComparisonProjector.UnclosedIgnoredBlockCode,
            OpenThreeWayFileComparisonUseCase.NotTextCode,
            OpenThreeWayFileComparisonUseCase.DuplicatePathCode,
            "MT-FMT-001",
            "MT-FMT-002",
            QuickFolderComparisonEngine.LeftRootMissingCode,
            QuickFolderComparisonEngine.RightRootMissingCode,
            QuickFolderComparisonEngine.LeftRootNotDirectoryCode,
            QuickFolderComparisonEngine.RightRootNotDirectoryCode,
            QuickFolderComparisonEngine.SameRootCode,
            QuickFolderComparisonEngine.BaseRootMissingCode,
            QuickFolderComparisonEngine.BaseRootNotDirectoryCode,
            QuickFolderComparisonEngine.DuplicateRootCode,
            "MT-FLD-006",
            "MT-FLD-010",
            "MT-FLD-011",
            "MT-FLD-012",
            "MT-FOL-010",
            "MT-FOL-020",
            "MT-FOL-021",
            "MT-FOL-022",
            "MT-FOL-023",
            "MT-FOL-024",
            "MT-FOL-025",
            "MT-FOL-026",
            DocumentPersistenceService.SaveConflictCode,
            DocumentPersistenceService.SaveFailureCode,
            DocumentPersistenceService.ReadOnlyDocumentCode,
            ThreeWayOutputPersistenceService.SaveFailureCode,
            "MT-IO-001",
            ManualTwoWayMergeService.ReadOnlyTargetCode,
            GoToLineService.OutOfRangeCode,
            GoToLineService.InvalidLineNumberCode,
            TextDecoder.HeuristicDecodingCode,
            TextDecoder.NotTextCode,
            TextSearchService.InvalidRegexCode,
            ThreeWayOutputMergeSession.StaleComparisonCode,
            ThreeWayOutputMergeSession.UnresolvedRegionsCode,
            ThreeWayOutputMergeSession.ManualEditInvalidatesCode,
            "MT-3WM-004",
        ];

        foreach (string code in productionCodes)
        {
            Assert.IsNotNull(DiagnosticRecovery.TryGetRecoveryHint(code), $"Missing recovery hint for {code}");
        }
    }
}
