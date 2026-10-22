# Diagnostics and safe failure modes

**Established:** POL-02

## Principles

- Every user-visible failure uses a stable `MT-*` code, a **safe** `Message`, and optional `TechnicalDetail` for support.
- Exception text never appears in `Message`; it belongs in `TechnicalDetail` only.
- WPF shows diagnostics through the shared `DiagnosticsPanel`: code, message, recovery hint, expandable technical detail, and **Copy details**.
- Operations that fail or are cancelled must not destroy the last completed comparison or folder scan.

## Core types

| Type | Role |
|------|------|
| `Diagnostic` | Immutable record: severity, code, message, optional technical detail/path/side |
| `DiagnosticRecovery` | Maps known codes to actionable recovery hints |
| `OperationDiagnostics` | Shared `MT-OP-001` (cancelled) and `MT-UNX-001` (unexpected) factories |
| `OperationResult` | Terminal status plus zero or more diagnostics |

## WPF presentation

- `WorkspaceViewModel.Diagnostics` is the single list per tab.
- `DiagnosticViewModel` wraps one `Diagnostic` for binding.
- `DiagnosticsPanel` is used in file compare, folder compare, and three-way views.
- `WorkspaceViewModel.RunOperationAsync` adds a cancellation diagnostic when an operation ends with no diagnostics, and maps unexpected exceptions to `MT-UNX-001`.

## Adding a new diagnostic

1. Define a `public const string …Code` next to the producing code (existing convention).
2. Emit `Diagnostic.Error` / `Warning` / `Info` with a user-safe message.
3. Add a recovery hint in `DiagnosticRecovery`.
4. Add the code to `SafeFailureModesTests.DiagnosticRecovery_CoversAllProductionCodes`.
5. If the failure is reachable from UI, verify it appears in `DiagnosticsPanel` without crashing and without mutating last-good state.

## Out of scope

- Cloud telemetry, crash reporting, or remote logging.
- Swallowing errors without surfacing a diagnostic.
