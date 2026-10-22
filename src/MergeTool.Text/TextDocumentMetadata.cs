using MergeTool.Core;

namespace MergeTool.Text;

/// <summary>Whether the product can treat the file as text.</summary>
public enum TextClassification
{
    Text = 0,
    NotTextForThisProduct = 1,
}

/// <summary>Line-ending style used when a single style must be chosen (writes, merges).</summary>
public enum LineEndingStyle
{
    CrLf = 0,
    Lf = 1,
    Cr = 2,
}

/// <summary>
/// Facts about a loaded text document needed for faithful comparison and preservation on save.
/// </summary>
public sealed record TextDocumentMetadata
{
    public required string NormalizedFullPath { get; init; }

    public required string DisplayName { get; init; }

    /// <summary>Encoding name from <see cref="TextEncodings"/> ("utf-8", "utf-16", "utf-16BE", "windows-1252").</summary>
    public required string EncodingWebName { get; init; }

    /// <summary>Whether the original file started with a byte order mark.</summary>
    public required bool HasByteOrderMark { get; init; }

    /// <summary>
    /// Most frequent line ending in the document. CRLF when the document has no line breaks
    /// (documented Windows default); ties resolve CRLF over LF over CR.
    /// </summary>
    public required LineEndingStyle DominantLineEnding { get; init; }

    public required bool HasMixedLineEndings { get; init; }

    /// <summary>True when the document's last line ends with a terminator.</summary>
    public required bool HasFinalNewline { get; init; }

    /// <summary>Original file length in bytes.</summary>
    public required long Length { get; init; }

    public required DateTime LastWriteTimeUtc { get; init; }

    public required bool IsReadOnly { get; init; }

    public required TextClassification Classification { get; init; }

    public required IReadOnlyList<Diagnostic> LoadDiagnostics { get; init; }

    /// <summary>The write policy that preserves this document's encoding and BOM.</summary>
    public TextWritePolicy ToWritePolicy() => new(EncodingWebName, HasByteOrderMark);
}
