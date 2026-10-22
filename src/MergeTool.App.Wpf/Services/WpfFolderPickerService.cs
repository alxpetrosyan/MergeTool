using Microsoft.Win32;

namespace MergeTool.App.Wpf.Services;

/// <summary>Standard Windows folder picker dialog.</summary>
public sealed class WpfFolderPickerService : IFolderPickerService
{
    public string? PickFolder(string dialogTitle)
    {
        OpenFolderDialog dialog = new()
        {
            Title = dialogTitle,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
