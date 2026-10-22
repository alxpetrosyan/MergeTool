# MVP-10 Testing Report

**Task:** Verify folder comparison workspace, filters, and paired text-file opening
**Date:** 2026-07-02
**Verdict:** **Pass**

## Commands run

| Command | Exit | Result |
|---------|------|--------|
| `dotnet restore` | 0 | OK |
| `dotnet build -c Debug` | 0 | OK |
| `dotnet test -c Debug` | 0 | 202 tests passed |
| `dotnet build -c Release` | 0 | OK |
| `dotnet test -c Release` | 0 | 202 tests passed |

## Automated coverage exercised

- `FolderCompareWorkspaceViewModelTests` — filter inclusion rules, root validation,
  compare + filter behaviour, paired open enablement, request construction, launcher paths.
- Existing shell lifecycle tests updated for new folder workspace factory.

## Manual scenarios (review-based)

The WPF app builds successfully. Manual walkthrough items from the testing task map to
implemented behaviour as follows:

1. **Mixed row states** — engine + VM tests use temp trees with match/different/left-only.
2. **Filters** — `FolderCompareFilterRules` unit tests lock inclusion semantics.
3. **Paired open** — launcher test verifies correct left/right paths passed to File Compare
   workflow without code-behind file reads.
4. **One-sided open** — command disabled; `OneSidedOpenMessage` defined for prompt path.
5. **Cancel** — workspace does not clear `_allRows` on cancelled scan; status text
   distinguishes cancelled rescan when prior results exist.
6. **Legend** — view binds `MetadataLegendText` with mandated wording.

## Architecture / licence checks

- No new packages; `docs/DEPENDENCIES.md` unchanged.
- Folder compare view models call application use cases only; paired open routes through
  `IFileComparisonLauncher` / `OpenFileComparisonUseCase`.

## Defects

None.

## Recommendation

**Proceed** to MVP-11 (integrated MVP stabilization and release gate).
