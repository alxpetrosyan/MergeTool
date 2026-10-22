# MVP-10 Development Report

**Task:** Build folder comparison workspace, filters, and paired text-file opening
**Date:** 2026-07-02
**Status:** Complete

## Feature boundary implemented

Full Folder Compare workspace in the existing WPF shell: folder pickers, compare/recompare,
virtualized flat grid, progress/status, semantic row styling, explicit metadata legend,
filters, and opening a paired File Compare tab through application workflow. No embedded
preview, folder copy/delete, tree mode, content hashing, or file watcher.

## Objective completed

### Services

- `IFolderPickerService` / `WpfFolderPickerService` — `Microsoft.Win32.OpenFolderDialog`
  (no new NuGet packages).
- `IFileComparisonLauncher` — implemented by `MainWindowViewModel` to create/activate a
  File Compare tab and run `CompareAsync` with supplied paths.
- `IUserPromptService.ShowInformation` — one-sided open attempts show the mandated message.

### View models

- `FolderCompareFilter` + `FolderCompareFilterRules` — All, Differences (excludes metadata
  match), Errors/Skipped.
- `FolderCompareRowViewModel` — grid columns including diagnostics text.
- `FolderCompareWorkspaceViewModel` — root validation (including same-folder rejection),
  async scan via `RunFolderComparisonUseCase`, progress text, filter application, selected
  row, `OpenPairedTextComparisonCommand` enabled only for paired file rows,
  `BuildOpenPairedComparisonRequest`, cancel retains prior completed rows with explicit
  status wording.

### View

- `FolderCompareView.xaml` — pickers, commands, filter buttons, virtualized `DataGrid`,
  legend with **Metadata match = size + UTC modification time only**, row background styles
  per state.

### Shell wiring

- `MainWindowViewModel` factory injects folder compare dependencies and implements paired
  file tab opening.
- `MainWindow.xaml` hosts `FolderCompareView` for folder tabs.

## Files/projects changed

- `src/MergeTool.App.Wpf/Services/*` — folder picker, launcher, prompt extension
- `src/MergeTool.App.Wpf/ViewModels/FolderCompare*.cs`, `MainWindowViewModel.cs`
- `src/MergeTool.App.Wpf/Views/FolderCompareView.xaml(.cs)`
- `src/MergeTool.App.Wpf/MainWindow.xaml`
- `tests/MergeTool.App.Wpf.Tests/FolderCompareWorkspaceViewModelTests.cs`
- `tests/MergeTool.App.Wpf.Tests/WorkspaceLifecycleTests.cs` (updated shell factory)

## Tests added/updated

`FolderCompareWorkspaceViewModelTests` — filter rules, root validation, compare populates
rows and respects active filter, paired-command enablement, tab-opening request
construction, launcher invocation with correct paths.

## Build/test commands and results

```powershell
dotnet build -c Debug    # succeeded
dotnet test -c Debug     # 35/35 App.Wpf tests passed (202 total solution-wide)
dotnet build -c Release  # succeeded
dotnet test -c Release   # 35/35 App.Wpf tests passed (202 total solution-wide)
```

## Known limitations / deliberate deferrals

- Filter selection shown as enum name in the toolbar (functional; polish deferred).
- Cancel-during-rescan UI retention verified by implementation review; dedicated automated
  stall/cancel VM test deferred (engine cancellation covered in MVP-09).

## Dependencies introduced

None.
