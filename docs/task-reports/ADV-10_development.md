# ADV-10 Development Report

**Task:** Add optional content/hash folder comparison and timestamp tolerance  
**Date:** 2026-07-02  
**Verdict:** Complete

## Summary

Added explicit folder comparison modes: **Quick Metadata** (default) and **Content Hash**. Quick mode supports configurable UTC timestamp tolerance. Content Hash mode computes SHA-256 via buffered streaming with progress, cancellation, and per-run session cache. Rows display `ContentMatch` / `ContentDifferent` distinctly from metadata states. Both 2-way and 3-way folder workspaces expose mode and tolerance controls.

## Changed files

### Folders (`MergeTool.Folders`)

- `FolderComparisonMode.cs` — `QuickMetadata`, `ContentHash`
- `FolderComparisonOptions.cs` — mode + `MetadataTimestampTolerance`
- `IFileContentHashAccess.cs`, `PhysicalFileContentHashAccess.cs`, `StreamingSha256Hasher.cs`
- `FolderContentHashSessionCache.cs` — in-memory cache per compare run
- `FolderComparisonProgress.cs` — `FolderComparisonPhase` (Scan / ContentHash)
- `QuickFolderComparisonEngine.cs` — tolerance in quick mode; hash phase in content mode
- `FolderRowStateLabels.cs` — content match explanation text

### WPF (`MergeTool.App.Wpf`)

- `ViewModels/FolderCompareWorkspaceViewModel.cs` — mode, tolerance, hash progress status
- `ViewModels/ThreeWayFolderCompareWorkspaceViewModel.cs` — same
- `ViewModels/FolderCompareFilter.cs` — `ContentDifferent` in Differences filter
- `Views/FolderCompareView.xaml`, `ThreeWayFolderCompareView.xaml` — mode UI, content row colors

### Tests

- `tests/MergeTool.Folders.Tests/ContentHashFolderComparisonTests.cs` — 7 tests
- `tests/MergeTool.App.Wpf.Tests/FolderCompareWorkspaceViewModelTests.cs` — filter update

### Docs

- `docs/SCOPE2_PROGRESS.md`

## Commands run

```powershell
.\build\run_tests_bounded.ps1
.\build\run_tests_bounded.ps1 -Configuration Release
.\build\phase1_checkpoint.ps1
```

## Test results

| Configuration | Result |
|---------------|--------|
| Debug bounded | **318/318** passed |
| Release bounded | **318/318** passed |
| Phase-1 checkpoint | **ALL PASSED** |

## Known limitations

- Hash cache is scoped to a single compare operation (not persisted across sessions).
- Content hash applies only to fully paired (2-way) or tripled (3-way) file rows.
- Timestamp tolerance is ignored in Content Hash mode.
- No binary visual diff or archive traversal.

## Remaining risks

- Very large folder trees in Content Hash mode may need per-file progress UI beyond status text.
