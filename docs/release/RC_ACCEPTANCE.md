# Release Candidate Acceptance

**Date:** 2026-07-02  
**Verdict:** **ACCEPTED** for controlled user testing (portable build)

## Scope delivered

| Area | Status |
|------|--------|
| 2-way file compare (diff, merge, save, search, rules) | Complete |
| 2-way folder compare (metadata + content hash) | Complete |
| 3-way file compare + manual output merge | Complete |
| 3-way folder compare | Complete |
| Settings, recents, pinned sessions | Complete |
| JSON/XML formatted comparison projection | Complete |
| Moved-block markers + rename candidates | Complete |
| Unified diagnostics + safe failure modes (POL-02) | Complete |
| Performance measurement + key optimizations (POL-03) | Complete |
| Usability / keyboard / DPI-oriented UI pass (POL-04 Required) | Complete |
| Expanded regression tests (POL-05) | Complete |
| Dependency/compliance review (POL-06) | Complete |
| Portable publish + user docs (POL-07) | Complete |

## Explicit exclusions (verified absent)

- Automatic merge/sync, binary/image/Office/PDF compare
- VCS integration, patch application, audit/archive/report designer
- Printing, MSI/MSIX installer, auto-updater, licensing/activation
- Cloud telemetry and crash reporting
- Cross-platform UI, theme editor, paid control suites

## Evidence

| Check | Result |
|-------|--------|
| Bounded tests (Debug + Release) | **378/378** pass |
| Phase-1 checkpoint | Pass |
| Performance smoke | Pass (`build/pol03_performance_smoke.ps1`) |
| Compliance tests | `ExcludedFeatureComplianceTests` pass |
| Publish verification | `build/publish_verify.ps1` — **PASS** (checkpoint on published exe) |
| Dependencies | `docs/DEPENDENCIES.md` — MSTest 4.0.2, AvalonEdit 6.3.1.120 only |

## Known issues (non-blocking backlog)

| ID | Severity | Item | Rationale |
|----|----------|------|-----------|
| BL-01 | Low | Shell recents bar always visible (POL4-M01) | Usability polish; not required for RC |
| BL-02 | Low | 3-way narrow-width 2×2 editor layout (POL4-M02) | Wide-screen assumption documented |
| BL-03 | Low | Command palette (POL4-D01) | Deferred nice-to-have |
| BL-04 | Low | Middle-click tab close (POL4-D04) | Deferred |
| BL-05 | Info | Full 125/150/200% DPI pixel verification | POL-04 layout fixes applied; manual pass on physical hardware recommended |

## Release blockers

None.

## Recommendation

Ship the portable **Release** build for internal/user acceptance testing. Triage backlog items after feedback.
