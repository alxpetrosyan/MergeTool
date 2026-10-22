using MergeTool.Application.Presentation;
using MergeTool.Text.Diff;

namespace MergeTool.App.Wpf.ViewModels;

public sealed class DifferenceListEntryViewModel : ViewModelBase
{
    private bool _isSelected;

    public DifferenceListEntryViewModel(DifferenceListEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        BlockOrdinal = entry.BlockOrdinal;
        ListIndex = entry.ListIndex;
        Kind = entry.Kind;
        Summary = entry.Summary;
        LeftAnchorLine = entry.LeftAnchorLine;
        RightAnchorLine = entry.RightAnchorLine;
        DisplayText = $"{ListIndex}. {Summary}";
    }

    public int BlockOrdinal { get; }

    public int ListIndex { get; }

    public DiffBlockKind Kind { get; }

    public string Summary { get; }

    public string DisplayText { get; }

    public int LeftAnchorLine { get; }

    public int RightAnchorLine { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
