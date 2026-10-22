using MergeTool.Core;
using MergeTool.Text;
using System.Text.Json;

namespace MergeTool.Text.Formatted;

public enum StructuredFormatKind
{
    None = 0,
    Json = 1,
    Xml = 2,
}

public enum FormattedProjectionMode
{
    Raw = 0,
    Formatted = 1,
}

public sealed record FormattedProjectionResult(
    FormattedProjectionMode Mode,
    IReadOnlyList<string>? FormattedLeftLines,
    IReadOnlyList<string>? FormattedRightLines,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public bool IsSuccess => Mode == FormattedProjectionMode.Formatted;
}

public static class StructuredTextProjection
{
    public static FormattedProjectionResult TryProject(
        string leftText,
        string rightText,
        StructuredFormatKind kind)
        => kind switch
        {
            StructuredFormatKind.Json => TryFormatJson(leftText, rightText),
            StructuredFormatKind.Xml => TryFormatXml(leftText, rightText),
            _ => new FormattedProjectionResult(FormattedProjectionMode.Raw, null, null, []),
        };

    public static FormattedProjectionResult TryFormatJson(string leftText, string rightText)
    {
        try
        {
            using JsonDocument left = JsonDocument.Parse(leftText);
            using JsonDocument right = JsonDocument.Parse(rightText);
            return new FormattedProjectionResult(
                FormattedProjectionMode.Formatted,
                SplitFormattedLines(FormatJson(left)),
                SplitFormattedLines(FormatJson(right)),
                []);
        }
        catch (Exception exception)
        {
            return RawWithDiagnostic(
                "MT-FMT-001",
                "JSON could not be parsed for formatted comparison.",
                exception);
        }
    }

    public static FormattedProjectionResult TryFormatXml(string leftText, string rightText)
    {
        try
        {
            string left = System.Xml.Linq.XDocument.Parse(leftText).ToString();
            string right = System.Xml.Linq.XDocument.Parse(rightText).ToString();
            return new FormattedProjectionResult(
                FormattedProjectionMode.Formatted,
                SplitFormattedLines(left),
                SplitFormattedLines(right),
                []);
        }
        catch (Exception exception)
        {
            return RawWithDiagnostic(
                "MT-FMT-002",
                "XML could not be parsed for formatted comparison.",
                exception);
        }
    }

    public static IReadOnlyList<TextLine> ToTextLines(IReadOnlyList<string> lines)
    {
        List<TextLine> result = [];
        for (int i = 0; i < lines.Count; i++)
        {
            result.Add(new TextLine(i, lines[i], LineTerminator.Lf));
        }

        return result;
    }

    public static string JoinLines(IReadOnlyList<TextLine> lines)
    {
        if (lines.Count == 0)
        {
            return string.Empty;
        }

        return string.Join('\n', lines.Select(line => line.Content));
    }

    private static string FormatJson(JsonDocument document)
        => JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });

    private static IReadOnlyList<string> SplitFormattedLines(string formatted)
        => TextLineSplitter.Split(formatted).Lines.Select(line => line.Content).ToArray();

    private static FormattedProjectionResult RawWithDiagnostic(string code, string message, Exception exception)
        => new(
            FormattedProjectionMode.Raw,
            null,
            null,
            [Diagnostic.Warning(code, message, technicalDetail: exception.Message)]);
}
