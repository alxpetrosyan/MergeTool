# ADV-05 Development Report

**Task:** Add search, go-to-line, and difference list  
**Date:** 2026-07-02  
**Verdict:** Complete

## Summary

Added fast inspection and navigation for large file comparisons: a non-UI `TextSearchService` (already present, now fully tested), `GoToLineService` with bounds validation, `DifferenceListBuilder` synchronized with block selection, and File Compare UI for search, go-to-line, and a difference list panel. Search operates on current document text and never mutates comparison results.

## Changed files

### Text (`MergeTool.Text`)

- `Search/TextSearchService.cs` — unchanged API; used by view model

### Application (`MergeTool.Application`)

- `Navigation/GoToLineService.cs` — 1-based line resolution with diagnostics
- `Presentation/ChangedLinesIndex.cs` — changed-line sets for search-in-changes
- `Presentation/DifferenceListModel.cs` — difference list entries from presentation map
- `Sessions/LineNavigationTarget.cs` — single-pane scroll target for search/go-to-line

### WPF (`MergeTool.App.Wpf`)

- `ViewModels/DifferenceListEntryViewModel.cs`
- `ViewModels/SearchPaneFilter.cs`
- `ViewModels/FileCompareWorkspaceViewModel.cs` — search, go-to-line, difference list sync
- `Views/FileCompareView.xaml` — search bar, go-to-line controls, difference list column
- `Views/FileCompareView.xaml.cs` — `LineNavigationRequested` scrolling with collapse reveal

### Tests

- `tests/MergeTool.Text.Tests/TextSearchServiceTests.cs` — expanded to 7 tests
- `tests/MergeTool.Application.Tests/GoToLineServiceTests.cs`
- `tests/MergeTool.Application.Tests/DifferenceListBuilderTests.cs`
- `tests/MergeTool.Application.Tests/ChangedLinesIndexTests.cs`
- `tests/MergeTool.App.Wpf.Tests/FileCompareSearchTests.cs`

### Docs

- `docs/SCOPE2_PROGRESS.md`

## Commands run

```powershell
.\build\run_tests_bounded.ps1
.\build\run_tests_bounded.ps1 -Configuration Release
.\build\phase1_checkpoint.ps1
```

## Test results

| Configuration | Result |
|---------------|--------|
| Debug bounded | **274/274** passed |
| Release bounded | **274/274** passed |
| Phase-1 checkpoint | **ALL PASSED** |

## Known limitations

- Search runs synchronously on the UI thread with per-line cancellation checks (no replace-all, no multi-file search).
- Difference list selection when comparison is stale scrolls to blocks but cannot call `SelectBlock` until recompare.
- Search does not highlight matches in the editor (navigation only).

## Remaining risks

- Very large files may need background search for responsiveness; current design matches MVP bounded scope.
