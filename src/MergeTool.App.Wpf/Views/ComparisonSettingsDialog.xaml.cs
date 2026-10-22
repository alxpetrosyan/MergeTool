using System.Windows;

namespace MergeTool.App.Wpf.Views;

/// <summary>
/// Modal dialog hosting low-frequency comparison settings. Pure binding surface: the
/// DataContext is the owning <see cref="ViewModels.FileCompareWorkspaceViewModel"/>.
/// </summary>
public partial class ComparisonSettingsDialog : Window
{
    public ComparisonSettingsDialog()
    {
        InitializeComponent();
    }
}
