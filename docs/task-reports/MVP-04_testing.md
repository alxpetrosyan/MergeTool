# MVP-04 Testing Report

**Task:** Verify: Implement 2-way line diff engine and deterministic result model
**Date:** 2026-07-02
**Verdict:** PASS

## Commands run and exit results

| Command | Result |
| --- | --- |
| `dotnet build -c Debug` | exit 0; 0 errors |
| `dotnet test -c Debug -m:1 --no-build` | exit 0; 100/100 passed |
| `dotnet build -c Release` | exit 0; 0 errors |
| `dotnet test -c Release -m:1 --no-build` | exit 0; 100/100 passed |

## Required deterministic cases

All exercised in `PatienceLineDiffEngineTests` with **exact block ordinal/kind/range
assertions**, not difference counts:

- identical inputs → single Equal block covering both documents
- one changed line → Equal/Modified/Equal with exact ranges
- insertion at start, middle, end (empty left range anchored at the insertion point)
- deletion at start, middle, end (empty right range anchored at the deletion point)
- empty left → one Inserted block; empty right → one Deleted block; both empty → zero blocks
- adjacent modifications → merged into one Modified block
- repeated equal lines (brace-heavy source) → unique-line anchors prevent misalignment
- source-code-like text with several separated blocks → exact 6-block sequence
- determinism: the same inputs run 5× produce identical block sequences; the algorithm
  contains no randomness, clocks, or culture-sensitive comparisons
- large input: 60,000 lines with sparse edits completes without recursion overflow (explicit
  stack), and the block chain exactly covers both documents in order

## Result-model inspection

- `LineRange` is zero-based `[StartIndex, Length]` everywhere; empty ranges keep a meaningful
  anchor `StartIndex` (verified by insertion/deletion tests and `LineRange` unit tests).
- `TwoWayTextComparisonResult` carries left/right document session IDs, left/right document
  revisions, ordered blocks, and summary counts — enough range information to plan a manual
  copy later (source and target ranges plus anchors are explicit on every block).
- `Modified` semantics are documented on the type and in the development report, and pinned
  by `UnbalancedReplacement_IsOneModifiedBlock` (ambiguous pairing falls back to one
  replacement block, never an undocumented split).

## Architecture/licence checks

- Engine and model live in `MergeTool.Text` (`net10.0`); no WPF/AvalonEdit dependency
  (reference-direction smoke tests still pass).
- No new packages; DiffPlex decision recorded in `docs/DEPENDENCIES.md` as "not used".

## Failure-condition review

No block maps to incorrect source lines (range-chain coverage asserted), output is
deterministic across repeated runs, empty and repeated-line cases pass without crashes, and
`Modified` semantics are documented and stable.

## Defects created

None. One incorrect test expectation (trailing zero-length Equal block) was fixed in the test
during development; production behavior was already correct.

## Recommendation

**Proceed** to MVP-05.
