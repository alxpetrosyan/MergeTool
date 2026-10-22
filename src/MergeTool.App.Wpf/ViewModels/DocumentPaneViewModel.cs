using MergeTool.Application.Sessions;
using MergeTool.Text;

namespace MergeTool.App.Wpf.ViewModels;

/// <summary>
/// Presents one document pane's metadata: path, encoding, line endings, and the
/// dirty/read-only indicators. Wraps the application-layer document session.
/// </summary>
public sealed class DocumentPaneViewModel : ViewModelBase
{
    public DocumentPaneViewModel(TextDocumentSession document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Document = document;
    }

    public TextDocumentSession Document { get; }

    public string FullPath => Document.Metadata.NormalizedFullPath;

    public string DisplayName => Document.Metadata.DisplayName;

    public string EncodingDisplay =>
        Document.Metadata.EncodingWebName.ToUpperInvariant()
        + (Document.Metadata.HasByteOrderMark ? " BOM" : string.Empty);

    public string LineEndingDisplay
    {
        get
        {
            string dominant = Document.Metadata.DominantLineEnding switch
            {
                LineEndingStyle.Lf => "LF",
                LineEndingStyle.Cr => "CR",
                _ => "CRLF",
            };
            return Document.Metadata.HasMixedLineEndings ? $"Mixed ({dominant})" : dominant;
        }
    }

    public bool IsDirty => Document.IsDirty;

    public bool IsReadOnly => Document.IsReadOnly;

    /// <summary>"Modified"/"Read-only" badge text; empty when neither applies.</summary>
    public string StatusBadge => (IsDirty, IsReadOnly) switch
    {
        (true, _) => "Modified",
        (false, true) => "Read-only",
        _ => string.Empty,
    };

    /// <summary>Re-raises the mutable indicators after an edit or recompare.</summary>
    public void Refresh()
    {
        RaisePropertyChanged(nameof(IsDirty));
        RaisePropertyChanged(nameof(IsReadOnly));
        RaisePropertyChanged(nameof(StatusBadge));
    }
}
