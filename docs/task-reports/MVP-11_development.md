# MVP-11 Development Report

**Task:** Stabilize integrated MVP and execute the Phase-1 release gate
**Date:** 2026-07-02
**Status:** Complete

## Objective completed

Integrated MVP stabilization only — no new product features. Release-gate documentation,
bounded test runner, expanded acceptance fixtures/script, and one end-to-end integration test.

## Deliverables created/updated

| Artifact | Purpose |
|----------|---------|
| `docs/mvp_acceptance.md` | Phase-1 gate checklist, commands, wording, scenarios |
| `docs/known_limitations.md` | Honest MVP boundaries |
| `docs/MVP_DEMO_SCRIPT.md` | <10 min non-developer walkthrough |
| `build/run_tests_bounded.ps1` | Sequential per-project test runner (stability) |
| `build/phase1_checkpoint.ps1` | Extended UI acceptance (delete/modified/identical/not-text) |
| `tests/TestData/CompareFixtures/delete_*.txt` | Deleted-hunk UI scenario |
| `tests/TestData/CompareFixtures/notext_nul.bin` | Not-text refusal scenario |
| `tests/MergeTool.Application.Tests/MvpEndToEndIntegrationTests.cs` | Folder row → paired file compare |

## Memory / stability investigation

| Observation | Detail |
|-------------|--------|
| Earlier failure | `OutOfMemoryException` during full-solution `dotnet test` (prior session) |
| MVP-11 reproduction attempt | Single `dotnet test -c Debug` (all assemblies) → **VSTEST protocol timeout** after 90s (not OOM) |
| Bounded runs | `run_tests_bounded.ps1` Debug + Release → **204/204 passed** each |
| Expensive groups alone | Application merge/save/open (25), Folders (11), App.Wpf (35) → all passed |
| Code review | No retained comparison sessions in tests; fixtures are small; parallelism is MSTest method-level across concurrent test hosts when solution-wide test runs |

**Conclusion:** Failure is **non-reproducible as application leak** under bounded runs. Likely **environment + parallel test host pressure**. **Not claimed permanently fixed.** Release gate uses `run_tests_bounded.ps1`.

## Files changed (MVP-11)

- `docs/mvp_acceptance.md`, `docs/known_limitations.md`, `docs/MVP_DEMO_SCRIPT.md`
- `build/run_tests_bounded.ps1`, `build/mvp11_acceptance.ps1`, `build/phase1_checkpoint.ps1`
- `tests/TestData/CompareFixtures/delete_left.txt`, `delete_right.txt`, `notext_nul.bin`
- `tests/MergeTool.Application.Tests/MvpEndToEndIntegrationTests.cs`
- `tests/MergeTool.Application.Tests/MergeAndSaveIntegrationTests.cs` (shared `TestRepoRoot`)

## Build/test commands and results

```powershell
powershell -File build/run_tests_bounded.ps1 -Configuration Debug   # 204/204 pass
powershell -File build/run_tests_bounded.ps1 -Configuration Release # 204/204 pass
dotnet build -c Release                                              # pass
powershell -File build/phase1_checkpoint.ps1                           # ALL CHECKS PASSED
```

## Known limitations

Documented in `docs/known_limitations.md`. No vertical padding, metadata-only folders, no binary compare, bounded test runner recommended on constrained machines.

## Remaining risks

- Full-solution `dotnet test` may still fail on low-memory hosts.
- WPF DataGrid row selection not UI-automatable; paired open validated via integration + VM tests.
- Background WMI/memory diagnostic commands previously hung — do not use in CI.

## Recommendation

**Phase 1 accepted** for Scope 2 entry, subject to product-owner sign-off on documented environment risk.

## Dependencies introduced

None.
