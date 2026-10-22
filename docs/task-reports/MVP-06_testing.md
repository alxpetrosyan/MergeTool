# MVP-06 Testing Report

**Task:** Verify: Build WPF shell, workspace tabs, and async UI state
**Date:** 2026-07-02
**Verdict:** PASS

## Commands run and exit results

| Command | Result |
| --- | --- |
| `dotnet restore` | exit 0; AvalonEdit 6.3.1.120 restored |
| `dotnet build -c Debug` | exit 0; 0 errors |
| `dotnet test -c Debug -m:1 --no-build` | exit 0; 139/139 passed |
| `dotnet build -c Release` | exit 0; 0 errors |
| `dotnet test -c Release -m:1 --no-build` | exit 0; 139/139 passed |

(`-m:1` per the memory-constraint note in `test.ps1`; equivalent coverage to the baseline
commands.)

## Tester checks

- **Single shell, discoverable workflows** — manual launch of the Release executable opens
  exactly one window ("MergeTool") hosting a command bar with `New File Compare` and
  `Folder Compare` buttons, a tab strip, and a status bar; the process stayed responsive
  and closed cleanly. No secondary windows exist anywhere in the project.
  `Shell_StartsWithOneActiveFileCompareTab` proves the launch state programmatically;
  `FolderCompare_IsCreatedOnceAndReused` proves both workflows live in the same shell.
- **Busy/progress/cancel via controlled operation** — the test hook is a `GatedReader`
  (an `IRawFileReader` whose reads block until released), driving the *real* use case and
  view models. `CancelCommand_IsEnabledWhileBusy_AndCancelsToSafeState` proves: cancel is
  enabled only while `Working`; executing it flows through the view model's
  `CancellationTokenSource` into the application layer; the workspace returns to a safe
  `Cancelled` state with `IsBusy == false`, no session, and cancel disabled again.
- **Request replacement** — `NewerCompare_SupersedesOlderOne`: two in-flight compares; the
  older is cancelled/discarded and the newest outcome wins (request-ID check in
  `WorkspaceViewModel.RunOperationAsync`).
- **Errors are readable, never a crash** — `FailedCompare_ShowsSafeDiagnostics_...` runs a
  compare against missing files on the real file system: state becomes `Failed`, the
  diagnostic shows exactly "The file could not be read.", and the technical detail
  (exception text) is exposed only after the user opts in via `ShowTechnicalDetail`.
- **Closing a workspace with no changes is safe** —
  `CloseWorkspace_WithNoChanges_IsSafeAndReactivatesNeighbor` and
  `CompletedWorkspace_WithCleanDocuments_CanClose`; conversely
  `CloseWorkspace_WhileBusy_IsRefused` proves a busy workspace cannot be torn down.
- **No operations in code-behind** — inspection: `MainWindow.xaml.cs` contains only
  `InitializeComponent` and the composition root; `App.xaml.cs` is empty. All async
  lifecycle logic lives in `WorkspaceViewModel`/`MainWindowViewModel`, and file/compare
  orchestration lives in `MergeTool.Application.UseCases.OpenFileComparisonUseCase`.
  `MergeTool.Application` has no WPF reference (verified by project references and the
  existing dependency-direction smoke tests).

## Architecture/licence checks

- `docs/DEPENDENCIES.md` updated with AvalonEdit 6.3.1.120 (MIT) before the package was
  referenced; version pinned centrally in `Directory.Packages.props`.
- AvalonEdit is referenced **only** by `MergeTool.App.Wpf`. No other package, architecture,
  or scope change found in the working tree.

## Failure-condition review

- UI split into disconnected windows: **not present** (single shell, tabbed).
- Cancel does nothing: **disproved by test** (state transitions to `Cancelled`).
- Simulated error crashes the app: **disproved by test** (controlled `Failed` state).
- Long-operation handling bypassing the application layer: **not present** (all operations
  route through `OperationRequestCoordinator` + use case).

## Defects created

None.

## Recommendation

**Proceed** to MVP-07.
