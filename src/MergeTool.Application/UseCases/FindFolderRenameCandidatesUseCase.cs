using MergeTool.Core;
using MergeTool.Folders;

namespace MergeTool.Application.UseCases;

/// <summary>
/// Optional post-scan rename-candidate discovery on a background thread.
/// </summary>
public sealed class FindFolderRenameCandidatesUseCase
{
    private readonly FolderRenameCandidateFinder _finder;

    public FindFolderRenameCandidatesUseCase(FolderRenameCandidateFinder? finder = null)
    {
        _finder = finder ?? new FolderRenameCandidateFinder();
    }

    public Task<OperationResult<IReadOnlyList<FolderRenameCandidate>>> FindAsync(
        IReadOnlyList<FolderComparisonRow> rows,
        FolderRenameCandidateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);

        return Task.Run(
            () => _finder.Find(rows, options, cancellationToken),
            cancellationToken);
    }
}
