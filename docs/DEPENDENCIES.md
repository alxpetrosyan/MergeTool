# Dependencies

Every direct dependency must be recorded here **before** it is committed, per
`00_LICENSE_AND_DEPENDENCY_POLICY.txt`. All package versions are pinned centrally in
`Directory.Packages.props`, and restore uses lock files (`packages.lock.json`).

## SDK-provided platform dependencies

| Dependency | Version | Purpose | Licence | Source | Approver |
| --- | --- | --- | --- | --- | --- |
| .NET SDK / runtime | 10.0.301 (pinned in `global.json`, rollForward latestPatch) | Build toolchain, BCL, runtime | MIT | https://github.com/dotnet/sdk | Task pack baseline (00_LICENSE_AND_DEPENDENCY_POLICY.txt) |
| WPF (Windows Desktop SDK) | Ships with .NET 10 SDK | Desktop UI framework for `MergeTool.App.Wpf` | MIT | https://github.com/dotnet/wpf | Task pack baseline (00_LICENSE_AND_DEPENDENCY_POLICY.txt) |

## NuGet packages (direct)

| Package | Version | Purpose | Licence (direct/transitive) | Source | Approver |
| --- | --- | --- | --- | --- | --- |
| MSTest | 4.0.2 | Unit test framework and runner for all test projects | MIT (metapackage; transitive MSTest.* packages are also MIT) | https://github.com/microsoft/testfx | Task pack baseline (00_LICENSE_AND_DEPENDENCY_POLICY.txt) |
| AvalonEdit | 6.3.1.120 | Text editor control for the file-compare panes; referenced only by `MergeTool.App.Wpf` | MIT (no transitive dependencies) | https://github.com/icsharpcode/AvalonEdit | Task pack baseline (00_LICENSE_AND_DEPENDENCY_POLICY.txt); added in MVP-06 per plan |

## Decided against

- **DiffPlex** — MVP-04 decision: **not used**. The line diff is a small internal
  patience-style implementation (`MergeTool.Text.Diff.PatienceLineDiffEngine`), deterministic
  and dependency-free, so no adapter or package is required.

## POL-06 compliance audit (2026-07-02)

| Check | Result |
|-------|--------|
| Direct NuGet packages in `.csproj` files | **MSTest**, **AvalonEdit** only (`ExcludedFeatureComplianceTests`) |
| Lock files | `packages.lock.json` per project via `RestorePackagesWithLockFile` |
| Transitive packages (publish) | .NET runtime facades + AvalonEdit (no additional third-party UI) |
| Telemetry / updater / installer packages | **Absent** from source and project files |
| Araxis-originated assets | **None identified** — product uses original strings and layouts |
| Licences | MIT for all direct dependencies (see table above) |

Automated guard: `tests/MergeTool.Application.Tests/ExcludedFeatureComplianceTests.cs`.
