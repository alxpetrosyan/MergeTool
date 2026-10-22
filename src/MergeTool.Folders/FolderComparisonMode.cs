namespace MergeTool.Folders;

/// <summary>How folder rows are classified after a recursive scan.</summary>
public enum FolderComparisonMode
{
    QuickMetadata = 0,
    ContentHash = 1,
}
