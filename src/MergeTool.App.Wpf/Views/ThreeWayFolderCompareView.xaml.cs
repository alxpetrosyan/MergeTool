using System.Windows.Controls;

namespace MergeTool.App.Wpf.Views;

public partial class ThreeWayFolderCompareView : UserControl
{
    public ThreeWayFolderCompareView()
    {
        InitializeComponent();
    }

    private void FolderRows_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ViewModels.ThreeWayFolderCompareWorkspaceViewModel workspace)
        {
            return;
        }

        ViewModels.FolderCompareRowViewModel? selected = FolderRowsGrid.SelectedItem as ViewModels.FolderCompareRowViewModel;
        workspace.SelectedRow = selected;
    }
}
