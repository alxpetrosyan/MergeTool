# MVP-05 Testing Report

**Task:** Verify: Implement editable comparison sessions, presentation mapping, and stale-result contract
**Date:** 2026-07-02
**Verdict:** PASS

## Commands run and exit results

| Command | Result |
| --- | --- |
| `dotnet build -c Debug` | exit 0; 0 errors |
| `dotnet test -c Debug -m:1 --no-build` | exit 0; 128/128 passed |
| `dotnet build -c Release` | exit 0; 0 errors |
| `dotnet test -c Release -m:1 --no-build` | exit 0; 128/128 passed |

## Tester checks

- **Session revision changes** — `TextDocumentSessionTests` cover edit (+1), identical-text
  edit (no change), save (+1, clean), reload (+1, clean), read-only rejection.
- **Stale request rejection** — `StaleCompletion_NeverOverwritesNewerResult` and
  `OlderCompletionFinishingFirst_IsAlsoDiscarded` use a gated fake engine to interleave two
  in-flight recompares in both completion orders; in both cases the older completion is
  discarded entirely (no result replacement, no revision bump, no diagnostics update).
- **Edited text cannot be merged using an old result (UI-independent proof)** —
  `MergePreconditionTests`: after editing either source or target, validation of the
  previously captured precondition fails with the exact guardrail message ("Comparison is
  stale. Recompare before merging."). After a recompare, a precondition from the older
  comparison revision is still rejected. Additionally `CanMerge` is false while stale, and
  `CreateMergePrecondition` on a stale session throws. Tampered fingerprints and
  out-of-bounds ranges are also rejected — a merge can be proven invalid after any relevant
  edit or recompare.
- **Cancellation/newer-request logic** — `Cancellation_KeepsLastAppliedResultVisible` proves
  a cancelled recompare returns a controlled `Cancelled` status and leaves the last applied
  result and comparison revision untouched.
- **Mapping boundaries** — anchors for inserted/deleted blocks are explicit, non-null,
  clamped line indices (end-of-document insertion anchors clamp to the last valid line;
  deletion counterparts anchor to the block boundary). Out-of-range lookups clamp rather
  than throw. Every block kind has mapping tests. Empty documents yield safe defaults.
- **No mutation for display alignment** — code inspection: `ComparisonPresentationMap` only
  reads the result and builds lookup arrays; `TextDocumentSession` text changes only through
  the edit/save/reload API; no padding lines are inserted anywhere; blocks are immutable
  records.
- **One lifecycle owner** — `TwoWayComparisonSession` is the single place that owns
  request IDs, staleness, selection, and eligibility; no other component touches comparison
  state.

## Architecture/licence checks

No new packages (`docs/DEPENDENCIES.md` unchanged). All new code is in
`MergeTool.Application` (plain `net10.0`); reference-direction smoke tests pass.

## Defects created

None against production code. Three test-only mistakes (fake-engine ranges, two clamp
expectations) were fixed during development and are noted in the development report.

## Recommendation

**Proceed** to MVP-06.
