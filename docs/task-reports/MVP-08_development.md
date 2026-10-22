# MVP-08 Development Report

**Task:** Implement navigation, manual 2-way merge commands, and safe-save UI
**Date:** 2026-07-02
**Status:** Complete (verified at Phase-1 checkpoint)

## Feature boundary implemented

Manual 2-way text merge workflow end-to-end: difference navigation, block copy in both
directions for modified/inserted/deleted hunks, stale-precondition enforcement,
safe save/reload/conflict handling, and dirty-close prompts. No automatic merge, force
overwrite, multi-select merge, or 3-way output.

## Objective completed

### Application layer

- `DifferenceNavigator` — walks non-equal blocks by stable ordinals; explicit no-wrap at
  first/last difference; scroll anchors via `ComparisonPresentationMap`.
- `ManualTwoWayMergeService` — copies current hunk left↔right with per-kind behaviour
  (replace / insert at anchor / delete target range); normalizes copied lines to target
  line-ending convention; mutates target only; marks comparison stale after success.
- `MergePrecondition` validation — refuses stale revision/document mismatches with
  `MT-CMP-001` and the guardrail stale message.
- `DocumentPersistenceService` — save left/right/all through `SafeTextFileWriter`; external
  modification conflict returns `MT-IO-002` with Cancel/Reload only; reload refreshes
  identity.

### WPF UI (`FileCompareWorkspaceViewModel` + `FileCompareView`)

- Next/previous difference commands with `NavigationText` ("Difference n of m").
- Copy to Right / Copy to Left — enabled only when a difference is selected, comparison is
  current, and target is writable.
- Save Left / Save Right / Save All — enabled when the document is dirty and writable.
- Stale state disables navigation/merge and surfaces the guardrail message; Recompare
  restores a current result.
- `MainWindowViewModel` — dirty-close prompt (Save/Discard/Cancel) and exit confirmation;
  `CloseWorkspaceAsync` saves on request before closing.

## Files/projects changed (implementation present in tree)

- `src/MergeTool.Application/Sessions/DifferenceNavigator.cs`
- `src/MergeTool.Application/Sessions/ManualTwoWayMergeService.cs`
- `src/MergeTool.Application/Sessions/MergePrecondition.cs`
- `src/MergeTool.Application/UseCases/DocumentPersistenceService.cs`
- `src/MergeTool.App.Wpf/ViewModels/FileCompareWorkspaceViewModel.cs`
- `src/MergeTool.App.Wpf/Views/FileCompareView.xaml`
- `src/MergeTool.App.Wpf/Services/IUserPromptService.cs` (+ WPF implementation)
- `src/MergeTool.App.Wpf/ViewModels/MainWindowViewModel.cs`

## Tests added/updated

- `DifferenceNavigatorTests` (5) — order, no-wrap, stale disable.
- `ManualTwoWayMergeServiceTests` (14) — modified/inserted/deleted both directions,
  line-ending normalization, stale/read-only refusal, post-merge session contract.
- `DocumentPersistenceServiceTests` (7) — encoding/BOM/line-ending preservation, conflict
  refusal, reload, read-only.
- `FileCompareStaleFlowTests` (4) — VM stale/recompare/same-path/dirty-close gating.
- `MergeAndSaveIntegrationTests` (1) — fixture-based copy + safe save to disk (added at
  Phase-1 checkpoint).

## Build/test commands and results

```powershell
dotnet build -c Debug    # succeeded
dotnet test -c Debug     # 203 tests passed (after checkpoint additions)
dotnet build -c Release  # succeeded (prior run)
dotnet test -c Release   # 203 tests passed (prior run)
```

## Known limitations / deliberate deferrals

- No force-overwrite on save conflict (MVP policy).
- Merge operates on one selected block at a time.
- UI does not pad panes with blank lines for alignment (documented in MVP-07).

## Dependencies introduced

None.
