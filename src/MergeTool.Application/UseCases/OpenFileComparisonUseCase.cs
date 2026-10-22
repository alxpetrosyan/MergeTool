using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.Diff;

namespace MergeTool.Application.UseCases;

/// <summary>
/// Opens two text files as document sessions, creates a comparison session, and runs the
/// initial comparison. Cancellation is converted to a controlled result at this boundary.
/// </summary>
public sealed class OpenFileComparisonUseCase
{
    public const string NotTextCode = "MT-CMP-002";

    public const string SamePathCode = "MT-CMP-003";

    public const string SamePathMessage = "The same file cannot be compared with itself. Choose two different files.";

    private readonly TextDocumentLoader _loader;
    private readonly ITextDiffEngine _diffEngine;

    public OpenFileComparisonUseCase(IRawFileReader reader, ITextDiffEngine diffEngine)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(diffEngine);
        _loader = new TextDocumentLoader(reader);
        _diffEngine = diffEngine;
    }

    public async Task<OperationResult<TwoWayComparisonSession>> OpenAsync(
        string leftPath,
        string rightPath,
        TextComparisonOptions comparisonOptions,
        CancellationToken cancellationToken)
    {
        try
        {
            if (IsSameNormalizedPath(leftPath, rightPath))
            {
                return OperationResult<TwoWayComparisonSession>.Failed(
                    Diagnostic.Error(SamePathCode, SamePathMessage, path: leftPath));
            }

            OperationResult<TextDocumentSnapshot> left = await _loader.LoadAsync(leftPath, cancellationToken).ConfigureAwait(false);
            if (!left.IsCompleted)
            {
                return OperationResult<TwoWayComparisonSession>.Failed(WithSide(left.Diagnostics, ComparisonSide.Left));
            }

            OperationResult<TextDocumentSnapshot> right = await _loader.LoadAsync(rightPath, cancellationToken).ConfigureAwait(false);
            if (!right.IsCompleted)
            {
                return OperationResult<TwoWayComparisonSession>.Failed(WithSide(right.Diagnostics, ComparisonSide.Right));
            }

            List<Diagnostic> diagnostics =
            [
                .. WithSide(left.Value.Metadata.LoadDiagnostics, ComparisonSide.Left),
                .. WithSide(right.Value.Metadata.LoadDiagnostics, ComparisonSide.Right),
            ];

            if (left.Value.Metadata.Classification == TextClassification.NotTextForThisProduct
                || right.Value.Metadata.Classification == TextClassification.NotTextForThisProduct)
            {
                diagnostics.Add(Diagnostic.Error(
                    NotTextCode,
                    "One of the selected files is not text and cannot be compared by this product."));
                return OperationResult<TwoWayComparisonSession>.Failed(diagnostics);
            }

            TwoWayComparisonSession session = new(
                TextDocumentSession.FromSnapshot(left.Value),
                TextDocumentSession.FromSnapshot(right.Value),
                _diffEngine);

            OperationStatus status = await session.RecompareAsync(comparisonOptions, cancellationToken).ConfigureAwait(false);
            return status switch
            {
                OperationStatus.Completed => OperationResult<TwoWayComparisonSession>.Completed(session, diagnostics),
                OperationStatus.Cancelled => OperationResult<TwoWayComparisonSession>.Cancelled(diagnostics),
                OperationStatus.Failed => OperationResult<TwoWayComparisonSession>.Failed(
                    session.Diagnostics.Count > 0 ? session.Diagnostics : diagnostics),
                _ => OperationResult<TwoWayComparisonSession>.Failed(diagnostics),
            };
        }
        catch (OperationCanceledException)
        {
            return OperationResult<TwoWayComparisonSession>.Cancelled();
        }
    }

    private static Diagnostic[] WithSide(IReadOnlyList<Diagnostic> diagnostics, ComparisonSide side)
        => [.. diagnostics.Select(d => d.WithSide(side))];

    /// <summary>
    /// True when both inputs normalize to the same file. Paths that cannot be normalized are
    /// not rejected here; loading them produces the regular read-failure diagnostic instead.
    /// </summary>
    private static bool IsSameNormalizedPath(string leftPath, string rightPath)
    {
        try
        {
            return string.Equals(
                PathNormalization.NormalizeFullPath(leftPath),
                PathNormalization.NormalizeFullPath(rightPath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
