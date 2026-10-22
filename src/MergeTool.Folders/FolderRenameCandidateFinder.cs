using MergeTool.Core;

namespace MergeTool.Folders;

/// <summary>
/// Finds rename candidates between one-sided folder rows using bounded hash and name similarity.
/// Never mutates scan results or performs file operations.
/// </summary>
public sealed class FolderRenameCandidateFinder
{
    private readonly IFileContentHashAccess _hashAccess;

    public FolderRenameCandidateFinder(IFileContentHashAccess? hashAccess = null)
    {
        _hashAccess = hashAccess ?? new PhysicalFileContentHashAccess();
    }

    public OperationResult<IReadOnlyList<FolderRenameCandidate>> Find(
        IReadOnlyList<FolderComparisonRow> rows,
        FolderRenameCandidateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        FolderRenameCandidateOptions limits = options ?? FolderRenameCandidateOptions.Default;

        List<FolderComparisonRow> leftOnly = rows
            .Where(row => row.State == FolderRowState.LeftOnly && !row.IsDirectory && row.LeftFullPath is not null)
            .Take(limits.MaxRowsPerSide)
            .ToList();
        List<FolderComparisonRow> rightOnly = rows
            .Where(row => row.State == FolderRowState.RightOnly && !row.IsDirectory && row.RightFullPath is not null)
            .Take(limits.MaxRowsPerSide)
            .ToList();

        var candidates = new List<FolderRenameCandidate>();
        var usedRight = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hashCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (FolderComparisonRow left in leftOnly)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (candidates.Count >= limits.MaxCandidates)
            {
                break;
            }

            FolderRenameCandidate? best = null;
            foreach (FolderComparisonRow right in rightOnly)
            {
                if (usedRight.Contains(right.RelativePath))
                {
                    continue;
                }

                FolderRenameCandidate? match = TryMatch(left, right, limits, hashCache, cancellationToken);
                if (match is null)
                {
                    continue;
                }

                if (best is null || Rank(match.MatchKind) > Rank(best.MatchKind))
                {
                    best = match;
                }

                if (best.MatchKind == FolderRenameCandidateMatchKind.ContentHash)
                {
                    break;
                }
            }

            if (best is not null)
            {
                candidates.Add(best);
                usedRight.Add(best.RightRelativePath);
            }
        }

        return OperationResult<IReadOnlyList<FolderRenameCandidate>>.Completed(candidates);
    }

    private FolderRenameCandidate? TryMatch(
        FolderComparisonRow left,
        FolderComparisonRow right,
        FolderRenameCandidateOptions limits,
        Dictionary<string, string> hashCache,
        CancellationToken cancellationToken)
    {
        string leftPath = left.LeftFullPath!;
        string rightPath = right.RightFullPath!;

        if (left.LeftSizeBytes == right.RightSizeBytes
            && left.LeftSizeBytes is > 0 and var size
            && size <= limits.MaxFileBytesForHash
            && TryGetHash(leftPath, size, left.LeftLastWriteTimeUtc, hashCache, cancellationToken, out string? leftHash)
            && TryGetHash(rightPath, size, right.RightLastWriteTimeUtc, hashCache, cancellationToken, out string? rightHash)
            && string.Equals(leftHash, rightHash, StringComparison.OrdinalIgnoreCase))
        {
            return new FolderRenameCandidate(
                left.RelativePath,
                right.RelativePath,
                leftPath,
                rightPath,
                FolderRenameCandidateMatchKind.ContentHash);
        }

        if (AreNamesSimilar(leftPath, rightPath, limits.MaxNameEditDistance))
        {
            return new FolderRenameCandidate(
                left.RelativePath,
                right.RelativePath,
                leftPath,
                rightPath,
                FolderRenameCandidateMatchKind.NameSimilarity);
        }

        return null;
    }

    private bool TryGetHash(
        string filePath,
        long sizeBytes,
        DateTime? lastWriteUtc,
        Dictionary<string, string> cache,
        CancellationToken cancellationToken,
        out string? sha256Hex)
    {
        if (cache.TryGetValue(filePath, out string? cached))
        {
            sha256Hex = cached;
            return true;
        }

        if (!_hashAccess.TryComputeSha256(filePath, cancellationToken, out string computed, out _))
        {
            sha256Hex = null;
            return false;
        }

        cache[filePath] = computed;
        sha256Hex = computed;
        return true;
    }

    private static int Rank(FolderRenameCandidateMatchKind kind) => kind switch
    {
        FolderRenameCandidateMatchKind.ContentHash => 2,
        FolderRenameCandidateMatchKind.NameSimilarity => 1,
        _ => 0,
    };

    internal static bool AreNamesSimilar(string leftPath, string rightPath, int maxEditDistance)
    {
        string leftName = Path.GetFileName(leftPath);
        string rightName = Path.GetFileName(rightPath);
        if (!string.Equals(Path.GetExtension(leftName), Path.GetExtension(rightName), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string leftStem = Path.GetFileNameWithoutExtension(leftName);
        string rightStem = Path.GetFileNameWithoutExtension(rightName);
        if (leftStem.Length > 64 || rightStem.Length > 64)
        {
            return false;
        }

        if (string.Equals(leftStem, rightStem, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return LevenshteinDistance(leftStem, rightStem) <= maxEditDistance;
    }

    internal static int LevenshteinDistance(string left, string right)
    {
        if (left.Length == 0)
        {
            return right.Length;
        }

        if (right.Length == 0)
        {
            return left.Length;
        }

        int[] previous = new int[right.Length + 1];
        int[] current = new int[right.Length + 1];
        for (int j = 0; j <= right.Length; j++)
        {
            previous[j] = j;
        }

        for (int i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            for (int j = 1; j <= right.Length; j++)
            {
                int cost = char.ToUpperInvariant(left[i - 1]) == char.ToUpperInvariant(right[j - 1]) ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }
}
