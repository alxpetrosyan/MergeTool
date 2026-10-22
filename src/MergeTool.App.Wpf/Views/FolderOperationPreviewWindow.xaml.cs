using System.Collections.Generic;
using System.Windows;

namespace MergeTool.App.Wpf.Views;

public partial class FolderOperationPreviewWindow : Window
{
    public FolderOperationPreviewWindow(string summary, IReadOnlyList<string> actionLines, string? warning)
    {
        InitializeComponent();
        SummaryText.Text = summary;
        ActionList.ItemsSource = actionLines;
        if (string.IsNullOrWhiteSpace(warning))
        {
            WarningText.Visibility = Visibility.Collapsed;
        }
        else
        {
            WarningText.Text = warning;
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
