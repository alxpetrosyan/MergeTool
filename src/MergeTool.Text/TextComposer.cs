using System.Text;

namespace MergeTool.Text;

/// <summary>
/// Rebuilds document text from line records. Used for save roundtrips and, later, merges.
/// </summary>
public static class TextComposer
{
    public static string Compose(IReadOnlyList<TextLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        StringBuilder builder = new();
        foreach (TextLine line in lines)
        {
            builder.Append(line.Content);
            builder.Append(GetTerminatorText(line.Terminator));
        }

        return builder.ToString();
    }

    public static string GetTerminatorText(LineTerminator terminator) => terminator switch
    {
        LineTerminator.None => string.Empty,
        LineTerminator.CrLf => "\r\n",
        LineTerminator.Lf => "\n",
        LineTerminator.Cr => "\r",
        _ => throw new ArgumentOutOfRangeException(nameof(terminator), terminator, "Unknown terminator."),
    };

    public static string GetLineEndingText(LineEndingStyle style) => style switch
    {
        LineEndingStyle.CrLf => "\r\n",
        LineEndingStyle.Lf => "\n",
        LineEndingStyle.Cr => "\r",
        _ => throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown line ending style."),
    };
}
