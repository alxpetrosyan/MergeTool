# POL-02 Development Report

**Task:** Unify diagnostics and safe failure modes  
**Date:** 2026-07-02  
**Verdict:** Complete

## Summary

Normalized user-visible failure presentation across workspaces: centralized recovery hints for all production diagnostic codes, shared cancellation/unexpected-failure factories, clipboard copy support, a reusable WPF diagnostics panel on every workspace view, and safe exception handling in the workspace operation lifecycle.

## Changed files

### Core (`MergeTool.Core`)

- `Diagnostics/DiagnosticRecovery.cs` — recovery hints keyed by `MT-*` code
- `Diagnostics/OperationDiagnostics.cs` — `MT-OP-001` cancelled, `MT-UNX-001` unexpected
- `Diagnostic.cs` — `ToCopyPayload()` for clipboard export

### WPF (`MergeTool.App.Wpf`)

- `ViewModels/DiagnosticViewModel.cs` — code, recovery hint, copy command
- `ViewModels/WorkspaceViewModel.cs` — `HasDiagnostics`, cancellation/unexpected catch paths
- `Views/DiagnosticsPanel.xaml` / `.xaml.cs` — shared diagnostics UI
- `Views/FileCompareView.xaml`, `FolderCompareView.xaml`, `ThreeWayCompareView.xaml`, `ThreeWayFolderCompareView.xaml` — use `DiagnosticsPanel`

### Tests

- `tests/MergeTool.Core.Tests/DiagnosticRecoveryTests.cs` — new
- `tests/MergeTool.Application.Tests/SafeFailureModesTests.cs` — new
- `tests/MergeTool.App.Wpf.Tests/DiagnosticViewModelTests.cs` — new

### Docs

- `docs/architecture/diagnostics.md` — new conventions guide
- `docs/architecture/README.md` — POL-02 section
- `docs/SCOPE3_PROGRESS.md`

## Design decisions

| Decision | Rationale |
|----------|-----------|
| Recovery hints in Core, not WPF | Testable without UI; hints are product copy, not view logic |
| `DiagnosticsPanel` binds to workspace `Diagnostics` | One panel, four views; matches POL-01 `ReplaceDiagnostics` ownership |
| Default cancelled diagnostic only when result list is empty | Avoid duplicating session-level cancellation messages |
| Unexpected `catch` in `RunOperationAsync` | Prevents unhandled exceptions from crashing tabs without a user-safe message |

## Commands run

```powershell
.\build\run_tests_bounded.ps1
.\build\run_tests_bounded.ps1 -Configuration Release
.\build\phase1_checkpoint.ps1
```

## Test results

| Configuration | Result |
|---------------|--------|
| Debug bounded | **365/365** passed |
| Release bounded | **365/365** passed |
| Phase-1 checkpoint | **ALL PASSED** |

## Dependencies

`docs/DEPENDENCIES.md` unchanged — no new packages.
