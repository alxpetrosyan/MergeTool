using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.FileSystem;
using MergeTool.Text;

namespace MergeTool.Application.UseCases;

/// <summary>
/// Saves and reloads document sessions. Saving goes through <see cref="SafeTextFileWriter"/>
/// (external-modification check + atomic replace) and preserves the document's original
/// encoding and BOM. A conflict never overwrites; the caller offers Cancel/Reload only.
/// </summary>
public sealed class DocumentPersistenceService
{
    public const string SaveConflictCode = "MT-IO-002";

    public const string SaveConflictMessage =
        "The file was changed outside this application. Nothing was overwritten.";

    public const string SaveFailureCode = "MT-IO-003";

    public const string ReadOnlyDocumentCode = "MT-IO-004";

    private readonly SafeTextFileWriter _writer = new();
    private readonly TextDocumentLoader _loader;

    public DocumentPersistenceService(IRawFileReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _loader = new TextDocumentLoader(reader);
    }

    /// <summary>
    /// Saves the session's current text to its file. Preserves the loaded encoding, BOM,
    /// and the text exactly as edited (no line-ending conversion on save).
    /// </summary>
    public async Task<OperationResult> SaveAsync(TextDocumentSession document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.IsReadOnly)
        {
            return OperationResult.Failed(Diagnostic.Error(
                ReadOnlyDocumentCode,
                "The document is read-only and cannot be saved.",
                path: document.Metadata.NormalizedFullPath));
        }

        if (!document.IsDirty)
        {
            return OperationResult.Completed();
        }

        TextWritePolicy policy = new(document.Metadata.EncodingWebName, document.Metadata.HasByteOrderMark);
        SaveResult result;
        try
        {
            result = await _writer.SaveAsync(document.Identity, document.CurrentText, policy, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return OperationResult.Failed(Diagnostic.Error(
                SaveFailureCode,
                "The file could not be saved.",
                technicalDetail: exception.Message,
                path: document.Metadata.NormalizedFullPath));
        }

        if (result.Status == SaveStatus.ExternalModificationConflict)
        {
            return OperationResult.Failed(Diagnostic.Error(
                SaveConflictCode,
                SaveConflictMessage,
                path: document.Metadata.NormalizedFullPath));
        }

        document.MarkSaved(result.NewIdentity!);
        return OperationResult.Completed();
    }

    /// <summary>Reloads the session content from disk, discarding in-memory edits.</summary>
    public async Task<OperationResult> ReloadAsync(TextDocumentSession document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        OperationResult<TextDocumentSnapshot> load =
            await _loader.LoadAsync(document.Metadata.NormalizedFullPath, cancellationToken).ConfigureAwait(false);
        if (!load.IsCompleted)
        {
            return OperationResult.Failed(load.Diagnostics);
        }

        if (load.Value.Metadata.Classification == TextClassification.NotTextForThisProduct)
        {
            return OperationResult.Failed(Diagnostic.Error(
                OpenFileComparisonUseCase.NotTextCode,
                "The file on disk is no longer text and cannot be reloaded by this product.",
                path: document.Metadata.NormalizedFullPath));
        }

        document.ReloadFrom(load.Value);
        return OperationResult.Completed(load.Diagnostics);
    }
}
