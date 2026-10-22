using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using MergeTool.Application.Presentation;
using MergeTool.Core;

namespace MergeTool.App.Wpf.Rendering;

/// <summary>
/// Applies equal-region foldings to AvalonEdit without mutating document text. Fold state is
/// driven by <see cref="CollapseProjectionState"/>; expanding a region reveals original lines.
/// </summary>
public sealed class EqualRegionFoldingCoordinator
{
    private FoldingManager? _leftManager;
    private FoldingManager? _rightManager;
    private TextEditor? _leftEditor;
    private TextEditor? _rightEditor;
    private int _appliedRevision = -1;

    public void Attach(TextEditor leftEditor, TextEditor rightEditor)
    {
        ArgumentNullException.ThrowIfNull(leftEditor);
        ArgumentNullException.ThrowIfNull(rightEditor);

        Detach();
        _leftEditor = leftEditor;
        _rightEditor = rightEditor;
        _leftManager = FoldingManager.Install(leftEditor.TextArea);
        _rightManager = FoldingManager.Install(rightEditor.TextArea);
    }

    public void Detach()
    {
        if (_leftManager is not null && _leftEditor is not null)
        {
            FoldingManager.Uninstall(_leftManager);
        }

        if (_rightManager is not null && _rightEditor is not null)
        {
            FoldingManager.Uninstall(_rightManager);
        }

        _leftManager = null;
        _rightManager = null;
        _leftEditor = null;
        _rightEditor = null;
        _appliedRevision = -1;
    }

    public bool TryApply(FileComparePresentationBundle? bundle)
    {
        if (bundle is null)
        {
            ClearFoldings();
            _appliedRevision = -1;
            return true;
        }

        if (bundle.ComparisonRevision == _appliedRevision)
        {
            return true;
        }

        ApplySide(_leftManager, _leftEditor, ComparisonSide.Left, bundle.Collapse);
        ApplySide(_rightManager, _rightEditor, ComparisonSide.Right, bundle.Collapse);
        _appliedRevision = bundle.ComparisonRevision;
        return true;
    }

    public void ReapplyFolds(CollapseProjectionState collapse, int comparisonRevision)
    {
        ApplySide(_leftManager, _leftEditor, ComparisonSide.Left, collapse);
        ApplySide(_rightManager, _rightEditor, ComparisonSide.Right, collapse);
        _appliedRevision = comparisonRevision;
    }

    private void ClearFoldings()
    {
        _leftManager?.UpdateFoldings([], -1);
        _rightManager?.UpdateFoldings([], -1);
    }

    private static void ApplySide(
        FoldingManager? manager,
        TextEditor? editor,
        ComparisonSide side,
        CollapseProjectionState collapse)
    {
        if (manager is null || editor?.Document is null)
        {
            return;
        }

        var foldings = new List<NewFolding>();
        foreach (FoldSpecification spec in collapse.GetActiveFolds(side))
        {
            if (spec.StartLine >= spec.EndLineExclusive)
            {
                continue;
            }

            int firstLine = spec.StartLine + 1;
            int lastLine = spec.EndLineExclusive;
            if (lastLine > editor.Document.LineCount)
            {
                lastLine = editor.Document.LineCount;
            }

            if (firstLine > editor.Document.LineCount)
            {
                continue;
            }

            int startOffset = editor.Document.GetOffset(firstLine, 0);
            var lastDocumentLine = editor.Document.GetLineByNumber(lastLine);
            int endOffset = lastDocumentLine.EndOffset;
            foldings.Add(new NewFolding(startOffset, endOffset)
            {
                Name = spec.Title,
                DefaultClosed = true,
            });
        }

        manager.UpdateFoldings(foldings, -1);
    }
}
