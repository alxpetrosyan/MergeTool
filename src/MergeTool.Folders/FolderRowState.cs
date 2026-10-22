namespace MergeTool.Folders;

/// <summary>
/// Quick metadata comparison state for one folder row. A metadata match compares file length
/// and UTC last-write time only; it does not prove content equality.
/// </summary>
public enum FolderRowState
{
    LeftOnly = 0,
    RightOnly = 1,
    MetadataMatch = 2,
    MetadataDifferent = 3,
    Error = 4,
    SkippedReparsePoint = 5,
    ContentDifferent = 6,
    ContentMatch = 7,
    BaseOnly = 8,
    BaseLeftOnly = 9,
    BaseRightOnly = 10,
    LeftRightOnly = 11,
}
