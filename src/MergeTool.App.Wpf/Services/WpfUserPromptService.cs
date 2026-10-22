using System.Windows;

namespace MergeTool.App.Wpf.Services;

/// <summary>MessageBox-based prompts for the running application.</summary>
public sealed class WpfUserPromptService : IUserPromptService
{
    public SaveConflictDecision AskSaveConflict(string displayName)
    {
        MessageBoxResult result = MessageBox.Show(
            $"'{displayName}' was changed outside this application. Nothing was overwritten.\n\n"
            + "Reload the file from disk and discard your in-memory changes?",
            "File changed on disk",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        return result == MessageBoxResult.OK ? SaveConflictDecision.Reload : SaveConflictDecision.Cancel;
    }

    public DirtyCloseDecision AskCloseDirtyWorkspace(string workspaceTitle)
    {
        MessageBoxResult result = MessageBox.Show(
            $"'{workspaceTitle}' has unsaved changes.\n\nSave them before closing?",
            "Unsaved changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);
        return result switch
        {
            MessageBoxResult.Yes => DirtyCloseDecision.Save,
            MessageBoxResult.No => DirtyCloseDecision.Discard,
            _ => DirtyCloseDecision.Cancel,
        };
    }

    public bool ConfirmDiscardAllOnExit(int dirtyDocumentCount)
    {
        MessageBoxResult result = MessageBox.Show(
            $"{dirtyDocumentCount} document(s) have unsaved changes.\n\nExit and discard them?",
            "Unsaved changes",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        return result == MessageBoxResult.OK;
    }

    public void ShowInformation(string title, string message)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
}
