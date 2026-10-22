# ADV-08 Testing Report

**Task:** Verify manual 3-way output merge and resolution workflow  
**Date:** 2026-07-02  
**Verdict:** **PASS** — proceed to ADV-09

## Baseline checks

| Check | Result |
|-------|--------|
| `dotnet restore` | OK |
| `dotnet build -c Debug` | OK |
| `dotnet test -c Debug` (bounded) | **304/304** passed |
| `dotnet build -c Release` | OK |
| `dotnet test -c Release` (bounded) | **304/304** passed |
| `docs/DEPENDENCIES.md` | Unchanged (no new packages) |
| `build/phase1_checkpoint.ps1` | **ALL PASSED** |

## Automated coverage

| Fixture | Tests |
|---------|-------|
| Output initialized from base; differences unresolved | `ThreeWayOutputMergeSessionTests` |
| Apply left/base/right choice copies correct region text | `ThreeWayOutputTextBuilderTests`, `ThreeWayOutputMergeSessionTests` |
| Manual output edit marks regions stale | `ThreeWayOutputMergeSessionTests`, `ThreeWayOutputMergeTests` |
| Source documents unchanged after resolution | `ThreeWayOutputMergeSessionTests`, `ThreeWayOutputMergeTests` |
| VM resolution display and output status | `ThreeWayOutputMergeTests` |

## Manual verification notes

- 3-way workspace shows a 4th **Output** pane (editable) alongside read-only base/left/right.
- **Use Base / Left / Right** buttons apply explicit per-region choices; region list shows resolution state.
- **Next unresolved** navigates to the next unresolved difference region.
- Manual typing in the output pane invalidates resolution (stale/unresolved status).
- **Save output** prompts for a path; sources are not modified.

## Defects

One WPF test assertion (`ApplyRegionChoice_UpdatesOutputWithoutChangingSources`) initially expected ADV-07 fixture text (`a\r\nleft\r\n`) while the gated reader supplies `base\r\nshared\r\n` sources. Fixed to expect `base\r\nleft\r\n`. No product defect.

## Recommendation

**Proceed** with ADV-09 (three-way folder comparison). ADV-08 manual output merge workflow is complete and tested.
