using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.Diff;

namespace MergeTool.Application.UseCases;

/// <summary>Opens three text files and runs the initial 3-way comparison.</summary>
public sealed class OpenThreeWayFileComparisonUseCase
{
    public const string NotTextCode = "MT-CMP-012";

    public const string DuplicatePathCode = "MT-CMP-013";

    public const string DuplicatePathMessage =
        "Base, left, and right must be three different files.";

    private readonly TextDocumentLoader _loader;
    private readonly ITextDiffEngine _diffEngine;

    public OpenThreeWayFileComparisonUseCase(IRawFileReader reader, ITextDiffEngine diffEngine)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(diffEngine);
        _loader = new TextDocumentLoader(reader);
        _diffEngine = diffEngine;
    }

    public async Task<OperationResult<ThreeWayComparisonSession>> OpenAsync(
        string basePath,
        string leftPath,
        string rightPath,
        TextComparisonOptions comparisonOptions,
        CancellationToken cancellationToken)
    {
        try
        {
            if (HasDuplicatePaths(basePath, leftPath, rightPath))
            {
                return OperationResult<ThreeWayComparisonSession>.Failed(
                    Diagnostic.Error(DuplicatePathCode, DuplicatePathMessage, path: basePath));
            }

            OperationResult<TextDocumentSnapshot> baseLoad =
                await _loader.LoadAsync(basePath, cancellationToken).ConfigureAwait(false);
            if (!baseLoad.IsCompleted)
            {
                return OperationResult<ThreeWayComparisonSession>.Failed(baseLoad.Diagnostics);
            }

            OperationResult<TextDocumentSnapshot> leftLoad =
                await _loader.LoadAsync(leftPath, cancellationToken).ConfigureAwait(false);
            if (!leftLoad.IsCompleted)
            {
                return OperationResult<ThreeWayComparisonSession>.Failed(
                    WithSide(leftLoad.Diagnostics, ComparisonSide.Left));
            }

            OperationResult<TextDocumentSnapshot> rightLoad =
                await _loader.LoadAsync(rightPath, cancellationToken).ConfigureAwait(false);
            if (!rightLoad.IsCompleted)
            {
                return OperationResult<ThreeWayComparisonSession>.Failed(
                    WithSide(rightLoad.Diagnostics, ComparisonSide.Right));
            }

            List<Diagnostic> diagnostics =
            [
                .. baseLoad.Value.Metadata.LoadDiagnostics,
                .. WithSide(leftLoad.Value.Metadata.LoadDiagnostics, ComparisonSide.Left),
                .. WithSide(rightLoad.Value.Metadata.LoadDiagnostics, ComparisonSide.Right),
            ];

            if (HasNotText(baseLoad.Value) || HasNotText(leftLoad.Value) || HasNotText(rightLoad.Value))
            {
                diagnostics.Add(Diagnostic.Error(
                    NotTextCode,
                    "One of the selected files is not text and cannot be compared by this product."));
                return OperationResult<ThreeWayComparisonSession>.Failed(diagnostics);
            }

            ThreeWayComparisonSession session = new(
                TextDocumentSession.FromSnapshot(baseLoad.Value),
                TextDocumentSession.FromSnapshot(leftLoad.Value),
                TextDocumentSession.FromSnapshot(rightLoad.Value),
                _diffEngine);

            OperationStatus status = await session.RecompareAsync(comparisonOptions, cancellationToken)
                .ConfigureAwait(false);
            return status switch
            {
                OperationStatus.Completed => OperationResult<ThreeWayComparisonSession>.Completed(session, diagnostics),
                OperationStatus.Cancelled => OperationResult<ThreeWayComparisonSession>.Cancelled(diagnostics),
                OperationStatus.Failed => OperationResult<ThreeWayComparisonSession>.Failed(
                    session.Diagnostics.Count > 0 ? session.Diagnostics : diagnostics),
                _ => OperationResult<ThreeWayComparisonSession>.Failed(diagnostics),
            };
        }
        catch (OperationCanceledException)
        {
            return OperationResult<ThreeWayComparisonSession>.Cancelled();
        }
    }

    private static bool HasNotText(TextDocumentSnapshot snapshot)
        => snapshot.Metadata.Classification == TextClassification.NotTextForThisProduct;

    private static Diagnostic[] WithSide(IReadOnlyList<Diagnostic> diagnostics, ComparisonSide side)
        => [.. diagnostics.Select(d => d.WithSide(side))];

    private static bool HasDuplicatePaths(string basePath, string leftPath, string rightPath)
    {
        try
        {
            string normalizedBase = PathNormalization.NormalizeFullPath(basePath);
            string normalizedLeft = PathNormalization.NormalizeFullPath(leftPath);
            string normalizedRight = PathNormalization.NormalizeFullPath(rightPath);
            return string.Equals(normalizedBase, normalizedLeft, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedBase, normalizedRight, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
