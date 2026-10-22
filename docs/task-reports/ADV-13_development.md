# ADV-13 Development Report

**Task:** Add moved-block and renamed-file assistance  
**Date:** 2026-07-02  
**Verdict:** Complete

## Summary

Added optional moved-block detection for 2-way file compare and on-demand folder rename-candidate suggestions. Moved blocks are marked with a purple gutter indicator and “— moved” in the difference list; detection is bounded (block size and pair count limits) and never changes merge behavior. Rename candidates are discovered only when the user presses **Find rename candidates**; accepting a suggestion opens a cross-path file compare and does not rename, copy, or align folder rows.

## Changed files

### Text (`MergeTool.Text`)

- `Diff/MovedBlockDetector.cs` — pair cap (`DefaultMaxMovedPairs`)
- `Diff/TextComparisonOptions.cs` — `DetectMovedBlocks`

### Application (`MergeTool.Application`)

- `Sessions/TwoWayComparisonSession.cs` — `MovedBlockOrdinals` after recompare
- `Presentation/DifferenceListModel.cs` — moved suffix in list entries
- `UseCases/FindFolderRenameCandidatesUseCase.cs` — new

### Folders (`MergeTool.Folders`)

- `FolderRenameCandidate.cs`, `FolderRenameCandidateOptions.cs`, `FolderRenameCandidateFinder.cs` — bounded hash + name similarity

### WPF (`MergeTool.App.Wpf`)

- `Rendering/PaneRenderModel.cs` — `IsMovedLine`, moved ordinals parameter
- `Rendering/DiffLineBackgroundRenderer.cs` — purple moved gutter marker
- `ViewModels/FileCompareWorkspaceViewModel.cs` — **Detect moved blocks** rule
- `ViewModels/FolderCompareWorkspaceViewModel.cs` — find/accept/clear rename candidates
- `ViewModels/RenameCandidateViewModel.cs` — new
- `Views/FileCompareView.xaml` — moved-blocks checkbox + legend
- `Views/FolderCompareView.xaml` — rename candidate panel + toolbar button

### Tests

- `MovedBlockDetectorTests`, `FolderRenameCandidateFinderTests`
- Extended session, difference list, pane render, folder VM tests

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
| Debug bounded | **348/348** passed |
| Release bounded | **348/348** passed |
| Phase-1 checkpoint | **ALL PASSED** |

## Known limitations

- Moved-block detection matches deleted/inserted regions with identical text only; modified blocks are never marked as moved.
- Rename suggestions are 2-way folder compare only; no automatic grid realignment.
- Name similarity uses extension match + bounded Levenshtein distance; hash matching skips files over 10 MB.
- False-positive move/rename hints remain possible; user must explicitly accept compare actions.

## Remaining risks

- Large one-sided folder trees may still incur noticeable hash work when finding candidates (capped at 100 rows per side and 20 results).
