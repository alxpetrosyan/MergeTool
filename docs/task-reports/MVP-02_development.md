# MVP-02 Development Report

**Task:** Implement core contracts, diagnostics, and cancellable operation lifecycle
**Date:** 2026-07-02
**Status:** Complete

## Feature boundary implemented

Cross-cutting primitives in `MergeTool.Core` and the operation-lifecycle conventions (request
identity, stale rejection, last-good-result) in `MergeTool.Application`. No file IO, diffing,
folder scanning, or UI behavior.

## Objective completed

### MergeTool.Core (new files)

- `Ids.cs` — `DocumentSessionId`, `ComparisonSessionId`: opaque GUID-backed value IDs, never
  derived from paths.
- `Diagnostic.cs` — `Diagnostic` (severity, stable code, user-safe message, optional
  technical detail, optional path, optional `ComparisonSide`), plus `DiagnosticSeverity` and
  `ComparisonSide` enums. Factory helpers `Info`/`Warning`/`Error`. Immutable record; blank
  code/message rejected.
- `OperationStatus.cs` — `Idle | Running | Completed | Cancelled | Failed`.
- `OperationProgress.cs` — phase, optional current item, completed count, optional total.
- `OperationResult.cs` — `OperationResult` and `OperationResult<T>`: terminal status plus
  snapshotted diagnostics list. `Value` is available only on `Completed`; `TryGetValue`
  provided; non-terminal statuses are rejected at construction.
- `FileIdentity.cs` — `FileVersionToken` (length, UTC-only last-write time, normalized
  lowercase SHA-256 hex) and `FileIdentity` (normalized full path + token). No IO here.
- `ComparisonRequestId.cs` — monotonic value type; larger value is deterministically newer;
  comparison operators and `IsNewerThan`.

### MergeTool.Application (new files, `Operations` namespace)

- `OperationRequestCoordinator.cs` — thread-safe issuer of monotonic request IDs for one
  logical operation slot; `IsCurrent()` decides deterministically whether a completion is
  still current. `ComparisonRequestId.None` is never current.
- `LastGoodResultSlot.cs` — stores the last successfully completed value; stale completions
  are discarded entirely; current cancelled/failed completions update the terminal status but
  never overwrite the last good value.

### Documentation

- `docs/architecture/operation-lifecycle.md` — concrete rules: application layer owns
  scheduling and request IDs; every long-running use case takes a `CancellationToken`;
  cancellation is converted to a `Cancelled` result at the use-case boundary; stale
  completions are discarded; last completed result stays visible.

## Design decisions and alternatives rejected

- **Per-slot coordinator instead of a global sequence** — each workspace/operation slot gets
  its own `OperationRequestCoordinator`, so request currency is local and deterministic. A
  global static counter was rejected (hidden shared state, harder tests).
- **`LastGoodResultSlot<T>` combines currency + last-good rules** in one small class instead
  of an event bus/mediator (explicitly excluded by the task).
- **Cancellation is modelled as a terminal result**, with a documented boundary convention
  (catch `OperationCanceledException`, return `Cancelled`), keeping domain code free of
  UI/task concerns.
- **`OperationResult<T>` hides its value unless completed** (throws on misuse, `TryGetValue`
  for flow control) to prevent later workflows from consuming cancelled/failed payloads.
- Fixed a latent name collision: `App.xaml.cs`/`MainWindow.xaml.cs` now qualify
  `System.Windows.Application` explicitly because the `MergeTool.Application` namespace
  shadows it inside the `MergeTool.App.Wpf` namespace tree.

## Tests added/updated (26 new; 32 total)

- Core: `DiagnosticTests` (composition, safe-message separation, validation, value equality),
  `OperationResultTests` (value/cancel/fail semantics, diagnostics snapshot),
  `FileIdentityTests` (token equality incl. hash case normalization, UTC enforcement,
  validation, opaque IDs), `ComparisonRequestIdTests` (deterministic ordering, equality).
- Application: `OperationRequestCoordinatorTests` (strictly increasing IDs, stale detection,
  `None` never current, concurrent uniqueness), `LastGoodResultSlotTests` (store on success,
  discard stale entirely, cancellation/failure never overwrite last good value, cancellation
  as result not exception).

## Build/test commands and results

```powershell
dotnet build -c Debug        # success, 0 warnings, 0 errors
dotnet test -c Debug -m:1    # 32/32 passed
dotnet build -c Release      # success, 0 warnings, 0 errors
dotnet test -c Release -m:1  # 32/32 passed
```

## Known limitations / deliberate deferrals

- `OperationProgress` reporting plumbing (an `IProgress<OperationProgress>` convention) is
  defined but not yet exercised by a real use case; the first real consumer arrives with text
  loading (MVP-03) and folder scanning (MVP-09).
- `FileVersionToken` production (actual hashing/IO) is deferred to MVP-03 by design.

## Dependencies introduced

None.
