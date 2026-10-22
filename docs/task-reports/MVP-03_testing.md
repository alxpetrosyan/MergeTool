# MVP-03 Testing Report

**Task:** Verify: Implement text loading, identity tokens, encoding/line-ending preservation, and atomic save foundation
**Date:** 2026-07-02
**Verdict:** PASS

## Commands run and exit results

| Command | Result |
| --- | --- |
| `dotnet build -c Debug` | exit 0; 0 warnings, 0 errors |
| `dotnet test -c Debug -m:1 --no-build` | exit 0; 77/77 passed |
| `dotnet build -c Release` | exit 0; 0 errors |
| `dotnet test -c Release -m:1 --no-build` | exit 0; 77/77 passed |

## Required cases exercised

- **UTF-8 with BOM / without BOM, UTF-16 LE/BE with BOM, documented ANSI fallback** —
  automated in `TextDecoderTests` (in-memory bytes) *and* `FixtureFileTests` (committed
  fixture files under `tests/TestData/Text/`), plus byte-exact save roundtrips for each in
  `SafeTextFileWriterTests`. The ANSI fallback is windows-1252 and always adds warning
  `MT-TXT-001` ("decoding was heuristic").
- **CRLF, LF, CR, mixed endings, empty file, missing final newline** — `TextLineSplitterTests`
  (splitting, dominant-ending, tie rule, roundtrip) and save roundtrips preserve the original
  bytes exactly, including missing final newline.
- **Invalid/ambiguous bytes and NUL-heavy data** — invalid UTF-8 falls back with a warning;
  NUL bytes without BOM, NUL characters inside UTF-16, odd-length UTF-16, and invalid UTF-8
  behind a UTF-8 BOM all classify `NotTextForThisProduct` with error `MT-TXT-002`. No binary
  comparison path exists.
- **Stale identity** — `Save_RefusesToOverwriteExternallyModifiedFile`: load, externally
  alter bytes, attempt save → `ExternalModificationConflict`; the external content is
  verified untouched. `Save_RefusesWhenTargetFileWasDeleted` covers deletion.
- **Atomic save hygiene** — `Save_LeavesNoTempFileAfterSuccess` and
  `Save_LeavesNoTempFileAfterEncodingFailure` prove no temp file remains after success or
  failure, and that a failed save leaves the original file bytes intact.

## Source inspection

- Identity checking uses **bytes**: `FileVersionToken.FromBytes` computes SHA-256 over raw
  content; the writer re-reads disk bytes before overwriting. No timestamp-only shortcut.
- No UI types in Core/Text/FileSystem (all target `net10.0`; smoke tests enforce reference
  direction; no `System.Windows`/AvalonEdit usage).
- No silent encoding conversion: an ambiguous (fallback-decoded) document saves back to its
  original bytes; unmappable characters throw `EncoderFallbackException` instead of lossy
  substitution.
- Expected user-data errors (missing file, access denied) surface as failed
  `OperationResult` with safe diagnostics, not crashes
  (`PhysicalReader_MissingFile_BecomesFailedLoadResult`).

## Failure-condition review

No silent overwrite, no lossy save without warning (lossy save is impossible — it throws),
BOM/line-ending handling verified byte-exact, no crash on expected user-data errors, no
accidental binary comparison behavior.

## Architecture/licence checks

`docs/DEPENDENCIES.md` unchanged; no new packages. New shared primitives went to Core with a
documented rationale (`docs/architecture/README.md`, MVP-03 section).

## Defects created

None.

## Recommendation

**Proceed** to MVP-04.
