# MVP-02 Testing Report

**Task:** Verify: Implement core contracts, diagnostics, and cancellable operation lifecycle
**Date:** 2026-07-02
**Verdict:** PASS

## Commands run and exit results

| Command | Result |
| --- | --- |
| `dotnet build -c Debug` | exit 0; 0 warnings, 0 errors |
| `dotnet test -c Debug -m:1 --no-build` | exit 0; 32/32 passed |
| `dotnet build -c Release` | exit 0; 0 warnings, 0 errors |
| `dotnet test -c Release -m:1 --no-build` | exit 0; 32/32 passed |

(`-m:1` per the environment note recorded in MVP-01: parallel test hosts can exhaust this
machine's commit limit.)

## Tester checks

- **Core/Application compile without WPF references** — confirmed. Both target plain
  `net10.0`; the architecture smoke tests assert MergeTool reference direction, and no
  `UseWPF`/`System.Windows` reference exists outside the two WPF projects.
- **Request ordering / stale identification** — `OperationRequestCoordinatorTests` prove IDs
  are strictly increasing, an older request becomes stale the moment a newer one starts, and
  concurrent issuance never duplicates. `ComparisonRequestIdTests` prove newer-vs-older is a
  deterministic value comparison.
- **Cancellation behavior** — `LastGoodResultSlotTests.Cancellation_DoesNotOverwriteLastGoodValue`
  proves a current cancellation is recorded as a terminal state while the last completed value
  stays visible. `CancellationConvention_ProducesResultNotException` demonstrates the
  boundary convention: cancellation surfaces as `OperationResult.Cancelled`, not an unhandled
  exception.
- **Operation-lifecycle document** — `docs/architecture/operation-lifecycle.md` contains
  concrete rules for stale results (current-request check before applying any completion) and
  cancellation (controlled terminal state; no partial mutation; last result stays visible).
  Not vague; each rule maps to a tested type.
- **Diagnostics are not UI-bound** — `Diagnostic` lives in Core with no WPF types; user-safe
  message is enforced separately from technical detail
  (`DiagnosticTests.UserMessage_IsSeparateFromTechnicalDetail`).
- **No disproportionate framework** — no event bus, mediator, reactive framework, message
  queue, logging framework, or repository abstraction was added; the additions are two small
  classes in Application and seven focused files in Core.
- **Dependencies** — `docs/DEPENDENCIES.md` unchanged; no new packages;
  `Directory.Packages.props` still pins only MSTest 4.0.2.

## Defects created

None. (One build-time analyzer error, MSTEST0037, and one namespace collision with
`System.Windows.Application` were caught and fixed during development before test handoff.)

## Recommendation

**Proceed** to MVP-03.
