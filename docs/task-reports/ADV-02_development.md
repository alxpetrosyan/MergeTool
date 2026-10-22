# ADV-02 Development Report

**Task:** Add comparison rules with projection-safe normalization  
**Date:** 2026-07-02  
**Status:** Complete

## Objective completed

Explicit comparison rules (whitespace modes, ignore case, ignore empty lines, ignored-line regex,
ignored-block start/end regex) with a **projection-safe** pipeline: normalized/projected lines
feed the diff engine; results map back to **original** line indices for rendering, navigation,
and merge. Invalid regex and overlapping/unclosed ignored blocks return diagnostics without
mutating documents. UI exposes a rules panel and active-rule badge; rule changes require
explicit Recompare.

## Design decisions

| Decision | Rationale |
|----------|-----------|
| `ComparisonProjector` pre-diff layer | Keeps `PatienceLineDiffEngine` unchanged; original `TextLine` arrays remain display/merge source |
| `ComparisonProjectionMapper` splits non-contiguous originals | Ignored lines can leave gaps; equal/modified blocks must not span skipped originals |
| Session-owned `AppliedComparisonOptions` | Staleness and merge preconditions stay in original line space |
| Pending vs applied rules in VM | Task requires no invisible rule application |
| `OperationStatus.Failed` on projection errors | Invalid regex does not replace last result |

**Whitespace modes:** Exact, TrimEnd, TrimAll, IgnoreAll (remove all whitespace characters).

**Diagnostics:** `MT-CMP-004` invalid regex, `MT-CMP-005` overlapping ignored blocks, `MT-CMP-006` unclosed ignored block.

## Files changed

### New — `MergeTool.Text`

- `Diff/WhitespaceComparisonMode.cs`
- `Diff/LineComparisonNormalizer.cs`
- `Diff/ComparisonProjection.cs`, `ComparisonProjectionBuildResult` (in `ComparisonProjector.cs`)
- `Diff/ComparisonProjector.cs`
- `Diff/ComparisonProjectionMapper.cs`
- `Diff/ComparisonRulesSummary.cs`

### Updated

- `Diff/TextComparisonOptions.cs` — rule properties, `HasActiveComparisonRules`
- `Sessions/TwoWayComparisonSession.cs` — project → diff → map → enrich; options parameter
- `UseCases/OpenFileComparisonUseCase.cs` — accepts comparison options

### New — `MergeTool.App.Wpf`

- `Views/FileCompareView.xaml` — comparison rules expander, active-rule badge, rules-pending hint

### Updated — WPF

- `ViewModels/FileCompareWorkspaceViewModel.cs` — pending/applied rules, badge, recompare gating

### New — tests

- `tests/MergeTool.Text.Tests/ComparisonProjectorTests.cs`
- `tests/MergeTool.Application.Tests/ComparisonRulesIntegrationTests.cs`

### Updated tests

- `OpenFileComparisonUseCaseTests.cs`, `MvpEndToEndIntegrationTests.cs`, `MergeAndSaveIntegrationTests.cs`
- `TwoWayComparisonSessionTests.cs` — no longer assert raw engine block reference identity after mapping

## Tests added/updated

| Area | Coverage |
|------|----------|
| `ComparisonProjectorTests` | Mapping, ignore case/empty/regex/blocks, invalid regex, overlapping blocks, unclosed block |
| `ComparisonRulesIntegrationTests` | Ignore-case equal result, invalid regex failure, merge copies original source text |
| Session/open integration | Updated `OpenAsync` signature |

## Build/test commands and results

```powershell
dotnet restore                                    # pass
dotnet build -c Debug                             # pass (0 warnings)
dotnet test -c Debug                              # 226/226 pass
dotnet build -c Release                           # pass
dotnet test -c Release                            # OOM during Application.Tests discovery (environment); bounded runner pass
powershell -File build/run_tests_bounded.ps1 -Configuration Debug    # 226/226 pass
powershell -File build/run_tests_bounded.ps1 -Configuration Release  # 226/226 pass
```

## Dependencies

**None added.** `docs/DEPENDENCIES.md` unchanged.

## Known limitations

- Modified blocks spanning non-contiguous originals after ignored regions use first contiguous mapping chunk (rare for typical comment blocks).
- No rule persistence, profiles, or comment-parser semantics (per task guardrails).
- Ignored-block markers themselves are excluded from comparison.

## Proceed recommendation

Paired testing task (ADV-02_testing) may run. ADV-03 not started.
