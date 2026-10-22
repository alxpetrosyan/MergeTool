using MergeTool.Core;

namespace MergeTool.Text;

/// <summary>
/// Loads a text document snapshot from a file. IO comes through <see cref="IRawFileReader"/>
/// so this layer stays free of direct file-system calls. Expected user-data errors (missing
/// file, access denied, sharing violation) become failed results with safe diagnostics;
/// cancellation propagates as <see cref="OperationCanceledException"/> for the application
/// layer to convert.
/// </summary>
public sealed class TextDocumentLoader
{
    public const string ReadFailureCode = "MT-IO-001";

    private readonly IRawFileReader _reader;

    public TextDocumentLoader(IRawFileReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _reader = reader;
    }

    public async Task<OperationResult<TextDocumentSnapshot>> LoadAsync(string path, CancellationToken cancellationToken)
    {
        RawFileContent raw;
        try
        {
            raw = await _reader.ReadAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return OperationResult<TextDocumentSnapshot>.Failed(
                Diagnostic.Error(
                    ReadFailureCode,
                    "The file could not be read.",
                    technicalDetail: exception.Message,
                    path: path));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await Task.Run(
            () => BuildSnapshot(raw, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private OperationResult<TextDocumentSnapshot> BuildSnapshot(RawFileContent raw, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TextDecodingResult decoding = TextDecoder.Decode(raw.Bytes);
        FileVersionToken version = FileVersionToken.FromBytes(raw.Bytes, raw.LastWriteTimeUtc);
        FileIdentity identity = new(raw.NormalizedFullPath, version);

        if (decoding.Classification == TextClassification.NotTextForThisProduct)
        {
            TextDocumentMetadata notTextMetadata = BuildMetadata(raw, decoding, TextLineSplitter.Split(string.Empty));
            TextDocumentSnapshot notTextSnapshot = new(notTextMetadata, identity, string.Empty, []);
            return OperationResult<TextDocumentSnapshot>.Completed(notTextSnapshot, decoding.Diagnostics);
        }

        TextLineAnalysis analysis = TextLineSplitter.Split(decoding.Text);
        TextDocumentMetadata metadata = BuildMetadata(raw, decoding, analysis);
        TextDocumentSnapshot snapshot = new(metadata, identity, decoding.Text, analysis.Lines);
        return OperationResult<TextDocumentSnapshot>.Completed(snapshot, decoding.Diagnostics);
    }

    private static TextDocumentMetadata BuildMetadata(RawFileContent raw, TextDecodingResult decoding, TextLineAnalysis analysis) => new()
    {
        NormalizedFullPath = raw.NormalizedFullPath,
        DisplayName = Path.GetFileName(raw.NormalizedFullPath),
        EncodingWebName = decoding.EncodingWebName,
        HasByteOrderMark = decoding.HasByteOrderMark,
        DominantLineEnding = analysis.DominantLineEnding,
        HasMixedLineEndings = analysis.HasMixedLineEndings,
        HasFinalNewline = analysis.HasFinalNewline,
        Length = raw.Bytes.LongLength,
        LastWriteTimeUtc = raw.LastWriteTimeUtc,
        IsReadOnly = raw.IsReadOnly,
        Classification = decoding.Classification,
        LoadDiagnostics = decoding.Diagnostics,
    };
}
