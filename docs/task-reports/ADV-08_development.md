# ADV-08 Development Report

**Task:** Add manual 3-way output merge and resolution workflow  
**Date:** 2026-07-02  
**Verdict:** Complete

## Summary

Added a separate output document session initialized from base text, with per-region explicit resolution (use base / left / right). Manual edits to the output pane invalidate region resolution as stale/unresolved. Output is saved to a user-selected path via atomic new-file write; source documents remain read-only and untouched. No automatic merge or conflict-marker generation.

## Changed files

### Application (`MergeTool.Application`)

- `Sessions/ThreeWayOutputSourceChoice.cs` — `ThreeWayOutputSourceChoice`, `ThreeWayRegionResolutionState`, `ThreeWayRegionResolution`
- `Sessions/ThreeWayOutputTextBuilder.cs` — assembles output text from regions + resolutions
- `Sessions/ThreeWayOutputMergeSession.cs` — init from base, `ApplyRegionChoice`, `NotifyManualOutputEdit`, unresolved ordinals, `RefreshAfterRecompare`
- `UseCases/ThreeWayOutputPersistenceService.cs` — save guard for stale comparison and unresolved regions

### FileSystem (`MergeTool.FileSystem`)

- `SafeTextFileWriter.SaveNewFileAsync()` — atomic write for new output files

### WPF (`MergeTool.App.Wpf`)

- `Services/IFilePickerService.PickSaveFile()` + `WpfFilePickerService` implementation
- `ViewModels/ThreeWayCompareWorkspaceViewModel.cs` — output merge session, resolution commands, save, next-unresolved, unsaved flag
- `Views/ThreeWayCompareView.xaml(.cs)` — 4th editable **Output** pane, resolution buttons, save, resolution status in region list
- `ViewModels/MainWindowViewModel.cs` — wires `ThreeWayOutputPersistenceService`

### Tests

- `tests/MergeTool.Application.Tests/ThreeWayOutputTextBuilderTests.cs` — 2 tests
- `tests/MergeTool.Application.Tests/ThreeWayOutputMergeSessionTests.cs` — 4 tests
- `tests/MergeTool.App.Wpf.Tests/ThreeWayOutputMergeTests.cs` — 2 tests

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
| Debug bounded | **304/304** passed |
| Release bounded | **304/304** passed |
| Phase-1 checkpoint | **ALL PASSED** |

## Known limitations

- Output is initialized from base only (no empty-output alternative documented in UI).
- Save uses new-file atomic write; overwriting an existing file at the chosen path is not supported.
- Manual output edits mark all difference regions stale globally (no per-region diff against rebuilt text).
- Recompare refreshes output from resolutions but does not auto-resolve new divergence.

## Remaining risks

- Complex multi-region edits followed by recompare may need richer per-region stale detection as ADV-09 folder workflows land.
