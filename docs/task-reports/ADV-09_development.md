# ADV-09 Development Report

**Task:** Add 3-way folder comparison  
**Date:** 2026-07-02  
**Verdict:** Complete

## Summary

Extended the folder comparison model with a base side and explicit presence/metadata states for all eight path combinations. Added a dedicated **3-Way Folder Compare** workspace (base/left/right pickers, filtered grid, legend) with **Open 3-way text comparison** gated on `HasTripledFiles`. No automated folder merge or sync. Existing 2-way folder compare and copy/delete operations are unchanged.

## Changed files

### Core (`MergeTool.Core`)

- `Diagnostic.cs` — `ComparisonSide.Base`

### Folders (`MergeTool.Folders`)

- `FolderRowState.cs` — `BaseOnly`, `BaseLeftOnly`, `BaseRightOnly`, `LeftRightOnly`
- `FolderComparisonRequest.cs` — 3-root constructor; `IsThreeWay`
- `FolderComparisonRow.cs` — base path/metadata fields; `HasTripledFiles`
- `FolderRowStateLabels.cs` — labels for new states
- `QuickFolderComparisonEngine.cs` — 3-way scan, validation (`MT-FLD-007`–`009`), state derivation

### WPF (`MergeTool.App.Wpf`)

- `ViewModels/FolderCompareFilter.cs` — new states in Differences filter
- `ViewModels/FolderCompareRowViewModel.cs` — base columns; `CanOpenThreeWayTextComparison`
- `ViewModels/ThreeWayFolderCompareWorkspaceViewModel.cs` — 3-way folder workspace
- `Views/ThreeWayFolderCompareView.xaml(.cs)` — UI
- `Services/IFileComparisonLauncher.cs` — `OpenThreeWayComparisonAsync`
- `ViewModels/MainWindowViewModel.cs` — launcher + tab factory
- `MainWindow.xaml` — **3-Way Folder Compare** toolbar button + DataTemplate

### Tests

- `tests/MergeTool.Folders.Tests/QuickFolderComparisonEngineTests.cs` — 3 tests
- `tests/MergeTool.App.Wpf.Tests/ThreeWayFolderCompareWorkspaceViewModelTests.cs` — 4 tests
- `tests/MergeTool.App.Wpf.Tests/WorkspaceLifecycleTests.cs`, `FolderCompareWorkspaceViewModelTests.cs` — launcher wiring

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
| Debug bounded | **311/311** passed |
| Release bounded | **311/311** passed |
| Phase-1 checkpoint | **ALL PASSED** |

## Known limitations

- Quick metadata only (size + UTC mtime); `ContentMatch`/`ContentDifferent` still unused (ADV-10).
- 3-way folder workspace has no copy/delete operations (2-way folder ops remain on the 2-way tab).
- Partial-presence rows show metadata in columns but state reflects presence, not pairwise metadata.

## Remaining risks

- ADV-10 content-hash mode will need 3-way state extensions when wired.
