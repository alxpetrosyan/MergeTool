# ADV-07 Development Report

**Task:** Add 3-way text comparison model and inspection UI  
**Date:** 2026-07-02  
**Verdict:** Complete

## Summary

Implemented a 3-way text comparison model built from two base-relative 2-way diffs, classifying regions as unchanged, left-only, right-only, same change, or divergent. Added a read-only 3-pane inspection workspace (base / left / right) with region coloring, difference list, and navigation. No merge output pane or automatic resolution.

## Changed files

### Text (`MergeTool.Text`)

- `ThreeWay/ThreeWayComparisonBuilder.cs` — full region alignment, range mapping, SameChange detection
- `ThreeWayRegionKind`, `ThreeWayRegion`, `ThreeWayComparisonResult`, `ThreeWayDocumentSide`, labels

### Application (`MergeTool.Application`)

- `Sessions/ThreeWayComparisonSession.cs` — session lifecycle, staleness, dual recompare
- `Sessions/ThreeWayRegionNavigator.cs` — non-wrapping region navigation
- `UseCases/OpenThreeWayFileComparisonUseCase.cs` — load three files, validate distinct paths

### WPF (`MergeTool.App.Wpf`)

- `ViewModels/ThreeWayCompareWorkspaceViewModel.cs`
- `Views/ThreeWayCompareView.xaml(.cs)` — 3 read-only editors + region list
- `Rendering/ThreeWayPaneRenderModel.cs`, `ThreeWayLineBackgroundRenderer.cs`
- `MainWindow.xaml`, `ViewModels/MainWindowViewModel.cs` — **3-Way Compare** command/tab

### Tests

- `tests/MergeTool.Text.Tests/ThreeWayComparisonBuilderTests.cs` — 8 tests
- `tests/MergeTool.Application.Tests/ThreeWayComparisonSessionTests.cs`
- `tests/MergeTool.App.Wpf.Tests/ThreeWayCompareWorkspaceViewModelTests.cs`

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
| Debug bounded | **296/296** passed |
| Release bounded | **296/296** passed |
| Phase-1 checkpoint | **ALL PASSED** |

## Known limitations

- Inspection panes are read-only (no in-tab edit/recompare-from-edit loop).
- No synchronized scrolling across three panes (navigation scrolls each pane independently).
- SameChange detection compares joined region text literally (no comparison-rule projection).
- No intraline highlighting in 3-way view.

## Remaining risks

- Complex insertion/deletion alignment near shared anchors may need golden fixtures as the model evolves toward ADV-08 output merge.
