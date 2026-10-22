# ADV-01 Development Report

**Task:** Add bounded intraline diff rendering  
**Date:** 2026-07-02  
**Status:** Complete

## Objective completed

Bounded intraline diff for **Modified** line pairs only: optional immutable segment data on
`TwoWayTextComparisonResult`, LCS-based character diff with documented bounds, whole-line
fallback when limits are exceeded, and AvalonEdit overlay rendering without document mutation.
Stale comparisons clear inline overlays with whole-line overlays.

## Design decisions

| Decision | Rationale |
|----------|-----------|
| Post-diff enricher (`IntralineDiffEnricher`) | Keeps `PatienceLineDiffEngine` unchanged; intraline work is optional and isolated |
| LCS on Unicode scalar values (UTF-16 spans) | Surrogate pairs are never split; offsets match AvalonEdit document coordinates |
| `WholeLineFallback` on bound exceed | Avoids expensive work and UI freeze on long lines |
| `DocumentColorizingTransformer` | Renders inline spans without altering `TextDocument` text |
| `TextComparisonOptions.WithIntralineDiff` | Session enables intraline for UI; `Default` remains off for existing tests |

**Bounds (defaults):** `MaxLineLength = 4096`, `MaxWorkBudget = 262_144` (element-count product).

**Rejected:** DiffPlex or other packages (policy); mutating editor text for highlights; intraline on inserted/deleted-only lines (out of scope).

## Files changed

### New — `MergeTool.Text`

- `Diff/InlineDiffSegmentKind.cs`, `InlineDiffSegment.cs`, `InlineDiffLinePairResult.cs`
- `Diff/ModifiedLineInlineDiff.cs`, `IntralineDiffBounds.cs`
- `Diff/BoundedIntralineDiffCalculator.cs`, `IntralineDiffEnricher.cs`

### New — `MergeTool.App.Wpf`

- `Rendering/IntralineDiffColorizingTransformer.cs`

### New — tests

- `tests/MergeTool.Text.Tests/BoundedIntralineDiffCalculatorTests.cs`
- `tests/MergeTool.Text.Tests/IntralineDiffEnricherTests.cs`
- `tests/MergeTool.App.Wpf.Tests/PaneRenderModelIntralineTests.cs`

### Updated

- `TextComparisonOptions.cs` — `EnableIntralineDiff`, `IntralineBounds`, `WithIntralineDiff`
- `TwoWayTextComparisonResult.cs` — optional `InlineDiffs`, `WithInlineDiffs`
- `TwoWayComparisonSession.cs` — enrich after line diff
- `PaneRenderModel.cs` — per-line inline model
- `DiffLineBackgroundRenderer.cs` — `IntralineChangedBackground` palette entry
- `FileCompareView.xaml.cs` — wire colorizers; clear on stale

## Tests added

| Test area | Coverage |
|-----------|----------|
| `BoundedIntralineDiffCalculatorTests` | Replacement, insertion, deletion, whitespace, surrogate pairs, long-line fallback, work-budget fallback |
| `IntralineDiffEnricherTests` | Disabled path; modified-block pairing |
| `PaneRenderModelIntralineTests` | Whole-line Modified preserved on fallback; inline mapping per line |

## Build/test commands and results

```powershell
dotnet restore                                    # pass
dotnet build -c Debug                             # pass (0 warnings)
dotnet test -c Debug                              # 215/215 pass
dotnet build -c Release                           # pass
dotnet test -c Release                            # 215/215 pass
powershell -File build/run_tests_bounded.ps1 -Configuration Debug    # 215/215 pass
powershell -File build/run_tests_bounded.ps1 -Configuration Release  # 215/215 pass
```

## Dependencies

**None added.** `docs/DEPENDENCIES.md` unchanged.

## Known limitations / deferrals

- Intraline diff applies only to **paired lines inside Modified blocks** (not unpaired extra lines in asymmetric modified hunks).
- LCS alignment among duplicate characters (e.g. repeated spaces) may not match human intuition for offset, but whitespace differences are detected.
- No word-level algorithm; character/scalar LCS only (per task guardrails).
- Inline highlight is a second-layer background color on top of modified line amber.

## Proceed recommendation

Paired testing task (ADV-01_testing) may run. ADV-02 is not started.
