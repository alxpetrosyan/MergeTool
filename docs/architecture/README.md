# Architecture Notes

Authoritative guardrails live in the task pack (`mergetool_task_pack/00_ARCHITECTURE_GUARDRAILS.txt`).
This folder records project-specific decisions as they are made.

## Solution shape (established in MVP-01)

```text
/src
  MergeTool.App.Wpf              # Views, view models, WPF-only renderers and dialog adapters
  MergeTool.Application          # Use cases, sessions, commands, orchestration; no WPF types
  MergeTool.Core                 # Small shared primitives: diagnostics, result types, IDs, options
  MergeTool.Text                 # Document snapshots, diff/merge domain, text algorithms
  MergeTool.Folders              # Folder scans, quick compare model, operation plans
  MergeTool.FileSystem           # File IO abstraction, atomic saves, identities, path handling
/tests
  MergeTool.Core.Tests
  MergeTool.Text.Tests
  MergeTool.Folders.Tests
  MergeTool.Application.Tests
  MergeTool.App.Wpf.Tests        # View-model tests only; no UI-pixel tests
```

## Dependency direction

- `MergeTool.App.Wpf` → Application, Text, Folders, FileSystem, Core
- `MergeTool.Application` → Text, Folders, FileSystem, Core
- `MergeTool.Text` / `MergeTool.Folders` / `MergeTool.FileSystem` → Core only
- Only `MergeTool.App.Wpf` (and its test project) may reference WPF types.

## Decisions

### MVP-01

- Target frameworks: `net10.0-windows` for the WPF app and its test project; `net10.0` for
  all libraries and remaining test projects.
- Central Package Management (`Directory.Packages.props`) with restore lock files enabled
  repo-wide via `Directory.Build.props` (`RestorePackagesWithLockFile`).
- `TreatWarningsAsErrors` is on for the whole solution from the start, together with SDK
  analyzers at `AnalysisLevel=latest`.
- There is no dedicated `MergeTool.FileSystem.Tests` project because the guardrails do not
  list one; FileSystem smoke coverage lives in `MergeTool.Application.Tests`.
- No DI container, logging framework, event bus, or plugin abstractions until a task proves
  a need.

### MVP-03

- `IRawFileReader`/`RawFileContent` live in **Core** so the Text layer can consume file bytes
  without referencing FileSystem (guardrails allow Text → Core only). FileSystem provides the
  physical implementation (`PhysicalFileReader`).
- Encoding support (`TextEncodings`, `TextWritePolicy`) lives in **Core** because both Text
  (decode) and FileSystem (encode on save) need it and neither may reference the other.
- Windows-1252 is implemented as a small internal codec instead of adding the
  `System.Text.Encoding.CodePages` package. Decoding maps every byte deterministically;
  encoding **throws** for unmappable characters so a save can never silently lose data.
- Save conflict detection compares the full `FileVersionToken` (length + last-write UTC +
  SHA-256) of the bytes currently on disk against the identity captured at load. A touched
  but content-identical file counts as external modification (safe false positive; MVP offers
  Cancel/Reload).
- Atomic save: temp file in the target directory → flush-to-disk → `File.Replace`. Temp files
  are cleaned up on failure.
- Domain services let `OperationCanceledException` propagate; only the application layer
  converts cancellation into a `Cancelled` result (see operation-lifecycle.md).

### MVP-07

- Semantic rendering is a one-way pipeline: diff result → `ComparisonPresentationMap` →
  `PaneRenderModel` (per-line block kinds, WPF-free) → `DiffLineBackgroundRenderer`
  (AvalonEdit `IBackgroundRenderer`). The renderer never rescans editor text; the model is
  the only color source.
- Scroll synchronization is driver-based: only the pane the user interacts with (focus,
  mouse) drives the counterpart, decided by `ScrollSyncCoordinator` (WPF-free, tested).
  Follower echo events are ignored and moves within half a line are suppressed, so the
  panes cannot recursively fight.
- On a stale edit the semantic overlays and the scroll map are cleared until recompare;
  colors pointing at outdated line ranges would be misleading.
- Same-path rejection happens in `OpenFileComparisonUseCase` on normalized full paths
  (case-insensitive), before any file IO.
- The file-open dialog is behind `IFilePickerService` so view models stay testable; the
  WPF implementation is injected at the composition root.
- No vertical blank-line padding in MVP (per task): the limitation is stated in the UI
  help line instead of faking document content.

### POL-01

- Architecture guardrails enforced by `ArchitectureGuardrailsTests` (no WPF references in
  non-WPF assemblies; Application must not reference App.Wpf).
- `AppCompositionRoot` owns shell wiring; `MainWindowViewModel.CreateDefault()` delegates to it.
- `OpenPairedFileComparisonRequest` lives in `MergeTool.Application.Requests`.
- `WorkspaceViewModel.ReplaceDiagnostics` centralizes diagnostic list updates in workspace VMs.
- See `public-api-and-dependencies.md` for the full POL-01 review.

### POL-02

- `DiagnosticRecovery` and `OperationDiagnostics` in Core provide recovery hints and shared
  cancellation/unexpected-failure codes.
- `Diagnostic.ToCopyPayload()` supports clipboard copy from WPF.
- `DiagnosticsPanel` is the shared diagnostics UI across all workspace views.
- `WorkspaceViewModel` surfaces cancellation and unexpected failures safely.
- See `diagnostics.md` for conventions when adding new failure codes.
