namespace MergeTool.App.Wpf.Services;

/// <summary>Supplies recently used comparison paths for path selector dropdowns.</summary>
public interface IRecentPathProvider
{
    /// <summary>Distinct recent paths, most recent first.</summary>
    IReadOnlyList<string> GetRecentPaths();
}
