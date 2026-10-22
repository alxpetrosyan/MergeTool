# UI-REDESIGN-01 Development Report

**Verdict:** Complete

## Summary

Redesigned the comparison workspace into an Araxis-inspired layout — ribbon quick actions on
top, path selector row, dominant comparison area, status bar at the bottom — and replaced the
flat folder comparison grid with a real expandable tree-grid of directories and files. No
comparison logic moved into WPF; the tree is computed in `MergeTool.Folders` and the views
only bind to state and commands.

## Part 1 — File comparison workspace

- **Ribbon** (`MainWindow.xaml` + new `Controls/RibbonQuickActionButton.cs`, styled in
  `App.xaml`): groups New / Compare / Navigate / Merge / Save / View with Segoe MDL2 Assets
  glyphs (Windows system icon font, no third-party assets). Contextual buttons bind to
  `ActiveWorkspace.*Command`; a style trigger disables any button whose command does not
  resolve on the active workspace type, so enablement always follows `CanExecute`
  (no-difference-selected, stale, read-only target ⇒ disabled).
- **Comparison settings dialog** (`Views/ComparisonSettingsDialog.xaml`): whitespace mode,
  ignore case, ignore empty lines, moved-block detection, ignored line/block regexes,
  structured view, and the file-type profile picker moved out of the main page. Opened via
  `ShowComparisonSettingsCommand` → `ComparisonSettingsRequested` event → minimal
  code-behind in `FileCompareView`. Only the active profile badge stays on the main screen.
  Rules still apply on the next Compare/Recompare (unchanged semantics).
- **Path selector row** (`Controls/PathComboSelector.xaml(.cs)` + pure
  `Controls/PathSuggestionProvider.cs`): editable text, browse button, recent-path dropdown
  (fed by `IRecentPathProvider`, implemented by `MainWindowViewModel` over persisted
  recents), child folder/file completion after typing `\` or `/`, and a red border marking
  non-existing paths without ever throwing. Reused by file and folder workspaces.
- **Comparison area**: editors dominate the tab; the difference list/minimap rail stays on
  the right and remains collapsible. Old per-view nav/merge/save button rows were removed
  (now ribbon-only); the state banner, find-and-go expander, diagnostics panel, and legend
  footer remain.
- **Status bar** (`MainWindow.xaml`): workspace state (last operation result), busy progress
  + cancel, `STALE — recompare (F5)` badge, `Difference N of M`, and per-pane encodings.

## Part 2 — Folder comparison tree

- **`MergeTool.Folders/FolderComparisonTreeBuilder.cs`**: pure transformation of the flat
  recursive scan rows into `FolderComparisonTreeNode` hierarchy. Synthesizes intermediate
  directory nodes (the engine only emits rows for empty dirs), sorts directories before
  files (ordinal, case-insensitive), and aggregates `HasDifferenceDescendant` /
  `HasErrorDescendant` flags. `FolderRowStateClassification` centralizes what counts as a
  difference; metadata match is never promoted to content-identical.
- **`FolderCompareTreeRowViewModel`** (WPF): wraps a node with depth/indent, expander glyph,
  `IsExpanded`, type/extension column, and the flat `FolderCompareRowViewModel` for nodes
  with a scan row (synthesized dirs have none and are excluded from folder operations).
- **`FolderCompareWorkspaceViewModel`**: builds the tree on every completed scan, flattens
  it into `TreeRows` honoring filter + expansion, adds `SetFilterLeftOnlyCommand`,
  `SetFilterRightOnlyCommand`, `ExpandAllFoldersCommand`, `CollapseAllFoldersCommand`,
  `RevealLeft/RightInExplorerCommand` (injectable action; default `ExplorerReveal`), and
  `SetSelectedTreeRows` mapping tree selection onto the existing flat selection so folder
  operations, paired-open, and command gating are unchanged. The flat `FilteredRows` is
  kept intact for existing tests. Async compare, cancellation, and last-good-result
  preservation are untouched.
- **`FolderCompareView`**: DataGrid tree-grid bound to `TreeRows` with indentation +
  expander in the Name column; columns for state, type, left/right size, left/right
  modified (UTC), diagnostics. Filters: All / Differences / Left only / Right only /
  Errors-Skipped (directories stay visible while any descendant passes the filter).
  Double-click toggles directories and opens the paired file comparison for files
  (one-sided rows get the existing clear message). Context menu: open file comparison,
  copy L→R / R→L, reveal left/right in Explorer.

## Architecture notes

- Tree building lives in `MergeTool.Folders`; WPF receives computed nodes.
- No new NuGet packages, no paid or proprietary assets (icons are OS font glyphs).
- Code-behind additions are UI plumbing only (dialog opening, tree selection mapping,
  double-click routing, suggestion popup handling).
- Auto-selection of the first row after a folder scan was removed: merge/open commands now
  stay disabled until the user actually picks a row (matches ribbon gating rules).

## Changed files

New: `FolderComparisonTreeBuilder.cs`, `FolderCompareTreeRowViewModel.cs`,
`PathComboSelector.xaml(.cs)`, `PathSuggestionProvider.cs`, `RibbonQuickActionButton.cs`,
`ComparisonSettingsDialog.xaml(.cs)`, `ExplorerReveal.cs`, `IRecentPathProvider.cs`,
`build/ui_redesign_screens.ps1`, plus three test files.
Modified: `MainWindow.xaml`, `App.xaml`, `FileCompareView.xaml(.cs)`,
`FolderCompareView.xaml(.cs)`, `FileCompareWorkspaceViewModel.cs`,
`FolderCompareWorkspaceViewModel.cs`, `FolderCompareFilter.cs`, `MainWindowViewModel.cs`,
`AppCompositionRoot.cs`.
