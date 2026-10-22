using System.Collections.ObjectModel;
using System.IO;
using MergeTool.App.Wpf.Services;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.Core.Settings;
using MergeTool.Folders;

namespace MergeTool.App.Wpf.ViewModels;

/// <summary>
/// 3-way folder-comparison tab: pick base/left/right roots, run a quick metadata scan,
/// filter rows, and open a 3-way text comparison when all three file paths exist.
/// </summary>
public sealed class ThreeWayFolderCompareWorkspaceViewModel : WorkspaceViewModel
{
    public const string IncompleteTripleOpenMessage =
        "A 3-way file comparison needs base, left, and right file paths. "
        + "This row is missing one or more sides.";

    private readonly RunFolderComparisonUseCase _compareUseCase;
    private readonly IFolderPickerService? _folderPicker;
    private readonly IUserPromptService? _prompts;
    private readonly IFileComparisonLauncher? _fileComparisonLauncher;
    private readonly IComparisonSessionRecorder? _sessionRecorder;

    private string _baseRoot = string.Empty;
    private string _leftRoot = string.Empty;
    private string _rightRoot = string.Empty;
    private FolderComparisonMode _comparisonMode = FolderComparisonMode.QuickMetadata;
    private int _timestampToleranceSeconds;
    private string? _rootValidationMessage;
    private string? _scanStatusText;
    private FolderCompareFilter _activeFilter = FolderCompareFilter.All;
    private FolderCompareRowViewModel? _selectedRow;
    private bool _hasCompletedScan;
    private List<FolderCompareRowViewModel> _allRows = [];

    public ThreeWayFolderCompareWorkspaceViewModel(
        RunFolderComparisonUseCase compareUseCase,
        IFolderPickerService? folderPicker = null,
        IUserPromptService? prompts = null,
        IFileComparisonLauncher? fileComparisonLauncher = null,
        IComparisonSessionRecorder? sessionRecorder = null)
    {
        ArgumentNullException.ThrowIfNull(compareUseCase);
        _compareUseCase = compareUseCase;
        _folderPicker = folderPicker;
        _prompts = prompts;
        _fileComparisonLauncher = fileComparisonLauncher;
        _sessionRecorder = sessionRecorder;
        Title = "3-Way Folder Compare";

        CompareCommand = new RelayCommand(async _ => await CompareAsync(), _ => CanCompare);
        RecompareCommand = new RelayCommand(async _ => await CompareAsync(), _ => CanRecompare);
        ChooseBaseFolderCommand = new RelayCommand(_ => ChooseFolder(FolderSide.Base), _ => _folderPicker is not null);
        ChooseLeftFolderCommand = new RelayCommand(_ => ChooseFolder(FolderSide.Left), _ => _folderPicker is not null);
        ChooseRightFolderCommand = new RelayCommand(_ => ChooseFolder(FolderSide.Right), _ => _folderPicker is not null);
        OpenThreeWayTextComparisonCommand = new RelayCommand(
            async _ => await OpenThreeWayTextComparisonAsync(),
            _ => CanOpenThreeWayTextComparison);
        SetFilterAllCommand = new RelayCommand(_ => ActiveFilter = FolderCompareFilter.All);
        SetFilterDifferencesCommand = new RelayCommand(_ => ActiveFilter = FolderCompareFilter.Differences);
        SetFilterErrorsSkippedCommand = new RelayCommand(_ => ActiveFilter = FolderCompareFilter.ErrorsSkipped);

        FilteredRows = [];
    }

    public ObservableCollection<FolderCompareRowViewModel> FilteredRows { get; }

    public string BaseRoot
    {
        get => _baseRoot;
        set
        {
            if (SetProperty(ref _baseRoot, value))
            {
                UpdateRootValidationMessage();
                RaiseCompareCommandsChanged();
            }
        }
    }

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
        set
        {
            if (SetProperty(ref _selectedRow, value))
            {
                RaisePropertyChanged(nameof(CanOpenThreeWayTextComparison));
                OpenThreeWayTextComparisonCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanCompare =>
        !string.IsNullOrWhiteSpace(BaseRoot)
        && !string.IsNullOrWhiteSpace(LeftRoot)
        && !string.IsNullOrWhiteSpace(RightRoot)
        && RootValidationMessage is null
        && !IsBusy;

    public bool CanRecompare => _hasCompletedScan && CanCompare;

    public bool CanOpenThreeWayTextComparison =>
        !IsBusy && SelectedRow?.CanOpenThreeWayTextComparison == true;

    public RelayCommand CompareCommand { get; }

    public RelayCommand RecompareCommand { get; }

    public RelayCommand ChooseBaseFolderCommand { get; }

    public RelayCommand ChooseLeftFolderCommand { get; }

    public RelayCommand ChooseRightFolderCommand { get; }

    public RelayCommand OpenThreeWayTextComparisonCommand { get; }

    public RelayCommand SetFilterAllCommand { get; }

    public RelayCommand SetFilterDifferencesCommand { get; }

    public RelayCommand SetFilterErrorsSkippedCommand { get; }

    public OpenThreeWayFileComparisonRequest? BuildOpenThreeWayComparisonRequest()
    {
        FolderComparisonRow? row = SelectedRow?.Source;
        if (row is null || !row.HasTripledFiles)
        {
            return null;
        }

        return new OpenThreeWayFileComparisonRequest(row.BaseFullPath!, row.LeftFullPath!, row.RightFullPath!);
    }

    public async Task CompareAsync()
    {
        string baseRoot = BaseRoot;
        string leftRoot = LeftRoot;
        string rightRoot = RightRoot;

        await RunOperationAsync("Scanning folders…", async cancellationToken =>
        {
            Progress<FolderComparisonProgress> progress = new(report =>
            {
                ScanStatusText = report.Phase == FolderComparisonPhase.ContentHash
                    ? report.CurrentRelativePath is null
                        ? $"Hashing… {report.EntriesScanned} file(s)"
                        : $"Hashing… {report.EntriesScanned} file(s) — {report.CurrentRelativePath}"
                    : report.CurrentRelativePath is null
                        ? $"Scanning… {report.EntriesScanned} entries"
                        : $"Scanning… {report.EntriesScanned} entries — {report.CurrentRelativePath}";
            });

            OperationResult<FolderComparisonResult> result = await _compareUseCase.CompareAsync(
                new FolderComparisonRequest(baseRoot, leftRoot, rightRoot, BuildComparisonOptions()),
                progress,
                cancellationToken);

            if (result.IsCompleted)
            {
                ApplyRows(result.Value);
                _hasCompletedScan = true;
                ScanStatusText =
                    $"{result.Value.Rows.Count} row(s) from {result.Value.EntriesScanned} scanned entries.";
                _sessionRecorder?.RecordRecent(new RecentComparisonEntry(
                    ComparisonSessionKinds.FolderThreeWay,
                    leftRoot,
                    rightRoot,
                    baseRoot,
                    DateTime.UtcNow,
                    SessionFolderOptionsApplier.BuildOptionsJson(ComparisonMode, TimestampToleranceSeconds)));
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

        RaiseCompareCommandsChanged();
    }

    public async Task OpenThreeWayTextComparisonAsync()
    {
        OpenThreeWayFileComparisonRequest? request = BuildOpenThreeWayComparisonRequest();
        if (request is null)
        {
            if (SelectedRow?.Source.HasTripledFiles != true)
            {
                _prompts?.ShowInformation("Cannot open 3-way file comparison", IncompleteTripleOpenMessage);
            }

            return;
        }

        if (_fileComparisonLauncher is null)
        {
            return;
        }

        await _fileComparisonLauncher.OpenThreeWayComparisonAsync(
            request.BasePath,
            request.LeftPath,
            request.RightPath);
    }

    private FolderComparisonOptions BuildComparisonOptions()
        => new(
            ComparisonMode,
            TimeSpan.FromSeconds(TimestampToleranceSeconds));

    private void ApplyRows(FolderComparisonResult result)
    {
        _allRows = result.Rows.Select(row => new FolderCompareRowViewModel(row)).ToList();
        ApplyFilter();
        SelectedRow = FilteredRows.FirstOrDefault();
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
    }

    private void UpdateRootValidationMessage()
    {
        if (string.IsNullOrWhiteSpace(BaseRoot)
            || string.IsNullOrWhiteSpace(LeftRoot)
            || string.IsNullOrWhiteSpace(RightRoot))
        {
            RootValidationMessage = null;
            return;
        }

        try
        {
            string basePath = PathNormalization.NormalizeFullPath(BaseRoot);
            string left = PathNormalization.NormalizeFullPath(LeftRoot);
            string right = PathNormalization.NormalizeFullPath(RightRoot);
            if (string.Equals(basePath, left, StringComparison.OrdinalIgnoreCase)
                || string.Equals(basePath, right, StringComparison.OrdinalIgnoreCase)
                || string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
            {
                RootValidationMessage = QuickFolderComparisonEngine.DuplicateRootMessage;
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
        RaisePropertyChanged(nameof(CanOpenThreeWayTextComparison));
        OpenThreeWayTextComparisonCommand.RaiseCanExecuteChanged();
    }

    private void ChooseFolder(FolderSide side)
    {
        string title = side switch
        {
            FolderSide.Base => "Choose base folder",
            FolderSide.Left => "Choose left folder",
            FolderSide.Right => "Choose right folder",
            _ => "Choose folder",
        };

        string? path = _folderPicker?.PickFolder(title);
        if (path is null)
        {
            return;
        }

        switch (side)
        {
            case FolderSide.Base:
                BaseRoot = path;
                break;
            case FolderSide.Left:
                LeftRoot = path;
                break;
            case FolderSide.Right:
                RightRoot = path;
                break;
        }
    }

    private enum FolderSide
    {
        Base,
        Left,
        Right,
    }
}

/// <summary>Request to open a 3-way file comparison tab for three existing file paths.</summary>
public sealed record OpenThreeWayFileComparisonRequest(string BasePath, string LeftPath, string RightPath);
