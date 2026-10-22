namespace MergeTool.Folders;

/// <summary>Bounded limits for optional rename-candidate discovery.</summary>
public sealed record FolderRenameCandidateOptions
{
    public static FolderRenameCandidateOptions Default { get; } = new();

    public FolderRenameCandidateOptions(
        int maxCandidates = 20,
        int maxRowsPerSide = 100,
        long maxFileBytesForHash = 10 * 1024 * 1024,
        int maxNameEditDistance = 2)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCandidates);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRowsPerSide);
        ArgumentOutOfRangeException.ThrowIfNegative(maxFileBytesForHash);
        ArgumentOutOfRangeException.ThrowIfNegative(maxNameEditDistance);

        MaxCandidates = maxCandidates;
        MaxRowsPerSide = maxRowsPerSide;
        MaxFileBytesForHash = maxFileBytesForHash;
        MaxNameEditDistance = maxNameEditDistance;
    }

    public int MaxCandidates { get; }

    public int MaxRowsPerSide { get; }

    public long MaxFileBytesForHash { get; }

    public int MaxNameEditDistance { get; }
}
