using MergeTool.App.Wpf.Services;
using MergeTool.Application.Sessions;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.Core.Settings;
using MergeTool.Text.Diff;
using MergeTool.Text.ThreeWay;
using System.Collections.ObjectModel;

namespace MergeTool.App.Wpf.ViewModels;

/// <summary>
/// 3-way file comparison tab with source inspection and manual output merge.
/// </summary>
public sealed class ThreeWayCompareWorkspaceViewModel : WorkspaceViewModel
{
    private readonly OpenThreeWayFileComparisonUseCase _openUseCase;
    private readonly ThreeWayOutputPersistenceService _outputPersistence;
    private readonly IFilePickerService? _filePicker;
    private readonly IUserPromptService? _prompts;
    private readonly IComparisonSessionRecorder? _sessionRecorder;

    private readonly ThreeWayOutputMergeSession _outputMerge = new();

    private string _basePath = string.Empty;
    private string _leftPath = string.Empty;
    private string _rightPath = string.Empty;
    private string? _summaryText;
    private string? _navigationText;
    private string? _outputStatusText;
    private ThreeWayComparisonSession? _session;
    private ThreeWayRegionNavigator? _navigator;
    private DocumentPaneViewModel? _basePane;
    private DocumentPaneViewModel? _leftPane;
    private DocumentPaneViewModel? _rightPane;
    private DocumentPaneViewModel? _outputPane;
    private int? _selectedRegionOrdinal;

    public ThreeWayCompareWorkspaceViewModel(
        OpenThreeWayFileComparisonUseCase openUseCase,
        ThreeWayOutputPersistenceService outputPersistence,
        IFilePickerService? filePicker = null,
        IUserPromptService? prompts = null,
        IComparisonSessionRecorder? sessionRecorder = null)
    {
        ArgumentNullException.ThrowIfNull(openUseCase);
        ArgumentNullException.ThrowIfNull(outputPersistence);
        _openUseCase = openUseCase;
        _outputPersistence = outputPersistence;
        _filePicker = filePicker;
        _prompts = prompts;
        _sessionRecorder = sessionRecorder;
        Title = "3-Way Compare";

        CompareCommand = new RelayCommand(async _ => await CompareAsync(), _ => CanCompare);
        RecompareCommand = new RelayCommand(async _ => await RecompareAsync(), _ => CanRecompare);
        ChooseBaseFileCommand = new RelayCommand(_ => ChooseFile(ThreeWayDocumentSide.Base), _ => _filePicker is not null);
        ChooseLeftFileCommand = new RelayCommand(_ => ChooseFile(ThreeWayDocumentSide.Left), _ => _filePicker is not null);
        ChooseRightFileCommand = new RelayCommand(_ => ChooseFile(ThreeWayDocumentSide.Right), _ => _filePicker is not null);
        NextDifferenceCommand = new RelayCommand(_ => MoveToDifference(forward: true), _ => !IsBusy && (_navigator?.CanMoveNext ?? false));
        PreviousDifferenceCommand = new RelayCommand(_ => MoveToDifference(forward: false), _ => !IsBusy && (_navigator?.CanMovePrevious ?? false));
        NextUnresolvedCommand = new RelayCommand(_ => MoveToNextUnresolved(), _ => CanMoveToUnresolved);
        UseBaseForRegionCommand = new RelayCommand(_ => ApplyRegionChoice(ThreeWayOutputSourceChoice.Base), _ => CanApplyRegionChoice);
        UseLeftForRegionCommand = new RelayCommand(_ => ApplyRegionChoice(ThreeWayOutputSourceChoice.Left), _ => CanApplyRegionChoice);
        UseRightForRegionCommand = new RelayCommand(_ => ApplyRegionChoice(ThreeWayOutputSourceChoice.Right), _ => CanApplyRegionChoice);
        SaveOutputCommand = new RelayCommand(async _ => await SaveOutputAsync(), _ => CanSaveOutput);

        RegionList = [];
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(State))
            {
                RaiseSessionCommandsChanged();
            }
        };
    }

    public event EventHandler? SessionOpened;

    public event EventHandler? ComparisonUpdated;

    public event EventHandler? OutputUpdated;

    public event EventHandler<ThreeWayNavigationTarget>? NavigationRequested;

    public ObservableCollection<ThreeWayRegionEntryViewModel> RegionList { get; }

    public string BasePath
    {
        get => _basePath;
        set
        {
            if (SetProperty(ref _basePath, value))
            {
                RaiseCompareCommandsChanged();
            }
        }
    }

    public string LeftPath
    {
        get => _leftPath;
        set
        {
            if (SetProperty(ref _leftPath, value))
            {
                RaiseCompareCommandsChanged();
            }
        }
    }

    public string RightPath
    {
        get => _rightPath;
        set
        {
            if (SetProperty(ref _rightPath, value))
            {
                RaiseCompareCommandsChanged();
            }
        }
    }

    public string? SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    public string? NavigationText
    {
        get => _navigationText;
        private set => SetProperty(ref _navigationText, value);
    }

    public string? OutputStatusText
    {
        get => _outputStatusText;
        private set => SetProperty(ref _outputStatusText, value);
    }

    public ThreeWayComparisonSession? Session => _session;

    public ThreeWayOutputMergeSession OutputMerge => _outputMerge;

    public DocumentPaneViewModel? BasePane
    {
        get => _basePane;
        private set => SetProperty(ref _basePane, value);
    }

    public DocumentPaneViewModel? LeftPane
    {
        get => _leftPane;
        private set => SetProperty(ref _leftPane, value);
    }

    public DocumentPaneViewModel? RightPane
    {
        get => _rightPane;
        private set => SetProperty(ref _rightPane, value);
    }

    public DocumentPaneViewModel? OutputPane
    {
        get => _outputPane;
        private set => SetProperty(ref _outputPane, value);
    }

    public int? SelectedRegionOrdinal
    {
        get => _selectedRegionOrdinal;
        set
        {
            if (SetProperty(ref _selectedRegionOrdinal, value))
            {
                RaiseResolutionCommandsChanged();
            }
        }
    }

    public override bool HasUnsavedChanges => _outputMerge.Output?.IsDirty == true;

    public bool CanCompare =>
        !string.IsNullOrWhiteSpace(BasePath)
        && !string.IsNullOrWhiteSpace(LeftPath)
        && !string.IsNullOrWhiteSpace(RightPath)
        && !IsBusy;

    public bool CanRecompare => _session is not null && CanCompare;

    public bool CanApplyRegionChoice =>
        !IsBusy
        && _session?.CanNavigate == true
        && SelectedRegionOrdinal is int ordinal
        && _session.LatestResult?.Regions[ordinal].IsDifference == true;

    public bool CanSaveOutput =>
        !IsBusy
        && _session?.LatestResult is not null
        && _outputMerge.Output is not null;

    public bool CanMoveToUnresolved =>
        !IsBusy && _session?.LatestResult is not null && GetNextUnresolvedOrdinal(forward: true) is not null;

    public RelayCommand CompareCommand { get; }

    public RelayCommand RecompareCommand { get; }

    public RelayCommand ChooseBaseFileCommand { get; }

    public RelayCommand ChooseLeftFileCommand { get; }

    public RelayCommand ChooseRightFileCommand { get; }

    public RelayCommand NextDifferenceCommand { get; }

    public RelayCommand PreviousDifferenceCommand { get; }

    public RelayCommand NextUnresolvedCommand { get; }

    public RelayCommand UseBaseForRegionCommand { get; }

    public RelayCommand UseLeftForRegionCommand { get; }

    public RelayCommand UseRightForRegionCommand { get; }

    public RelayCommand SaveOutputCommand { get; }

    public async Task CompareAsync()
    {
        string basePath = BasePath;
        string leftPath = LeftPath;
        string rightPath = RightPath;

        await RunOperationAsync("Comparing files…", async cancellationToken =>
        {
            OperationResult<ThreeWayComparisonSession> result = await _openUseCase.OpenAsync(
                basePath,
                leftPath,
                rightPath,
                TextComparisonOptions.Default,
                cancellationToken);

            if (result.IsCompleted)
            {
                ApplySession(result.Value);
                _sessionRecorder?.RecordRecent(new RecentComparisonEntry(
                    ComparisonSessionKinds.FileThreeWay,
                    leftPath,
                    rightPath,
                    basePath,
                    DateTime.UtcNow));
            }

            return result.IsCompleted
                ? OperationResult.Completed(result.Diagnostics)
                : result.IsCancelled
                    ? OperationResult.Cancelled(result.Diagnostics)
                    : OperationResult.Failed(result.Diagnostics);
        });
    }

    public async Task RecompareAsync()
    {
        if (_session is null)
        {
            return;
        }

        await RunOperationAsync("Comparing files…", async cancellationToken =>
        {
            OperationStatus status = await _session.RecompareAsync(TextComparisonOptions.Default, cancellationToken);
            if (status == OperationStatus.Completed)
            {
                _outputMerge.RefreshAfterRecompare(_session);
                RefreshFromSession();
            }

            return status switch
            {
                OperationStatus.Completed => OperationResult.Completed(_session.Diagnostics),
                OperationStatus.Cancelled => OperationResult.Cancelled(_session.Diagnostics),
                _ => OperationResult.Failed(_session.Diagnostics),
            };
        });
    }

    public async Task SaveOutputAsync()
    {
        if (_session is null || _outputMerge.Output is null)
        {
            return;
        }

        string? targetPath = _filePicker?.PickSaveFile(
            "Save merged output",
            _outputMerge.OutputSavePath ?? _outputMerge.Output.Metadata.NormalizedFullPath);
        if (targetPath is null)
        {
            return;
        }

        await RunOperationAsync("Saving output…", cancellationToken =>
            _outputPersistence.SaveOutputAsync(_session, _outputMerge, targetPath, cancellationToken));

        if (State == WorkspaceUiState.Failed)
        {
            DiagnosticViewModel? failure = Diagnostics.LastOrDefault(d => d.Severity == DiagnosticSeverity.Error);
            if (failure is not null)
            {
                _prompts?.ShowInformation("Cannot save output", failure.Message);
            }
        }
        else if (State == WorkspaceUiState.Completed)
        {
            OutputPane?.Refresh();
            UpdateOutputStatusText();
            RaisePropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public void NavigateToRegion(int regionOrdinal)
    {
        if (_session is null || !_session.CanNavigate)
        {
            return;
        }

        _session.SelectRegion(regionOrdinal);
        SelectedRegionOrdinal = regionOrdinal;
        ThreeWayRegion region = _session.LatestResult!.Regions[regionOrdinal];
        IReadOnlyList<int> differences = _session.LatestResult.DifferenceRegionOrdinals;
        int differenceNumber = 0;
        for (int i = 0; i < differences.Count; i++)
        {
            if (differences[i] == regionOrdinal)
            {
                differenceNumber = i + 1;
                break;
            }
        }

        NavigationText = differences.Count == 0 || differenceNumber == 0
            ? null
            : $"Difference {differenceNumber} of {differences.Count} ({ThreeWayRegionKindLabels.GetDisplayName(region.Kind)})";
        NavigationRequested?.Invoke(
            this,
            new ThreeWayNavigationTarget(
                regionOrdinal,
                region.BaseRange.StartIndex,
                region.LeftRange.StartIndex,
                region.RightRange.StartIndex,
                differenceNumber,
                differences.Count));
    }

    public void NotifyOutputEdited(string newText)
    {
        if (_session is null)
        {
            return;
        }

        _outputMerge.NotifyManualOutputEdit(_session, newText);
        RefreshRegionList();
        UpdateOutputStatusText();
        OutputPane?.Refresh();
        RaisePropertyChanged(nameof(HasUnsavedChanges));
        RaiseResolutionCommandsChanged();
    }

    public void ApplyRegionChoice(ThreeWayOutputSourceChoice choice)
    {
        if (_session is null || SelectedRegionOrdinal is not int ordinal)
        {
            return;
        }

        OperationResult result = _outputMerge.ApplyRegionChoice(_session, ordinal, choice);
        if (!result.IsCompleted)
        {
            ReplaceDiagnostics(result.Diagnostics);
            _prompts?.ShowInformation("Cannot apply choice", result.Diagnostics[0].Message);
            return;
        }

        RefreshRegionList();
        UpdateOutputStatusText();
        OutputUpdated?.Invoke(this, EventArgs.Empty);
        OutputPane?.Refresh();
        RaisePropertyChanged(nameof(HasUnsavedChanges));
        RaiseResolutionCommandsChanged();
    }

    private void ApplySession(ThreeWayComparisonSession session)
    {
        _session = session;
        _navigator = new ThreeWayRegionNavigator(session);
        BasePane = new DocumentPaneViewModel(session.Base);
        LeftPane = new DocumentPaneViewModel(session.Left);
        RightPane = new DocumentPaneViewModel(session.Right);
        _outputMerge.Initialize(session);
        OutputPane = new DocumentPaneViewModel(_outputMerge.Output!);
        Title = $"{session.Base.Metadata.DisplayName} | {session.Left.Metadata.DisplayName} | {session.Right.Metadata.DisplayName}";
        RefreshFromSession();
        SessionOpened?.Invoke(this, EventArgs.Empty);
        OutputUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshFromSession()
    {
        if (_session?.LatestResult is null)
        {
            return;
        }

        ThreeWayComparisonResult result = _session.LatestResult;
        int leftOnly = result.Regions.Count(region => region.Kind == ThreeWayRegionKind.LeftOnlyChange);
        int rightOnly = result.Regions.Count(region => region.Kind == ThreeWayRegionKind.RightOnlyChange);
        int same = result.Regions.Count(region => region.Kind == ThreeWayRegionKind.SameChange);
        int divergent = result.Regions.Count(region => region.Kind == ThreeWayRegionKind.Divergent);
        int unresolved = _outputMerge.UnresolvedDifferenceRegionOrdinals(result).Count;
        SummaryText =
            $"{result.DifferenceRegionOrdinals.Count} difference region(s): "
            + $"{leftOnly} left-only, {rightOnly} right-only, {same} same, {divergent} divergent. "
            + $"{unresolved} unresolved in output.";

        RefreshRegionList();
        NavigationText = null;
        UpdateOutputStatusText();
        ComparisonUpdated?.Invoke(this, EventArgs.Empty);
        RaiseSessionCommandsChanged();
    }

    private void RefreshRegionList()
    {
        if (_session?.LatestResult is null)
        {
            return;
        }

        RegionList.Clear();
        IReadOnlyList<ThreeWayRegionResolution> resolutions = _outputMerge.Resolutions;
        foreach (ThreeWayRegion region in _session.LatestResult.Regions.Where(region => region.IsDifference))
        {
            ThreeWayRegionResolution resolution = resolutions[region.Ordinal];
            RegionList.Add(new ThreeWayRegionEntryViewModel(region, resolution));
        }
    }

    private void UpdateOutputStatusText()
    {
        if (_session?.LatestResult is null)
        {
            OutputStatusText = null;
            return;
        }

        int unresolved = _outputMerge.UnresolvedDifferenceRegionOrdinals(_session.LatestResult).Count;
        OutputStatusText = _outputMerge.HasManualOutputEdit
            ? "Output manually edited; difference regions are unresolved until you choose base/left/right again."
            : unresolved == 0
                ? "All difference regions resolved in output."
                : $"{unresolved} unresolved difference region(s) remain in the output.";
    }

    private void MoveToDifference(bool forward)
    {
        ThreeWayNavigationTarget? target = forward ? _navigator?.MoveNext() : _navigator?.MovePrevious();
        if (target is null)
        {
            return;
        }

        SelectedRegionOrdinal = target.RegionOrdinal;
        NavigationText =
            $"Difference {target.DifferenceNumber} of {target.DifferenceCount} "
            + $"({ThreeWayRegionKindLabels.GetDisplayName(_session!.LatestResult!.Regions[target.RegionOrdinal].Kind)})";
        NavigationRequested?.Invoke(this, target);
    }

    private void MoveToNextUnresolved()
    {
        int? ordinal = GetNextUnresolvedOrdinal(forward: true);
        if (ordinal is int regionOrdinal)
        {
            NavigateToRegion(regionOrdinal);
        }
    }

    private int? GetNextUnresolvedOrdinal(bool forward)
    {
        if (_session?.LatestResult is null)
        {
            return null;
        }

        IReadOnlyList<int> unresolved = _outputMerge.UnresolvedDifferenceRegionOrdinals(_session.LatestResult);
        if (unresolved.Count == 0)
        {
            return null;
        }

        if (SelectedRegionOrdinal is not int current)
        {
            return unresolved[forward ? 0 : ^1];
        }

        if (forward)
        {
            foreach (int ordinal in unresolved)
            {
                if (ordinal > current)
                {
                    return ordinal;
                }
            }

            return null;
        }

        for (int i = unresolved.Count - 1; i >= 0; i--)
        {
            if (unresolved[i] < current)
            {
                return unresolved[i];
            }
        }

        return null;
    }

    private void RaiseCompareCommandsChanged()
    {
        RaisePropertyChanged(nameof(CanCompare));
        RaisePropertyChanged(nameof(CanRecompare));
        CompareCommand.RaiseCanExecuteChanged();
        RecompareCommand.RaiseCanExecuteChanged();
    }

    private void RaiseSessionCommandsChanged()
    {
        RaiseCompareCommandsChanged();
        RaiseResolutionCommandsChanged();
        NextDifferenceCommand.RaiseCanExecuteChanged();
        PreviousDifferenceCommand.RaiseCanExecuteChanged();
    }

    private void RaiseResolutionCommandsChanged()
    {
        RaisePropertyChanged(nameof(CanApplyRegionChoice));
        RaisePropertyChanged(nameof(CanSaveOutput));
        RaisePropertyChanged(nameof(CanMoveToUnresolved));
        UseBaseForRegionCommand.RaiseCanExecuteChanged();
        UseLeftForRegionCommand.RaiseCanExecuteChanged();
        UseRightForRegionCommand.RaiseCanExecuteChanged();
        SaveOutputCommand.RaiseCanExecuteChanged();
        NextUnresolvedCommand.RaiseCanExecuteChanged();
    }

    private void ChooseFile(ThreeWayDocumentSide side)
    {
        string? path = _filePicker?.PickFile($"Choose {side.ToString().ToLowerInvariant()} file");
        if (path is null)
        {
            return;
        }

        switch (side)
        {
            case ThreeWayDocumentSide.Base:
                BasePath = path;
                break;
            case ThreeWayDocumentSide.Left:
                LeftPath = path;
                break;
            case ThreeWayDocumentSide.Right:
                RightPath = path;
                break;
        }
    }
}

public sealed class ThreeWayRegionEntryViewModel
{
    public ThreeWayRegionEntryViewModel(ThreeWayRegion region, ThreeWayRegionResolution resolution)
    {
        RegionOrdinal = region.Ordinal;
        KindDisplay = ThreeWayRegionKindLabels.GetDisplayName(region.Kind);
        ResolutionDisplay = FormatResolution(resolution);
        Summary = $"base {FormatRange(region.BaseRange)} | left {FormatRange(region.LeftRange)} | right {FormatRange(region.RightRange)}";
    }

    public int RegionOrdinal { get; }

    public string KindDisplay { get; }

    public string ResolutionDisplay { get; }

    public string Summary { get; }

    private static string FormatRange(LineRange range)
        => range.IsEmpty ? $"@{range.StartIndex}" : $"{range.StartIndex + 1}-{range.EndExclusive}";

    private static string FormatResolution(ThreeWayRegionResolution resolution) => resolution.State switch
    {
        ThreeWayRegionResolutionState.Resolved when resolution.ChosenSource is ThreeWayOutputSourceChoice source =>
            $"Resolved: {source}",
        ThreeWayRegionResolutionState.Stale => "Stale (manual output edit)",
        _ => "Unresolved",
    };
}
