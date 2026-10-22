# ADV-12 Development Report

**Task:** Add safe JSON/XML formatted comparison projection  
**Date:** 2026-07-02  
**Verdict:** Complete

## Summary

Integrated optional JSON/XML pretty-print projection into the two-way compare pipeline. Users choose **Raw**, **Formatted JSON**, or **Formatted XML** under Comparison rules; Recompare applies the projection. Parse failures fall back to raw comparison with safe diagnostics (`MT-FMT-001` / `MT-FMT-002`). Formatted mode shows a **Raw / Formatted** indicator, displays pretty-printed text in read-only editors, and disables merge/copy because there is no original-text mapping.

## Changed files

### Text (`MergeTool.Text`)

- `Formatted/StructuredTextProjection.cs` — `StructuredFormatKind`, multi-line JSON/XML formatting, `TryProject`, `ToTextLines` / `JoinLines`
- `Diff/TextComparisonOptions.cs` — `StructuredFormat` option

### Application (`MergeTool.Application`)

- `Sessions/TwoWayComparisonSession.cs` — formatted projection before diff, `IsFormattedProjectionActive`, display lines, `CanMerge` gate

### WPF (`MergeTool.App.Wpf`)

- `ViewModels/FileCompareWorkspaceViewModel.cs` — structured format selector, projection label, merge-blocked message
- `ViewModels/StructuredFormatOption.cs` — combo display items
- `Views/FileCompareView.xaml` — structured view dropdown, projection/merge indicators
- `Views/FileCompareView.xaml.cs` — formatted editor text, read-only panes, overlay line counts

### Tests

- `tests/MergeTool.Text.Tests/StructuredTextProjectionTests.cs` — new
- `tests/MergeTool.Application.Tests/TwoWayComparisonSessionTests.cs` — formatted merge gate + fallback
- `tests/MergeTool.App.Wpf.Tests/MainWindowSessionHistoryTests.cs` — fix flaky recent selection

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
| Debug bounded | **336/336** passed |
| Release bounded | **336/336** passed |
| Phase-1 checkpoint | **ALL PASSED** |

## Known limitations

- Formatted mode is display/compare only; no write-back formatter.
- Editors are read-only while formatted projection is active.
- Search and go-to-line still operate on raw document text in the session (not formatted overlay).
- No Office/PDF structured projection.

## Remaining risks

- Very large JSON/XML documents may be slower to parse and pretty-print on each recompare.
