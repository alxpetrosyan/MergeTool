# MVP-01 Testing Report

**Task:** Verify: Create repository, solution, deterministic build contract, and project boundaries
**Date:** 2026-07-02
**Verdict:** PASS

## Commands run and exit results

Executed after `clean.ps1` removed all `bin`/`obj`/`TestResults` output (closest available
equivalent to a clean checkout — git is intentionally not used in this environment per
product-owner instruction):

| Command | Result |
| --- | --- |
| `dotnet restore` | exit 0; all 11 projects restored, lock files honoured |
| `dotnet build -c Debug --no-restore` | exit 0; 0 warnings, 0 errors |
| `dotnet test -c Debug -m:1 --no-build` | exit 0; 6/6 tests passed (5 assemblies) |
| `dotnet build -c Release --no-restore` | exit 0; 0 warnings, 0 errors |
| `dotnet test -c Release -m:1 --no-build` | exit 0; 6/6 tests passed (5 assemblies) |

Environment note: a fully parallel `dotnet test` can abort with `OutOfMemoryException`
(Windows commit limit) on this machine because five test hosts start concurrently. `test.ps1`
therefore pins `-m:1`. This is an environment constraint, recorded in the development report;
it is not a product defect.

## Automated test coverage exercised

- One smoke test per non-WPF production assembly (Core, Text, Folders, FileSystem,
  Application). Each verifies the assembly loads **and** that its MergeTool references follow
  the guardrail dependency direction.
- One placeholder smoke test for the WPF assembly (view-model tests will replace it when view
  models exist).

## Manual scenarios exercised

- Launched `MergeTool.App.Wpf.exe` (Release build). The shell window appeared with title
  "MergeTool", a product-name header, and disabled "File Compare" / "Folder Compare"
  navigation buttons. Process reported `Responding=True`; closed cleanly. The Debug build was
  launched the same way during development with the same result.

## Architecture and licence checks

- `global.json` pins SDK 10.0.301 (real installed SDK, `rollForward: latestPatch`).
- `Directory.Build.props` enables nullable, implicit usings, deterministic build,
  warnings-as-errors, SDK analyzers, and restore lock files — no placeholders.
- `Directory.Packages.props` is the only place a package version appears; grep across all
  `.csproj` files found no inline `Version=` attributes. Eleven `packages.lock.json` files
  are present (one per project).
- `UseWPF` appears only in `MergeTool.App.Wpf` and `MergeTool.App.Wpf.Tests`. No
  AvalonEdit/System.Windows/WPF reference exists in any non-WPF project (also enforced by the
  smoke tests).
- Only dependency is MSTest 4.0.2 (MIT), documented in `docs/DEPENDENCIES.md` with SDK
  platform entries. No unlicensed or unexpected packages.
- README names the explicit product exclusions.
- Build scripts `build.ps1`, `test.ps1`, `clean.ps1` exist and work.

## Defects created

None.

## Recommendation

**Proceed** to MVP-02.
