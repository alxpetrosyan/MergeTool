# Operation Lifecycle Conventions

Established in MVP-02. These rules apply to every long-running use case (file loading, diff
calculation, hashing, folder scanning, saving).

## Threading and scheduling

- All IO, diff calculation, hashing, and recursive folder scanning run outside the UI thread.
- **The application layer controls scheduling.** Domain services (Text, Folders, FileSystem)
  expose synchronous or genuinely asynchronous IO-bound APIs; they never call `Task.Run` to
  appear asynchronous, never touch UI dispatchers, and never create background tasks
  themselves.
- WPF code renders state and forwards user intent; it does not orchestrate workflows.

## Cancellation

- Every long-running use case receives a `CancellationToken` from its caller.
- Cancellation is a **controlled terminal state**, not an unhandled exception. Use cases catch
  `OperationCanceledException` at the application boundary and return
  `OperationResult.Cancelled(...)` (or `OperationResult<T>.Cancelled(...)`).
- A cancelled operation must not partially mutate an active result. Work happens on private
  state and is published only on successful completion.
- Cancelling leaves the last completed result visible (see result slots below).

## Request identity and stale completions

- `ComparisonRequestId` (in Core) is a monotonic value: a larger value is always a newer
  request.
- The application layer owns request IDs through `OperationRequestCoordinator` — one
  coordinator per logical operation slot (for example, per workspace comparison). Calling
  `BeginRequest()` makes every earlier request stale immediately.
- A completion may be applied to workspace state only when
  `OperationRequestCoordinator.IsCurrent(requestId)` is true. Stale completions are discarded
  entirely; they must not update state, progress, or diagnostics.
- Newer-versus-older is decided deterministically by comparing the monotonic values; wall
  clocks are never used.

## Last-good-result rule

- `LastGoodResultSlot<T>` stores the last successfully completed value of a slot.
- Only a **successful, current** completion replaces the stored value.
- Cancelled or failed completions update the slot's terminal status (so the UI can say
  "cancelled" or show diagnostics) but never clear or overwrite the last good value.

## Results and diagnostics

- Operations return `OperationResult` / `OperationResult<T>`: a terminal status
  (Completed / Cancelled / Failed) plus a list of `Diagnostic` entries.
- `Diagnostic.Message` is always safe to show to the user. Raw exception text, stack traces,
  and internal details go only in `Diagnostic.TechnicalDetail`.
- Partial failure is expressed as a completed result carrying warning/error diagnostics
  (for example, a folder scan that could not read one subdirectory).

## What we deliberately do not build

No event bus, mediator, reactive framework, message queue, logging framework, or generic
repository abstraction. The primitives above are sufficient for the product's workflows.
