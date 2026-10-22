namespace MergeTool.Folders;

/// <summary>User-visible labels for folder row states. Never implies content equality.</summary>
public static class FolderRowStateLabels
{
    public const string MetadataMatchExplanation =
        "Metadata match = size + UTC modification time only.";

    public const string ContentMatchExplanation =
        "Content match = SHA-256 of file bytes agrees on all compared sides.";

    public static string GetDisplayName(FolderRowState state)
        => state switch
        {
            FolderRowState.LeftOnly => "Left only",
            FolderRowState.RightOnly => "Right only",
            FolderRowState.MetadataMatch => "Metadata match",
            FolderRowState.MetadataDifferent => "Metadata different",
            FolderRowState.Error => "Error",
            FolderRowState.SkippedReparsePoint => "Skipped reparse point",
            FolderRowState.ContentDifferent => "Content different",
            FolderRowState.ContentMatch => "Content match",
            FolderRowState.BaseOnly => "Base only",
            FolderRowState.BaseLeftOnly => "Base + left only",
            FolderRowState.BaseRightOnly => "Base + right only",
            FolderRowState.LeftRightOnly => "Left + right only",
            _ => state.ToString(),
        };
}
