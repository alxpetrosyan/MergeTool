using MergeTool.Core.Diagnostics;

namespace MergeTool.Core.Tests;

[TestClass]
public sealed class DiagnosticRecoveryTests
{
    [TestMethod]
    public void TryGetRecoveryHint_KnownCode_ReturnsHint()
    {
        string? hint = DiagnosticRecovery.TryGetRecoveryHint("MT-IO-002");

        Assert.IsNotNull(hint);
        Assert.IsTrue(hint.Contains("Reload", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TryGetRecoveryHint_UnknownCode_ReturnsNull()
    {
        Assert.IsNull(DiagnosticRecovery.TryGetRecoveryHint("MT-TST-999"));
    }

    [TestMethod]
    public void ToCopyPayload_IncludesCodeMessagePathAndTechnicalDetail()
    {
        Diagnostic diagnostic = Diagnostic.Error(
            "MT-IO-002",
            "The file changed on disk.",
            technicalDetail: "length mismatch",
            path: @"C:\work\a.txt");

        string payload = diagnostic.ToCopyPayload();

        StringAssert.Contains(payload, "MT-IO-002");
        StringAssert.Contains(payload, "The file changed on disk.");
        StringAssert.Contains(payload, @"C:\work\a.txt");
        StringAssert.Contains(payload, "length mismatch");
    }

    [TestMethod]
    public void OperationDiagnostics_Cancelled_IsUserSafe()
    {
        Diagnostic diagnostic = OperationDiagnostics.OperationCancelled();

        Assert.AreEqual(OperationDiagnostics.CancelledCode, diagnostic.Code);
        Assert.AreEqual(OperationDiagnostics.CancelledMessage, diagnostic.Message);
        Assert.IsNull(diagnostic.TechnicalDetail);
        Assert.IsNotNull(DiagnosticRecovery.TryGetRecoveryHint(diagnostic.Code));
    }
}
