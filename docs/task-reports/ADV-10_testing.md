# ADV-10 Testing Report

**Task:** Verify optional content/hash folder comparison and timestamp tolerance  
**Date:** 2026-07-02  
**Verdict:** **PASS** — proceed to ADV-11

## Baseline checks

| Check | Result |
|-------|--------|
| `dotnet restore` | OK |
| `dotnet build -c Debug` | OK |
| `dotnet test -c Debug` (bounded) | **318/318** passed |
| `dotnet build -c Release` | OK |
| `dotnet test -c Release` (bounded) | **318/318** passed |
| `docs/DEPENDENCIES.md` | Unchanged (no new packages) |
| `build/phase1_checkpoint.ps1` | **ALL PASSED** |

## Automated coverage

| Fixture | Tests |
|---------|-------|
| Identical bytes, different timestamps → `ContentMatch` in hash mode | `ContentHashFolderComparisonTests` |
| Same metadata, different bytes → `ContentDifferent` in hash mode | `ContentHashFolderComparisonTests` |
| Quick mode keeps `MetadataDifferent` with zero tolerance | `ContentHashFolderComparisonTests` |
| Timestamp tolerance → `MetadataMatch` in quick mode | `ContentHashFolderComparisonTests` |
| Hash cancellation returns cancelled | `ContentHashFolderComparisonTests` |
| Streaming hasher + large file hash | `ContentHashFolderComparisonTests` |
| `ContentDifferent` in Differences filter; `ContentMatch` excluded | `FolderCompareWorkspaceViewModelTests` |
| Default quick metadata regression | existing folder engine + checkpoint tests |

## Manual verification notes

- Folder Compare and 3-Way Folder Compare show **Mode** combo (default Quick Metadata).
- **Timestamp tolerance (seconds)** enabled only in quick metadata mode.
- Content Hash mode status shows hashing progress; row states use Content match / Content different labels and colors.

## Defects

None found in automated suite.

## Recommendation

**Proceed** with ADV-11 (settings, recents, and pinned sessions UI). ADV-10 content hash mode is complete.
