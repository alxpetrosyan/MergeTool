# MVP-07 Testing Report

**Task:** Verify: Implement 2-way file comparison workspace, semantic rendering, and scroll mapping
**Date:** 2026-07-02
**Verdict:** PASS

## Commands run and exit results

| Command | Result |
| --- | --- |
| `dotnet restore` | exit 0 |
| `dotnet build -c Debug` | exit 0; 0 errors |
| `dotnet test -c Debug -m:1 --no-build` | exit 0; 159/159 passed |
| `dotnet build -c Release` | exit 0; 0 errors |
| `dotnet test -c Release -m:1 --no-build` | exit 0; 159/159 passed |
| `build/mvp07_ui_check.ps1` (UI Automation on Release app) | exit 0; all checks passed |

## Fixture walkthrough (real app, real files)

Fixtures in `tests/TestData/CompareFixtures/`; driven through the actual Release UI via UI
Automation, with screenshots in `artifacts/mvp07/`:

1. **Identical text files** (`identical_a/b.txt`) — "No differences found." No overlays.
2. **Insertion/deletion** (`insert_left/right.txt`) — "1 difference block(s): … 1 inserted";
   only the inserted right-pane line is painted green; screenshot confirms the left pane has
   no classification (deletion is the mirrored case, covered by `PaneRenderModelTests`).
3. **Several separated modifications** (`mods_left/right.txt`, changes at lines 5/18/33) —
   "3 difference block(s): 3 modified"; screenshot shows exactly lines 5 and 18 painted
   amber in both visible panes, all other lines unpainted.
4. **Significantly different length** (8 vs 308 lines) — compare completes ("1 difference
   block(s)": the unterminated last common line plus the 300-line tail form one modified
   block per engine semantics); UI stays responsive; scrolling clamps to valid lines
   (`DifferentLengthDocuments_ClampToValidLines`).
5. **Undecodable/NUL-heavy file** (`notext_nul.bin`) — refused with "One of the selected
   files is not text and cannot be compared by this product." No crash, no binary view.
6. **Manual edit after comparison** — covered by `FileCompareStaleFlowTests`: the workspace
   enters `Stale`, shows "Comparison is stale. Recompare before merging.", disables
   navigation/merge on the session, clears overlays, enables Recompare; recompare restores
   a current comparison.

## Checks

- **Colors follow block semantics, not line content** —
  `ClassificationComesFromBlocks_NotFromLineContent`: identical text ("dup") is unpainted in
  its equal occurrence and green in its inserted occurrence. Code inspection: the renderer
  consumes only `PaneRenderModel` (built from `ComparisonPresentationMap`); nothing rescans
  editor text.
- **Scrolling does not loop/jitter/freeze** — design + tests: only the user-driven pane may
  drive (`OnlyTheActivePane_DrivesTheCounterpart` proves follower echoes are ignored) and
  in-tolerance targets are suppressed (`TargetsWithinHalfALine_AreSuppressed`). Manual
  wheel-scrolling in both panes during the UI run showed no oscillation; the process stayed
  responsive throughout the automated UI session.
- **Stale state disables merge/navigation until recompare** —
  `EditingAPane_MarksComparisonStale_AndDisablesMergeNavigation` and
  `Recompare_RestoresACurrentComparison` (State `Stale` → `Completed`,
  `CanNavigate`/`CanMerge` false → true, stale message shown verbatim).
- **File paths/metadata match loaded fixtures** — screenshots show the full normalized
  fixture paths, "UTF-8", and "CRLF" in both pane headers matching the files on disk.
- **Same-path comparison rejected safely** — UI check 4 (differently-cased spelling of one
  file) shows the clear message; `SamePath_IsRejectedWithoutTouchingTheFileSystem` proves no
  IO happens; the workspace fails controlled with no session.

## Failure-condition review

- Displayed blocks diverging from core diff results: **not possible by construction**
  (single pipeline result → map → model → renderer; verified by `PaneRenderModelTests`
  against the real engine).
- Scrolling recursion: **disproved** (driver model + tests above).
- Stale actions remaining enabled: **disproved by test**.
- Non-text data treated as supported binary comparison: **disproved** (UI check 5, use-case
  test).

## Architecture/licence checks

No new packages; `docs/DEPENDENCIES.md` unchanged. New WPF-free logic
(`PaneRenderModel`, `ScrollSyncCoordinator`) lives in the UI project but touches only
Application/Text/Core types; application services remain WPF-free (verified by project
references). Same-path policy sits in the Application layer, not in code-behind.

## Defects created

None open. One development-time build error (`TextView.ExtentWidth` missing from the
AvalonEdit API) was fixed immediately and is noted in the development report.

## Recommendation

**Proceed** to MVP-08.
