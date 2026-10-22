# ADV-06 Development Report

**Task:** Add manual folder copy/delete operations with preview  
**Date:** 2026-07-02  
**Verdict:** Complete

## Summary

Delivered safe, user-controlled folder synchronization for the Folder Compare workspace: immutable `FolderOperationPlan` with metadata preconditions, path-safety validation, byte-preserving copy, Recycle Bin delete on Windows, preview/confirm UI, progress/cancel via the existing workspace operation slot, partial-failure handling, and automatic recompare after successful operations.

## Changed files

### Folders (`MergeTool.Folders`)

- `Operations/FolderOperationPrecondition.cs` — length + last-write-time precondition
- `Operations/FolderPathSafety.cs` — relative-path and root-escape guards
- `Operations/FolderOperationPlanner.cs` — roots, preconditions, overwrite detection
- `Operations/FolderOperationPlanValidator.cs` — pre-execute validation
- `Operations/FolderOperationPlanPreviewFormatter.cs` — human-readable preview lines
- `Operations/IFolderFileOperations.cs` — copy/delete port
- `Operations/PhysicalFolderFileOperations.cs` — `File.Copy` + directory creation
- `Operations/RecycleBinSupport.cs` — Windows `SHFileOperation` (no new packages)
- `Operations/FolderOperationExecutor.cs` — precondition checks, per-item try/catch, cancellation

### WPF (`MergeTool.App.Wpf`)

- `Services/IFolderOperationPromptService.cs`, `WpfFolderOperationPromptService.cs`
- `Views/FolderOperationPreviewWindow.xaml(.cs)` — preview dialog
- `ViewModels/FolderCompareWorkspaceViewModel.cs` — copy/delete commands, operation flow, recompare
- `Views/FolderCompareView.xaml(.cs)` — operation buttons, extended row selection
- `ViewModels/MainWindowViewModel.cs` — wires prompt service

### Tests

- `tests/MergeTool.Folders.Tests/FolderPathSafetyTests.cs`
- `tests/MergeTool.Folders.Tests/FolderOperationPlannerTests.cs` — expanded
- `tests/MergeTool.Folders.Tests/FolderOperationPlanValidatorTests.cs`
- `tests/MergeTool.Folders.Tests/FolderOperationExecutorTests.cs`
- `tests/MergeTool.App.Wpf.Tests/FolderCompareOperationTests.cs`

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
| Debug bounded | **285/285** passed |
| Release bounded | **285/285** passed |
| Phase-1 checkpoint | **ALL PASSED** |

## Known limitations

- Preconditions use scan metadata (size + mtime), not content SHA-256.
- Recycle Bin is Windows-only; other platforms require explicit permanent-delete confirmation.
- Failed Recycle Bin deletes are reported as errors; no silent permanent fallback.
- Operations run synchronously on the operation thread (UI remains cancellable via workspace cancel).

## Remaining risks

- Very large multi-select batches may need a progress bar per item; current design reports via workspace busy text and diagnostics only.
