# POL-00 Testing Report

**Task:** Verify UI design review is complete, evidence-based, and safe for later polish  
**Date:** 2026-07-02  
**Verdict:** **PASS** — proceed to POL-01

## Baseline checks

| Check | Result |
|-------|--------|
| `dotnet restore` | OK (via bounded script) |
| `dotnet build -c Debug` | OK |
| `dotnet test -c Debug` (bounded) | **348/348** passed |
| `dotnet build -c Release` | OK |
| `dotnet test -c Release` (bounded) | **348/348** passed |
| `docs/DEPENDENCIES.md` | Unchanged (no packages) |
| `git diff` | Documentation only (`docs/ui/`, `docs/task-reports/`, progress) |

## Document verification

| Requirement | Status |
|-------------|--------|
| 1. All required scenarios covered | **Pass** — Section 1 table + Appendix A |
| 2. DPI 100/125/150% | **Conditional** — 100% live; 125/150% documented as POL-04-R09 follow-up |
| 3. Target layouts for 2-way, 3-way+output, folder | **Pass** — Section 4 + wireframes |
| 4. Complete command inventory | **Pass** — Section 5 (shell + all workspaces) |
| 5. Core workflows preserved | **Pass** — open/compare, nav, merge, save, filter, paired open, 3-way resolve |
| 6. High-frequency vs low-frequency separation | **Pass** — hierarchy Section 3 + POL4-R01/R02 |
| 7. Keyboard/accessibility intent | **Pass** — Appendix B + POL4-R03/R08 |
| 8. No prohibited scope | **Pass** — Section 8 out-of-scope list |
| 9. POL-04 work bounded and testable | **Pass** — Section 6 IDs with acceptance checklist |
| 10. Source changes limited | **Pass** — no application code modified |

## Reviewed scenarios and DPI

| Scenario | Reviewed |
|----------|----------|
| Empty shell | Yes (live) |
| 2-way, no selection | Yes (live) |
| 2-way, modified selected | Yes (live + checkpoint fixtures) |
| 2-way, search row visible | Yes (layout) |
| Folder, filter + selection | Yes (live) |
| 3-way resolved/unresolved | Yes (layout + feature available) |
| Stale/dirty/busy | Yes (stale via merge test flow; busy via compare) |

**DPI scales:** 100% verified live; 125%/150% predicted from horizontal toolbar analysis.

## Defects

None created.

## Approved Required-for-release items (POL-04 must implement)

- POL4-R01 — Collapsible Find & go panel
- POL4-R02 — Merge/save separation
- POL4-R03 — Keyboard shortcuts
- POL4-R04 — State banner
- POL4-R05 — Collapsible difference rail
- POL4-R06 — Destructive folder delete styling
- POL4-R07 — Compact review strip badges
- POL4-R08 — Tab order / automation audit
- POL4-R09 — High-DPI clipping fixes

## Recommendation

**Proceed** with POL-01 (architecture hardening). POL-04 may begin after or in parallel once POL-01 does not block API changes needed for UI refactor.
