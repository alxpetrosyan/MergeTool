using MergeTool.Core;

namespace MergeTool.Text;

/// <summary>
/// An immutable snapshot of a text document as loaded from disk: metadata, identity token,
/// decoded text, and per-line records. Editing sessions (later tasks) derive from snapshots;
/// a snapshot itself never changes.
/// </summary>
public sealed record TextDocumentSnapshot
{
    public TextDocumentSnapshot(
        TextDocumentMetadata metadata,
        FileIdentity identity,
        string text,
        IReadOnlyList<TextLine> lines)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(lines);

        Metadata = metadata;
        Identity = identity;
        Text = text;
        Lines = lines;
    }

    public TextDocumentMetadata Metadata { get; }

    /// <summary>Identity captured from the loaded bytes; used to detect external changes.</summary>
    public FileIdentity Identity { get; }

    /// <summary>The full decoded text, terminators included.</summary>
    public string Text { get; }

    /// <summary>Zero-based immutable line records (content and terminator facts).</summary>
    public IReadOnlyList<TextLine> Lines { get; }
}
