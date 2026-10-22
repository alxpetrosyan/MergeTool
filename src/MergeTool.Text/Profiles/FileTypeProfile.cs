using MergeTool.Text.Diff;

namespace MergeTool.Text.Profiles;

/// <summary>Data-driven profile: extensions, editor tab size, optional comparison defaults, syntax key.</summary>
public sealed record FileTypeProfile(
    FileTypeProfileId Id,
    string DisplayName,
    IReadOnlyList<string> ExtensionMasks,
    int TabSize,
    TextComparisonOptions? DefaultComparisonOptions,
    string? AvalonEditHighlightingName)
{
    public bool HasComparisonOptionDefaults => DefaultComparisonOptions is not null;
}
