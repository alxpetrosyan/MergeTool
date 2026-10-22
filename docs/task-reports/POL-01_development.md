# POL-01 Development Report

**Task:** Harden architecture and review public APIs  
**Date:** 2026-07-02  
**Verdict:** Complete

## Summary

Reviewed solution dependencies, public API surface, cancellation/diagnostics ownership, and WPF coupling. Applied focused refactors with measurable simplification: centralized shell composition, moved a shared request DTO to Application, consolidated diagnostic binding, tightened internal helpers, and added automated architecture guardrail tests.

## Changed files

### Application (`MergeTool.Application`)

- `Requests/OpenPairedFileComparisonRequest.cs` — moved from WPF view-model file

### WPF (`MergeTool.App.Wpf`)

- `Composition/AppCompositionRoot.cs` — new; owns infrastructure wiring
- `ViewModels/MainWindowViewModel.cs` — `CreateDefault()` delegates to composition root; removed unused usings
- `ViewModels/WorkspaceViewModel.cs` — `ReplaceDiagnostics` helper
- `ViewModels/FileCompareWorkspaceViewModel.cs`, `FolderCompareWorkspaceViewModel.cs`, `ThreeWayCompareWorkspaceViewModel.cs` — use `ReplaceDiagnostics`

### Folders (`MergeTool.Folders`)

- `FolderRenameCandidateFinder.cs` — `AreNamesSimilar` made `internal`
- `MergeTool.Folders.csproj` — `InternalsVisibleTo` for tests

### Tests

- `tests/MergeTool.Application.Tests/ArchitectureGuardrailsTests.cs` — new

### Docs

- `docs/architecture/public-api-and-dependencies.md` — new
- `docs/architecture/README.md` — POL-01 section
- `docs/SCOPE3_PROGRESS.md`

## Design decisions

| Decision | Rationale |
|----------|-----------|
| Keep `Task.Run` in comparison sessions | Sessions are application orchestration; moving to use cases is behavior-neutral deferral |
| No DI container | Guardrails explicitly forbid unless proven necessary |
| `AppCompositionRoot` vs inline `CreateDefault` | Single wiring location; view models stay injectable |
| Reject generic workspace interface | No measurable simplification |

## Commands run

```powershell
.\build\run_tests_bounded.ps1
.\build\run_tests_bounded.ps1 -Configuration Release
.\build\phase1_checkpoint.ps1
```

## Test results

| Configuration | Result |
|---------------|--------|
| Debug bounded | **355/355** passed |
| Release bounded | **355/355** passed |
| Phase-1 checkpoint | **ALL PASSED** |

## Known limitations

- WPF view models still reference Text/Folders types directly for presentation; documented as intentional.
- Architecture tests verify assembly references, not every `public` member policy.

## Remaining risks

- Future features should add guardrail tests when introducing new project references.
