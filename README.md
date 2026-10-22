# MergeTool

A lightweight, Windows-only WPF desktop application for **manual** comparison and merging of
text files and visual comparison of folder trees. All processing is local; the application
makes no cloud calls and collects no telemetry.

## Status

Early foundation (MVP scope in progress). The shell launches, but comparison workspaces are
not implemented yet.

## Requirements

- Windows 11 (Windows 10 22H2 acceptable), 64-bit
- .NET 10 SDK (pinned via `global.json`; verify with `dotnet --list-sdks`)
- PowerShell for the build scripts

## Build and test

```powershell
./build.ps1        # restore + build Debug and Release
./test.ps1         # run all tests in Debug and Release
./clean.ps1        # remove bin/obj output
```

Or directly:

```powershell
dotnet restore
dotnet build -c Debug
dotnet test -c Debug
dotnet build -c Release
dotnet test -c Release
```

Run the application:

```powershell
dotnet run --project src/MergeTool.App.Wpf
```

## Solution layout

| Project | Purpose |
| --- | --- |
| `src/MergeTool.App.Wpf` | Views, view models, WPF-only renderers and dialog adapters |
| `src/MergeTool.Application` | Use cases, sessions, commands, orchestration; no WPF types |
| `src/MergeTool.Core` | Small shared primitives: diagnostics, result types, IDs, options |
| `src/MergeTool.Text` | Document snapshots, diff/merge domain, text algorithms |
| `src/MergeTool.Folders` | Folder scans, quick compare model, operation plans |
| `src/MergeTool.FileSystem` | File IO abstraction, atomic saves, identities, path handling |
| `tests/*` | MSTest projects mirroring the source projects |

Dependency direction: the WPF app may reference all other projects; `MergeTool.Application`
may reference Text, Folders, FileSystem, and Core; Text/Folders/FileSystem may reference Core
only. Non-WPF projects must never reference WPF types.

## Explicit product exclusions

The product deliberately does **not** include:

- Automatic merge or automatic folder synchronization
- Binary, image, PDF, or Office document comparison
- Patch file support
- Version-control (Git/SVN/Mercurial) integration
- Report generation or printing
- Installers, licensing, auto-update, or cross-platform support
- Cloud processing, accounts, or telemetry

## Dependencies

All NuGet packages are centrally pinned in `Directory.Packages.props` and restored with lock
files (`packages.lock.json`). Every direct dependency is documented in
`docs/DEPENDENCIES.md` before it is introduced.
