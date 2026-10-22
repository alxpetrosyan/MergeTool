# ADV-11 Development Report

**Task:** Add settings, recents, and pinned comparison entries  
**Date:** 2026-07-02  
**Verdict:** Complete

## Summary

Extended versioned JSON settings with atomic write, corruption diagnostics, recent/pinned session lists, and shell UI to open, pin, clear, and reset entries. Successful comparisons record paths (and optional folder comparison options) only—never file contents or hashes. Settings load from `%LocalAppData%\MergeTool\mergetool.settings.json`.

## Changed files

### Core (`MergeTool.Core`)

- `Settings/AppSettingsStore.cs` — `LoadWithDiagnostics`, `ComparisonSessionHistory`, extended entry records
- `Settings/AppSettingsMutations.cs` — migrate, dedupe recents, pin, reset, path validation, options JSON
- `Settings/ComparisonSessionKinds.cs` — session kind constants + `StoredComparisonOptions`

### WPF (`MergeTool.App.Wpf`)

- `Services/IComparisonSessionRecorder.cs`
- `ViewModels/MainWindowViewModel.cs` — recents/pinned UI state, open/pin/reset commands
- `ViewModels/SessionListItemViewModel.cs`, `SessionLauncher.cs`, `SessionFolderOptionsApplier.cs`
- `ViewModels/FileCompareWorkspaceViewModel.cs` (+ folder/3-way VMs) — record on successful compare
- `MainWindow.xaml` — recents/pinned bar with reset controls

### Tests

- `tests/MergeTool.Core.Tests/AppSettingsStoreTests.cs` — expanded
- `tests/MergeTool.App.Wpf.Tests/MainWindowSessionHistoryTests.cs` — new

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
| Debug bounded | **328/328** passed |
| Release bounded | **328/328** passed |
| Phase-1 checkpoint | **ALL PASSED** |

## Known limitations

- Pin-active command does not re-evaluate when workspace paths change until tab focus changes.
- Folder comparison options persisted are mode + timestamp tolerance only (not content-hash cache).
- No cloud sync or cross-machine settings.

## Remaining risks

- Very large recent lists are capped at 20; users with more may want configurable cap in a future settings panel.
