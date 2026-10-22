using MergeTool.Core;

namespace MergeTool.Core.Tests;

[TestClass]
public sealed class DiagnosticTests
{
    [TestMethod]
    public void FactoryMethods_SetSeverityAndFields()
    {
        Diagnostic info = Diagnostic.Info("MT-TST-001", "Informational note");
        Diagnostic warning = Diagnostic.Warning("MT-TST-002", "Heuristic decoding used", technicalDetail: "fallback: windows-1252");
        Diagnostic error = Diagnostic.Error("MT-TST-003", "The file could not be read.", technicalDetail: "IOException: ...", path: @"C:\data\a.txt", side: ComparisonSide.Left);

        Assert.AreEqual(DiagnosticSeverity.Info, info.Severity);
        Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity);
        Assert.AreEqual(DiagnosticSeverity.Error, error.Severity);
        Assert.AreEqual("MT-TST-003", error.Code);
        Assert.AreEqual("The file could not be read.", error.Message);
        Assert.AreEqual("IOException: ...", error.TechnicalDetail);
        Assert.AreEqual(@"C:\data\a.txt", error.Path);
        Assert.AreEqual(ComparisonSide.Left, error.Side);
    }

    [TestMethod]
    public void UserMessage_IsSeparateFromTechnicalDetail()
    {
        Diagnostic diagnostic = Diagnostic.Error(
            "MT-IO-001",
            "The file could not be read.",
            technicalDetail: "System.IO.IOException: The process cannot access the file");

        Assert.IsFalse(diagnostic.Message.Contains("Exception", StringComparison.OrdinalIgnoreCase));
        Assert.IsNotNull(diagnostic.TechnicalDetail);
    }

    [TestMethod]
    public void Constructor_RejectsBlankCodeOrMessage()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = new Diagnostic(DiagnosticSeverity.Info, "", "message"));
        Assert.ThrowsExactly<ArgumentException>(() => _ = new Diagnostic(DiagnosticSeverity.Info, "MT-TST-004", " "));
    }

    [TestMethod]
    public void Diagnostics_AreValueEqual()
    {
        Diagnostic first = Diagnostic.Warning("MT-TST-005", "Same content");
        Diagnostic second = Diagnostic.Warning("MT-TST-005", "Same content");

        Assert.AreEqual(first, second);
    }
}
