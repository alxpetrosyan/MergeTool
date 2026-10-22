# ADV-12 Testing Report

**Task:** Verify safe JSON/XML formatted comparison projection  
**Date:** 2026-07-02  
**Verdict:** **PASS** — proceed to ADV-13

## Baseline checks

| Check | Result |
|-------|--------|
| `dotnet restore` | OK |
| `dotnet build -c Debug` | OK |
| `dotnet test -c Debug` (bounded) | **336/336** passed |
| `dotnet build -c Release` | OK |
| `dotnet test -c Release` (bounded) | **336/336** passed |
| `docs/DEPENDENCIES.md` | Unchanged (no new packages) |
| `build/phase1_checkpoint.ps1` | **ALL PASSED** |

## Automated coverage

| Fixture | Tests |
|---------|-------|
| Valid JSON pretty-print (multi-line) | `StructuredTextProjectionTests` |
| Valid XML pretty-print (multi-line) | `StructuredTextProjectionTests` |
| Invalid JSON → diagnostic `MT-FMT-001` | `StructuredTextProjectionTests`, `TwoWayComparisonSessionTests` |
| Invalid XML → diagnostic `MT-FMT-002` | `StructuredTextProjectionTests` |
| Formatted JSON disables merge, allows navigation | `TwoWayComparisonSessionTests` |
| Formatted XML disables merge | `TwoWayComparisonSessionTests` |
| Source documents not mutated on parse failure | `TwoWayComparisonSessionTests` |
| Line round-trip helpers | `StructuredTextProjectionTests` |

## Manual verification notes

- **Comparison rules → Structured view** offers Raw / Formatted JSON / Formatted XML.
- After Recompare with valid JSON/XML, pane header shows **Formatted JSON** or **Formatted XML**; raw mode shows **Raw**.
- Invalid input shows warning diagnostic; comparison still completes on original text.
- **Copy to Left/Right** disabled in formatted mode with explanatory message.
- Editors are read-only in formatted mode.

## Defects

None found in automated suite.

## Recommendation

**Proceed** with ADV-13 (moved-block markers and rename hints UI integration).
