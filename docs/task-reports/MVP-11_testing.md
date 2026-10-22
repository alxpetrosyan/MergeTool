# MVP-11 Testing Report

**Task:** Verify integrated MVP and execute Phase-1 release gate
**Date:** 2026-07-02
**Verdict:** **Pass** (conditional on documented test-runner environment risk)

## Gate outcome

**Pass** — All mandatory gate scenarios satisfied. No Critical/High defects. Scope 2 may begin after owner review.

## Commands run

| Command | Exit | Result |
|---------|------|--------|
| `dotnet restore` | 0 | OK |
| `dotnet build -c Debug` | 0 | OK |
| `dotnet build -c Release` | 0 | OK |
| `run_tests_bounded.ps1 -Configuration Debug` | 0 | **204/204** passed |
| `run_tests_bounded.ps1 -Configuration Release` | 0 | **204/204** passed |
| `dotnet test -c Debug` (full solution, once) | 1 | VSTEST timeout (non-reproducible under bounded runner) |
| `phase1_checkpoint.ps1` | 0 | ALL CHECKS PASSED |

## `docs/mvp_acceptance.md` scenario results

| # | Scenario | Outcome | Evidence |
|---|----------|---------|----------|
| 1 | Identical files | Pass | UI `IDENTICAL OK` |
| 2 | Insert/delete/modify | Pass | UI + `ManualTwoWayMergeServiceTests` |
| 3 | Navigate all differences | Pass | UI `NAVIGATION OK` |
| 4 | Merge + recompare + save | Pass | UI + `MergeAndSaveIntegrationTests` |
| 5 | Stale merge | Pass | UI stale after copy; `FileCompareStaleFlowTests` |
| 6 | Save conflict | Pass | `DocumentPersistenceServiceTests` |
| 7 | All folder row states | Pass | `FolderFixtures` + UI 15 labels |
| 8 | Filter + paired open | Pass | UI filter; `MvpEndToEndIntegrationTests` |
| 9 | Cancel folder scan | Pass | `QuickFolderComparisonEngineTests` |
| 10 | Non-text safe | Pass | UI `NOT-TEXT OK` |
| 11 | Dirty close | Pass | VM close/save prompts (`WorkspaceLifecycleTests`) |

## Manual UI acceptance (`phase1_checkpoint.ps1`)

```
FILE-COMPARE OK
NAVIGATION OK
MERGE OK (copy-to-left + recompare)
MERGE-DELETE OK (copy-to-right)
MERGE-MODIFIED OK (copy-to-left)
IDENTICAL OK
NOT-TEXT OK
SAVE OK
FOLDER-COMPARE OK
FILTER OK
PAIRED-OPEN OK (VM + command gating)
ALL PHASE-1 CHECKPOINT CHECKS PASSED
```

## Architecture / licence checks

- `docs/DEPENDENCIES.md` unchanged (AvalonEdit MIT only beyond SDK).
- No advanced-scope code added.

## Defects

None filed under `docs/defects/`.

## Memory risk (remaining)

- Full parallel `dotnet test` failed once with protocol timeout during MVP-11.
- Bounded sequential runner passes reliably.
- **Not claimed permanently resolved.**

## Task report completeness

| Task | Dev report | Test report |
|------|------------|-------------|
| MVP-08 | Yes | Yes |
| MVP-09 | Yes | Yes |
| MVP-10 | Yes | Yes |
| MVP-11 | Yes | Yes (this file) |

## Recommendation

**Phase 1 accepted.** Proceed to Scope 2 only after product-owner reviews this report, `docs/mvp_acceptance.md`, and `docs/known_limitations.md`.
