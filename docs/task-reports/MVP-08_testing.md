# MVP-08 Testing Report

**Task:** Verify navigation, manual 2-way merge commands, and safe-save UI
**Date:** 2026-07-02
**Verdict:** **Pass** (verified at Phase-1 checkpoint; report created retroactively)

## Commands run

| Command | Exit | Result |
|---------|------|--------|
| `dotnet restore` | 0 | OK |
| `dotnet build -c Debug` | 0 | OK |
| `dotnet test -c Debug` | 0 | 203 tests passed |
| `dotnet build -c Release` | 0 | OK (prior checkpoint) |
| `dotnet test -c Release` | 0 | 203 tests passed (prior checkpoint) |

## Automated coverage exercised

| Area | Tests |
|------|-------|
| Navigation | `DifferenceNavigatorTests` |
| Merge hunks | `ManualTwoWayMergeServiceTests` |
| Safe save | `DocumentPersistenceServiceTests`, `SafeTextFileWriterTests` |
| Stale VM flow | `FileCompareStaleFlowTests` |
| Copy + save integration | `MergeAndSaveIntegrationTests` |

## Manual / UI verification (`build/phase1_checkpoint.ps1`)

| Scenario | Result |
|----------|--------|
| Modified hunks compared (mods fixtures) | PASS |
| Next/previous navigation, no wrap at last | PASS |
| Inserted hunk copy-to-left + recompare | PASS |
| Modified hunk copy-to-right + save right to disk | PASS |
| Merge disabled while stale (after copy) | PASS |

Save conflict, read-only target, and dirty-close MessageBox flows are covered by automated
tests (`DocumentPersistenceServiceTests`, `ManualTwoWayMergeServiceTests`,
`MainWindowViewModel` close paths) rather than UI automation dialogs.

## Architecture / licence checks

- No new packages; `docs/DEPENDENCIES.md` unchanged.
- Merge/save logic remains in Application layer; WPF forwards intent only.

## Defects

None blocking.

## Recommendation

**Proceed** — MVP-08 implementation and evidence are complete. Paired with MVP-09/MVP-10
for integrated MVP gate (MVP-11).
