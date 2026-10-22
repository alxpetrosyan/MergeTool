# POL-01 Testing Report

**Task:** Verify architecture hardening and public API review  
**Date:** 2026-07-02  
**Verdict:** **PASS** — proceed to POL-02

## Baseline checks

| Check | Result |
|-------|--------|
| `dotnet restore` | OK |
| `dotnet build -c Debug` | OK |
| `dotnet test -c Debug` (bounded) | **355/355** passed |
| `dotnet build -c Release` | OK |
| `dotnet test -c Release` (bounded) | **355/355** passed |
| `docs/DEPENDENCIES.md` | Unchanged |
| `build/phase1_checkpoint.ps1` | **ALL PASSED** |

## Regression and architecture verification

| Area | Evidence |
|------|----------|
| Dependency direction | `ArchitectureGuardrailsTests` — no WPF/AvalonEdit refs in Core/Text/Folders/FileSystem/Application |
| Application ↔ WPF | Application does not reference `MergeTool.App.Wpf` |
| Domain → Core only | Text/Folders/FileSystem reference Core (+ BCL) |
| Workflows | Phase-1 checkpoint: file compare, merge, save, folder compare, paired open |
| Public API docs | `docs/architecture/public-api-and-dependencies.md` documents moves and visibility |
| No needless abstraction | No DI container, mediator, or plugin layer added |

## Manual / behavioral notes

- `OpenPairedFileComparisonRequest` consumed from Application namespace in folder VM tests — paired open flow unchanged.
- Shell startup via `AppCompositionRoot.CreateShell()` — same workspace factories as before refactor.

## Defects

None created.

## Recommendation

**Proceed** with POL-02 (unify diagnostics and safe failure modes).
