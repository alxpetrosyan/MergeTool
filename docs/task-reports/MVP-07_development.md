# MVP-07 Development Report

**Task:** Implement 2-way file comparison workspace, semantic rendering, and scroll mapping
**Date:** 2026-07-02
**Status:** Complete

## Feature boundary implemented

The File Compare workspace is now visibly useful: choose two files, open them through the
application layer, inspect color-coded blocks in two AvalonEdit panes with line numbers and
metadata, scroll both panes in sync, edit a pane, and recompare. No intraline highlighting,
minimap, syntax profiles, virtual padding, or 3-way UI.

## Objective completed

### File workflow commands

- **Choose left/right file** — `ChooseLeftFileCommand`/`ChooseRightFileCommand` behind a new
  `IFilePickerService` abstraction (WPF `OpenFileDialog` implementation injected at the
  composition root; view models stay WPF-free and testable).
- **Same-path rejection** — `OpenFileComparisonUseCase` rejects both sides normalizing to
  the same file (`PathNormalization`, case-insensitive) *before any file IO* with
  `MT-CMP-003`: "The same file cannot be compared with itself. Choose two different files."
- **Async loading** — unchanged path through `TextDocumentLoader` via the use case.
- **Not-text refusal** — `NotTextForThisProduct` files fail the open with the safe
  `MT-CMP-002` diagnostic (no "binary comparison" mode).
- **Compare/recompare** — `RecompareCommand` (enabled exactly when a session exists, is
  stale, and the workspace is idle) runs `TwoWayComparisonSession.RecompareAsync` through
  the workspace operation slot.

### Rendering (`MergeTool.App.Wpf.Rendering`)

- Two AvalonEdit panes (`FileCompareView`) bound to the sessions' current text, with source
  line numbers, read-only enforcement for read-only files, and per-pane metadata header:
  full path, encoding (+BOM), line-ending style (incl. "Mixed (…)"), Modified/Read-only badge.
- **`PaneRenderModel`** — WPF-free, per-line block-kind classification derived exclusively
  from `ComparisonPresentationMap` (never from rescanning text).
- **`DiffLineBackgroundRenderer`** (`IBackgroundRenderer`) — paints the fixed semantic
  palette (`DiffBlockPalette`: green inserted, red deleted, amber modified; equal =
  unpainted) from the model only. A color legend sits in the status row.
- Status summary shows difference counts; the state (Idle/Working/Completed/Stale/…)
  shows in the shell status bar.
- **Stale contract in the view**: editing a pane routes `TextChanged` →
  `NotifyPaneEdited` → `TwoWayComparisonSession.NotifyDocumentEdited`. The workspace enters
  the `Stale` state, shows the guardrail message, disables navigation/merge
  (`Session.CanNavigate/CanMerge` false), clears overlays and the scroll map (colors would
  point at outdated ranges), and enables Recompare.

### Synchronized scrolling

- **`ScrollSyncCoordinator`** (WPF-free, tested) decides scroll targets: only the *active*
  pane (mouse/keyboard interaction) drives the counterpart; echo events from the follower
  are ignored; a follower already within half a line is never nudged. Line mapping uses
  `ComparisonPresentationMap.MapLeftLineToRightAnchor`/`MapRightLineToLeftAnchor` (relative
  offset in block, clamped at insert/delete boundaries and document ends).
- The view wires AvalonEdit `ScrollOffsetChanged`/`DefaultLineHeight` to the coordinator.

### Known limitation surfaced in UI

No vertical blank-line padding in MVP; the help line under the summary states it verbatim
(`FileCompareWorkspaceViewModel.NoPaddingLimitationText`) instead of faking text changes.

## Design decisions and alternatives rejected

- **Driver-based scroll sync** instead of a reentrancy flag alone: a boolean guard fails
  when WPF applies scrolls asynchronously; making the interacting pane the only driver plus
  a half-line tolerance provably terminates (documented in `docs/architecture/README.md`).
- **Overlays are cleared on stale** rather than kept: stale ranges may exceed the edited
  document and would color the wrong lines.
- Renderer adapters split so everything decidable is WPF-free (`PaneRenderModel`,
  `ScrollSyncCoordinator`); the AvalonEdit-facing classes are thin and untested by design
  (no fragile pixel tests).

## Tests added/updated (16 new + code-behind wiring; 159 total)

- `OpenFileComparisonUseCaseTests` (4, Application): same-path rejected without IO;
  normalized-form comparison (casing + `..` segments); NUL-heavy file refused with
  `MT-CMP-002`; happy path yields a session with an applied comparison.
- `PaneRenderModelTests` (6): modified on both sides; inserted only right; deleted only
  left; classification follows blocks not line content (duplicate text); out-of-range safe;
  palette fixed/distinct/frozen.
- `ScrollSyncCoordinatorTests` (5): no map/geometry → no sync; only the active pane drives
  (follower echo ignored); mapping through the presentation map; half-line tolerance;
  different-length clamping.
- `FileCompareStaleFlowTests` (5): edit → Stale state, merge/navigation disabled, overlays
  invalidated once, stale message shown; recompare restores a current comparison;
  identical-text edit does not go stale; same-path rejection through the view model; dirty
  workspace refuses to close.
- Fixtures for manual verification under `tests/TestData/CompareFixtures/` (identical,
  insertion, three separated modifications, 8 vs 308 lines, NUL-heavy binary).

## Build/test commands and results

```powershell
dotnet build -c Debug        # success, 0 errors
dotnet test -c Debug -m:1    # 159/159 passed
dotnet build -c Release      # success, 0 errors
dotnet test -c Release -m:1  # 159/159 passed
```

UI-level verification: `build/mvp07_ui_check.ps1` drives the real Release app through UI
Automation (fixtures above): modification/insertion/identical/different-length summaries,
same-path rejection, and not-text refusal all pass; screenshots in `artifacts/mvp07/`.

## Known limitations / deliberate deferrals

- No blank-line padding (stated in UI); block-level navigation buttons, merge actions and
  save UI are MVP-08.
- One build error during development (`TextView.ExtentWidth` does not exist in AvalonEdit)
  was fixed by computing the paint width from `HorizontalOffset + ActualWidth`.

## Dependencies introduced

None (AvalonEdit was added in MVP-06).
