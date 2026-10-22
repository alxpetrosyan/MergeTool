using MergeTool.App.Wpf.Services;
using MergeTool.Core.Settings;

namespace MergeTool.App.Wpf.ViewModels;

internal static class SessionLauncher
{
    public static async Task OpenAsync(MainWindowViewModel shell, SessionListItemViewModel item)
    {
        switch (item.Kind)
        {
            case ComparisonSessionKinds.FilePair:
                await shell.OpenPairedComparisonAsync(item.PathA, item.PathB!);
                return;
            case ComparisonSessionKinds.FileThreeWay:
                await shell.OpenThreeWayComparisonAsync(item.PathBase!, item.PathA, item.PathB!);
                return;
            case ComparisonSessionKinds.FolderPair:
            {
                FolderCompareWorkspaceViewModel workspace = shell.ShowFolderCompareWorkspace();
                workspace.LeftRoot = item.PathA;
                workspace.RightRoot = item.PathB!;
                SessionFolderOptionsApplier.Apply(workspace, item.OptionsJson);
                await workspace.CompareAsync();
                return;
            }

            case ComparisonSessionKinds.FolderThreeWay:
            {
                ThreeWayFolderCompareWorkspaceViewModel workspace = shell.ShowThreeWayFolderCompareWorkspace();
                workspace.BaseRoot = item.PathBase!;
                workspace.LeftRoot = item.PathA;
                workspace.RightRoot = item.PathB!;
                SessionFolderOptionsApplier.Apply(workspace, item.OptionsJson);
                await workspace.CompareAsync();
                return;
            }

            default:
                return;
        }
    }
}
