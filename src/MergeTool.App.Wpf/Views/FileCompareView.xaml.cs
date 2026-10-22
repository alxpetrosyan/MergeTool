using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using MergeTool.App.Wpf.Rendering;
using MergeTool.App.Wpf.ViewModels;
using MergeTool.Application.Presentation;
using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text.Diff;
using MergeTool.Text.Formatted;

namespace MergeTool.App.Wpf.Views;

/// <summary>
/// File-compare workspace view. Code-behind is limited to AvalonEdit-specific wiring:
/// loading editor text from the session, forwarding edits to the view model, attaching the
/// semantic background renderers, synchronized scrolling, equal-region folding, and the
/// difference minimap. All comparison behavior lives in the view model and application layer.
/// </summary>
public partial class FileCompareView : UserControl
{
    private readonly DiffLineBackgroundRenderer _leftRenderer = new();
    private readonly DiffLineBackgroundRenderer _rightRenderer = new();
    private readonly IntralineDiffColorizingTransformer _leftIntraline = new();
    private readonly IntralineDiffColorizingTransformer _rightIntraline = new();
    private readonly ScrollSyncCoordinator _scrollSync = new();
    private readonly EqualRegionFoldingCoordinator _folding = new();

    private FileCompareWorkspaceViewModel? _viewModel;
    private bool _suppressEditorEvents;
    private int _appliedPresentationRevision = -1;

    public FileCompareView()
    {
        InitializeComponent();

        LeftEditor.TextArea.TextView.BackgroundRenderers.Add(_leftRenderer);
        RightEditor.TextArea.TextView.BackgroundRenderers.Add(_rightRenderer);
        LeftEditor.TextArea.TextView.LineTransformers.Add(_leftIntraline);
        RightEditor.TextArea.TextView.LineTransformers.Add(_rightIntraline);

        _folding.Attach(LeftEditor, RightEditor);
        Minimap.BlockClicked += OnMinimapBlockClicked;

        LeftEditor.TextChanged += (_, _) => OnEditorTextChanged(ComparisonSide.Left, LeftEditor);
        RightEditor.TextChanged += (_, _) => OnEditorTextChanged(ComparisonSide.Right, RightEditor);

        // Only the pane the user is interacting with drives synchronized scrolling.
        HookActivation(LeftEditor, ComparisonSide.Left);
        HookActivation(RightEditor, ComparisonSide.Right);

        LeftEditor.TextArea.TextView.ScrollOffsetChanged += (_, _) => OnScrollChanged(ComparisonSide.Left);
        RightEditor.TextArea.TextView.ScrollOffsetChanged += (_, _) => OnScrollChanged(ComparisonSide.Right);

        DataContextChanged += OnDataContextChanged;
    }

    private void OnEditorPresentationChanged(object? sender, EventArgs e) => ApplyEditorPresentation();

    private void HookActivation(TextEditor editor, ComparisonSide side)
    {
        editor.PreviewMouseDown += (_, _) => _scrollSync.SetActiveSide(side);
        editor.PreviewMouseWheel += (_, _) => _scrollSync.SetActiveSide(side);
        editor.GotKeyboardFocus += (_, _) => _scrollSync.SetActiveSide(side);
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.SessionOpened -= OnSessionOpened;
            _viewModel.ComparisonUpdated -= OnComparisonUpdated;
            _viewModel.ComparisonInvalidated -= OnComparisonInvalidated;
            _viewModel.DocumentTextReplaced -= OnDocumentTextReplaced;
            _viewModel.NavigationRequested -= OnNavigationRequested;
            _viewModel.EditorPresentationChanged -= OnEditorPresentationChanged;
            _viewModel.PresentationStateChanged -= OnPresentationStateChanged;
            _viewModel.LineNavigationRequested -= OnLineNavigationRequested;
            _viewModel.ComparisonSettingsRequested -= OnComparisonSettingsRequested;
        }

        _viewModel = e.NewValue as FileCompareWorkspaceViewModel;
        if (_viewModel is not null)
        {
            _viewModel.SessionOpened += OnSessionOpened;
            _viewModel.ComparisonUpdated += OnComparisonUpdated;
            _viewModel.ComparisonInvalidated += OnComparisonInvalidated;
            _viewModel.DocumentTextReplaced += OnDocumentTextReplaced;
            _viewModel.NavigationRequested += OnNavigationRequested;
            _viewModel.EditorPresentationChanged += OnEditorPresentationChanged;
            _viewModel.PresentationStateChanged += OnPresentationStateChanged;
            _viewModel.LineNavigationRequested += OnLineNavigationRequested;
            _viewModel.ComparisonSettingsRequested += OnComparisonSettingsRequested;
            if (_viewModel.Session is not null)
            {
                LoadEditorsFromSession();
                RefreshOverlays();
                RefreshPresentationUi();
            }
        }
    }

    private void OnSessionOpened(object? sender, EventArgs e)
    {
        LoadEditorsFromSession();
        ApplyEditorPresentation();
    }

    /// <summary>Opens the modal comparison settings dialog bound to this workspace.</summary>
    private void OnComparisonSettingsRequested(object? sender, EventArgs e)
    {
        ComparisonSettingsDialog dialog = new()
        {
            Owner = Window.GetWindow(this),
            DataContext = _viewModel,
        };
        dialog.ShowDialog();
    }

    private void ApplyEditorPresentation()
    {
        if (_viewModel is null)
        {
            return;
        }

        var profile = _viewModel.ActiveProfile;
        SyntaxHighlightingAdapter.Apply(LeftEditor, profile);
        SyntaxHighlightingAdapter.Apply(RightEditor, profile);
    }

    /// <summary>A merge or reload replaced a document's text outside the editor.</summary>
    private void OnDocumentTextReplaced(object? sender, ComparisonSide side)
    {
        var session = _viewModel?.Session;
        if (session is null)
        {
            return;
        }

        if (session.IsFormattedProjectionActive)
        {
            LoadEditorsFromSession();
            RefreshPresentationUi();
            return;
        }

        TextEditor editor = side == ComparisonSide.Left ? LeftEditor : RightEditor;
        var document = side == ComparisonSide.Left ? session.Left : session.Right;
        _suppressEditorEvents = true;
        try
        {
            editor.Text = document.CurrentText;
        }
        finally
        {
            _suppressEditorEvents = false;
        }

        RefreshPresentationUi();
    }

    /// <summary>Brings the selected difference into view in both panes.</summary>
    private void OnNavigationRequested(object? sender, Application.Sessions.NavigationTarget target)
    {
        ScrollToLine(LeftEditor, target.LeftAnchorLine);
        ScrollToLine(RightEditor, target.RightAnchorLine);
        UpdateMinimapViewport(LeftEditor);
    }

    private void OnMinimapBlockClicked(object? sender, int blockOrdinal)
        => _viewModel?.NavigateToBlock(blockOrdinal);

    private void OnLineNavigationRequested(object? sender, LineNavigationTarget target)
    {
        _viewModel?.RevealLineForNavigation(target.Side, target.ZeroBasedLineIndex);
        TextEditor editor = target.Side == ComparisonSide.Left ? LeftEditor : RightEditor;
        ScrollToLine(editor, target.ZeroBasedLineIndex);
        UpdateMinimapViewport(editor);
    }

    private static void ScrollToLine(TextEditor editor, int zeroBasedLine)
    {
        int line = Math.Clamp(zeroBasedLine + 1, 1, Math.Max(1, editor.Document?.LineCount ?? 1));
        editor.TextArea.Caret.Line = line;
        editor.TextArea.Caret.Column = 1;
        editor.ScrollToLine(line);
    }

    private void OnComparisonUpdated(object? sender, EventArgs e)
    {
        LoadEditorsFromSession();
        RefreshOverlays();
        RefreshPresentationUi();
    }

    private void OnPresentationStateChanged(object? sender, EventArgs e) => RefreshPresentationUi();

    private void OnComparisonInvalidated(object? sender, EventArgs e)
    {
        // Stale: colors would point at outdated ranges, so clear them until recompare.
        _scrollSync.Map = null;
        _leftRenderer.SetModel(PaneRenderModel.Empty);
        _rightRenderer.SetModel(PaneRenderModel.Empty);
        _leftIntraline.SetModel(PaneRenderModel.Empty);
        _rightIntraline.SetModel(PaneRenderModel.Empty);
        _appliedPresentationRevision = -1;
        Minimap.Model = null;
        _folding.TryApply(null);
        RedrawEditors();
    }

    private void LoadEditorsFromSession()
    {
        var session = _viewModel?.Session;
        if (session is null)
        {
            return;
        }

        _suppressEditorEvents = true;
        try
        {
            if (session.IsFormattedProjectionActive
                && session.FormattedDisplayLeftLines is not null
                && session.FormattedDisplayRightLines is not null)
            {
                LeftEditor.Text = StructuredTextProjection.JoinLines(session.FormattedDisplayLeftLines);
                RightEditor.Text = StructuredTextProjection.JoinLines(session.FormattedDisplayRightLines);
                LeftEditor.IsReadOnly = true;
                RightEditor.IsReadOnly = true;
            }
            else
            {
                LeftEditor.Text = session.Left.CurrentText;
                RightEditor.Text = session.Right.CurrentText;
                LeftEditor.IsReadOnly = session.Left.IsReadOnly;
                RightEditor.IsReadOnly = session.Right.IsReadOnly;
            }
        }
        finally
        {
            _suppressEditorEvents = false;
        }
    }

    private void RefreshOverlays()
    {
        var session = _viewModel?.Session;
        var map = session?.PresentationMap;
        if (session is null || map is null)
        {
            return;
        }

        _scrollSync.Map = map;
        IReadOnlyList<ModifiedLineInlineDiff>? inlineDiffs = session.LatestResult?.InlineDiffs;
        IReadOnlySet<int> movedOrdinals = session.MovedBlockOrdinals;
        int leftLineCount = session.IsFormattedProjectionActive
            ? session.FormattedDisplayLeftLines!.Count
            : session.Left.CurrentLines.Count;
        int rightLineCount = session.IsFormattedProjectionActive
            ? session.FormattedDisplayRightLines!.Count
            : session.Right.CurrentLines.Count;
        PaneRenderModel leftModel = PaneRenderModel.Create(map, ComparisonSide.Left, leftLineCount, inlineDiffs, movedOrdinals);
        PaneRenderModel rightModel = PaneRenderModel.Create(map, ComparisonSide.Right, rightLineCount, inlineDiffs, movedOrdinals);
        _leftRenderer.SetModel(leftModel);
        _rightRenderer.SetModel(rightModel);
        _leftIntraline.SetModel(leftModel);
        _rightIntraline.SetModel(rightModel);
        RedrawEditors();
    }

    private void RefreshPresentationUi()
    {
        var session = _viewModel?.Session;
        FileComparePresentationBundle? bundle = _viewModel?.PresentationBundle;
        if (session is null || bundle is null)
        {
            Minimap.Model = null;
            _folding.TryApply(null);
            _appliedPresentationRevision = -1;
            return;
        }

        if (bundle.ComparisonRevision != session.ComparisonRevision)
        {
            return;
        }

        Minimap.Model = bundle.Minimap;
        if (bundle.ComparisonRevision != _appliedPresentationRevision)
        {
            _folding.TryApply(bundle);
            _appliedPresentationRevision = bundle.ComparisonRevision;
        }
        else
        {
            _folding.ReapplyFolds(bundle.Collapse, bundle.ComparisonRevision);
        }

        UpdateMinimapViewport(LeftEditor);
    }

    private void OnScrollChanged(ComparisonSide sourceSide)
    {
        SyncScroll(sourceSide);
        TextEditor source = sourceSide == ComparisonSide.Left ? LeftEditor : RightEditor;
        UpdateMinimapViewport(source);
    }

    private void UpdateMinimapViewport(TextEditor editor)
    {
        if (_viewModel?.PresentationBundle is null || editor.Document is null)
        {
            return;
        }

        double lineHeight = editor.TextArea.TextView.DefaultLineHeight;
        if (lineHeight <= 0)
        {
            return;
        }

        int firstVisible = (int)Math.Floor(editor.TextArea.TextView.ScrollOffset.Y / lineHeight);
        int visibleLines = (int)Math.Ceiling(editor.TextArea.TextView.ActualHeight / lineHeight);
        Minimap.Viewport = _viewModel.CalculateMinimapViewport(firstVisible, visibleLines);
    }

    private void RedrawEditors()
    {
        LeftEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        RightEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        LeftEditor.TextArea.TextView.Redraw();
        RightEditor.TextArea.TextView.Redraw();
    }

    private void OnEditorTextChanged(ComparisonSide side, TextEditor editor)
    {
        if (_suppressEditorEvents || _viewModel?.Session?.IsFormattedProjectionActive == true)
        {
            return;
        }

        _viewModel?.NotifyPaneEdited(side, editor.Text);
    }

    private void SyncScroll(ComparisonSide sourceSide)
    {
        TextEditor source = sourceSide == ComparisonSide.Left ? LeftEditor : RightEditor;
        TextEditor target = sourceSide == ComparisonSide.Left ? RightEditor : LeftEditor;
        TextView sourceView = source.TextArea.TextView;
        TextView targetView = target.TextArea.TextView;

        double? targetOffset = _scrollSync.GetCounterpartOffset(
            sourceSide,
            sourceView.ScrollOffset.Y,
            sourceView.DefaultLineHeight,
            targetView.ScrollOffset.Y);

        if (targetOffset is double offset)
        {
            target.ScrollToVerticalOffset(offset);
        }
    }
}
