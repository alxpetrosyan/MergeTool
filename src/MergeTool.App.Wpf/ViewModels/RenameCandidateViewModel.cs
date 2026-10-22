using MergeTool.Folders;

namespace MergeTool.App.Wpf.ViewModels;

public sealed class RenameCandidateViewModel
{
    public RenameCandidateViewModel(FolderRenameCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        Source = candidate;
        DisplayText = $"{candidate.LeftRelativePath} ↔ {candidate.RightRelativePath} ({candidate.DisplayReason})";
    }

    public FolderRenameCandidate Source { get; }

    public string DisplayText { get; }
}
