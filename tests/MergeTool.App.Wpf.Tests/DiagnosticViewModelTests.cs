using MergeTool.App.Wpf.ViewModels;
using MergeTool.Application.UseCases;
using MergeTool.Core;

namespace MergeTool.App.Wpf.Tests;

[TestClass]
public sealed class DiagnosticViewModelTests
{
    [TestMethod]
    public void RecoveryHint_IsPopulatedForKnownCode()
    {
        Diagnostic diagnostic = Diagnostic.Error(
            DocumentPersistenceService.SaveConflictCode,
            DocumentPersistenceService.SaveConflictMessage);

        DiagnosticViewModel viewModel = new(diagnostic);

        Assert.IsTrue(viewModel.HasRecoveryHint);
        Assert.IsNotNull(viewModel.RecoveryHint);
    }

    [TestMethod]
    public void CopyPayload_UsesDiagnosticFormatter()
    {
        Diagnostic diagnostic = Diagnostic.Error(
            "MT-CMP-004",
            "The comparison pattern is invalid.",
            technicalDetail: "Unterminated [");

        DiagnosticViewModel viewModel = new(diagnostic);

        Assert.AreEqual(diagnostic.ToCopyPayload(), viewModel.Diagnostic.ToCopyPayload());
        Assert.IsTrue(viewModel.CopyDetailsCommand.CanExecute(null));
    }
}
