using MergeTool.Core.Settings;
using MergeTool.Folders;

namespace MergeTool.App.Wpf.ViewModels;

internal static class SessionFolderOptionsApplier
{
    public static void Apply(FolderCompareWorkspaceViewModel workspace, string? optionsJson)
    {
        ApplyCore(
            optionsJson,
            mode => workspace.ComparisonMode = mode,
            seconds => workspace.TimestampToleranceSeconds = seconds);
    }

    public static void Apply(ThreeWayFolderCompareWorkspaceViewModel workspace, string? optionsJson)
    {
        ApplyCore(
            optionsJson,
            mode => workspace.ComparisonMode = mode,
            seconds => workspace.TimestampToleranceSeconds = seconds);
    }

    public static string? BuildOptionsJson(FolderComparisonMode mode, int toleranceSeconds)
    {
        if (mode == FolderComparisonMode.QuickMetadata && toleranceSeconds == 0)
        {
            return null;
        }

        return AppSettingsMutations.SerializeOptions(new StoredComparisonOptions
        {
            FolderComparisonMode = mode.ToString(),
            TimestampToleranceSeconds = toleranceSeconds,
        });
    }

    private static void ApplyCore(
        string? optionsJson,
        Action<FolderComparisonMode> setMode,
        Action<int> setToleranceSeconds)
    {
        StoredComparisonOptions? options = AppSettingsMutations.DeserializeOptions(optionsJson);
        if (options is null)
        {
            return;
        }

        if (options.FolderComparisonMode is not null
            && Enum.TryParse(options.FolderComparisonMode, out FolderComparisonMode mode))
        {
            setMode(mode);
        }

        if (options.TimestampToleranceSeconds is int seconds)
        {
            setToleranceSeconds(Math.Max(0, seconds));
        }
    }
}
