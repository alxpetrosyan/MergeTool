using MergeTool.Core;
using MergeTool.Folders;

namespace MergeTool.Application.UseCases;

/// <summary>
/// Runs a quick recursive folder comparison on a background thread. Cancellation is converted
/// to a controlled result at this boundary.
/// </summary>
public sealed class RunFolderComparisonUseCase
{
    private readonly QuickFolderComparisonEngine _engine;

    public RunFolderComparisonUseCase(QuickFolderComparisonEngine? engine = null)
    {
        _engine = engine ?? new QuickFolderComparisonEngine();
    }

    public Task<OperationResult<FolderComparisonResult>> CompareAsync(
        FolderComparisonRequest request,
        IProgress<FolderComparisonProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.Run(
            () => _engine.Compare(request, progress, cancellationToken),
            cancellationToken);
    }
}
