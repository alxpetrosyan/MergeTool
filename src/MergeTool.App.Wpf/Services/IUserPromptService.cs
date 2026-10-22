namespace MergeTool.App.Wpf.Services;

/// <summary>User decision when a save hits an external modification. No force overwrite in MVP.</summary>
public enum SaveConflictDecision
{
    Cancel = 0,
    Reload = 1,
}

/// <summary>User decision when closing a workspace with unsaved changes.</summary>
public enum DirtyCloseDecision
{
    Cancel = 0,
    Save = 1,
    Discard = 2,
}

/// <summary>
/// Abstraction over blocking user prompts so view models stay testable without WPF.
/// </summary>
public interface IUserPromptService
{
    /// <summary>The file changed on disk since load; offer Cancel or Reload only.</summary>
    SaveConflictDecision AskSaveConflict(string displayName);

    /// <summary>A workspace with unsaved changes is closing; offer Save, Discard, or Cancel.</summary>
    DirtyCloseDecision AskCloseDirtyWorkspace(string workspaceTitle);

    /// <summary>The application is exiting with unsaved changes; true allows discarding them.</summary>
    bool ConfirmDiscardAllOnExit(int dirtyDocumentCount);

    /// <summary>Shows an informational message the user must acknowledge.</summary>
    void ShowInformation(string title, string message);
}
