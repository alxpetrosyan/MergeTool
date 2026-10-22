# Scope 3 (Polish) — Release Candidate Assessment

**Product:** MergeTool — Windows manual text/folder comparison and merge tool  
**Assessment date:** 2026-07-02  
**Audience:** CEO / CTO / architecture review  
**Prepared after:** MVP (Scope 1), Advanced (Scope 2), Polish (Scope 3) per task pack index

---

## 1. Final feature matrix

### Implemented (release scope)

| Feature | Evidence |
|---------|----------|
| 2-way text file compare (line diff, intraline, rules, profiles) | MVP-07/08, ADV-01–03, phase-1 checkpoint |
| Manual 2-way merge + safe save (conflict detection, atomic write) | MVP-03/08, `DocumentPersistenceServiceTests` |
| Folder quick metadata compare + filters + paired open | MVP-09/10, checkpoint |
| Folder content-hash mode + timestamp tolerance | ADV-10, `ContentHashFolderComparisonTests` |
| Folder copy/delete with preview + recycle-bin preference | ADV-06, `FolderOperationExecutorTests` |
| Search, go-to-line, difference list, minimap, collapse | ADV-04/05 |
| 3-way text inspection + manual output merge | ADV-07/08 |
| 3-way folder compare | ADV-09 |
| Settings, recents, pinned sessions | ADV-11, `AppSettingsStoreTests` |
| JSON/XML formatted comparison projection (display-only merge gate) | ADV-12 |
| Moved-block markers + rename candidate suggestions | ADV-13 |
| Unified diagnostics, recovery hints, copy details | POL-02 |
| Keyboard shortcuts, collapsed find panel, state banner (POL4-R01–R09) | POL-04, `FileCompareUsabilityViewModelTests` |
| Portable Windows x64 publish | POL-07, `build/publish_verify.ps1` |

### Deliberately excluded

| Excluded capability | Verification |
|---------------------|--------------|
| Automatic merge / sync | No auto-merge code paths; all merges are explicit commands |
| Binary / image / Office / PDF compare | `NotTextForThisProduct` classification; UI rejects non-text |
| VCS / patch / diff export integration | No VCS packages or APIs (`ExcludedFeatureComplianceTests`) |
| Printing, report designer, audit archive | Not present in codebase |
| MSI/MSIX installer, auto-updater, licensing | Publish is portable folder only |
| Telemetry / crash reporting | Forbidden token scan passes |
| Cross-platform UI, theme editor, paid UI suites | WPF + AvalonEdit only |
| Command palette, middle-click tab close | POL-00 deferred backlog |

### Partially implemented / bounded

| Item | Limitation |
|------|------------|
| Intraline diff | Bounded by `IntralineDiffBounds` (line length / work budget) |
| Large files | Full in-memory load; performance smoke guards, not streaming editor |
| 3-way layout on narrow screens | Four columns; 2×2 responsive layout deferred (BL-02) |
| DPI verification | Layout mitigations at 100%; 125/150% manual pass recommended (BL-05) |
| Formatted JSON/XML | Display projection only; invalid input falls back to raw with diagnostic |

---

## 2. Architecture overview

```text
MergeTool.App.Wpf          → views, view models, renderers, dialogs
MergeTool.Application      → use cases, sessions, orchestration
MergeTool.Text             → documents, diff, merge domain
MergeTool.Folders          → scans, row model, operation plans
MergeTool.FileSystem       → IO, atomic save, identities
MergeTool.Core             → diagnostics, results, settings primitives
```

**Dependency direction:** Enforced by `ArchitectureGuardrailsTests` — only WPF references WPF/AvalonEdit; Application never references WPF.

**Intentional deviations documented in POL-01:**

- Settings in Core (not separate assembly) — small JSON, no WPF coupling
- `Task.Run` in comparison sessions — orchestration acceptable at application layer
- View models reference Text/Folders types for presentation labels — workflows still enter via Application use cases
- No DI container — `AppCompositionRoot` manual wiring

---

## 3. Commercial readiness / licence inventory

| Component | Version | Licence | Shipped in publish? |
|-----------|---------|---------|---------------------|
| .NET SDK / runtime | 10.0.301 (`global.json`) | MIT | Runtime required on target (framework-dependent publish) |
| WPF | SDK-bundled | MIT | Yes |
| AvalonEdit | 6.3.1.120 | MIT | Yes (`ICSharpCode.AvalonEdit.dll`) |
| MSTest | 4.0.2 | MIT | **No** (test projects only) |

**Lock files:** `packages.lock.json` per project.  
**Compliance automation:** `ExcludedFeatureComplianceTests` — only MSTest + AvalonEdit as direct NuGet refs.  
**Third-party notices:** `artifacts/MergeTool-portable-win-x64/THIRD_PARTY_NOTICES.txt`  
**Araxis/origin audit:** No verbatim third-party merge-tool assets identified.

*This assessment is engineering evidence, not legal advice.*

---

## 4. Data-safety assessment

| Risk | Mitigation | Test / evidence |
|------|------------|-----------------|
| Silent overwrite on save | External-modification check before write; conflict diagnostic `MT-IO-002` | `Save_AfterExternalModification_RefusesAndOverwritesNothing` |
| Atomic save failure | `SafeTextFileWriter`; IO errors as diagnostics | `DocumentPersistenceServiceTests` |
| Stale merge | Session rejects merge when document revision ≠ comparison revision | `MergePreconditionTests`, stale banner in UI |
| 3-way unresolved regions | Save blocked until all regions resolved | `ThreeWayOutputMergeSession` tests |
| Folder delete | Confirmation dialog; recycle bin when available; permanent-delete prompt | ADV-06 VM/UI tests |
| Folder path escape | Plan validator rejects paths outside roots | `FolderOperationPlanValidatorTests`, `FolderPathSafetyTests` |
| Cancelled operations | Last completed result preserved | `Cancellation_KeepsLastAppliedResultVisible` |
| Unexpected UI exceptions | `MT-UNX-001` diagnostic; no silent swallow | POL-02 `WorkspaceViewModel` catch |

**Untested / residual risk:** Extremely large files held entirely in memory — safe but may fail with OOM rather than graceful degrade (documented in `KNOWN_LIMITATIONS.md`).

---

## 5. Correctness assessment

| Domain | Assessment |
|--------|------------|
| Line diff | Patience-style engine; deterministic; golden/property tests (`RegressionGoldenTests`, `SameInputs_RepeatedRuns`) |
| Merge (2-way) | Copy-block semantics; stale gating; checkpoint merge scenarios pass |
| 3-way manual output | Region resolution + output document; unresolved gate |
| Folder quick metadata | Size + UTC time; tolerance option tested |
| Folder content hash | Streaming SHA-256; metadata mismatch vs content match distinguished |
| Encoding / line endings | Preserved on save; BOM handling tested in MVP-03 suite |
| Comparison rules / projection | Invalid regex returns diagnostic; prior result preserved |

---

## 6. Performance evidence and limits

**Script:** `build/pol03_performance_smoke.ps1`  
**Budgets:** `docs/performance/POL-03_budgets.md`

| Scenario | CI smoke budget (generous) |
|----------|---------------------------|
| 1 MB text open + compare | ≤ 30 s |
| 1,000 folder entries | ≤ 8 s |
| 10,000 folder entries | ≤ 45 s |
| 200-file content-hash tree | ≤ 30 s |

**Optimizations applied:** folder grid virtualization layout, throttled scan progress, thread-pool text decode/split.

**Limits:** No streaming diff or mmap loader; 50k+ folder scans may be slow but cancellable.

---

## 7. Accessibility / keyboard / DPI evidence

| Area | Status |
|------|--------|
| Keyboard (file compare) | F5/F7/Shift+F7/F3/Shift+F3/Ctrl+F/Ctrl+Shift+S — `FileCompareView` InputBindings |
| Automation names | Present on primary controls (checkpoint + XAML audit) |
| State communication | Text banner for stale/dirty/read-only (not colour-only) |
| Destructive actions | Red-styled delete buttons + confirmation |
| DPI | WrapPanel on folder ops; collapsed find panel; **full 125/150/200% pixel pass not automated** |

---

## 8. Portable publish verification

| Check | Result |
|-------|--------|
| Script | `build/publish-portable.ps1` |
| End-to-end verifier | `build/publish_verify.ps1` (tests + publish + checkpoint on published exe) |
| Output path | `artifacts/MergeTool-portable-win-x64/` |
| Required files | `MergeTool.App.Wpf.exe`, app DLLs, `ICSharpCode.AvalonEdit.dll`, `THIRD_PARTY_NOTICES.txt`, `README.txt` |
| Runtime | **Framework-dependent** — .NET 10 Desktop Runtime required unless `-SelfContained` |
| UI smoke on published build | Phase-1 checkpoint **PASSED** against published exe (2026-07-02) |

---

## 9. Automated and manual acceptance evidence

| Command | Outcome |
|---------|---------|
| `.\build\run_tests_bounded.ps1` (Debug) | **378/378** pass |
| `.\build\run_tests_bounded.ps1 -Configuration Release` | **378/378** pass |
| `.\build\pol03_performance_smoke.ps1 -Configuration Release` | Pass |
| `.\build\phase1_checkpoint.ps1` | Pass (dev Release build) |
| `.\build\publish_verify.ps1 -Configuration Release` | Pass (published build + checkpoint) |
| `ExcludedFeatureComplianceTests` | Pass |

Manual: POL-04 layout review per `docs/ui/POL-00_ui_design_review.md`; user docs in `docs/user/`.

---

## 10. Remaining defects

| ID | Severity | Description | Workaround |
|----|----------|-------------|------------|
| BL-01 | Low | Recents bar always visible | None needed |
| BL-02 | Low | 3-way cramped below ~1400 px width | Widen window |
| BL-03 | Low | No command palette | Use mouse/toolbar |
| BL-04 | Low | No middle-click tab close | Use tab ✕ |
| BL-05 | Info | DPI not fully verified above 100% | Manual check on target hardware |

**Release blockers:** None.

---

## 11. Technical debt / backlog (out of product scope)

- Command palette, theme editor, custom shortcut editor
- Responsive 3-way 2×2 editor grid
- Collapsible shell sessions bar
- Drag-drop paths onto headers
- Streaming/lazy load for very large files
- DI container / plugin architecture (explicitly rejected in POL-01)

---

## 12. Final decision

## **RELEASE CANDIDATE PASS**

The product meets the agreed task-pack scope for a Windows-only manual text/folder comparison and merge tool. Automated regression (378 tests), phase-1 UI checkpoint, performance smoke, compliance scan, and **published-build verification** all pass. No critical or high-severity defects are open.

**Recommended next step:** Distribute `artifacts/MergeTool-portable-win-x64/` for controlled user acceptance testing with `docs/user/QUICK_START.md`.
