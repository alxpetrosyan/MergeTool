# ADV-01 Testing Report

**Task:** Verify bounded intraline diff rendering  
**Date:** 2026-07-02  
**Verdict:** **PASS**

## Baseline checks

| Command | Result |
|---------|--------|
| `dotnet restore` | Pass |
| `dotnet build -c Debug` | Pass (0 warnings) |
| `dotnet test -c Debug` | **215/215** pass |
| `dotnet build -c Release` | Pass |
| `dotnet test -c Release` | **215/215** pass |
| `build/run_tests_bounded.ps1` Debug | **215/215** pass |
| `build/run_tests_bounded.ps1` Release | **215/215** pass |

Workspace is not a git repository; `git diff` was not available. Manual file inspection confirms changes are limited to ADV-01 intraline scope.

## `docs/DEPENDENCIES.md`

**No unexpected additions.** Still only SDK, MSTest, and AvalonEdit (MIT).

## Requirement verification

| Requirement | Evidence |
|-------------|----------|
| Replacement spans | `BoundedIntralineDiffCalculatorTests.ReplacementSpans_AreDetectedOnBothSides` |
| Insertion spans | `BoundedIntralineDiffCalculatorTests.InsertionSpans_AreDetectedOnRightSideOnly` |
| Deletion spans | `BoundedIntralineDiffCalculatorTests.DeletionSpans_AreDetectedOnLeftSideOnly` |
| Whitespace-significant | `BoundedIntralineDiffCalculatorTests.Whitespace_IsSignificant` |
| Unicode surrogate-pair safety | `BoundedIntralineDiffCalculatorTests.SurrogatePairs_AreNotSplit` (2 UTF-16 units per emoji) |
| Long-line / work-budget fallback | `LongLine_ExceedingMaxLength_ReturnsWholeLineFallback`, `WorkBudgetExceeded_ReturnsWholeLineFallback` |
| Whole-line rendering when inline skipped | `PaneRenderModelIntralineTests.WholeLineFallback_PreservesModifiedLineClassification` |
| UTF-16 offsets map to line text | `PaneRenderModelIntralineTests.InlineSegments_MapToCorrectLineOnEachSide` |
| Optional / disabled intraline | `IntralineDiffEnricherTests.Enrich_WhenDisabled_LeavesInlineDiffsEmpty` |
| No document text mutation | `IntralineDiffColorizingTransformer` uses `ChangeLinePart` only; no `Document.Text` changes |
| Stale invalidation | `FileCompareView.OnComparisonInvalidated` clears both background and intraline models |

## UI / performance notes

- Long-line fallback is covered by unit tests; no UI freeze observed in test runs.
- Manual WPF launch was not required for this tester pass; rendering wiring is covered by view-model/render-model tests and existing compare view integration pattern.

## Defects

**None filed.** No `docs/defects/ADV-01-*.md` created.

## Limitations accepted for this task

- Duplicate-character LCS tie-breaking may place whitespace highlights at a different UTF-16 offset than a human would choose; difference is still visible.
- Intraline highlights only on paired modified lines (documented in development report).

## Proceed / fix recommendation

**Proceed to ADV-02.** ADV-01 gate is satisfied. ADV-02 (comparison rules with projection-safe normalization) can extend `TextComparisonOptions` without conflicting with the optional intraline model introduced here.
