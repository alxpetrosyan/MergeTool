# Public API and Dependency Review (POL-01)

**Date:** 2026-07-02  
**Scope:** Architecture hardening after Scope 2 feature accumulation.

## Dependency direction (verified)

Automated guardrails in `ArchitectureGuardrailsTests` assert:

| Assembly | May reference | Must not reference |
|----------|---------------|-------------------|
| `MergeTool.Core` | BCL only | WPF, AvalonEdit, other product assemblies |
| `MergeTool.Text` | Core | Application, WPF, Folders, FileSystem |
| `MergeTool.Folders` | Core | Application, WPF, Text, FileSystem |
| `MergeTool.FileSystem` | Core | Application, WPF, Text, Folders |
| `MergeTool.Application` | Core, Text, Folders, FileSystem | WPF, AvalonEdit |
| `MergeTool.App.Wpf` | All libraries above | — (only WPF host) |

Settings persistence lives in **Core** (`MergeTool.Core.Settings`) rather than a separate `MergeTool.Settings` project, because recents/pinned data is small JSON and shared by shell and tests without pulling WPF.

## Scheduling and cancellation ownership

| Concern | Owner |
|---------|--------|
| `OperationRequestCoordinator` per workspace operation slot | `WorkspaceViewModel` (WPF) and per-session coordinators in `TwoWayComparisonSession` / `ThreeWayComparisonSession` |
| `Task.Run` for folder scan / rename find | Application use cases (`RunFolderComparisonUseCase`, `FindFolderRenameCandidatesUseCase`) |
| `Task.Run` inside comparison sessions | Application sessions (orchestration layer; acceptable—sessions are not domain) |
| Cancellation → `OperationResult.Cancelled` | Application use cases and `WorkspaceViewModel.RunOperationAsync` |
| Domain services (Text, Folders, FileSystem) | Synchronous APIs; no `Task.Run`, no request IDs |

## Public API decisions

### Moved to Application layer

- `OpenPairedFileComparisonRequest` — was defined at the bottom of `FolderCompareWorkspaceViewModel.cs`; now `MergeTool.Application.Requests` so folder and shell code share one type without WPF-owned DTOs.

### Tightened visibility

- `FolderRenameCandidateFinder.AreNamesSimilar` and `LevenshteinDistance` are `internal`; `MergeTool.Folders.Tests` uses `InternalsVisibleTo` for characterization tests.

### Intentionally public domain types

Text (`DiffBlock`, `TextComparisonOptions`, `ITextDiffEngine`), Folders (`FolderComparisonRow`, `FolderRenameCandidateFinder`), and Application sessions/use cases remain public for testability and the WPF composition root. No blanket `internal` sweep—would break view-model tests and use-case construction without measurable gain.

### WPF direct references to Text/Folders

View models may reference Text/Folders types for presentation (profiles, filters, row state labels). Workflows still enter through Application use cases and sessions. POL-04 UI refactor must not move comparison logic into views.

## Composition root

`AppCompositionRoot.CreateShell()` centralizes infrastructure wiring previously embedded in `MainWindowViewModel.CreateDefault()`. View models receive use cases via constructor injection; the composition root is the only place that constructs `PhysicalFileReader`, diff engines, and WPF dialog adapters.

## Diagnostics presentation

`WorkspaceViewModel.ReplaceDiagnostics` consolidates repeated clear/add loops in workspace view models. Diagnostics ownership remains: **Core** defines `Diagnostic`; Application returns them in `OperationResult`; WPF wraps them in `DiagnosticViewModel` for binding.

## Duplication reviewed, not extracted

| Candidate | Decision |
|-----------|----------|
| Separate coordinators in session vs workspace | **Keep** — different lifecycles (per-tab vs per-recompare) |
| `PaneRenderModel` vs `ThreeWayPaneRenderModel` | **Keep** — different region semantics |
| Folder 2-way vs 3-way workspace VMs | **Keep** — distinct commands and grid columns |
| Generic `IWorkspaceViewModel` interface | **Reject** — unnecessary abstraction |

## Out of scope (POL-01)

- DI container
- Mediator / event bus
- Moving `Task.Run` out of sessions into use cases (behavior-neutral; defer unless profiling requires)
- Plugin system or repository pattern

## Follow-up

- **POL-04** implements POL-00 UI layout using the same workspace/view-model boundaries documented here.
- **POL-02** may further unify diagnostic display patterns if additional duplication appears.
