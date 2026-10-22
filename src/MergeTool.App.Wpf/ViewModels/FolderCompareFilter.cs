using MergeTool.Folders;

namespace MergeTool.App.Wpf.ViewModels;

/// <summary>Row filters for the folder comparison views.</summary>
public enum FolderCompareFilter
{
    All = 0,
    Differences = 1,
    ErrorsSkipped = 2,
    LeftOnly = 3,
    RightOnly = 4,
}

/// <summary>Inclusion rules for folder comparison filters.</summary>
public static class FolderCompareFilterRules
{
    public static bool Includes(FolderCompareFilter filter, FolderRowState state)
        => filter switch
        {
            FolderCompareFilter.All => true,
            FolderCompareFilter.Differences => FolderRowStateClassification.IsDifference(state),
            FolderCompareFilter.ErrorsSkipped => FolderRowStateClassification.IsErrorOrSkipped(state),
            FolderCompareFilter.LeftOnly => state is FolderRowState.LeftOnly or FolderRowState.BaseLeftOnly,
            FolderCompareFilter.RightOnly => state is FolderRowState.RightOnly or FolderRowState.BaseRightOnly,
            _ => false,
        };
}
