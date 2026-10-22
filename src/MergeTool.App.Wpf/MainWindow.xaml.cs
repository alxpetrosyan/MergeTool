using System.Windows;
using MergeTool.App.Wpf.ViewModels;

namespace MergeTool.App.Wpf;

/// <summary>
/// Shell window. Code-behind only wires the composition root; all behavior lives in the
/// view models and application layer.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.CreateDefault();
        Closing += (_, e) =>
        {
            if (DataContext is MainWindowViewModel shell && !shell.ConfirmApplicationExit())
            {
                e.Cancel = true;
            }
        };
    }
}
