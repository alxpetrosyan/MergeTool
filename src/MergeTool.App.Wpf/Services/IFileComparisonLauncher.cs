namespace MergeTool.App.Wpf.Services;

/// <summary>
/// Opens file comparison workspaces through the application shell. View models must not read
/// editor content directly.
/// </summary>
public interface IFileComparisonLauncher
{
    Task OpenPairedComparisonAsync(string leftPath, string rightPath);

    Task OpenThreeWayComparisonAsync(string basePath, string leftPath, string rightPath);
}
