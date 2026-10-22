# Known Limitations (MVP / Phase 1)

Honest boundaries of the current product. Do not market beyond this list.

## File comparison display

- **No vertical blank-line padding.** Matching blocks can sit at different vertical positions in the two panes. Synchronized scrolling maps by block, not by inserted spacer lines.
- **Line-level blocks only.** No intraline (character-level) highlighting within a line.
- **No syntax highlighting or file-type profiles.**

## Manual merge

- **One selected block at a time.** No multi-select merge or automatic merge-all.
- **Stale gate.** After any edit or merge, navigation and copy commands disable until Recompare completes.
- **No insert-before/after variants** beyond the diff engine’s planned anchor.

## Save safety

- **No force overwrite** when the file changed on disk. User choices: Cancel or Reload (MVP).
- Saves preserve loaded encoding/BOM; merged text adopts target line-ending convention in memory, then writes per document policy.

## Folder comparison

- **Quick metadata only.** `Metadata match` means **file size + UTC last-write time** match. It does **not** prove content equality.
- UI must never label metadata match as “Same”, “Identical”, or “Equal content”.
- **No content hash mode**, timestamp tolerance, or default folder exclusions.
- **Flat virtualized grid** only (no tree, no folder copy/delete/sync).
- **Reparse-point directories** are skipped, not followed.

## Unsupported inputs

- **Not-text files** (e.g. NUL-heavy binaries) are refused with a safe diagnostic. No binary comparison mode.
- No archive, image, PDF, or Office document comparison.

## Performance and environment

- Targets normal source files (~5 MB) and folder trees (~5,000 entries) on a typical developer machine with progress and cancel.
- Running all test assemblies in parallel (`dotnet test` on full solution) may exhaust memory or time out the test host on constrained machines. Use `build/run_tests_bounded.ps1` for release verification.

## Excluded product features (Phase 1)

Automatic merge, 3-way text/folder compare, patch support, version-control integration, reports, printing, portable installer, settings persistence (Scope 2+).
