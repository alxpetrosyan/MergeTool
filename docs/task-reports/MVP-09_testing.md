# MVP-09 Testing Report

**Task:** Verify recursive 2-way quick folder comparison engine
**Date:** 2026-07-02
**Verdict:** **Pass**

## Commands run

| Command | Exit | Result |
|---------|------|--------|
| `dotnet restore` | 0 | OK |
| `dotnet build -c Debug` | 0 | OK |
| `dotnet test -c Debug` | 0 | All tests passed |
| `dotnet build -c Release` | 0 | OK |
| `dotnet test -c Release` | 0 | All tests passed |

## Automated coverage exercised

- `MergeTool.Folders.Tests` (12 tests): metadata match/different, nested left/right-only,
  inaccessible path row with other rows retained, cancellation, same-root rejection,
  deterministic sort, reparse skip (fake access), case behaviour, missing root.

## Manual scenarios

Not required for this engine-only task. Code review confirmed `PhysicalFolderScanAccess`
uses `FileInfo` metadata APIs only (no `File.ReadAllBytes` or stream reads of bodies).

## Architecture / licence checks

- `MergeTool.Folders` references `MergeTool.Core` only (smoke test still passes).
- No new packages; `docs/DEPENDENCIES.md` unchanged.

## Defects

None.

## Recommendation

**Proceed** to MVP-10 (folder workspace UI).
