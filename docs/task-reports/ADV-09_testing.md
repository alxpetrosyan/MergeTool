# ADV-09 Testing Report

**Task:** Verify 3-way folder comparison  
**Date:** 2026-07-02  
**Verdict:** **PASS** — proceed to ADV-10

## Baseline checks

| Check | Result |
|-------|--------|
| `dotnet restore` | OK |
| `dotnet build -c Debug` | OK |
| `dotnet test -c Debug` (bounded) | **311/311** passed |
| `dotnet build -c Release` | OK |
| `dotnet test -c Release` (bounded) | **311/311** passed |
| `docs/DEPENDENCIES.md` | Unchanged (no new packages) |
| `build/phase1_checkpoint.ps1` | **ALL PASSED** |

## Automated coverage

| Fixture | Tests |
|---------|-------|
| All 8 presence combinations + metadata match/diff on triple | `QuickFolderComparisonEngineTests.ThreeWay_AllPresenceCombinations_*` |
| Duplicate roots rejected | `QuickFolderComparisonEngineTests.ThreeWay_DuplicateRoots_*` |
| `HasTripledFiles` paths | `QuickFolderComparisonEngineTests.ThreeWay_TripledFileRow_*` |
| VM root validation (3 distinct roots) | `ThreeWayFolderCompareWorkspaceViewModelTests` |
| Scan populates 3-way states | `ThreeWayFolderCompareWorkspaceViewModelTests` |
| 3-way file open gated + launcher paths | `ThreeWayFolderCompareWorkspaceViewModelTests` |
| 2-way folder compare regression | existing `FolderCompareWorkspaceViewModelTests`, `QuickFolderComparisonEngineTests` |

## Manual verification notes

- **3-Way Folder Compare** toolbar opens a workspace with Base / Left / Right folder pickers.
- Grid shows base/left/right size and modified columns with color-coded presence states.
- **Open 3-way text comparison** enabled only when all three file paths exist on the selected row.
- No copy/delete/sync buttons on the 3-way folder tab; sources are not modified by scanning.

## Defects

None found in automated suite.

## Recommendation

**Proceed** with ADV-10 (content-hash scan mode). ADV-09 3-way folder comparison is complete.
