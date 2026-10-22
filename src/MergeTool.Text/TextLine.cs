namespace MergeTool.Text;

/// <summary>
/// Terminator that originally followed a line. <see cref="None"/> appears only on the last
/// line of a document that has no final newline.
/// </summary>
public enum LineTerminator
{
    None = 0,
    CrLf = 1,
    Lf = 2,
    Cr = 3,
}

/// <summary>
/// One immutable line of a loaded document. <paramref name="Index"/> is zero-based;
/// <paramref name="Content"/> excludes the terminator.
/// </summary>
public sealed record TextLine(int Index, string Content, LineTerminator Terminator);
