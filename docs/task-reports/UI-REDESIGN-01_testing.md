# UI-REDESIGN-01 Testing Report

**Verdict:** Pass — all suites green in Debug and Release; full UI checkpoint passed against
the redesigned Release build.

## Automated tests

Commands run (2026-07-09):

| Command | Result |
| --- | --- |
| `dotnet restore` | Up to date |
| `dotnet build -c Debug` | 0 errors |
| `dotnet test -c Debug` | 406/406 passed (Core 30, Text 102, Folders 42, App.Wpf 93, Application 139) |
| `dotnet build -c Release` | 0 errors |
| `dotnet test -c Release` | 406/406 passed |

No VSTEST full-suite timeout occurred this run; the bounded script
(`build/run_tests_bounded.ps1`) was therefore not needed.

### New tests

- `MergeTool.Folders.Tests/FolderComparisonTreeBuilderTests` (7): nested path synthesis,
  directory-with-changed-descendants flag, left/right-only classification, error
  propagation up the chain, explicit empty-dir rows, metadata-match-is-not-a-difference,
  deterministic ordering.
- `MergeTool.App.Wpf.Tests/FolderCompareTreeViewModelTests` (9): tree build from a real
  scan, expand/collapse of one directory and of all, Differences filter keeps ancestor
  folders and hides matches, Left-only/Right-only filters, tree selection → paired-open
  launcher flow, one-sided open shows the clear message, synthesized dirs excluded from
  operations, reveal-in-Explorer paths.
- `MergeTool.App.Wpf.Tests/PathSuggestionProviderTests` (7): recents (distinct,
  prefix-filtered), child completion after separator, partial-segment completion, invalid
  characters never throw, invalid-path detection per selector kind, separator detection.
- `MergeTool.App.Wpf.Tests/RibbonCommandEnablementTests` (5): copy disabled without a
  selected difference and while stale, navigation disabled while stale, save gated on dirty
  writable documents, find-panel command, settings-dialog request event.

## UI automation

`build/phase1_checkpoint.ps1` (Release exe) — all 11 checks passed, including
FOLDER-COMPARE (18 state labels — tree now includes folder rows), FILTER (Differences hides
`same.txt`), and PAIRED-OPEN gating. Evidence: `artifacts/phase1/checkpoint_results.txt`,
`file_compare_highlighted.png`, `folder_compare_grid.png`.

`build/ui_redesign_screens.ps1` — nested-tree and settings-dialog evidence:
`artifacts/ui-redesign/folder_tree_nested.png`, `settings_dialog.png`.

## Manual verification (against the checklist)

1. App launches with the ribbon, path row, dominant panes — verified via checkpoint
   screenshots at 100% scaling.
2. Compare/navigate/copy/save flows — exercised end-to-end by the checkpoint (real UIA
   clicks on the ribbon buttons).
3. Settings dialog opens from the ribbon Options button and exposes whitespace/case/regex/
   structured-view/profile settings (screenshot evidence).
4. Nested folder compare shows `docs`, `src`, `src\app` folders with indented children,
   expand/collapse buttons, per-state colors, and one-sided/changed/match states
   (screenshot evidence + VM tests).
5. Double-click open and filters covered by VM tests; filter buttons exercised by the
   checkpoint.
6. DPI: not manually swept at 125%/150% this run — flagged as a known limitation; layout
   uses only scalable WPF primitives and a horizontally scrollable ribbon.
