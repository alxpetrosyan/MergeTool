using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using MergeTool.App.Wpf.ViewModels;

namespace MergeTool.App.Wpf.Views;

public partial class FolderCompareView : UserControl
{
    public FolderCompareView()
    {
        InitializeComponent();
    }

    private void FolderRows_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not FolderCompareWorkspaceViewModel workspace)
        {
            return;
        }

        FolderCompareTreeRowViewModel[] selected = FolderRowsGrid.SelectedItems
            .OfType<FolderCompareTreeRowViewModel>()
            .ToArray();
        workspace.SetSelectedTreeRows(selected);
    }

    /// <summary>Double-click: toggle directories, open a file comparison for file rows.</summary>
    private async void FolderRows_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not FolderCompareWorkspaceViewModel workspace
            || FolderRowsGrid.SelectedItem is not FolderCompareTreeRowViewModel row)
        {
            return;
        }

        if (row.IsDirectory)
        {
            if (row.HasChildren)
            {
                row.IsExpanded = !row.IsExpanded;
            }

            return;
        }

        // One-sided rows surface a clear message through the view model prompt service.
        await workspace.OpenPairedTextComparisonAsync();
    }
}
