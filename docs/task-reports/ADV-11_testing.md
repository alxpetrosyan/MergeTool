# ADV-11 Testing Report

**Task:** Verify settings, recents, and pinned comparison entries  
**Date:** 2026-07-02  
**Verdict:** **PASS** — proceed to ADV-12

## Baseline checks

| Check | Result |
|-------|--------|
| `dotnet restore` | OK |
| `dotnet build -c Debug` | OK |
| `dotnet test -c Debug` (bounded) | **328/328** passed |
| `dotnet build -c Release` | OK |
| `dotnet test -c Release` (bounded) | **328/328** passed |
| `docs/DEPENDENCIES.md` | Unchanged (no new packages) |
| `build/phase1_checkpoint.ps1` | **ALL PASSED** |

## Automated coverage

| Fixture | Tests |
|---------|-------|
| Settings roundtrip + version | `AppSettingsStoreTests` |
| Corrupt JSON fallback + `MT-SET-001` diagnostic | `AppSettingsStoreTests` |
| Schema migration from version 0 | `AppSettingsStoreTests` |
| Recent dedupe, ordering, max cap | `AppSettingsStoreTests` |
| Reset clears recents and pinned | `AppSettingsStoreTests` |
| JSON stores paths only (no content) | `AppSettingsStoreTests` |
| Shell records and displays recents | `MainWindowSessionHistoryTests` |
| Missing paths disable open | `MainWindowSessionHistoryTests` |
| Pin active + reset settings | `MainWindowSessionHistoryTests` |

## Manual verification notes

- Shell bar shows **Recents** and **Pinned** dropdowns with Open buttons.
- **Pin active**, **Clear recents**, and **Reset settings** are visible and explicit.
- Corrupt settings file shows a user-visible status/diagnostic on launch.
- Successful file/folder compares append to recents after scan/compare completes.

## Defects

None found in automated suite.

## Recommendation

**Proceed** with ADV-12 (JSON/XML formatted comparison projection integration). ADV-11 settings and session history are complete.
