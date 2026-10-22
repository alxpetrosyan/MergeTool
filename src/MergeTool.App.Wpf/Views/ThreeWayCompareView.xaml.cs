using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using MergeTool.App.Wpf.Rendering;
using MergeTool.App.Wpf.ViewModels;
using MergeTool.Application.Sessions;
using MergeTool.Text.ThreeWay;

namespace MergeTool.App.Wpf.Views;

public partial class ThreeWayCompareView : UserControl
{
    private readonly ThreeWayLineBackgroundRenderer _baseRenderer = new();
    private readonly ThreeWayLineBackgroundRenderer _leftRenderer = new();
    private readonly ThreeWayLineBackgroundRenderer _rightRenderer = new();

    private ThreeWayCompareWorkspaceViewModel? _viewModel;
    private bool _suppressOutputEvents;

    public ThreeWayCompareView()
    {
        InitializeComponent();

        BaseEditor.TextArea.TextView.BackgroundRenderers.Add(_baseRenderer);
        LeftEditor.TextArea.TextView.BackgroundRenderers.Add(_leftRenderer);
        RightEditor.TextArea.TextView.BackgroundRenderers.Add(_rightRenderer);

        OutputEditor.TextChanged += (_, _) => OnOutputTextChanged();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.SessionOpened -= OnSessionOpened;
            _viewModel.ComparisonUpdated -= OnComparisonUpdated;
            _viewModel.OutputUpdated -= OnOutputUpdated;
            _viewModel.NavigationRequested -= OnNavigationRequested;
        }

        _viewModel = e.NewValue as ThreeWayCompareWorkspaceViewModel;
        if (_viewModel is not null)
        {
            _viewModel.SessionOpened += OnSessionOpened;
            _viewModel.ComparisonUpdated += OnComparisonUpdated;
            _viewModel.OutputUpdated += OnOutputUpdated;
            _viewModel.NavigationRequested += OnNavigationRequested;
            if (_viewModel.Session is not null)
            {
                LoadEditorsFromSession();
                RefreshOverlays();
            }
        }
    }

    private void OnSessionOpened(object? sender, EventArgs e)
    {
        LoadEditorsFromSession();
        RefreshOverlays();
    }

    private void OnComparisonUpdated(object? sender, EventArgs e) => RefreshOverlays();

    private void OnOutputUpdated(object? sender, EventArgs e) => LoadOutputEditor();

    private void OnNavigationRequested(object? sender, ThreeWayNavigationTarget target)
    {
        ScrollToLine(BaseEditor, target.BaseAnchorLine);
        ScrollToLine(LeftEditor, target.LeftAnchorLine);
        ScrollToLine(RightEditor, target.RightAnchorLine);
    }

    private void RegionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is null || RegionListBox.SelectedItem is not ThreeWayRegionEntryViewModel entry)
        {
            return;
        }

        _viewModel.SelectedRegionOrdinal = entry.RegionOrdinal;
    }

    private void RegionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RegionListBox.SelectedItem is ThreeWayRegionEntryViewModel entry)
        {
            _viewModel?.NavigateToRegion(entry.RegionOrdinal);
        }
    }

    private void OnOutputTextChanged()
    {
        if (_suppressOutputEvents || _viewModel is null)
        {
            return;
        }

        _viewModel.NotifyOutputEdited(OutputEditor.Text);
    }

    private void LoadEditorsFromSession()
    {
        var session = _viewModel?.Session;
        if (session is null)
        {
            return;
        }

        BaseEditor.Text = session.Base.CurrentText;
        LeftEditor.Text = session.Left.CurrentText;
        RightEditor.Text = session.Right.CurrentText;
        LoadOutputEditor();
    }

    private void LoadOutputEditor()
    {
        string? text = _viewModel?.OutputMerge.Output?.CurrentText;
        if (text is null)
        {
            return;
        }

        _suppressOutputEvents = true;
        try
        {
            OutputEditor.Text = text;
        }
        finally
        {
            _suppressOutputEvents = false;
        }
    }

    private void RefreshOverlays()
    {
        var session = _viewModel?.Session;
        var result = session?.LatestResult;
        if (session is null || result is null)
        {
            return;
        }

        _baseRenderer.SetModel(ThreeWayPaneRenderModel.Create(result, ThreeWayDocumentSide.Base, session.Base.CurrentLines.Count));
        _leftRenderer.SetModel(ThreeWayPaneRenderModel.Create(result, ThreeWayDocumentSide.Left, session.Left.CurrentLines.Count));
        _rightRenderer.SetModel(ThreeWayPaneRenderModel.Create(result, ThreeWayDocumentSide.Right, session.Right.CurrentLines.Count));
        RedrawSourceEditors();
    }

    private static void ScrollToLine(TextEditor editor, int zeroBasedLine)
    {
        int line = Math.Clamp(zeroBasedLine + 1, 1, Math.Max(1, editor.Document?.LineCount ?? 1));
        editor.TextArea.Caret.Line = line;
        editor.TextArea.Caret.Column = 1;
        editor.ScrollToLine(line);
    }

    private void RedrawSourceEditors()
    {
        BaseEditor.TextArea.TextView.InvalidateLayer(_baseRenderer.Layer);
        LeftEditor.TextArea.TextView.InvalidateLayer(_leftRenderer.Layer);
        RightEditor.TextArea.TextView.InvalidateLayer(_rightRenderer.Layer);
    }
}
