# MVP-09 Development Report

**Task:** Implement recursive 2-way quick folder comparison engine with accurate states
**Date:** 2026-07-02
**Status:** Complete

## Feature boundary implemented

Quick metadata folder comparison in `MergeTool.Folders`: recursive scan of two roots,
deterministic row model, accurate states, cancellation/progress hooks, and no file-body
reads. No folder UI, no content hashing, no copy/delete, no default exclusions, no tree
widget.

## Objective completed

### Domain model (`MergeTool.Folders`)

- `FolderRowState` — `LeftOnly`, `RightOnly`, `MetadataMatch`, `MetadataDifferent`,
  `Error`, `SkippedReparsePoint`.
- `FolderComparisonRequest`, `FolderComparisonOptions`, `FolderComparisonRow`,
  `FolderComparisonResult`, `FolderComparisonProgress`.
- `FolderRowStateLabels` — display names; `MetadataMatch` is never labelled as content-
  identical. Includes the mandated explanation string for UI reuse.

### Engine

- `IFolderScanAccess` abstraction plus `PhysicalFolderScanAccess` (System.IO metadata only).
- `QuickFolderComparisonEngine` — validates roots (exist, are directories, differ after
  normalization), recursively scans both sides, merges paths with Windows
  ordinal-ignore-case keys while retaining left-side display casing, compares file rows on
  length + `LastWriteTimeUtc` only, skips reparse-point directories without descending,
  emits row diagnostics for inaccessible paths without aborting the scan, sorts rows
  deterministically (ordinal-ignore-case path, ordinal tie-breaker, then state).

### Application orchestration

- `RunFolderComparisonUseCase` in `MergeTool.Application` — schedules the synchronous
  engine on a background task and converts cancellation to controlled results.

## Files/projects changed

- `src/MergeTool.Folders/*` — new engine and model types
- `src/MergeTool.Application/UseCases/RunFolderComparisonUseCase.cs`
- `tests/MergeTool.Folders.Tests/QuickFolderComparisonEngineTests.cs`

## Design decisions

- **Metadata-only proof:** `MetadataMatch` explicitly means size + UTC timestamp match; the
  engine never reads file bytes during scan.
- **Testable IO:** `IFolderScanAccess` allows simulated enumeration failures and reparse
  behaviour without real junctions.
- **Error rows before one-sided classification:** rows with scan diagnostics use `Error`
  even when only one side contributed path information.

## Tests added/updated

`QuickFolderComparisonEngineTests` covers metadata match/different, nested left/right-only,
case-insensitive merge with left casing retained, simulated directory failure with other
rows preserved, cancellation, same-root rejection, missing root, deterministic sort, and
reparse-point skip via fake access.

## Build/test commands and results

```powershell
dotnet build -c Debug    # succeeded
dotnet test -c Debug     # 12/12 Folders tests passed (202 total solution-wide)
dotnet build -c Release  # succeeded
dotnet test -c Release   # 12/12 Folders tests passed (202 total solution-wide)
```

## Known limitations / deliberate deferrals

- Directory rows appear for empty directories, reparse points, and errors; matching
  interior directories without children are not emitted (flat file grid focus).
- Content/hash comparison, folder operations, and 3-way folders remain out of scope.

## Dependencies introduced

None.
