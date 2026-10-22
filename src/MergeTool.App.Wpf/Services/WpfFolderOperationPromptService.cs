using System.Windows;
using MergeTool.Folders.Operations;

namespace MergeTool.App.Wpf.Services;

public sealed class WpfFolderOperationPromptService : IFolderOperationPromptService
{
    public FolderOperationConfirmation ConfirmPlan(FolderOperationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Views.FolderOperationPreviewWindow window = new(
            FolderOperationPlanPreviewFormatter.FormatSummary(plan),
            FolderOperationPlanPreviewFormatter.FormatActionLines(plan),
            FolderOperationPlanPreviewFormatter.FormatWarning(plan))
        {
            Owner = System.Windows.Application.Current?.MainWindow,
        };

        bool? result = window.ShowDialog();
        return result == true
            ? FolderOperationConfirmation.Confirm
            : FolderOperationConfirmation.Cancel;
    }

    public bool ConfirmPermanentDelete(int fileCount)
    {
        MessageBoxResult result = MessageBox.Show(
            fileCount == 1
                ? "Recycle Bin is unavailable. Permanently delete 1 file?"
                : $"Recycle Bin is unavailable. Permanently delete {fileCount} files?",
            "Permanent delete",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        return result == MessageBoxResult.OK;
    }
}
