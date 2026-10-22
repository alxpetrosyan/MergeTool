using Microsoft.Win32;
using System.IO;

namespace MergeTool.App.Wpf.Services;

/// <summary>Standard Windows open-file dialog.</summary>
public sealed class WpfFilePickerService : IFilePickerService
{
    public string? PickFile(string dialogTitle)
    {
        OpenFileDialog dialog = new()
        {
            Title = dialogTitle,
            CheckFileExists = true,
            Filter = "All files (*.*)|*.*",
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickSaveFile(string dialogTitle, string? suggestedPath = null)
    {
        SaveFileDialog dialog = new()
        {
            Title = dialogTitle,
            Filter = "All files (*.*)|*.*",
            OverwritePrompt = true,
            FileName = suggestedPath is null ? string.Empty : Path.GetFileName(suggestedPath),
            InitialDirectory = suggestedPath is null
                ? null
                : Path.GetDirectoryName(suggestedPath),
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
