namespace MergeTool.Core;

/// <summary>
/// Instructions for encoding text when writing a file: which encoding to use and whether to
/// emit a byte order mark. Produced by the Text layer from loaded document metadata so saves
/// preserve the original encoding and BOM by default.
/// </summary>
/// <param name="EncodingWebName">IANA/web name, e.g. "utf-8", "utf-16", "utf-16BE", "windows-1252".</param>
/// <param name="IncludeByteOrderMark">Whether the output must start with a BOM.</param>
public sealed record TextWritePolicy(string EncodingWebName, bool IncludeByteOrderMark)
{
    public static TextWritePolicy Utf8NoBom { get; } = new("utf-8", false);
}
