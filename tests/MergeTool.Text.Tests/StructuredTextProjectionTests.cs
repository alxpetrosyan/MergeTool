using MergeTool.Text.Formatted;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class StructuredTextProjectionTests
{
    [TestMethod]
    public void TryFormatJson_ProducesIndentedMultilineOutput()
    {
        const string compact = "{\"name\":\"MergeTool\",\"enabled\":true}";

        FormattedProjectionResult result = StructuredTextProjection.TryFormatJson(compact, compact);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.FormattedLeftLines);
        Assert.IsGreaterThan(1, result.FormattedLeftLines!.Count);
        Assert.Contains("MergeTool", string.Join('\n', result.FormattedLeftLines));
    }

    [TestMethod]
    public void TryFormatXml_ProducesIndentedMultilineOutput()
    {
        const string compact = "<root><item id=\"1\">A</item></root>";

        FormattedProjectionResult result = StructuredTextProjection.TryFormatXml(compact, compact);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.FormattedLeftLines);
        Assert.IsGreaterThan(1, result.FormattedLeftLines!.Count);
        Assert.Contains("<item", string.Join('\n', result.FormattedLeftLines));
    }

    [TestMethod]
    public void TryFormatJson_InvalidInput_FallsBackWithDiagnostic()
    {
        FormattedProjectionResult result = StructuredTextProjection.TryFormatJson("{not-json", "{not-json");

        Assert.IsFalse(result.IsSuccess);
        Assert.IsNull(result.FormattedLeftLines);
        Assert.HasCount(1, result.Diagnostics);
        Assert.AreEqual("MT-FMT-001", result.Diagnostics[0].Code);
    }

    [TestMethod]
    public void TryFormatXml_InvalidInput_FallsBackWithDiagnostic()
    {
        FormattedProjectionResult result = StructuredTextProjection.TryFormatXml("<root>", "<root>");

        Assert.IsFalse(result.IsSuccess);
        Assert.HasCount(1, result.Diagnostics);
        Assert.AreEqual("MT-FMT-002", result.Diagnostics[0].Code);
    }

    [TestMethod]
    public void ToTextLines_AndJoinLines_RoundTrip()
    {
        string[] lines = ["{", "  \"a\": 1", "}"];

        var textLines = StructuredTextProjection.ToTextLines(lines);
        string joined = StructuredTextProjection.JoinLines(textLines);

        Assert.AreEqual(string.Join('\n', lines), joined);
    }
}
