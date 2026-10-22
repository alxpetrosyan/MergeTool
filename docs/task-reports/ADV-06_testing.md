# ADV-06 Testing Report

**Task:** Verify manual folder copy/delete operations with preview  
**Date:** 2026-07-02  
**Verdict:** **PASS** — proceed to ADV-07

## Baseline checks

| Check | Result |
|-------|--------|
| `dotnet restore` | OK |
| `dotnet build -c Debug` | OK |
| `dotnet test -c Debug` (bounded) | **285/285** passed |
| `dotnet build -c Release` | OK |
| `dotnet test -c Release` (bounded) | **285/285** passed |
| `docs/DEPENDENCIES.md` | Unchanged (no new packages) |
| `build/phase1_checkpoint.ps1` | **ALL PASSED** |

## Automated coverage

| Area | Tests |
|------|-------|
| Path traversal / root escape | `FolderPathSafetyTests` |
| Plan build (preconditions, overwrite, skip dirs) | `FolderOperationPlannerTests` |
| Identity mismatch, unsafe paths | `FolderOperationPlanValidatorTests` |
| Partial failure, cancellation, parent dir creation | `FolderOperationExecutorTests` |
| No execute before preview confirm | `FolderCompareOperationTests` |
| Copy + recompare on confirm | `FolderCompareOperationTests` |

## Manual verification notes

- Preview dialog lists exact actions with overwrite warnings before execution.
- Delete uses Recycle Bin on Windows (`RecycleBinSupport`); non-Windows path requires extra permanent-delete confirmation.
- Workspace cancel during `RunOperationAsync` propagates `OperationCanceledException` from the executor loop.
- Successful operations trigger `CompareAsync()` to refresh folder rows.

## Defects

None found in automated suite.

## Recommendation

**Proceed** with ADV-07 development. ADV-06 core and UI are complete with bounded tests green in Debug and Release.
