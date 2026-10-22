# ADV-05 Testing Report

**Task:** Verify search, go-to-line, and difference list  
**Date:** 2026-07-02  
**Verdict:** **PASS** — proceed to ADV-06

## Baseline checks

| Check | Result |
|-------|--------|
| `dotnet restore` | OK |
| `dotnet build -c Debug` | OK |
| `dotnet test -c Debug` (bounded) | **274/274** passed |
| `dotnet build -c Release` | OK |
| `dotnet test -c Release` (bounded) | **274/274** passed |
| `docs/DEPENDENCIES.md` | Unchanged — no new packages |
| `build/phase1_checkpoint.ps1` | **ALL PHASE-1 CHECKPOINT CHECKS PASSED** |

## Automated evidence

### Text search (`TextSearchServiceTests`)

- Literal, ignore case, whole word, changed-lines-only, pane filter, invalid regex, cancellation

### Go-to-line (`GoToLineServiceTests`)

- Valid line, out of range, below 1, empty document

### Difference list (`DifferenceListBuilderTests`, `ChangedLinesIndexTests`)

- Ordered entries, anchor alignment, per-side changed lines

### ViewModel / integration (`FileCompareSearchTests`)

- Difference list populated after compare
- Find next raises line navigation
- Invalid regex diagnostic
- Go-to-line out of range and valid navigation
- List selection navigates to block
- Next difference syncs list selection

## Manual verification (recommended)

1. Compare two files with several differences — list shows all blocks; clicking an item scrolls both panes.
2. Search for a literal in changed-lines-only mode — matches only on difference lines.
3. Enter an invalid regex — diagnostic appears; comparison unchanged.
4. Go to line beyond file end — bound error shown.
5. Use Next difference — list highlight follows current block.

## Defects found

None blocking ADV-05 acceptance.

## Proceed / fix recommendation

**Proceed with ADV-06.** ADV-05 acceptance criteria are met.
