# MVP-06 Development Report

**Task:** Build WPF shell, workspace tabs, and async UI state
**Date:** 2026-07-02
**Status:** Complete

## Feature boundary implemented

One coherent WPF shell hosting File Compare and Folder Compare workspaces as tabs, with
busy/progress/cancel presentation, safe diagnostic display, and view models that consume
application sessions. No compare rendering, no folder grid, no navigation framework, no
theming/docking — placeholder content is explicitly labelled as arriving in MVP-07/MVP-10.

## Objective completed

### Dependency added (per policy, only now)

- **AvalonEdit 6.3.1.120** (MIT) pinned centrally in `Directory.Packages.props`, entry added
  to `docs/DEPENDENCIES.md` *before* the package reference. Referenced only by
  `MergeTool.App.Wpf`; used by MVP-07 for rendering (not yet wired to controls).

### Application layer (`MergeTool.Application.UseCases`)

- `OpenFileComparisonUseCase` — orchestrates the "open two files and compare" workflow
  entirely outside WPF: reads both files via `IRawFileReader`, decodes them
  (`TextContentDecoder`), rejects non-text files, builds `TextDocumentSession`s and a
  `TwoWayComparisonSession`, and runs the first comparison. Returns
  `OperationResult<TwoWayComparisonSession>` with side-attributed diagnostics; cancellation
  yields a controlled `Cancelled` result. `Diagnostic.WithSide` added in Core to support
  side attribution.

### WPF shell (`MergeTool.App.Wpf`)

- `ViewModelBase`, `RelayCommand` — minimal MVVM plumbing (no framework package).
- `WorkspaceViewModel` (abstract) — the single owner of per-workspace async UI state:
  - `WorkspaceUiState` enum: `Idle`, `Working`, `Completed`, `Cancelled`, `Failed`, `Stale`
    — the consistent transition set required by the task.
  - `RunOperationAsync` — issues request IDs via `OperationRequestCoordinator`, creates a
    linked `CancellationTokenSource` per run, cancels/supersedes the previous run, and
    applies completions **only if still current** (stale completions are discarded).
  - `CancelCommand` — enabled exactly while busy; cancels the token source; the operation
    returns a controlled `Cancelled` state (never an exception to the user).
  - `Diagnostics` collection of `DiagnosticViewModel`; `CanClose` (false while busy or with
    unsaved changes).
- `FileCompareWorkspaceViewModel` — left/right path input, `CompareCommand` (requires both
  paths), runs `OpenFileComparisonUseCase` through `RunOperationAsync`, exposes the
  resulting `TwoWayComparisonSession`, difference-count summary, and a title derived from
  the file names.
- `FolderCompareWorkspaceViewModel` — clearly-labelled placeholder (engine arrives MVP-09,
  grid MVP-10).
- `MainWindowViewModel` — owns the workspace tab collection: one active file-compare tab at
  launch, `NewFileCompareCommand`, `ShowFolderCompareCommand` (folder workspace created once
  and reused), `CloseWorkspaceCommand` (refused while the workspace is busy or dirty;
  activates a neighbor after close). Takes a workspace factory so tests can inject a gated
  reader.
- `DiagnosticViewModel` — safe `Message` always visible; `TechnicalDetail` exposed **only**
  when `ShowTechnicalDetail` is set by the user.
- `MainWindow.xaml` — command bar (New File Compare / Folder Compare / Cancel), tab strip
  bound to the workspace collection, status bar bound to the active workspace's state/busy
  text, diagnostics list with a "details" expander, per-workspace `DataTemplate`s.
  Code-behind is limited to `InitializeComponent` + composition root.

## Design decisions and alternatives rejected

- **View models take a factory/use case via constructor** instead of a service locator —
  keeps the composition root in `MainWindow` code-behind and makes every view model testable
  with a gated fake `IRawFileReader` (no UI automation framework needed).
- **Folder Compare is a reusable singleton tab** rather than multiple instances — matches
  the product workflow (one folder session; file tabs spawn from it in MVP-10).
- **`RunOperationAsync` cancels the superseded run** rather than letting both race — the
  newest user intent wins; older completions are discarded by request-ID check regardless.
- No MVVM framework package (Prism/CommunityToolkit) — `RelayCommand` + `ViewModelBase` are
  ~60 lines; a framework would violate the drift-prevention rule.

## Tests added/updated (11 new; 139 total)

`tests/MergeTool.App.Wpf.Tests` (plain `net10.0-windows`, no UI automation):

- `WorkspaceLifecycleTests` (5): launch has one active file-compare tab; new-tab adds and
  activates; folder workspace created once and reused; closing clean workspaces is safe and
  reactivates a neighbor; **closing is refused while busy** (gated reader).
- `FileCompareWorkspaceViewModelTests` (6): compare requires both paths;
  Idle→Working→Completed transition with busy text and summary; **cancel command enables
  while busy, cancels to a controlled `Cancelled` state, and disables again**; **a newer
  compare supersedes an older in-flight one**; a failed compare (missing files) shows the
  safe message with technical detail hidden until demanded; a completed clean workspace can
  close.

## Build/test commands and results

```powershell
dotnet restore               # success (AvalonEdit restored)
dotnet build -c Debug        # success, 0 errors
dotnet test -c Debug -m:1    # 139/139 passed
dotnet build -c Release      # success, 0 errors
dotnet test -c Release -m:1  # 139/139 passed
```

Manual launch check: `MergeTool.App.Wpf.exe` (Release) starts a single shell window titled
"MergeTool", stays responsive, and exits cleanly.

## Known limitations / deliberate deferrals

- File-compare tab shows path inputs + summary only; side-by-side rendering, scroll
  mapping, and AvalonEdit wiring are MVP-07.
- Folder workspace is a labelled placeholder until MVP-09/MVP-10.
- Save UI and merge actions are MVP-08.

## Dependencies introduced

AvalonEdit 6.3.1.120 (MIT) — UI project only; recorded in `docs/DEPENDENCIES.md`.
