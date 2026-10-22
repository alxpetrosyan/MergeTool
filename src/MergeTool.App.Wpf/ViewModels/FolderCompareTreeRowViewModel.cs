using System.IO;
using System.Windows;
using MergeTool.Folders;

namespace MergeTool.App.Wpf.ViewModels;

/// <summary>
/// One visible row in the folder comparison tree-grid: a directory (expandable) or a file.
/// Wraps a <see cref="FolderComparisonTreeNode"/>; file nodes and scanned directory nodes also
/// carry the flat <see cref="FolderCompareRowViewModel"/> used by folder operations.
/// </summary>
public sealed class FolderCompareTreeRowViewModel : ViewModelBase
{
    private readonly Action<FolderCompareTreeRowViewModel> _expansionChanged;
    private bool _isExpanded;

    public FolderCompareTreeRowViewModel(
        FolderComparisonTreeNode node,
        int depth,
        Action<FolderCompareTreeRowViewModel> expansionChanged)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(expansionChanged);

        Node = node;
        Depth = depth;
        _expansionChanged = expansionChanged;
        _isExpanded = true;
        RowViewModel = node.Row is null ? null : new FolderCompareRowViewModel(node.Row);
        Children = node.Children
            .Select(child => new FolderCompareTreeRowViewModel(child, depth + 1, expansionChanged))
            .ToList();
        ToggleExpandCommand = new RelayCommand(_ => IsExpanded = !IsExpanded, _ => HasChildren);
    }

    public FolderComparisonTreeNode Node { get; }

    public IReadOnlyList<FolderCompareTreeRowViewModel> Children { get; }

    /// <summary>Flat row for the underlying scan entry; null for synthesized directory nodes.</summary>
    public FolderCompareRowViewModel? RowViewModel { get; }

    public int Depth { get; }

    public Thickness Indent => new(Depth * 18, 0, 0, 0);

    public string Name => Node.Name;

    public string RelativePath => Node.RelativePath;

    public bool IsDirectory => Node.IsDirectory;

    public bool HasChildren => Children.Count > 0;

    public string ExpanderGlyph => !HasChildren ? string.Empty : IsExpanded ? "▾" : "▸";

    public string TypeDisplay
    {
        get
        {
            if (IsDirectory)
            {
                return "Folder";
            }

            string extension = Path.GetExtension(Name);
            return string.IsNullOrEmpty(extension) ? "File" : extension.TrimStart('.').ToUpperInvariant();
        }
    }

    /// <summary>
    /// Directories without a scan row show a neutral label; metadata match is never presented
    /// as content-identical (labels come from <see cref="FolderRowStateLabels"/>).
    /// </summary>
    public string StateDisplay => Node.Row is null
        ? (Node.HasDifferenceDescendant ? "Contains differences" : string.Empty)
        : FolderRowStateLabels.GetDisplayName(Node.Row.State);

    /// <summary>Scanned state for row coloring; null for synthesized directory nodes.</summary>
    public FolderRowState? State => Node.Row?.State;

    public bool IsDifference => Node.Row is not null && FolderRowStateClassification.IsDifference(Node.Row.State);

    public string LeftSizeDisplay => RowViewModel?.LeftSizeDisplay ?? string.Empty;

    public string RightSizeDisplay => RowViewModel?.RightSizeDisplay ?? string.Empty;

    public string LeftTimeDisplay => RowViewModel?.LeftTimeDisplay ?? string.Empty;

    public string RightTimeDisplay => RowViewModel?.RightTimeDisplay ?? string.Empty;

    public string DiagnosticsDisplay => RowViewModel?.DiagnosticsDisplay ?? string.Empty;

    public RelayCommand ToggleExpandCommand { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value))
            {
                RaisePropertyChanged(nameof(ExpanderGlyph));
                _expansionChanged(this);
            }
        }
    }

    public void SetExpandedRecursive(bool expanded)
    {
        _isExpanded = expanded;
        RaisePropertyChanged(nameof(IsExpanded));
        RaisePropertyChanged(nameof(ExpanderGlyph));
        foreach (FolderCompareTreeRowViewModel child in Children)
        {
            child.SetExpandedRecursive(expanded);
        }
    }
}
