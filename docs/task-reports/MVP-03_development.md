# MVP-03 Development Report

**Task:** Implement text loading, identity tokens, encoding/line-ending preservation, and atomic save foundation
**Date:** 2026-07-02
**Status:** Complete

## Feature boundary implemented

UI-independent text document loading (decode policy, line records, metadata, identity token)
in `MergeTool.Text`, raw IO and safe atomic saving in `MergeTool.FileSystem`, shared
encoding/path primitives in `MergeTool.Core`. No editor UI, no syntax highlighting, no diff
logic, no editing sessions.

## Objective completed

### MergeTool.Core

- `PathNormalization.NormalizeFullPath` — the product-wide path normalization primitive.
- `TextEncodings` — supported encodings ("utf-8", "utf-16", "utf-16BE", "windows-1252"),
  write-encoding factory (no implicit BOM emission), and BOM byte tables.
- `Windows1252Encoding` — internal ANSI fallback codec; decodes every byte deterministically,
  throws `EncoderFallbackException` on unmappable characters when encoding.
- `TextWritePolicy` — encoding name + BOM flag used to preserve a document's byte format.
- `FileVersionToken.FromBytes` — SHA-256-based token construction from raw bytes.
- `IRawFileReader` / `RawFileContent` — byte-read abstraction so Text never references
  FileSystem (dependency direction preserved).

### MergeTool.Text

- `TextLine` (zero-based index, content, original terminator) and `LineTerminator`.
- `TextDocumentMetadata` — normalized path, display name, encoding name, BOM flag, dominant
  line ending, mixed-ending flag, final-newline flag, byte length, last-write UTC, read-only
  flag, classification, load diagnostics; `ToWritePolicy()` yields the preserving policy.
- `TextDocumentSnapshot` — immutable snapshot (metadata + identity + text + lines).
- `TextDecoder` — the exact guardrail policy: BOM detection (UTF-8/UTF-16 LE/BE) → strict
  UTF-8 → windows-1252 fallback with `MT-TXT-001` warning; NUL-heavy/undecodable bytes become
  `NotTextForThisProduct` (`MT-TXT-002`), never binary support.
- `TextLineSplitter` — single-pass splitting preserving terminator facts; dominant-ending
  rule (most frequent; ties CRLF > LF > CR; CRLF default when no breaks); final-newline flag.
- `TextComposer` — rebuilds exact text from line records (roundtrip proven by tests).
- `TextDocumentLoader.LoadAsync(path, ct)` — builds snapshot + `FileIdentity` from load
  bytes; expected IO errors become failed results with safe diagnostics (`MT-IO-001`);
  cancellation propagates for the application layer to convert.

### MergeTool.FileSystem

- `PhysicalFileReader` — reads bytes, normalized path, last-write UTC, read-only flag.
- `SafeTextFileWriter.SaveAsync(expectedIdentity, text, writePolicy, ct)`:
  1. Re-reads the current file identity; any mismatch (including deletion) returns
     `SaveStatus.ExternalModificationConflict` without writing.
  2. Writes BOM + encoded payload to a temp file in the target directory, flushes to disk,
     then `File.Replace` for atomic replacement; temp file removed on failure.
  3. Preserves encoding/BOM via the caller-supplied policy; never converts line endings;
     unmappable characters throw rather than silently re-encoding.
  4. Returns the new `FileIdentity` for subsequent saves.

### Fixtures

`tests/TestData/Text/` now contains the committed fixture set (UTF-8 ±BOM, UTF-16 LE/BE,
ANSI fallback, CR-only, mixed endings without final newline, empty, NUL-heavy) with a README.
Generation snippet (PowerShell, byte-exact):
see `FixtureFileTests` for expected content; fixtures were created with
`[IO.File]::WriteAllBytes` using explicit byte arrays.

## Design decisions and alternatives rejected

- **Internal windows-1252 codec** instead of `System.Text.Encoding.CodePages`: avoids a
  package for one code page, and gives strict (throwing) encode behavior so saves cannot
  silently lose characters. Rejected alternative: package + `Encoding.GetEncoding(1252)`,
  whose best-fit fallback can silently substitute `?`.
- **Encodings and the reader abstraction live in Core** because Text and FileSystem may not
  reference each other (guardrail direction). Rejected: duplicating the codec per layer.
- **Full-token conflict check** (length + timestamp + hash): a touch without content change is
  reported as a conflict — safe false positive; MVP resolves via Reload.
- **`SaveAsync` takes composed text + policy**, not Text-layer metadata, keeping FileSystem
  independent of Text types (the task's `metadata` parameter is satisfied by
  `TextDocumentMetadata.ToWritePolicy()`).
- **No-BOM UTF-16 is unsupported by policy** (NUL bytes → `NotTextForThisProduct`), matching
  the guardrails' documented decode order.

## Tests added/updated (45 new; 77 total)

- `TextDecoderTests` (12): BOM detection ×3, strict UTF-8, windows-1252 fallback + warning,
  NUL-heavy, UTF-16 embedded NUL, odd-length UTF-16, invalid UTF-8 after BOM, empty file,
  1252 byte roundtrip, 1252 strict-encode throw.
- `TextLineSplitterTests` (10): CRLF/LF/CR, mixed + dominant, tie rule, no final newline,
  empty text, CRLF non-splitting, empty lines, exact compose roundtrip over 7 samples.
- `TextDocumentLoaderTests` (4): metadata/identity construction, failed-read diagnostics,
  not-text classification, cancellation propagation.
- `FixtureFileTests` (6): committed on-disk fixtures decode/classify exactly as documented.
- `SafeTextFileWriterTests` (13, in Application.Tests per architecture decision): byte-exact
  roundtrips for UTF-8 BOM / UTF-16 LE / UTF-16 BE / ANSI fallback / mixed endings without
  final newline / empty file; external-modification refusal; deleted-target refusal; no temp
  file after success or after encoding failure; new-identity chaining; physical missing-file
  diagnostics; read-only flag.

## Build/test commands and results

```powershell
dotnet build -c Debug        # success, 0 warnings, 0 errors
dotnet test -c Debug -m:1    # 77/77 passed
dotnet build -c Release      # success, 0 errors
dotnet test -c Release -m:1  # 77/77 passed
```

## Known limitations / deliberate deferrals

- Editing sessions and `DocumentRevision` arrive in MVP-05 per the task plan; documents are
  read-only snapshots here.
- `File.Replace` requires same-volume temp placement, which is guaranteed by writing the temp
  file into the target directory.
- Progress reporting is not wired for single-file loads (they are fast); folder scanning
  (MVP-09) will exercise `OperationProgress`.

## Dependencies introduced

None.
