namespace MergeTool.Folders;

public enum FolderRenameCandidateMatchKind
{
    ContentHash = 0,
    NameSimilarity = 1,
}

/// <summary>
/// A suggested rename pairing between a left-only and right-only file row.
/// Suggestions never mutate folder scan results or perform file operations.
/// </summary>
public sealed record FolderRenameCandidate(
    string LeftRelativePath,
    string RightRelativePath,
    string LeftFullPath,
    string RightFullPath,
    FolderRenameCandidateMatchKind MatchKind)
{
    public string DisplayReason => MatchKind switch
    {
        FolderRenameCandidateMatchKind.ContentHash => "Identical content hash",
        FolderRenameCandidateMatchKind.NameSimilarity => "Similar file name",
        _ => MatchKind.ToString(),
    };
}
