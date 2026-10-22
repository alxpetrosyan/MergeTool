namespace MergeTool.App.Wpf.Services;

/// <summary>
/// Abstraction over the file-open dialog so view models stay testable without WPF.
/// </summary>
public interface IFilePickerService
{
    /// <summary>Asks the user to pick one existing file; null when the dialog is dismissed.</summary>
    string? PickFile(string dialogTitle);

    /// <summary>Asks the user to pick a save path; null when the dialog is dismissed.</summary>
    string? PickSaveFile(string dialogTitle, string? suggestedPath = null);
}
