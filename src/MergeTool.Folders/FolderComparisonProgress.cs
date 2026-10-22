namespace MergeTool.Folders;

/// <summary>Phase of a folder comparison operation.</summary>
public enum FolderComparisonPhase
{
    Scan = 0,
    ContentHash = 1,
}

/// <summary>Structured progress for a recursive folder scan and optional content hashing.</summary>
public sealed record FolderComparisonProgress(
    int EntriesScanned,
    string? CurrentRelativePath,
    FolderComparisonPhase Phase = FolderComparisonPhase.Scan);
