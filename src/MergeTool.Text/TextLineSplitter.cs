namespace MergeTool.Text;

/// <summary>Line records plus line-ending facts computed in one pass over the text.</summary>
public sealed record TextLineAnalysis(
    IReadOnlyList<TextLine> Lines,
    LineEndingStyle DominantLineEnding,
    bool HasMixedLineEndings,
    bool HasFinalNewline);

/// <summary>
/// Splits decoded text into immutable line records in a single scan, preserving terminator
/// facts. An empty document produces zero lines.
/// </summary>
public static class TextLineSplitter
{
    public static TextLineAnalysis Split(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        List<TextLine> lines = [];
        int crlfCount = 0, lfCount = 0, crCount = 0;
        int lineStart = 0;
        int index = 0;

        while (index < text.Length)
        {
            char c = text[index];
            if (c == '\r')
            {
                bool isCrLf = index + 1 < text.Length && text[index + 1] == '\n';
                LineTerminator terminator = isCrLf ? LineTerminator.CrLf : LineTerminator.Cr;
                lines.Add(new TextLine(lines.Count, text[lineStart..index], terminator));
                if (isCrLf)
                {
                    crlfCount++;
                    index += 2;
                }
                else
                {
                    crCount++;
                    index += 1;
                }

                lineStart = index;
            }
            else if (c == '\n')
            {
                lines.Add(new TextLine(lines.Count, text[lineStart..index], LineTerminator.Lf));
                lfCount++;
                index += 1;
                lineStart = index;
            }
            else
            {
                index += 1;
            }
        }

        bool hasFinalNewline = text.Length > 0 && lineStart == text.Length;
        if (!hasFinalNewline && text.Length > 0)
        {
            lines.Add(new TextLine(lines.Count, text[lineStart..], LineTerminator.None));
        }

        int distinctStyles = (crlfCount > 0 ? 1 : 0) + (lfCount > 0 ? 1 : 0) + (crCount > 0 ? 1 : 0);
        LineEndingStyle dominant = DetermineDominant(crlfCount, lfCount, crCount);

        return new TextLineAnalysis(lines, dominant, distinctStyles > 1, hasFinalNewline);
    }

    /// <summary>Most frequent style; ties resolve CRLF over LF over CR; CRLF when no line breaks exist.</summary>
    private static LineEndingStyle DetermineDominant(int crlfCount, int lfCount, int crCount)
    {
        if (lfCount > crlfCount && lfCount >= crCount)
        {
            return LineEndingStyle.Lf;
        }

        if (crCount > crlfCount && crCount > lfCount)
        {
            return LineEndingStyle.Cr;
        }

        return LineEndingStyle.CrLf;
    }
}
