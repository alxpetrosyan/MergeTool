using MergeTool.Core;
using MergeTool.Text;

namespace MergeTool.Application.Sessions;

/// <summary>
/// One open document within the application: the loaded snapshot plus the current editable
/// text. <see cref="DocumentRevision"/> increments on every in-memory edit, reload, and save
/// replacement, which is the basis of the stale-comparison contract.
/// Sessions are single-threaded by convention (driven from the UI thread through the
/// application layer); background work only ever reads the immutable values captured from
/// them at request time.
/// </summary>
public sealed class TextDocumentSession
{
    private TextDocumentSession(TextDocumentSnapshot snapshot)
    {
        OriginalSnapshot = snapshot;
        Metadata = snapshot.Metadata;
        Identity = snapshot.Identity;
        CurrentText = snapshot.Text;
        CurrentLines = snapshot.Lines;
    }

    public static TextDocumentSession FromSnapshot(TextDocumentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new TextDocumentSession(snapshot);
    }

    public DocumentSessionId Id { get; } = DocumentSessionId.New();

    /// <summary>The snapshot from the most recent load or reload.</summary>
    public TextDocumentSnapshot OriginalSnapshot { get; private set; }

    public TextDocumentMetadata Metadata { get; private set; }

    /// <summary>Identity expected on disk; replaced after a successful save or reload.</summary>
    public FileIdentity Identity { get; private set; }

    public string CurrentText { get; private set; }

    public IReadOnlyList<TextLine> CurrentLines { get; private set; }

    /// <summary>Increments on every edit, reload, and save replacement. Starts at 0.</summary>
    public int DocumentRevision { get; private set; }

    public bool IsDirty { get; private set; }

    public bool IsReadOnly => Metadata.IsReadOnly;

    /// <summary>Applies an in-memory edit (replaces the whole text).</summary>
    public void ReplaceText(string newText)
    {
        ArgumentNullException.ThrowIfNull(newText);
        if (IsReadOnly)
        {
            throw new InvalidOperationException("The document is read-only and cannot be edited.");
        }

        if (string.Equals(newText, CurrentText, StringComparison.Ordinal))
        {
            return; // No content change; revision must not move.
        }

        CurrentText = newText;
        CurrentLines = TextLineSplitter.Split(newText).Lines;
        DocumentRevision++;
        IsDirty = true;
    }

    /// <summary>Records a successful save: new on-disk identity, clean state, new revision epoch.</summary>
    public void MarkSaved(FileIdentity newIdentity)
    {
        ArgumentNullException.ThrowIfNull(newIdentity);
        Identity = newIdentity;
        IsDirty = false;
        DocumentRevision++;
    }

    /// <summary>Replaces the session content from a fresh snapshot (reload).</summary>
    public void ReloadFrom(TextDocumentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        OriginalSnapshot = snapshot;
        Metadata = snapshot.Metadata;
        Identity = snapshot.Identity;
        CurrentText = snapshot.Text;
        CurrentLines = snapshot.Lines;
        IsDirty = false;
        DocumentRevision++;
    }
}
