namespace MergeTool.App.Wpf.Services;

/// <summary>Abstraction over the folder-browse dialog so view models stay testable without WPF.</summary>
public interface IFolderPickerService
{
    /// <summary>Asks the user to pick one existing folder; null when the dialog is dismissed.</summary>
    string? PickFolder(string dialogTitle);
}
