# MVP-04 Development Report

**Task:** Implement 2-way line diff engine and deterministic result model
**Date:** 2026-07-02
**Status:** Complete

## Feature boundary implemented

Line-level two-way diff engine and its deterministic result model in `MergeTool.Text.Diff`.
No intraline segments, no regex/ignore options, no moved blocks, no UI render data, no
editor-facing control.

## Objective completed

New namespace `MergeTool.Text.Diff`:

- `LineRange` — the product-wide zero-based `[StartIndex, Length]` range; an empty range's
  `StartIndex` is the safe insertion/deletion anchor. `EndExclusive`/`Contains` helpers.
- `DiffBlock` — deterministic zero-based `Ordinal` (block ID within its result),
  `DiffBlockKind` (`Equal`, `Inserted`, `Deleted`, `Modified`), left/right `LineRange`, and
  safe source anchors (`LeftAnchorLine`/`RightAnchorLine`, clamped by construction).
- `TextComparisonOptions` — explicit MVP defaults only (exact ordinal comparison; terminator
  *style* is not a difference because it is preserved on save; a missing final terminator on
  the last line *is* a difference). No ignore rules.
- `ITextDiffEngine` — one interface, one implementation; no plug-in system.
- `PatienceLineDiffEngine` — internal deterministic patience-style algorithm:
  1. Lines map to integer keys (ordinal content + no-final-newline marker).
  2. Windows trim common prefix/suffix, anchor on lines unique to both sides (LIS via
     patience sorting), and split around anchors using an **explicit stack** — no recursion,
     no overflow on large inputs.
  3. A window with no unique anchors is emitted whole: lines on both sides → one `Modified`
     replacement block; one-sided → `Inserted`/`Deleted`. This is the documented
     ambiguous-pairing fallback.
- `TwoWayTextComparisonResult` — input document session IDs, input document revisions,
  ordered blocks, and `DiffSummary` counts (blocks and lines per kind, difference count).
- `DiffSummary` — aggregate counts; `HasDifferences`.

## Modified-block semantics (documented, tested)

A difference region containing lines on **both** sides is always one `Modified` block, even
when line counts differ (`UnbalancedReplacement_IsOneModifiedBlock`). Pure one-sided regions
are `Inserted`/`Deleted`. Pairing inside a `Modified` block is block-level, not line-level;
line pairing (`LinePair`) was not needed by this model and is deferred until a renderer or
merge planner proves a need.

## Algorithm policy decisions

- **Internal implementation instead of DiffPlex** — recorded in `docs/DEPENDENCIES.md`
  ("Decided against"). The patience approach is ~200 lines, deterministic, cancellable, and
  gives good alignment behavior around repeated lines, which the tests demand.
- **Determinism** — no randomness, no wall clock, no culture-dependent comparison (ordinal
  string keys); repeated runs are asserted identical.
- **Cancellation** — the engine checks the token per window and throws
  `OperationCanceledException` (application layer converts, per lifecycle conventions).

## Tests added/updated (23 new; 100 total)

`PatienceLineDiffEngineTests` (21): identical inputs; one changed line; insertion at
start/middle/end; deletion at start/middle/end; empty left/right/both; adjacent modifications
merging into one `Modified`; unbalanced replacement; repeated equal lines with unique anchors
(no misalignment); repeated-lines-only trimming; source-code-like text with several separated
blocks (exact 6-block sequence asserted); final-newline difference; terminator-style
non-difference; 5× repeated-run determinism; 60,000-line input without recursion overflow
plus full range-chain coverage validation; cancellation.
`TwoWayTextComparisonResultTests` (2): identity/revision/summary carriage with exact counts;
`LineRange` zero-based `[start,length]` convention.

All block assertions check exact ordinals, kinds, and ranges — never just difference counts.

## Build/test commands and results

```powershell
dotnet build -c Debug        # success, 0 errors
dotnet test -c Debug -m:1    # 100/100 passed
dotnet build -c Release      # success, 0 errors
dotnet test -c Release -m:1  # 100/100 passed
```

One test expectation was corrected during development (a bogus trailing zero-length Equal
block in the source-code scenario); the engine itself needed no fix.

## Known limitations / deliberate deferrals

- `LinePair` alignment records deferred (see Modified-block semantics above).
- Cancellation granularity is per window, which is fine for MVP sizes; Polish-scope
  benchmarks may refine it.

## Dependencies introduced

None (DiffPlex explicitly rejected and recorded).
