namespace MergeTool.App.Wpf.ViewModels;

/// <summary>One recent or pinned comparison entry shown in the shell.</summary>
public sealed class SessionListItemViewModel
{
    public SessionListItemViewModel(
        string displayLabel,
        string kind,
        string pathA,
        string? pathB,
        string? pathBase,
        bool pathsExist,
        string? optionsJson,
        string? pinnedLabel = null)
    {
        DisplayLabel = displayLabel;
        Kind = kind;
        PathA = pathA;
        PathB = pathB;
        PathBase = pathBase;
        PathsExist = pathsExist;
        OptionsJson = optionsJson;
        PinnedLabel = pinnedLabel;
    }

    public string DisplayLabel { get; }

    public string Kind { get; }

    public string PathA { get; }

    public string? PathB { get; }

    public string? PathBase { get; }

    public bool PathsExist { get; }

    public string? OptionsJson { get; }

    public string? PinnedLabel { get; }

    public string AvailabilityDisplay => PathsExist ? string.Empty : " (missing paths)";
}
