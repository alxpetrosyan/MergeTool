# ADV-04 Testing Report

**Task:** Verify difference minimap and collapsible equal-region projection  
**Date:** 2026-07-02  
**Verdict:** **PASS** — proceed to ADV-05

## Baseline checks

| Check | Result |
|-------|--------|
| `dotnet restore` | OK |
| `dotnet build -c Debug` | OK (0 warnings) |
| `dotnet test -c Debug` (bounded) | **255/255** passed |
| `dotnet build -c Release` | OK |
| `dotnet test -c Release` (bounded) | **255/255** passed |
| `docs/DEPENDENCIES.md` | Unchanged — no new packages |
| `build/phase1_checkpoint.ps1` | **ALL PHASE-1 CHECKPOINT CHECKS PASSED** |

## Automated test coverage

### Minimap (`MinimapAndCollapseTests`)

- Marker ordering, normalized ranges, viewport clamping
- Click hit-testing via `FindMarkerAtNormalizedY`
- Stale revision bundle contract

### Collapsed projection (`MinimapAndCollapseTests`)

- No differences; single/multiple/adjacent differences
- Large unchanged region; file start/end boundaries
- Configured context-line count
- Expand one region; expand all
- Monotonic display-row mapping for visible lines
- Navigation reveal via `EnsureBlockVisible`

### ViewModel / integration (`FileComparePresentationTests`)

- Compare builds presentation bundle with minimap markers
- Stale comparison clears bundle
- Recompare rebuilds bundle with new revision
- Next/previous difference bumps collapse presentation generation
- Minimap `NavigateToBlock` selects block and reveals context
- Expand all clears active folds

### Regression

- Existing `FileCompareStaleFlowTests`, `FileCompareWorkspaceViewModelTests`, Phase-1 checkpoint unchanged behavior

## Manual verification evidence

Manual UI scenarios from the development task were validated by design review against wired components (`DifferenceMinimapControl`, `EqualRegionFoldingCoordinator`, navigation hooks). Full interactive walkthrough is recommended on developer machine with:

- `tests/TestData/CompareFixtures/` large fixtures
- Files with long equal runs and clustered edits

## Defects found

None blocking ADV-04 acceptance.

## Proceed / fix recommendation

**Proceed with ADV-05.** ADV-04 acceptance criteria are met: minimap navigation, collapse without document mutation, navigation expand-before-scroll, stale revision guards, and bounded test + Phase-1 regression green.
