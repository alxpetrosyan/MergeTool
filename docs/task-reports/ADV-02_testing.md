# ADV-02 Testing Report

**Task:** Verify comparison rules with projection-safe normalization  
**Date:** 2026-07-02  
**Verdict:** **PASS**

## Baseline checks

| Command | Result |
|---------|--------|
| `dotnet restore` | Pass |
| `dotnet build -c Debug` | Pass (0 warnings) |
| `dotnet test -c Debug` | **226/226** pass |
| `dotnet build -c Release` | Pass |
| `dotnet test -c Release` | Application.Tests discovery OOM (known environment issue); other assemblies pass |
| `build/run_tests_bounded.ps1` Debug | **226/226** pass |
| `build/run_tests_bounded.ps1` Release | **226/226** pass |

Workspace is not a git repository; `git diff` unavailable. File inspection confirms ADV-02 scope only.

## `docs/DEPENDENCIES.md`

**No unexpected additions.**

## Requirement verification

| Requirement | Evidence |
|-------------|----------|
| Whitespace / case behavior | `ComparisonProjectorTests.TrimAll_TreatsPaddedLinesAsEqual`, `IgnoreCase_MapsBackToOriginalLineIndices` |
| Ignore empty lines | `ComparisonProjectorTests.IgnoreEmptyLines_ExcludesBlankLinesFromProjection` |
| Ignored-line regex | `ComparisonProjectorTests.IgnoredLineRegex_ExcludesMatchingLines` |
| Ignored-block boundaries | `ComparisonProjectorTests.IgnoredBlock_ExcludesLinesBetweenMarkers` |
| Projection maps to original indices | `ComparisonProjectorTests` mapping assertions; `ComparisonRulesIntegrationTests.IgnoreCase_ProducesEqualBlockInOriginalSpace` |
| Invalid regex → safe diagnostics | `ComparisonProjectorTests.InvalidRegex_ReturnsDiagnostic`; `ComparisonRulesIntegrationTests.InvalidRegex_FailsWithoutUpdatingAppliedRules` |
| Overlapping ignored blocks | `ComparisonProjectorTests.OverlappingIgnoredBlocks_ReturnsDiagnostic` |
| Unclosed ignored block | `ComparisonProjectorTests.UnclosedIgnoredBlock_ReturnsDiagnostic` |
| Rules change outcome only after recompare | `FileCompareWorkspaceViewModel.HasPendingRuleChanges`, `RulesPendingText`, `CanRecompare`; overlays cleared via `ComparisonInvalidated` on rule edit |
| Original text unchanged in editors | Architecture: projection never mutates `TextDocumentSession` |
| Merge copies original source text | `ComparisonRulesIntegrationTests.Merge_WithIgnoreCaseRules_CopiesOriginalSourceText` |
| No document mutation for normalization | Projected lines are ephemeral; merge uses `sourceLines[i].Content` (unchanged path) |

## Defects

**None filed.**

## Limitations accepted

- Full-solution Release `dotnet test` may OOM on constrained hosts; use bounded runner (documented from MVP-11).
- Equal regions split across ignored lines appear as multiple equal blocks in original space (correct, not a defect).

## Proceed / fix recommendation

**Proceed to ADV-03** (file-type profiles and syntax highlighting). ADV-02 gate satisfied.
