using System.Collections.ObjectModel;
using System.IO;
using MergeTool.App.Wpf.Services;
using MergeTool.Application.Operations;
using MergeTool.Application.Progress;
using MergeTool.Application.Requests;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.Core.Settings;
using MergeTool.Folders;
using MergeTool.Folders.Operations;

namespace MergeTool.App.Wpf.ViewModels;

/// <summary>
/// Folder-comparison tab: pick two roots, run a quick metadata scan, filter rows, and open a
/// paired text comparison through the application shell.
/// </summary>
public sealed class FolderCompareWorkspaceViewModel : WorkspaceViewModel
{
    public const string OneSidedOpenMessage =
        "A 2-way file comparison needs both a left and a right file path. "
        + "This row exists on one side only.";

    private readonly RunFolderComparisonUseCase _compareUseCase;
    private readonly FindFolderRenameCandidatesUseCase _renameCandidatesUseCase;
    private readonly FolderOperationExecutor _folderOperationExecutor;
    private readonly IFolderPickerService? _folderPicker;
    private readonly IUserPromptService? _prompts;
    private readonly IFolderOperationPromptService? _operationPrompts;
    private readonly IFileComparisonLauncher? _fileComparisonLauncher;
    private readonly IComparisonSessionRecorder? _sessionRecorder;

    private string _leftRoot = string.Empty;
    private string _rightRoot = string.Empty;
    private FolderComparisonMode _comparisonMode = FolderComparisonMode.QuickMetadata;
    private int _timestampToleranceSeconds;
    private string? _rootValidationMessage;
    private string? _scanStatusText;
    private FolderCompareFilter _activeFilter = FolderCompareFilter.All;
    private FolderCompareRowViewModel? _selectedRow;
    private IReadOnlyList<FolderCompareRowViewModel> _selectedRows = [];
    private bool _hasCompletedScan;
    private List<FolderCompareRowViewModel> _allRows = [];
    private List<FolderCompareTreeRowViewModel> _treeRoots = [];
    private ObservableCollection<FolderCompareTreeRowViewModel> _treeRows = [];
    private RenameCandidateViewModel? _selectedRenameCandidate;
    private string? _renameCandidatesStatusText;
    private readonly Action<string> _revealInExplorer;
    private readonly IRecentPathProvider? _recentPathProvider;

    public FolderCompareWorkspaceViewModel(
        RunFolderComparisonUseCase compareUseCase,
        IFolderPickerService? folderPicker = null,
        IUserPromptService? prompts = null,
        IFileComparisonLauncher? fileComparisonLauncher = null,
        IFolderOperationPromptService? operationPrompts = null,
        IFolderFileOperations? folderFileOperations = null,
        IComparisonSessionRecorder? sessionRecorder = null,
        FindFolderRenameCandidatesUseCase? renameCandidatesUseCase = null,
        Action<string>? revealInExplorer = null,
        IRecentPathProvider? recentPathProvider = null)
    {
        _revealInExplorer = revealInExplorer ?? ExplorerReveal.RevealPath;
        _recentPathProvider = recentPathProvider;
        ArgumentNullException.ThrowIfNull(compareUseCase);
        _compareUseCase = compareUseCase;
        _renameCandidatesUseCase = renameCandidatesUseCase ?? new FindFolderRenameCandidatesUseCase();
        _folderOperationExecutor = new FolderOperationExecutor(
            folderFileOperations ?? new PhysicalFolderFileOperations());
        _folderPicker = folderPicker;
        _prompts = prompts;
        _operationPrompts = operationPrompts;
        _fileComparisonLauncher = fileComparisonLauncher;
        _sessionRecorder = sessionRecorder;
        Title = "Folder Compare";

        CompareCommand = new RelayCommand(async _ => await CompareAsync(), _ => CanCompare);
        RecompareCommand = new RelayCommand(async _ => await CompareAsync(), _ => CanRecompare);
        ChooseLeftFolderCommand = new RelayCommand(_ => ChooseFolder(isLeft: true), _ => _folderPicker is not null);
        ChooseRightFolderCommand = new RelayCommand(_ => ChooseFolder(isLeft: false), _ => _folderPicker is not null);
        OpenPairedTextComparisonCommand = new RelayCommand(
            async _ => await OpenPairedTextComparisonAsync(),
            _ => CanOpenPairedTextComparison);
        CopyLeftToRightCommand = new RelayCommand(
            async _ => await RunFolderOperationAsync(FolderOperationKind.CopyLeftToRight),
            _ => CanRunFolderOperations);
        CopyRightToLeftCommand = new RelayCommand(
            async _ => await RunFolderOperationAsync(FolderOperationKind.CopyRightToLeft),
            _ => CanRunFolderOperations);
        DeleteLeftCommand = new RelayCommand(
            async _ => await RunFolderOperationAsync(FolderOperationKind.DeleteLeft),
            _ => CanRunFolderOperations);
        DeleteRightCommand = new RelayCommand(
            async _ => await RunFolderOperationAsync(FolderOperationKind.DeleteRight),
            _ => CanRunFolderOperations);
        SetFilterAllCommand = new RelayCommand(_ => ActiveFilter = FolderCompareFilter.All);
        SetFilterDifferencesCommand = new RelayCommand(_ => ActiveFilter = FolderCompareFilter.Differences);
        SetFilterErrorsSkippedCommand = new RelayCommand(_ => ActiveFilter = FolderCompareFilter.ErrorsSkipped);
        SetFilterLeftOnlyCommand = new RelayCommand(_ => ActiveFilter = FolderCompareFilter.LeftOnly);
        SetFilterRightOnlyCommand = new RelayCommand(_ => ActiveFilter = FolderCompareFilter.RightOnly);
        ExpandAllFoldersCommand = new RelayCommand(_ => SetAllExpanded(true), _ => _treeRoots.Count > 0);
        CollapseAllFoldersCommand = new RelayCommand(_ => SetAllExpanded(false), _ => _treeRoots.Count > 0);
        RevealLeftInExplorerCommand = new RelayCommand(
            _ => Reveal(SelectedRow?.Source.LeftFullPath),
            _ => SelectedRow?.Source.LeftFullPath is not null);
        RevealRightInExplorerCommand = new RelayCommand(
            _ => Reveal(SelectedRow?.Source.RightFullPath),
            _ => SelectedRow?.Source.RightFullPath is not null);

        FilteredRows = [];
        RenameCandidates = [];

        FindRenameCandidatesCommand = new RelayCommand(
            async _ => await FindRenameCandidatesAsync(),
            _ => CanFindRenameCandidates);
        AcceptRenameCandidateCommand = new RelayCommand(
            async _ => await AcceptRenameCandidateAsync(),
            _ => CanAcceptRenameCandidate);
        ClearRenameCandidatesCommand = new RelayCommand(_ => ClearRenameCandidates(), _ => RenameCandidates.Count > 0);

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SelectedRow) or nameof(SelectedRows) or nameof(IsBusy) or nameof(State))
            {
                RaiseFolderOperationCommandsChanged();
                RaiseRenameCandidateCommandsChanged();
            }
        };
    }

    public ObservableCollection<FolderCompareRowViewModel> FilteredRows { get; }

    /// <summary>Visible tree-grid rows: filtered, expansion-aware, depth-first order.</summary>
    public ObservableCollection<FolderCompareTreeRowViewModel> TreeRows
    {
        get => _treeRows;
        private set => SetProperty(ref _treeRows, value);
    }

    public ObservableCollection<RenameCandidateViewModel> RenameCandidates { get; }

    public RenameCandidateViewModel? SelectedRenameCandidate
    {
        get => _selectedRenameCandidate;
        set
        {
            if (SetProperty(ref _selectedRenameCandidate, value))
            {
                RaiseRenameCandidateCommandsChanged();
            }
        }
    }

    public string? RenameCandidatesStatusText
    {
        get => _renameCandidatesStatusText;
        private set => SetProperty(ref _renameCandidatesStatusText, value);
    }

    public bool CanFindRenameCandidates => _hasCompletedScan && !IsBusy;

    public bool CanAcceptRenameCandidate =>
        !IsBusy && SelectedRenameCandidate is not null && _fileComparisonLauncher is not null;

    public string LeftRoot
    {
        get => _leftRoot;
        set
        {
            if (SetProperty(ref _leftRoot, value))
            {
                UpdateRootValidationMessage();
                RaiseCompareCommandsChanged();
            }
        }
    }

    public string RightRoot
    {
        get => _rightRoot;
        set
        {
            if (SetProperty(ref _rightRoot, value))
            {
                UpdateRootValidationMessage();
                RaiseCompareCommandsChanged();
            }
        }
    }

    public IReadOnlyList<FolderComparisonMode> ComparisonModes { get; } =
        Enum.GetValues<FolderComparisonMode>();

    public FolderComparisonMode ComparisonMode
    {
        get => _comparisonMode;
        set
        {
            if (SetProperty(ref _comparisonMode, value))
            {
                RaisePropertyChanged(nameof(IsTimestampToleranceEnabled));
            }
        }
    }

    public bool IsTimestampToleranceEnabled =>
        ComparisonMode == FolderComparisonMode.QuickMetadata;

    public int TimestampToleranceSeconds
    {
        get => _timestampToleranceSeconds;
        set => SetProperty(ref _timestampToleranceSeconds, Math.Max(0, value));
    }

    public string? RootValidationMessage
    {
        get => _rootValidationMessage;
        private set => SetProperty(ref _rootValidationMessage, value);
    }

    public string? ScanStatusText
    {
        get => _scanStatusText;
        private set => SetProperty(ref _scanStatusText, value);
    }

    public string MetadataLegendText => FolderRowStateLabels.MetadataMatchExplanation;

    public string ContentLegendText => FolderRowStateLabels.ContentMatchExplanation;

    public FolderCompareFilter ActiveFilter
    {
        get => _activeFilter;
        set
        {
            if (SetProperty(ref _activeFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    public FolderCompareRowViewModel? SelectedRow
    {
        get => _selectedRow;
        set => SetProperty(ref _selectedRow, value);
    }

    public IReadOnlyList<FolderCompareRowViewModel> SelectedRows
    {
        get => _selectedRows;
        private set => SetProperty(ref _selectedRows, value);
    }

    public bool CanCompare =>
        !string.IsNullOrWhiteSpace(LeftRoot)
        && !string.IsNullOrWhiteSpace(RightRoot)
        && RootValidationMessage is null
        && !IsBusy;

    public bool CanRecompare => _hasCompletedScan && CanCompare;

    public bool CanOpenPairedTextComparison =>
        !IsBusy && SelectedRow?.CanOpenPairedTextComparison == true;

    public bool CanRunFolderOperations =>
        !IsBusy
        && _hasCompletedScan
        && _operationPrompts is not null
        && GetOperationTargetRows().Count > 0;

    public RelayCommand CompareCommand { get; }

    public RelayCommand RecompareCommand { get; }

    public RelayCommand ChooseLeftFolderCommand { get; }

    public RelayCommand ChooseRightFolderCommand { get; }

    public RelayCommand OpenPairedTextComparisonCommand { get; }

    public RelayCommand CopyLeftToRightCommand { get; }

    public RelayCommand CopyRightToLeftCommand { get; }

    public RelayCommand DeleteLeftCommand { get; }

    public RelayCommand DeleteRightCommand { get; }

    public RelayCommand SetFilterAllCommand { get; }

    public RelayCommand SetFilterDifferencesCommand { get; }

    public RelayCommand SetFilterErrorsSkippedCommand { get; }

    public RelayCommand SetFilterLeftOnlyCommand { get; }

    public RelayCommand SetFilterRightOnlyCommand { get; }

    public RelayCommand ExpandAllFoldersCommand { get; }

    public RelayCommand CollapseAllFoldersCommand { get; }

    public RelayCommand RevealLeftInExplorerCommand { get; }

    public RelayCommand RevealRightInExplorerCommand { get; }

    /// <summary>Recent comparison paths for the path selector dropdowns.</summary>
    public IReadOnlyList<string> RecentPaths => _recentPathProvider?.GetRecentPaths() ?? [];

    public RelayCommand FindRenameCandidatesCommand { get; }

    public RelayCommand AcceptRenameCandidateCommand { get; }

    public RelayCommand ClearRenameCandidatesCommand { get; }

    /// <summary>Builds the tab-opening request without reading file bodies.</summary>
    public OpenPairedFileComparisonRequest? BuildOpenPairedComparisonRequest()
    {
        FolderComparisonRow? row = SelectedRow?.Source;
        if (row is null || !row.HasPairedFiles)
        {
            return null;
        }

        return new OpenPairedFileComparisonRequest(row.LeftFullPath!, row.RightFullPath!);
    }

    public async Task CompareAsync()
    {
        string leftRoot = LeftRoot;
        string rightRoot = RightRoot;
        string? completionStatus = null;

        await RunOperationAsync("Scanning folders…", async cancellationToken =>
        {
            Progress<FolderComparisonProgress> inner = new(report =>
            {
                ScanStatusText = report.Phase == FolderComparisonPhase.ContentHash
                    ? report.CurrentRelativePath is null
                        ? $"Hashing… {report.EntriesScanned} file(s)"
                        : $"Hashing… {report.EntriesScanned} file(s) — {report.CurrentRelativePath}"
                    : report.CurrentRelativePath is null
                        ? $"Scanning… {report.EntriesScanned} entries"
                        : $"Scanning… {report.EntriesScanned} entries — {report.CurrentRelativePath}";
            });
            ThrottledProgress<FolderComparisonProgress> progress = new(inner, TimeSpan.FromMilliseconds(200));

            OperationResult<FolderComparisonResult> result = await _compareUseCase.CompareAsync(
                new FolderComparisonRequest(leftRoot, rightRoot, BuildComparisonOptions()),
                progress,
                cancellationToken);

            progress.Flush();

            if (result.IsCompleted)
            {
                ApplyRows(result.Value);
                _hasCompletedScan = true;
                completionStatus =
                    $"{result.Value.Rows.Count} row(s) from {result.Value.EntriesScanned} scanned entries.";
                ScanStatusText = completionStatus;
                _sessionRecorder?.RecordRecent(new RecentComparisonEntry(
                    ComparisonSessionKinds.FolderPair,
                    leftRoot,
                    rightRoot,
                    null,
                    DateTime.UtcNow,
                    SessionFolderOptionsApplier.BuildOptionsJson(ComparisonMode, TimestampToleranceSeconds)));
                RaisePropertyChanged(nameof(RecentPaths));
            }

            return result.IsCompleted
                ? OperationResult.Completed(result.Diagnostics)
                : result.IsCancelled
                    ? OperationResult.Cancelled(result.Diagnostics)
                    : OperationResult.Failed(result.Diagnostics);
        });

        if (State == WorkspaceUiState.Cancelled && _hasCompletedScan)
        {
            ScanStatusText = "Scan cancelled. Showing last completed results.";
        }
        else if (State == WorkspaceUiState.Cancelled)
        {
            ScanStatusText = "Scan cancelled.";
        }
        else if (completionStatus is not null)
        {
            // Progress<T> posts reports asynchronously to the UI thread, so a late throttled
            // report can land after the completion text; re-assert the final status here.
            ScanStatusText = completionStatus;
        }

        RaiseCompareCommandsChanged();
    }

    public async Task OpenPairedTextComparisonAsync()
    {
        OpenPairedFileComparisonRequest? request = BuildOpenPairedComparisonRequest();
        if (request is null)
        {
            if (SelectedRow?.Source.LeftFullPath is null || SelectedRow.Source.RightFullPath is null)
            {
                _prompts?.ShowInformation("Cannot open file comparison", OneSidedOpenMessage);
            }

            return;
        }

        if (_fileComparisonLauncher is null)
        {
            return;
        }

        await _fileComparisonLauncher.OpenPairedComparisonAsync(request.LeftPath, request.RightPath);
    }

    public async Task FindRenameCandidatesAsync()
    {
        if (!_hasCompletedScan)
        {
            return;
        }

        IReadOnlyList<FolderComparisonRow> rows = _allRows.Select(row => row.Source).ToList();
        await RunOperationAsync("Finding rename candidates…", async cancellationToken =>
        {
            OperationResult<IReadOnlyList<FolderRenameCandidate>> result =
                await _renameCandidatesUseCase.FindAsync(rows, cancellationToken: cancellationToken);

            if (result.IsCompleted)
            {
                RenameCandidates.Clear();
                foreach (FolderRenameCandidate candidate in result.Value)
                {
                    RenameCandidates.Add(new RenameCandidateViewModel(candidate));
                }

                SelectedRenameCandidate = RenameCandidates.FirstOrDefault();
                RenameCandidatesStatusText = RenameCandidates.Count == 0
                    ? "No rename candidates found among one-sided files."
                    : $"{RenameCandidates.Count} rename candidate(s) found. Select one and press Compare files to inspect.";
            }

            return result.IsCompleted
                ? OperationResult.Completed(result.Diagnostics)
                : result.IsCancelled
                    ? OperationResult.Cancelled(result.Diagnostics)
                    : OperationResult.Failed(result.Diagnostics);
        });

        RaiseRenameCandidateCommandsChanged();
    }

    public async Task AcceptRenameCandidateAsync()
    {
        FolderRenameCandidate? candidate = SelectedRenameCandidate?.Source;
        if (candidate is null || _fileComparisonLauncher is null)
        {
            return;
        }

        await _fileComparisonLauncher.OpenPairedComparisonAsync(candidate.LeftFullPath, candidate.RightFullPath);
    }

    public void ClearRenameCandidates()
    {
        RenameCandidates.Clear();
        SelectedRenameCandidate = null;
        RenameCandidatesStatusText = null;
        RaiseRenameCandidateCommandsChanged();
    }

    /// <summary>Updates multi-row selection from the folder grid.</summary>
    public void SetSelectedRows(IReadOnlyList<FolderCompareRowViewModel> rows)
    {
        SelectedRows = rows;
        SelectedRow = rows.Count > 0 ? rows[^1] : null;
    }

    public async Task RunFolderOperationAsync(FolderOperationKind kind)
    {
        if (_operationPrompts is null)
        {
            return;
        }

        IReadOnlyList<FolderCompareRowViewModel> targetRows = GetOperationTargetRows();
        if (targetRows.Count == 0)
        {
            return;
        }

        IEnumerable<FolderComparisonRow> sourceRows = targetRows.Select(row => row.Source);
        FolderOperationPlan plan = kind switch
        {
            FolderOperationKind.CopyLeftToRight => FolderOperationPlanner.BuildCopyLeftToRight(
                sourceRows,
                LeftRoot,
                RightRoot),
            FolderOperationKind.CopyRightToLeft => FolderOperationPlanner.BuildCopyRightToLeft(
                sourceRows,
                LeftRoot,
                RightRoot),
            FolderOperationKind.DeleteLeft => FolderOperationPlanner.BuildDeleteLeft(
                sourceRows,
                LeftRoot,
                RightRoot),
            FolderOperationKind.DeleteRight => FolderOperationPlanner.BuildDeleteRight(
                sourceRows,
                LeftRoot,
                RightRoot),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        FolderOperationPlanValidationResult validation = FolderOperationPlanValidator.Validate(
            plan,
            LeftRoot,
            RightRoot);
        if (!validation.IsValid)
        {
            ReplaceDiagnostics(validation.Diagnostics);

            _prompts?.ShowInformation(
                "Cannot run folder operations",
                string.Join(
                    Environment.NewLine,
                    validation.Diagnostics.Select(d => d.Message)));
            return;
        }

        if (_operationPrompts.ConfirmPlan(plan) != FolderOperationConfirmation.Confirm)
        {
            return;
        }

        bool preferRecycleBin = true;
        if (plan.HasDeleteOperations && !RecycleBinSupport.IsAvailable
            && !_operationPrompts.ConfirmPermanentDelete(
                plan.Items.Count(item => item.Kind is FolderOperationKind.DeleteLeft or FolderOperationKind.DeleteRight)))
        {
            return;
        }

        int succeededCount = 0;
        await RunOperationAsync("Running folder operations…", cancellationToken =>
        {
            FolderOperationResult result = _folderOperationExecutor.Execute(
                plan,
                LeftRoot,
                RightRoot,
                preferRecycleBin,
                cancellationToken);
            succeededCount = result.SucceededCount;

            OperationResult operationResult = result.FailedCount == 0
                ? OperationResult.Completed(result.Diagnostics)
                : OperationResult.Failed(result.Diagnostics);
            return Task.FromResult(operationResult);
        });

        if (succeededCount > 0 && State != WorkspaceUiState.Cancelled)
        {
            await CompareAsync();
        }
    }

    private FolderComparisonOptions BuildComparisonOptions()
        => new(
            ComparisonMode,
            TimeSpan.FromSeconds(TimestampToleranceSeconds));

    private void ApplyRows(FolderComparisonResult result)
    {
        ClearRenameCandidates();
        _allRows = result.Rows.Select(row => new FolderCompareRowViewModel(row)).ToList();
        _treeRoots = FolderComparisonTreeBuilder.Build(result.Rows)
            .Select(node => new FolderCompareTreeRowViewModel(node, depth: 0, OnTreeExpansionChanged))
            .ToList();
        ApplyFilter();
        // No auto-selection: merge/open commands stay disabled until the user picks a row.
        SelectedRow = null;
        SelectedRows = [];
        RaiseFolderOperationCommandsChanged();
        ExpandAllFoldersCommand.RaiseCanExecuteChanged();
        CollapseAllFoldersCommand.RaiseCanExecuteChanged();
    }

    private void ApplyFilter()
    {
        FilteredRows.Clear();
        foreach (FolderCompareRowViewModel row in _allRows.Where(r => FolderCompareFilterRules.Includes(ActiveFilter, r.State)))
        {
            FilteredRows.Add(row);
        }

        if (SelectedRow is not null && !FilteredRows.Contains(SelectedRow))
        {
            SelectedRow = FilteredRows.FirstOrDefault();
        }

        RebuildTreeRows();
    }

    /// <summary>Flattens the tree into visible rows honoring the active filter and expansion.</summary>
    private void RebuildTreeRows()
    {
        List<FolderCompareTreeRowViewModel> visible = [];
        foreach (FolderCompareTreeRowViewModel root in _treeRoots)
        {
            AppendVisible(root, visible);
        }

        TreeRows = new ObservableCollection<FolderCompareTreeRowViewModel>(visible);
    }

    private void AppendVisible(FolderCompareTreeRowViewModel row, List<FolderCompareTreeRowViewModel> target)
    {
        if (!IsIncludedByFilter(row))
        {
            return;
        }

        target.Add(row);
        if (!row.IsExpanded)
        {
            return;
        }

        foreach (FolderCompareTreeRowViewModel child in row.Children)
        {
            AppendVisible(child, target);
        }
    }

    private bool IsIncludedByFilter(FolderCompareTreeRowViewModel row)
    {
        if (ActiveFilter == FolderCompareFilter.All)
        {
            return true;
        }

        if (row.RowViewModel is not null && FolderCompareFilterRules.Includes(ActiveFilter, row.RowViewModel.State))
        {
            return true;
        }

        // Directories stay visible when any descendant passes the filter.
        return row.Children.Any(IsIncludedByFilter);
    }

    private void OnTreeExpansionChanged(FolderCompareTreeRowViewModel _) => RebuildTreeRows();

    private void SetAllExpanded(bool expanded)
    {
        foreach (FolderCompareTreeRowViewModel root in _treeRoots)
        {
            root.SetExpandedRecursive(expanded);
        }

        RebuildTreeRows();
    }

    /// <summary>Updates multi-row selection from the folder tree-grid.</summary>
    public void SetSelectedTreeRows(IReadOnlyList<FolderCompareTreeRowViewModel> rows)
    {
        SetSelectedRows(rows
            .Select(row => row.RowViewModel)
            .Where(row => row is not null)
            .Cast<FolderCompareRowViewModel>()
            .ToArray());
        RevealLeftInExplorerCommand.RaiseCanExecuteChanged();
        RevealRightInExplorerCommand.RaiseCanExecuteChanged();
    }

    private void Reveal(string? fullPath)
    {
        if (fullPath is not null)
        {
            _revealInExplorer(fullPath);
        }
    }

    private void UpdateRootValidationMessage()
    {
        if (string.IsNullOrWhiteSpace(LeftRoot) || string.IsNullOrWhiteSpace(RightRoot))
        {
            RootValidationMessage = null;
            return;
        }

        try
        {
            string left = PathNormalization.NormalizeFullPath(LeftRoot);
            string right = PathNormalization.NormalizeFullPath(RightRoot);
            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
            {
                RootValidationMessage = QuickFolderComparisonEngine.SameRootMessage;
                return;
            }
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            RootValidationMessage = "One of the folder paths is invalid.";
            return;
        }

        RootValidationMessage = null;
    }

    private void RaiseCompareCommandsChanged()
    {
        RaisePropertyChanged(nameof(CanCompare));
        RaisePropertyChanged(nameof(CanRecompare));
        CompareCommand.RaiseCanExecuteChanged();
        RecompareCommand.RaiseCanExecuteChanged();
        RaiseFolderOperationCommandsChanged();
        RaiseRenameCandidateCommandsChanged();
    }

    private void RaiseRenameCandidateCommandsChanged()
    {
        RaisePropertyChanged(nameof(CanFindRenameCandidates));
        RaisePropertyChanged(nameof(CanAcceptRenameCandidate));
        FindRenameCandidatesCommand.RaiseCanExecuteChanged();
        AcceptRenameCandidateCommand.RaiseCanExecuteChanged();
        ClearRenameCandidatesCommand.RaiseCanExecuteChanged();
    }

    private void RaiseFolderOperationCommandsChanged()
    {
        RaisePropertyChanged(nameof(CanOpenPairedTextComparison));
        RaisePropertyChanged(nameof(CanRunFolderOperations));
        OpenPairedTextComparisonCommand.RaiseCanExecuteChanged();
        CopyLeftToRightCommand.RaiseCanExecuteChanged();
        CopyRightToLeftCommand.RaiseCanExecuteChanged();
        DeleteLeftCommand.RaiseCanExecuteChanged();
        DeleteRightCommand.RaiseCanExecuteChanged();
        RevealLeftInExplorerCommand.RaiseCanExecuteChanged();
        RevealRightInExplorerCommand.RaiseCanExecuteChanged();
    }

    private IReadOnlyList<FolderCompareRowViewModel> GetOperationTargetRows()
    {
        if (SelectedRows.Count > 0)
        {
            return SelectedRows;
        }

        return SelectedRow is null ? [] : [SelectedRow];
    }

    private void ChooseFolder(bool isLeft)
    {
        string? path = _folderPicker?.PickFolder(isLeft ? "Choose left folder" : "Choose right folder");
        if (path is null)
        {
            return;
        }

        if (isLeft)
        {
            LeftRoot = path;
        }
        else
        {
            RightRoot = path;
        }
    }
}
