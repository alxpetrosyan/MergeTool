# UI-REDESIGN-01 Layout Notes

## 1. Before / after

**Before.** Every workspace tab carried its own stack of dense control rows: path boxes with
a Compare/Recompare column, a profile combo row, a "Comparison rules" expander, a navigation
strip, a merge/save strip, search expander, diagnostics, and only then the editors. The
folder tab showed a flat list of relative paths with no hierarchy and its own
compare/filter/operation rows. Command placement differed per tab and the editors got
whatever vertical space was left.

**After.** One shell-level ribbon owns all high-frequency commands, identical for every tab
(buttons disable themselves when the active workspace lacks the command). Each tab starts
with a single path selector row, followed directly by the comparison content, which now
dominates the window. Low-frequency comparison settings moved into a modal dialog. A single
status bar reports state, progress, stale flag, difference position, and encodings. The
folder tab renders a real tree-grid of directories and files.

## 2. Command grouping

| Ribbon group | Commands | Scope |
| --- | --- | --- |
| New | File Compare, Folder Compare, 3-Way Files, 3-Way Folders | Shell (always enabled) |
| Compare | Compare, Recompare (F5) | Active workspace |
| Navigate | Previous difference (Shift+F7), Next difference (F7) | File compare workspaces |
| Merge | Copy to Left, Copy to Right | File compare workspaces |
| Save | Save Left, Save Right, Save All (Ctrl+Shift+S) | File compare workspaces |
| View | Expand All (collapsed regions), Find (Ctrl+F), Options (settings dialog) | File compare workspaces |

Kept inside the workspace views: folder filters and folder operations (copy/delete/rename
candidates — they act on grid selection), Reveal selection, Difference panel toggle, find &
go expander.

## 3. Ribbon

`RibbonQuickActionButton` (Button subclass) renders a Segoe MDL2 Assets glyph above a short
label — the system icon font that ships with Windows, so no external or paid assets. The
implicit style in `App.xaml` gives hover/pressed chrome, dims disabled buttons, and contains
the key trick: a trigger disables any button whose `Command` binding resolved to null, which
happens exactly when the active workspace type does not expose that command. Contextual
buttons bind through `ActiveWorkspace.XxxCommand`, so enablement follows each command's
`CanExecute` — merge buttons disable with no selected difference, while stale, or when the
target document is read-only; saves disable unless a dirty writable document exists. The
ribbon is one row (~64 px) inside a horizontal ScrollViewer so it degrades gracefully on
narrow windows.

## 4. Path selector (`PathComboSelector`)

Reusable control used by both file and folder tabs (2-way today; the 3-way tabs still use
plain text boxes, see limitations). Features:

- free typing with two-way binding to the workspace path property;
- browse button bound to the workspace's existing picker command;
- dropdown button listing recent comparison paths (persisted recents via
  `IRecentPathProvider`, most recent first, de-duplicated);
- typing a trailing `\` or `/` pops child folders/files of that directory (directories
  first, capped at 25, best-effort — IO errors are swallowed);
- a partial last segment filters children by prefix; Down-arrow opens the list, Enter
  accepts, Escape closes;
- non-existing paths get a red border and tooltip (`File`/`Folder`/`Any` validation kinds);
  invalid input never throws.

Suggestion/validation logic is a pure static class (`PathSuggestionProvider`) covered by
unit tests.

## 5. Folder comparison tree

`FolderComparisonTreeBuilder` (in `MergeTool.Folders`) turns the flat scan rows into a
hierarchy, synthesizing intermediate directory nodes and flagging subtrees that contain
differences or errors. The WPF workspace flattens the tree into visible rows (filter- and
expansion-aware) rendered by a virtualized DataGrid:

- Name column with depth indentation and ▸/▾ expander buttons; folders sort before files;
- columns: State, Type (Folder/extension), Left/Right size, Left/Right modified (UTC),
  Diagnostics;
- states: Metadata match, Metadata different, Content match/different (hash mode), Left
  only, Right only, Error, Skipped reparse point; directories without their own scan row
  show "Contains differences" or blank — metadata match is never labelled content-identical;
- filters: All, Differences, Left only, Right only, Errors/Skipped; ancestor folders stay
  visible while any descendant passes the filter; Expand all / Collapse all buttons;
- double-click: folders toggle, paired text files open a file comparison tab, one-sided
  files show a clear "pair is missing" message;
- context menu: open file comparison, copy left→right, copy right→left, reveal left/right
  in Explorer;
- scans stay asynchronous and cancellable; cancellation preserves the last good tree.

## 6. Known limitations

- 3-way file/folder tabs kept their previous in-view layouts and plain path boxes; only
  shared ribbon commands (Compare/Recompare, saves where applicable) apply to them.
- The settings dialog hosts the 2-way comparison rules; 3-way tabs have no Options dialog.
- Folder tree rows default to fully expanded after a scan; very large trees may prefer
  collapsed-by-default (cheap follow-up).
- Recents-based path suggestions refresh after a completed comparison, not live while
  another tab records a session.
- 125%/150% DPI was not manually swept this run; the layout is DPI-independent WPF and the
  ribbon scrolls horizontally, but a visual sweep is still recommended.

## 7. Screenshots

- `artifacts/phase1/file_compare_highlighted.png` — redesigned file compare (ribbon, path
  row, dominant panes, difference rail).
- `artifacts/phase1/folder_compare_grid.png` — folder compare after checkpoint run.
- `artifacts/ui-redesign/folder_tree_nested.png` — nested tree with expanders, folder
  states, one-sided/changed rows.
- `artifacts/ui-redesign/settings_dialog.png` — comparison settings dialog over the shell.

Regenerate with `build/ui_redesign_screens.ps1` (Release build required).
