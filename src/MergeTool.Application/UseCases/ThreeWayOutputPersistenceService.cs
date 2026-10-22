using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.FileSystem;
using MergeTool.Text;

namespace MergeTool.Application.UseCases;

/// <summary>Saves 3-way merge output to a user-selected path without touching source files.</summary>
public sealed class ThreeWayOutputPersistenceService
{
    public const string SaveFailureCode = "MT-IO-005";

    public const string UnresolvedRegionsMessage =
        "Resolve all difference regions before saving the output file.";

    private readonly SafeTextFileWriter _writer = new();
    private readonly TextDocumentLoader _loader;

    public ThreeWayOutputPersistenceService(IRawFileReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _loader = new TextDocumentLoader(reader);
    }

    public async Task<OperationResult> SaveOutputAsync(
        ThreeWayComparisonSession comparison,
        ThreeWayOutputMergeSession outputMerge,
        string targetPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        ArgumentNullException.ThrowIfNull(outputMerge);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        if (outputMerge.Output is null || comparison.LatestResult is null)
        {
            return OperationResult.Failed(Diagnostic.Error(
                ThreeWayOutputMergeSession.StaleComparisonCode,
                ThreeWayComparisonSession.StaleComparisonMessage));
        }

        IReadOnlyList<int> unresolved = outputMerge.UnresolvedDifferenceRegionOrdinals(comparison.LatestResult);
        if (unresolved.Count > 0)
        {
            return OperationResult.Failed(Diagnostic.Error(
                ThreeWayOutputMergeSession.UnresolvedRegionsCode,
                UnresolvedRegionsMessage));
        }

        string normalizedPath = PathNormalization.NormalizeFullPath(targetPath);
        TextWritePolicy policy = comparison.Base.Metadata.ToWritePolicy();
        TextDocumentSession output = outputMerge.Output;

        try
        {
            SaveResult result;
            if (File.Exists(normalizedPath))
            {
                FileIdentity expectedIdentity = outputMerge.OutputSavePath is not null
                    && string.Equals(outputMerge.OutputSavePath, normalizedPath, StringComparison.OrdinalIgnoreCase)
                    ? output.Identity
                    : await LoadIdentityAsync(normalizedPath, cancellationToken).ConfigureAwait(false);
                result = await _writer.SaveAsync(expectedIdentity, output.CurrentText, policy, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                result = await _writer.SaveNewFileAsync(normalizedPath, output.CurrentText, policy, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (result.Status == SaveStatus.ExternalModificationConflict)
            {
                return OperationResult.Failed(Diagnostic.Error(
                    DocumentPersistenceService.SaveConflictCode,
                    DocumentPersistenceService.SaveConflictMessage,
                    path: normalizedPath));
            }

            output.MarkSaved(result.NewIdentity!);
            outputMerge.SetOutputSavePath(normalizedPath);
            return OperationResult.Completed();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return OperationResult.Failed(Diagnostic.Error(
                SaveFailureCode,
                "The output file could not be saved.",
                technicalDetail: exception.Message,
                path: normalizedPath));
        }
    }

    private async Task<FileIdentity> LoadIdentityAsync(string path, CancellationToken cancellationToken)
    {
        OperationResult<TextDocumentSnapshot> load = await _loader.LoadAsync(path, cancellationToken).ConfigureAwait(false);
        if (!load.IsCompleted)
        {
            throw new IOException("The target file could not be read before saving.");
        }

        return load.Value.Identity;
    }
}
