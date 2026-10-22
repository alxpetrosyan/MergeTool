# POL-00 UI Design Review and Information Architecture Plan

**Date:** 2026-07-02  
**Reviewer basis:** Live application launch at 100% DPI (1920×1080, default window 1100×700) plus inspection of WPF views (`MainWindow.xaml`, workspace views) and view models.  
**DPI note:** 125% and 150% observations are extrapolated from fixed-width controls, horizontal toolbars, and non-wrapping `StackPanel` rows documented below; full pixel verification is assigned to POL-04 manual pass.

**Related wireframes:** `docs/ui/POL-00_layout_wireframes.txt`

---

## 1. Current-state assessment

MergeTool delivers a functionally complete comparison product across four workspace types (2-way file, folder, 3-way file with output merge, 3-way folder). The architecture correctly keeps comparison logic in the application layer; WPF presents state and forwards intent.

The dominant UX problem is **chrome density**: persistent multi-row toolbars and side panels consume vertical space before editors or folder grids receive focus. The shell adds two full-width bars (workspace launch + recents/pinned) above every tab. File compare stacks up to **seven auto-height rows** above the editor region (setup, profile, rules expander header, navigation/merge/save, search/go-to, diagnostics, pane metadata). At the default 700 px window height, editors often begin below ~45–55% of the client area.

Secondary issues: **merge and save sit on the same toolbar row** as navigation; **search/filter occupies a permanent row** despite being intermittent; **no keyboard shortcuts** are defined (`InputBindings` absent); **state feedback** (stale, dirty, read-only, formatted mode) is text-only in small header badges easy to miss; **folder compare** mixes filtering, destructive ops, and open-compare on one horizontal strip that will clip at 125%+ DPI.

Strengths: semantic diff colours with legend; collapsible comparison-rules expander (good pattern to extend); difference list + minimap aid navigation; 3-way output workflow is logically grouped; automation names exist on many controls; recents/pinned session bar is useful once comparison is underway.

### Scenario evidence summary

| # | Scenario | 100% DPI observation | 125%/150% risk |
|---|----------|------------------------|----------------|
| 1 | Empty shell | Launch bar + empty recents row + blank tab area; product name visible; no onboarding hint | Recents row buttons may wrap or clip on narrow widths |
| 2 | 2-way compare, no selection | Seven chrome rows before editors; summary in footer; navigation text empty | Search row and merge/save row compete for width; path textboxes truncate |
| 3 | 2-way compare, modified selected | Nav text “Difference n of m”; merge buttons enable; colours in editors | Same clipping; selected diff not echoed in header strip |
| 4 | 2-way with search visible | Full search row always visible (pattern, 4 checkboxes, pane combo, go-to) | High probability of horizontal overflow / hidden controls |
| 5 | Folder compare, filter + selection | Mode row + filter/ops row + grid; legend footer | Ops toolbar (8+ buttons) clips; grid remains usable |
| 6 | 3-way with resolved/unresolved | Four editor columns narrow; region list 220 px; resolve row separate | Four columns unreadable below ~1400 px width; list competes with output |
| 7 | Stale/dirty/busy states | Stale message in summary; dirty badge in pane header; busy in status bar | Messages easy to miss; no persistent banner |

---

## 2. Issues (prioritized)

| ID | Severity | Issue |
|----|----------|-------|
| IA-01 | **High** | File compare editors are not the primary visual focus; ~50% vertical space lost to chrome at default window size. |
| IA-02 | **High** | Merge direction commands and save commands share one toolbar row without visual separation; mis-click risk. |
| IA-03 | **High** | Search, go-to-line, and filter controls use a permanent full-width row (low-frequency, high footprint). |
| IA-04 | **High** | No application keyboard shortcuts; daily use requires mouse-only navigation. |
| IA-05 | **Medium** | Shell recents/pinned bar always visible; pushes workspace content down even for first-time compare. |
| IA-06 | **Medium** | Folder compare operations toolbar mixes destructive delete with copy/open on one line. |
| IA-07 | **Medium** | 3-way four-column layout does not scale below wide screens; output editor width insufficient. |
| IA-08 | **Medium** | Stale/dirty/read-only/formatted states rely on small orange text; not glanceable. |
| IA-09 | **Medium** | Difference list + minimap always consume 180+ px horizontal space; no collapse. |
| IA-10 | **Medium** | 3-way folder grid has many state colours; legend is long; colour-only distinction for some states. |
| IA-11 | **Low** | Profile + rules + structured format spread across multiple areas; advanced setup fragmented. |
| IA-12 | **Low** | Footer legends duplicate information already in docs; consume vertical space. |
| IA-13 | **Low** | Tab close “✕” is small; no middle-click close convention documented. |
| IA-14 | **Low** | Rename-candidate panel appears only after find; discoverability depends on toolbar scan. |

---

## 3. Recommended command hierarchy

### A. Global / workspace actions (shell)
- Open workspace types: File Compare, Folder Compare, 3-Way Folder, 3-Way Compare
- Session history: Recents, Pinned, Open, Pin active, Clear recents, Reset settings
- Tab management: Close workspace
- Operation lifecycle: Cancel (while busy), status text

### B. Comparison setup (per workspace header)
- Path/root pickers and browse (…)
- Compare / Recompare
- Profile (file compare)
- Comparison mode / timestamp tolerance (folder)
- Advanced rules (collapsible): whitespace, ignore rules, moved blocks, structured view

### C. Review / navigation
- Previous / Next difference
- Next unresolved (3-way)
- Difference index (“n of m”)
- Expand all / Reveal selection (file compare collapse)
- Region list selection (3-way)
- Minimap click navigation
- Difference list selection

### D. Contextual merge actions (only when selection + eligible)
- Copy to Right / Copy to Left (2-way)
- Use base / Use left / Use right (3-way region resolution)
- **Visually separated from save**

### E. Save actions
- Save Left / Save Right / Save All (2-way)
- Save output… (3-way)
- Distinct grouping; never adjacent to merge without separator

### F. Find / Filter / Go-to (collapsible panel)
- Search pattern, find prev/next, search options
- Go to line
- Folder filters: All / Differences / Errors-Skipped
- Find rename candidates (folder)

### G. Optional panels (collapsible side rail)
- Difference list
- Minimap
- Rename candidates results (folder)
- Diagnostics (when non-empty)

### H. Folder contextual operations (secondary strip or context menu)
- Open paired / Open 3-way text
- Copy L→R, Copy R→L
- Delete left, Delete right (destructive styling + confirm)

---

## 4. Proposed target layouts

See ASCII wireframes in `docs/ui/POL-00_layout_wireframes.txt`. Principles:

1. **Single comparison header** (paths + primary actions + collapsed advanced entry points).
2. **Single review strip** (navigation + state badges).
3. **Contextual merge strip** (merge group | separator | save group).
4. **Collapsible “Find & filter” panel** default collapsed.
5. **Collapsible right rail** for difference list + minimap (default open ≥1200 px width, collapsed below).
6. **Folder grid** fills remaining height; ops move to contextual strip or right-click menu.
7. **3-way**: consider 2×2 editor grid on narrow widths (POL-04 responsive rule), region list in rail.

---

## 5. Command inventory and post-redesign placement

No command is removed; each maps to a target location.

### Shell (`MainWindow`)

| Command | Current | Target |
|---------|---------|--------|
| New File Compare | Top bar | Global launch bar |
| Folder Compare | Top bar | Global launch bar |
| 3-Way Folder Compare | Top bar | Global launch bar |
| 3-Way Compare | Top bar | Global launch bar |
| Recents Open | Session bar | Session bar (collapsible section) |
| Pinned Open | Session bar | Session bar |
| Pin active | Session bar | Session bar |
| Clear recents | Session bar | Session bar overflow / settings |
| Reset settings | Session bar | Session bar overflow / settings |
| Close tab | Tab ✕ | Tab ✕ |
| Cancel operation | Status bar | Status bar |

### 2-way file compare

| Command | Current | Target |
|---------|---------|--------|
| Choose left/right file | Row 0 | Comparison header |
| Compare / Recompare | Row 0 | Comparison header |
| Profile | Row 1 | Comparison header |
| Comparison rules (all fields) | Expander row 2 | Rules flyout / expander in header |
| Previous / Next difference | Row 3 | Review strip |
| Expand all / Reveal selection | Row 3 | Review strip or rail menu |
| Copy to Right / Left | Row 3 | Contextual merge strip |
| Save Left/Right/All | Row 3 | Save group (separated) |
| Search + options + find | Row 4 | Find panel (collapsed) |
| Go to line | Row 4 | Find panel |
| Diagnostics | Row 5 | Optional panel (auto-show when errors) |
| Pane metadata badges | Row 6 | Review strip badges |
| Difference list | Rail | Collapsible rail |
| Minimap | Rail | Collapsible rail |

### Folder compare (2-way)

| Command | Current | Target |
|---------|---------|--------|
| Choose folders | Row 0 | Header |
| Compare / Recompare | Row 0 | Header |
| Mode / tolerance | Row 1 | Header |
| Filters | Row 3 | Review strip or find panel |
| Open paired compare | Row 3 | Contextual strip |
| Copy L→R / R→L | Row 3 | Contextual strip |
| Delete left/right | Row 3 | Contextual strip (destructive) |
| Find rename candidates | Row 3 | Find panel |
| Accept / Clear rename | Panel row 6 | Find panel results |
| Grid | Row 5 | Primary content |
| Legend | Footer | Collapsible footer |

### 3-way file compare

| Command | Current | Target |
|---------|---------|--------|
| Choose base/left/right | Row 0 | Header |
| Compare / Recompare | Row 0 | Header |
| Prev / Next / Next unresolved | Row 1 | Review strip |
| Use base/left/right | Row 2 | Contextual merge strip |
| Save output | Row 2 | Save group (separated) |
| Region list | Rail | Collapsible rail |
| Editors (4) | Row 4 | Primary content |
| Legend | Footer | Collapsible footer |

### 3-way folder compare

| Command | Current | Target |
|---------|---------|--------|
| Choose roots | Row 0 | Header |
| Compare / Recompare | Row 0 | Header |
| Mode / tolerance | Row 1 | Header |
| Filters | Row 3 | Review strip |
| Open 3-way text | Row 3 | Contextual strip |
| Grid | Row 5 | Primary content |
| Legend | Footer | Collapsible footer |

---

## 6. POL-04 implementation backlog

### Required for release

| ID | Change | Rationale |
|----|--------|-----------|
| POL4-R01 | Collapse search/go-to into **“Find & go” expander** default closed | IA-03 |
| POL4-R02 | Split navigation row from merge/save; add **visual separator** between merge and save groups | IA-02 |
| POL4-R03 | Add **keyboard shortcuts** for next/prev diff, compare/recompare, save all, find next (document in tooltips) | IA-04 |
| POL4-R04 | **State banner** below review strip for stale/dirty/read-only/formatted (icon + text, not colour alone) | IA-08 |
| POL4-R05 | Make **difference list + minimap rail collapsible** (toggle button, persist per session) | IA-09 |
| POL4-R06 | Folder ops: **destructive delete** buttons use danger style; keep grouped but separated from copy | IA-06 |
| POL4-R07 | Reduce default chrome: move pane metadata into review strip badges | IA-01 |
| POL4-R08 | Tab order + automation names audit for merged layout | Accessibility |
| POL4-R09 | Verify layouts at **100/125/150%** DPI; fix clipping on file search row and folder ops row | IA-01, IA-05 |

### Recommended if low risk

| ID | Change | Rationale |
|----|--------|-----------|
| POL4-M01 | Collapse shell recents bar behind **“Sessions ▾”** toggle | IA-05 |
| POL4-M02 | 3-way: stack editors 2×2 when width < 1280 px | IA-07 |
| POL4-M03 | Consolidate footer legend into **“?” popover** | IA-12 |
| POL4-M04 | Comparison rules + structured view in single **“Rules…”** dialog | IA-11 |
| POL4-M05 | Rename-candidate button moves into find panel | IA-14 |

### Deferred backlog

| ID | Change | Notes |
|----|--------|-------|
| POL4-D01 | Command palette (Ctrl+Shift+P) | Nice-to-have |
| POL4-D02 | Custom workspace themes | Out of scope |
| POL4-D03 | Drag-drop paths onto headers | New interaction |
| POL4-D04 | Middle-click close tab | Low priority |
| POL4-D05 | User-configurable shortcut editor | POL-04 forbids theme editor; defer |

---

## 7. Release acceptance checklist (redesigned UI)

- [ ] At 100% DPI, file compare editors occupy **≥60%** of workspace client height with default window after first compare.
- [ ] Merge and save controls are in **separate groups** with visible separator.
- [ ] Search/go-to available within **one click** but not visible by default.
- [ ] All commands in Section 5 remain reachable with documented location.
- [ ] Next/previous difference usable via keyboard.
- [ ] Stale comparison disables merge with **visible banner** message.
- [ ] Dirty and read-only states visible without opening menus.
- [ ] Folder delete requires confirmation; button visually distinct.
- [ ] 3-way output merge: resolve + save workflow completable at 125% DPI without horizontal scroll on shell.
- [ ] No new NuGet UI packages; WPF + AvalonEdit retained.
- [ ] Automated bounded tests still pass.

---

## 8. Explicitly out of scope

- Ribbon UI
- Replacement of WPF or AvalonEdit
- Paid / proprietary control suites
- Theme editor or skin marketplace
- Report designer, printing, PDF export
- VCS integration, automatic merge, patch application
- Binary/image compare UI
- Copying third-party merge tool layouts, icons, or wording
- Moving comparison logic into views (architecture guardrail)

---

## 9. POL-01+ sequencing note

POL-00 is documentation only. **POL-01** (architecture/API hardening) may proceed in parallel with POL-04 planning. **POL-04** implements this plan’s Required items. POL-02/03/05–08 follow task index order after their prerequisites.

---

## Appendix A — Review method

1. Built and launched `MergeTool.App.Wpf` (Release) on Windows 10.0.26200.
2. Walked scenarios 1–7 with sample text/folder fixtures used in phase-1 checkpoint.
3. Cross-checked XAML row definitions and `AutomationProperties.Name` coverage.
4. Measured approximate chrome vs editor ratio from live window and layout tree.

## Appendix B — Keyboard gap (current)

No `KeyBinding` or `InputBindings` in shell or workspace views. POL-04-R03 should add at minimum:

| Action | Proposed shortcut |
|--------|-------------------|
| Next difference | F7 |
| Previous difference | Shift+F7 |
| Recompare | F5 |
| Save all | Ctrl+Shift+S |
| Find next | F3 |
| Find previous | Shift+F3 |
| Focus find panel | Ctrl+F |

(Exact bindings subject to POL-04 conflict check.)
