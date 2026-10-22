# ADV-04 Development Report

**Task:** Add difference minimap and collapsible equal-region projection  
**Date:** 2026-07-02  
**Verdict:** Complete

## Summary

Implemented end-to-end minimap overview and collapsible equal-region folding in the File Compare workspace. Presentation models live in the application layer; AvalonEdit folding and minimap rendering stay in WPF. Document text is never mutated for collapse—foldings hide line ranges visually while merge and navigation continue to use original line indices from the comparison session.

## Changed files

### Application (`MergeTool.Application`)

- `Presentation/MinimapModel.cs` — marker/viewport models with normalized coordinates
- `Presentation/MinimapModelBuilder.cs` — builds ordered markers from diff blocks
- `Presentation/CollapsedViewProjection.cs` — per-pane collapse layout and builder
- `Presentation/CollapseProjectionState.cs` — expand one/all, navigation reveal, fold specs
- `Presentation/FileComparePresentationBundle.cs` — revision-tagged bundle factory
- `Sessions/ComparisonPresentationMap.cs` — `TryGetBlock` for collapse/navigation

### WPF (`MergeTool.App.Wpf`)

- `Views/DifferenceMinimapControl.cs` — minimap rendering, viewport band, click navigation
- `Rendering/EqualRegionFoldingCoordinator.cs` — AvalonEdit `FoldingManager` adapter
- `Views/FileCompareView.xaml` / `.xaml.cs` — minimap column, fold refresh, stale guards
- `ViewModels/FileCompareWorkspaceViewModel.cs` — bundle lifecycle, expand commands, minimap navigation

### Tests

- `tests/MergeTool.Application.Tests/MinimapAndCollapseTests.cs` — 16 unit tests
- `tests/MergeTool.App.Wpf.Tests/FileComparePresentationTests.cs` — 6 integration tests

### Docs

- `docs/SCOPE2_PROGRESS.md`

## Design notes

- **Minimap:** One shared overview beside both panes; markers colored by `DiffBlockKind`; viewport tracks scroll offset of the active editor.
- **Collapse:** Long equal blocks collapse middle lines; configurable threshold (default 8) and context lines (default 2). `CollapseProjectionState.EnsureBlockVisible` expands preceding equal regions before difference navigation.
- **Stale results:** View compares `FileComparePresentationBundle.ComparisonRevision` to `TwoWayComparisonSession.ComparisonRevision` before applying minimap/folds; invalidation clears bundle and overlays.
- **No new packages:** Uses existing AvalonEdit folding APIs.

## Commands run

```powershell
dotnet build -c Debug
dotnet build -c Release
.\build\run_tests_bounded.ps1
.\build\run_tests_bounded.ps1 -Configuration Release
.\build\phase1_checkpoint.ps1
```

## Test results

| Configuration | Result |
|---------------|--------|
| Debug bounded | **255/255** passed |
| Release bounded | **255/255** passed |
| Phase-1 checkpoint | **ALL PASSED** |

## Manual UI verification (recommended)

1. Compare two files with many distant changes — minimap shows separated markers; click jumps to block.
2. Compare files with a large unchanged middle — equal region folds; “Expand all” / navigation reveals targets.
3. Compare adjacent changes — all difference lines remain visible; markers cluster.
4. Edit a file, recompare — minimap and folds rebuild; revision guard prevents stale UI.

## Known limitations

- Collapse threshold/context are view-model properties (defaults only); no settings UI yet.
- Minimap uses left-editor scroll for viewport when both panes are synced (acceptable for current scroll-sync contract).
- Interactive unfold via AvalonEdit fold margin does not sync back to `CollapseProjectionState` (expand commands and navigation do).

## Remaining risks

- Very large files: fold rebuild runs on UI thread; acceptable for MVP sizes but may need background projection for extreme files.
- Asymmetric equal blocks (after projection rules) collapse per-pane independently—intentional but may look uneven between panes.
