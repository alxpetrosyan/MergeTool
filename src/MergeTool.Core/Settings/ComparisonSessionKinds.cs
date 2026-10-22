namespace MergeTool.Core.Settings;

/// <summary>Well-known comparison session kinds stored in settings.</summary>
public static class ComparisonSessionKinds
{
    public const string FilePair = "file-pair";
    public const string FolderPair = "folder-pair";
    public const string FileThreeWay = "file-three-way";
    public const string FolderThreeWay = "folder-three-way";
}

/// <summary>Optional user choices persisted with a session entry (paths only in the entry itself).</summary>
public sealed record StoredComparisonOptions
{
    public string? FolderComparisonMode { get; init; }

    public int? TimestampToleranceSeconds { get; init; }
}
