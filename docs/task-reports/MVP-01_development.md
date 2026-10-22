# MVP-01 Development Report

**Task:** Create repository, solution, deterministic build contract, and project boundaries
**Date:** 2026-07-02
**Status:** Complete

## Feature boundary implemented

Solution foundation only: solution/projects, build contract files, build scripts, docs
skeleton, a minimal WPF shell (product name plus two disabled navigation entries), and one
smoke test per non-WPF project. No comparison, file loading, folder scanning, or merge
behavior was implemented.

## Objective completed

- `MergeTool.slnx` solution with the exact project set from `00_ARCHITECTURE_GUARDRAILS.txt`:
  - `src/MergeTool.Core`, `src/MergeTool.Text`, `src/MergeTool.Folders`,
    `src/MergeTool.FileSystem`, `src/MergeTool.Application` (all `net10.0`)
  - `src/MergeTool.App.Wpf` (`net10.0-windows`, WPF)
  - `tests/MergeTool.Core.Tests`, `tests/MergeTool.Text.Tests`,
    `tests/MergeTool.Folders.Tests`, `tests/MergeTool.Application.Tests` (`net10.0`, MSTest)
  - `tests/MergeTool.App.Wpf.Tests` (`net10.0-windows`, MSTest, reserved for view-model tests)
- `global.json` pins SDK `10.0.301` with `rollForward: latestPatch`.
- `Directory.Build.props`: nullable, implicit usings, deterministic builds,
  `TreatWarningsAsErrors=true`, SDK analyzers at `AnalysisLevel=latest`, and
  `RestorePackagesWithLockFile=true` (lock files committed per dependency policy).
- `Directory.Packages.props`: Central Package Management; MSTest 4.0.2 is the only pinned
  package.
- Root files: `README.md` (with explicit product exclusions), `.editorconfig`, `.gitignore`,
  `build.ps1`, `test.ps1`, `clean.ps1`.
- Docs skeleton: `docs/DEPENDENCIES.md`, `docs/architecture/README.md`,
  `docs/task-reports/`, `docs/defects/`.
- WPF shell: title bar with product name "MergeTool" and two disabled navigation buttons
  ("File Compare", "Folder Compare"); no functional UI.

## Project reference graph

- Text, Folders, FileSystem → Core only.
- Application → Core, Text, Folders, FileSystem.
- App.Wpf → all of the above.
- Each test project references only its target project. No non-WPF project references
  WPF/System.Windows/AvalonEdit.

## Design decisions and alternatives rejected

- **`.slnx` solution format** produced by the .NET 10 `dotnet new sln`; kept because the
  pinned SDK fully supports it and it diffs cleanly. A classic `.sln` was rejected as adding
  no value under this SDK.
- **No `MergeTool.FileSystem.Tests` project** because the guardrails' baseline solution shape
  does not list one; the FileSystem smoke test lives in `MergeTool.Application.Tests`
  (documented in `docs/architecture/README.md`).
- **Smoke tests assert dependency direction** (referenced-assembly names) rather than trivial
  truths, so the architecture guardrail is machine-checked from day one. A first version
  asserting a constant was rejected by MSTest analyzer MSTEST0032 (always-true assertion).
- **No DI container, logging framework, AvalonEdit, or diff package** — explicitly excluded by
  the task.
- **`test.ps1` and documented test runs use `dotnet test -m:1`** (sequential test projects).
  Parallel MSBuild nodes spawned five concurrent test hosts, which exhausted the Windows
  commit limit on the development machine (see Known limitations).

## Tests added/updated

- `MergeTool.Core.Tests/SmokeTests.cs` — Core loads; references no other MergeTool assembly.
- `MergeTool.Text.Tests/SmokeTests.cs` — Text loads; may reference Core only.
- `MergeTool.Folders.Tests/SmokeTests.cs` — Folders loads; may reference Core only.
- `MergeTool.Application.Tests/SmokeTests.cs` — Application loads; allowed reference set only.
- `MergeTool.Application.Tests/FileSystemSmokeTests.cs` — FileSystem loads; Core only.
- `MergeTool.App.Wpf.Tests/SmokeTests.cs` — WPF assembly loads (placeholder until view models
  exist).

## Build/test commands and results

```powershell
dotnet restore          # success
dotnet build -c Debug   # success, 0 warnings, 0 errors
dotnet test -c Debug    # 6/6 passed
dotnet build -c Release # success, 0 warnings, 0 errors
dotnet test -c Release -m:1  # 6/6 passed
```

Manual launch: `src\MergeTool.App.Wpf\bin\Debug\net10.0-windows\MergeTool.App.Wpf.exe`
started, showed the "MergeTool" window (verified process `MainWindowTitle='MergeTool'`,
`Responding=True`), and was closed cleanly.

## Known limitations / deliberate deferrals

- The first `dotnet test -c Release` (parallel) aborted with `OutOfMemoryException` /
  `HRESULT 0x800705AF` (commit limit) on this machine. This is an environment constraint,
  not a product defect; the repeatable mitigation (`-m:1`) is baked into `test.ps1` and
  documented here. Individual test runs pass in both configurations.
- Source control (git) is not used in this environment per product-owner instruction; work is
  on a local copy only. "Clean checkout" verification is approximated by `clean.ps1` plus a
  full rebuild.
- PowerShell 7 (`pwsh`) is not installed; scripts run under Windows PowerShell 5.1 and are
  written to be compatible (`#Requires -Version 5.1`).

## Dependencies introduced

- MSTest 4.0.2 (MIT) — recorded in `docs/DEPENDENCIES.md` together with SDK-provided .NET/WPF
  platform dependencies. No other packages.
