using MergeTool.Folders.Operations;

namespace MergeTool.App.Wpf.Services;

public enum FolderOperationConfirmation
{
    Cancel = 0,
    Confirm = 1,
}

/// <summary>Preview and confirmation prompts for manual folder operations.</summary>
public interface IFolderOperationPromptService
{
    FolderOperationConfirmation ConfirmPlan(FolderOperationPlan plan);

    bool ConfirmPermanentDelete(int fileCount);
}
