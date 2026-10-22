# ADV-13 Testing Report

**Task:** Verify moved-block and renamed-file assistance  
**Date:** 2026-07-02  
**Verdict:** **PASS** — Scope 2 advanced tasks complete; proceed to Scope 3 polish (POL-01..08)

## Baseline checks

| Check | Result |
|-------|--------|
| `dotnet restore` | OK |
| `dotnet build -c Debug` | OK |
| `dotnet test -c Debug` (bounded) | **348/348** passed |
| `dotnet build -c Release` | OK |
| `dotnet test -c Release` (bounded) | **348/348** passed |
| `docs/DEPENDENCIES.md` | Unchanged (no new packages) |
| `build/phase1_checkpoint.ps1` | **ALL PASSED** |

## Automated coverage

| Fixture | Tests |
|---------|-------|
| Moved deleted/inserted pair detection | `MovedBlockDetectorTests` |
| Oversized block skip | `MovedBlockDetectorTests` |
| Session moved ordinals when option enabled | `TwoWayComparisonSessionTests` |
| Difference list “— moved” suffix | `DifferenceListBuilderTests` |
| Pane moved-line marker model | `PaneRenderModelTests` |
| Content-hash rename candidate across paths | `FolderRenameCandidateFinderTests` |
| Name-similarity fallback | `FolderRenameCandidateFinderTests` |
| Find candidates requires completed scan | `FolderCompareWorkspaceViewModelTests` |
| Accept opens cross-path compare only | `FolderCompareWorkspaceViewModelTests` |

## Manual verification notes

- **Comparison rules → Detect moved blocks** + Recompare shows purple gutter on moved delete/insert lines.
- Difference list entries include “— moved” where applicable; modified hunks stay unmarked.
- **Folder Compare → Find rename candidates** lists suggestions without changing grid rows.
- **Compare files** on a candidate opens file compare at mismatched relative paths.
- No folder copy/delete/rename runs from rename-candidate flow.

## Defects

None found in automated suite.

## Recommendation

**Proceed** to Scope 3 polish tasks (POL-01..08). ADV-13 assistance features are complete and non-authoritative.
