# MVP Acceptance Criteria (Phase 1 Gate)

This document is the mandatory manual and automated acceptance checklist for Scope 1.
Execute it before starting Scope 2 (advanced features).

## In scope (must work)

One integrated WPF application (`MergeTool.App.Wpf`) that:

1. Opens two text files in a File Compare workspace and compares them asynchronously.
2. Shows deterministic line-level highlighting for **modified**, **inserted** (right-only block), and **deleted** (left-only block) regions.
3. Navigates next/previous differences without wrapping at ends.
4. Copies the selected difference block left→right and right→left (one block at a time).
5. Marks comparison **stale** after merge or edit; requires **Recompare** before another merge.
6. Saves dirty documents safely (encoding/BOM/line endings preserved; external-change conflict refuses overwrite).
7. Opens two folder roots, runs recursive quick metadata comparison with cancel support.
8. Labels folder rows: **Left only**, **Right only**, **Metadata match**, **Metadata different**, **Error**, **Skipped reparse point**.
9. Filters folder rows: All, Differences (excludes metadata match), Errors/Skipped.
10. Opens a paired text file comparison from a folder row when both file paths exist.

## Explicit exclusions (must NOT be claimed)

- Automatic merge, 3-way compare, intraline diff, syntax highlighting, minimap.
- Folder copy/delete/sync, content hashing, tree view, file watcher auto-refresh.
- Binary/image/PDF/Office comparison, patch export, VCS integration, reports, printing, installer.
- Force overwrite on save conflict.

## Test data setup

| Path | Purpose |
|------|---------|
| `tests/TestData/CompareFixtures/` | File compare: identical, insert, delete, modify, length, not-text |
| `tests/TestData/FolderFixtures/left` & `right` | Folder compare states + `changed.txt` for paired open |
| `tests/TestData/Text/` | Encoding/line-ending unit fixtures |

Sync `FolderFixtures/left/same.txt` and `right/same.txt` timestamps before folder runs if metadata-match row is required.

## Build and test commands

```powershell
dotnet restore
dotnet build -c Debug
dotnet build -c Release

# Recommended release gate (sequential test projects, avoids parallel host pressure):
powershell -ExecutionPolicy Bypass -File build/run_tests_bounded.ps1 -Configuration Debug
powershell -ExecutionPolicy Bypass -File build/run_tests_bounded.ps1 -Configuration Release

# Full solution test (may stress memory on constrained machines):
dotnet test -c Debug
dotnet test -c Release
```

## UI automation acceptance

```powershell
dotnet build -c Release
powershell -ExecutionPolicy Bypass -File build/phase1_checkpoint.ps1
```

Expected final line: `ALL PHASE-1 CHECKPOINT CHECKS PASSED`

Evidence: `artifacts/phase1/checkpoint_results.txt`

## Expected UI wording (must appear verbatim where noted)

| Context | Text |
|---------|------|
| Stale comparison | `Comparison is stale. Recompare before merging.` |
| Folder legend | `Metadata match = size + UTC modification time only.` |
| Metadata match row state | `Metadata match` (never “Same” or “Identical”) |
| Same file compare | `The same file cannot be compared with itself. Choose two different files.` |
| Not-text file | `One of the selected files is not text and cannot be compared by this product.` |
| Save conflict | `The file was changed outside this application. Nothing was overwritten.` |

## Gate scenarios (MVP-11 tester checklist)

| # | Scenario | Primary evidence |
|---|----------|------------------|
| 1 | Identical text files | UI: `No differences` |
| 2 | Insert/delete/modify blocks | UI + `ManualTwoWayMergeServiceTests` |
| 3 | Navigate all differences | UI navigation 3-of-3 |
| 4 | Merge each hunk type both directions, recompare, save | UI + `MergeAndSaveIntegrationTests` |
| 5 | Stale merge refused | `FileCompareStaleFlowTests`, UI stale after copy |
| 6 | External save conflict | `DocumentPersistenceServiceTests` |
| 7 | Folder roots with every MVP row state | `FolderFixtures` + UI grid |
| 8 | Filter + paired file open | `FolderCompareWorkspaceViewModelTests` + `MvpEndToEndIntegrationTests` |
| 9 | Cancel folder scan | `QuickFolderComparisonEngineTests` cancellation |
| 10 | Non-text input safe | UI `notext_nul.bin` + `OpenFileComparisonUseCaseTests` |
| 11 | Dirty close no silent data loss | `MainWindowViewModel` close prompts (automated VM) |

## Known limitations

See `docs/known_limitations.md`.

## Pass criteria

All gate scenarios pass with no open Critical/High defects. Medium/Low items require owner approval for conditional pass.
