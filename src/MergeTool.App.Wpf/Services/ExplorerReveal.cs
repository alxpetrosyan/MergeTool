using System.Diagnostics;
using System.IO;

namespace MergeTool.App.Wpf.Services;

/// <summary>Opens Windows Explorer with the given file or folder selected.</summary>
public static class ExplorerReveal
{
    public static void RevealPath(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return;
        }

        try
        {
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
            }
        }
        catch (Exception)
        {
            // Best-effort convenience action; never take the app down for a shell launch failure.
        }
    }
}
