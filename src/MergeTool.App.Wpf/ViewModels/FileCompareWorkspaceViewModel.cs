using MergeTool.App.Wpf.Services;
using MergeTool.Application.Navigation;
using MergeTool.Application.Presentation;
using MergeTool.Application.Sessions;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.Core.Settings;
using MergeTool.Text.Diff;
using MergeTool.Text.Formatted;
using MergeTool.Text.Profiles;
using MergeTool.Text.Search;
using System.Collections.ObjectModel;

namespace MergeTool.App.Wpf.ViewModels;

/// <summary>
/// One file-comparison tab: choose two files, open them through the application layer,
/// render color-coded blocks in two panes, navigate differences, copy blocks between the
/// documents, and save safely. All comparison/merge state lives in the application layer
/// (<see cref="TwoWayComparisonSession"/>, <see cref="ManualTwoWayMergeService"/>,
/// <see cref="DocumentPersistenceService"/>); this view model presents it and forwards intent.
/// </summary>
public sealed class FileCompareWorkspaceViewModel : WorkspaceViewModel
{
    /// <summary>Shown in UI help: MVP renders without vertical blank-line padding.</summary>
    public const string NoPaddingLimitationText =
        "Panes are not padded with blank lines in this version; matching blocks can sit at "
        + "different heights. Synchronized scrolling keeps the matching block in view.";

    private readonly OpenFileComparisonUseCase _openUseCase;
    private readonly DocumentPersistenceService _persistence;
    private readonly ManualTwoWayMergeService _mergeService = new();
    private readonly IFilePickerService? _filePicker;
    private readonly IUserPromptService? _prompts;
    private readonly IComparisonSessionRecorder? _sessionRecorder;
    private readonly IRecentPathProvider? _recentPathProvider;

    private string _leftPath = string.Empty;
    private string _rightPath = string.Empty;
    private string? _summaryText;
    private string? _navigationText;
    private WhitespaceComparisonMode _whitespaceMode = WhitespaceComparisonMode.Exact;
    private bool _ignoreCase;
    private bool _ignoreEmptyLines;
    private string _ignoredLinePattern = string.Empty;
    private string _ignoredBlockStartPattern = string.Empty;
    private string _ignoredBlockEndPattern = string.Empty;
    private StructuredFormatKind _structuredFormat = StructuredFormatKind.None;
    private bool _detectMovedBlocks;
    private TwoWayComparisonSession? _session;
    private DifferenceNavigator? _navigator;
    private DocumentPaneViewModel? _leftPane;
    private DocumentPaneViewModel? _rightPane;
    private FileComparePresentationBundle? _presentationBundle;
    private int _collapsePresentationGeneration;
    private bool _syncingDifferenceListSelection;
    private IReadOnlyList<TextSearchMatch>? _searchMatches;
    private int _searchMatchIndex = -1;
    private bool _isFindPanelExpanded;
    private bool _isDifferenceRailVisible = true;
    private string? _searchMatchesKey;

    public const int DefaultCollapseThreshold = CollapsedViewProjectionBuilder.DefaultCollapseThreshold;

    public const int DefaultCollapseContextLines = CollapsedViewProjectionBuilder.DefaultContextLines;

    public FileCompareWorkspaceViewModel(
        OpenFileComparisonUseCase openUseCase,
        DocumentPersistenceService persistence,
        IFilePickerService? filePicker = null,
        IUserPromptService? prompts = null,
        IComparisonSessionRecorder? sessionRecorder = null,
        IRecentPathProvider? recentPathProvider = null)
    {
        _recentPathProvider = recentPathProvider;
        ArgumentNullException.ThrowIfNull(openUseCase);
        ArgumentNullException.ThrowIfNull(persistence);
        _openUseCase = openUseCase;
        _persistence = persistence;
        _filePicker = filePicker;
        _prompts = prompts;
        _sessionRecorder = sessionRecorder;
        Title = "File Compare";

        CompareCommand = new RelayCommand(async _ => await CompareAsync(), _ => CanCompare);
        RecompareCommand = new RelayCommand(async _ => await RecompareAsync(), _ => CanRecompare);
        ChooseLeftFileCommand = new RelayCommand(_ => ChooseFile(ComparisonSide.Left), _ => _filePicker is not null);
        ChooseRightFileCommand = new RelayCommand(_ => ChooseFile(ComparisonSide.Right), _ => _filePicker is not null);
        NextDifferenceCommand = new RelayCommand(_ => MoveToDifference(forward: true), _ => !IsBusy && (_navigator?.CanMoveNext ?? false));
        PreviousDifferenceCommand = new RelayCommand(_ => MoveToDifference(forward: false), _ => !IsBusy && (_navigator?.CanMovePrevious ?? false));
        CopyToRightCommand = new RelayCommand(_ => CopyCurrentBlock(ComparisonSide.Left), _ => CanCopyTo(ComparisonSide.Right));
        CopyToLeftCommand = new RelayCommand(_ => CopyCurrentBlock(ComparisonSide.Right), _ => CanCopyTo(ComparisonSide.Left));
        SaveLeftCommand = new RelayCommand(async _ => await SaveAsync(ComparisonSide.Left), _ => CanSave(ComparisonSide.Left));
        SaveRightCommand = new RelayCommand(async _ => await SaveAsync(ComparisonSide.Right), _ => CanSave(ComparisonSide.Right));
        SaveAllCommand = new RelayCommand(async _ => await SaveAllAsync(), _ => CanSave(ComparisonSide.Left) || CanSave(ComparisonSide.Right));
        ExpandAllCollapsedCommand = new RelayCommand(_ => ExpandAllCollapsed(), _ => CanExpandCollapsed);
        ExpandSelectedCollapsedCommand = new RelayCommand(_ => ExpandCollapsedForSelection(), _ => CanExpandCollapsed);
        FindNextCommand = new RelayCommand(_ => FindMatch(forward: true), _ => CanSearch);
        FindPreviousCommand = new RelayCommand(_ => FindMatch(forward: false), _ => CanSearch);
        GoToLineCommand = new RelayCommand(_ => GoToLine(), _ => Session is not null && !IsBusy);
        ShowFindPanelCommand = new RelayCommand(_ => IsFindPanelExpanded = true);
        ToggleDifferenceRailCommand = new RelayCommand(_ => IsDifferenceRailVisible = !IsDifferenceRailVisible);
        ShowComparisonSettingsCommand = new RelayCommand(
            _ => ComparisonSettingsRequested?.Invoke(this, EventArgs.Empty));

        // Busy/state transitions from the base class affect most command availabilities.
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(State))
            {
                RaiseSessionCommandsChanged();
            }
        };
    }

    /// <summary>A new document session was opened; the view loads editor text from it.</summary>
    public event EventHandler? SessionOpened;

    /// <summary>A comparison result was applied; the view refreshes overlays and scroll map.</summary>
    public event EventHandler? ComparisonUpdated;

    /// <summary>The comparison went stale (edit/merge/save); the view clears semantic overlays.</summary>
    public event EventHandler? ComparisonInvalidated;

    /// <summary>A document's text was replaced outside the editor (merge or reload).</summary>
    public event EventHandler<ComparisonSide>? DocumentTextReplaced;

    /// <summary>The view should bring this difference into view in both panes.</summary>
    public event EventHandler<NavigationTarget>? NavigationRequested;

    /// <summary>Syntax highlighting or tab size changed; the view reapplies editor settings.</summary>
    public event EventHandler? EditorPresentationChanged;

    /// <summary>Minimap/collapse bundle changed; the view reapplies foldings and minimap.</summary>
    public event EventHandler? PresentationStateChanged;

    /// <summary>Scroll one pane to a source line (search or go-to-line).</summary>
    public event EventHandler<LineNavigationTarget>? LineNavigationRequested;

    /// <summary>The user asked for the comparison settings dialog; the view opens it.</summary>
    public event EventHandler? ComparisonSettingsRequested;

    public ObservableCollection<DifferenceListEntryViewModel> DifferenceList { get; } = [];

    public IReadOnlyList<SearchPaneFilter> SearchPaneFilters { get; } =
        Enum.GetValues<SearchPaneFilter>();

    public IReadOnlyList<ComparisonSide> GoToLineSides { get; } =
        [ComparisonSide.Left, ComparisonSide.Right];

    public string LeftPath
    {
        get => _leftPath;
        set
        {
            if (SetProperty(ref _leftPath, value))
            {
                RaisePropertyChanged(nameof(DetectedProfile));
                RaisePropertyChanged(nameof(ActiveProfile));
                RaisePropertyChanged(nameof(ProfileBadge));
                RaisePropertyChanged(nameof(CanCompare));
                CompareCommand.RaiseCanExecuteChanged();
                if (ProfileOverride is null)
                {
                    EditorPresentationChanged?.Invoke(this, EventArgs.Empty);
                }
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
                RaisePropertyChanged(nameof(CanCompare));
                CompareCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanCompare => !string.IsNullOrWhiteSpace(LeftPath) && !string.IsNullOrWhiteSpace(RightPath);

    /// <summary>Recompare is offered when documents or rules are stale.</summary>
    public bool CanRecompare => Session is not null && (IsComparisonStale || HasPendingRuleChanges) && !IsBusy;

    public bool IsComparisonStale => Session?.IsStale ?? false;

    public bool HasPendingRuleChanges =>
        Session is not null
        && BuildPendingComparisonOptions() != (Session.AppliedComparisonOptions ?? TextComparisonOptions.WithIntralineDiff);

    public IReadOnlyList<WhitespaceComparisonMode> WhitespaceModes { get; } =
        Enum.GetValues<WhitespaceComparisonMode>();

    public WhitespaceComparisonMode WhitespaceMode
    {
        get => _whitespaceMode;
        set
        {
            if (SetProperty(ref _whitespaceMode, value))
            {
                OnComparisonRulesChanged();
            }
        }
    }

    public bool IgnoreCase
    {
        get => _ignoreCase;
        set
        {
            if (SetProperty(ref _ignoreCase, value))
            {
                OnComparisonRulesChanged();
            }
        }
    }

    public bool IgnoreEmptyLines
    {
        get => _ignoreEmptyLines;
        set
        {
            if (SetProperty(ref _ignoreEmptyLines, value))
            {
                OnComparisonRulesChanged();
            }
        }
    }

    public string IgnoredLinePattern
    {
        get => _ignoredLinePattern;
        set
        {
            if (SetProperty(ref _ignoredLinePattern, value))
            {
                OnComparisonRulesChanged();
            }
        }
    }

    public string IgnoredBlockStartPattern
    {
        get => _ignoredBlockStartPattern;
        set
        {
            if (SetProperty(ref _ignoredBlockStartPattern, value))
            {
                OnComparisonRulesChanged();
            }
        }
    }

    public string IgnoredBlockEndPattern
    {
        get => _ignoredBlockEndPattern;
        set
        {
            if (SetProperty(ref _ignoredBlockEndPattern, value))
            {
                OnComparisonRulesChanged();
            }
        }
    }

    public IReadOnlyList<StructuredFormatOption> StructuredFormatOptions { get; } =
    [
        new(StructuredFormatKind.None, "Raw"),
        new(StructuredFormatKind.Json, "Formatted JSON"),
        new(StructuredFormatKind.Xml, "Formatted XML"),
    ];

    public StructuredFormatKind StructuredFormat
    {
        get => _structuredFormat;
        set
        {
            if (SetProperty(ref _structuredFormat, value))
            {
                OnComparisonRulesChanged();
            }
        }
    }

    public bool DetectMovedBlocks
    {
        get => _detectMovedBlocks;
        set
        {
            if (SetProperty(ref _detectMovedBlocks, value))
            {
                OnComparisonRulesChanged();
            }
        }
    }

    /// <summary>Whether the latest comparison used raw or formatted projection.</summary>
    public string? ComparisonProjectionLabel
    {
        get
        {
            if (Session is null)
            {
                return null;
            }

            if (Session.IsFormattedProjectionActive)
            {
                return Session.AppliedComparisonOptions?.StructuredFormat switch
                {
                    StructuredFormatKind.Json => "Formatted JSON",
                    StructuredFormatKind.Xml => "Formatted XML",
                    _ => "Formatted",
                };
            }

            return "Raw";
        }
    }

    /// <summary>Shown when merge/copy are blocked because formatted mode has no source mapping.</summary>
    public string? FormattedMergeBlockedText =>
        Session?.IsFormattedProjectionActive == true
            ? "Merge and copy are disabled in formatted comparison mode. Switch to Raw and Recompare to merge."
            : null;

    /// <summary>Summary of rules reflected in the latest comparison result.</summary>
    public string ActiveRulesBadge =>
        ComparisonRulesSummary.FormatActiveRules(Session?.AppliedComparisonOptions ?? TextComparisonOptions.Default);

    /// <summary>Shown when pending rules differ from the applied comparison.</summary>
    public string? RulesPendingText => HasPendingRuleChanges ? "Rules changed — press Recompare." : null;

    public IReadOnlyList<FileTypeProfile> AvailableProfiles { get; } = FileTypeProfileRegistry.Profiles;

    public FileTypeProfile DetectedProfile => FileTypeProfileMatcher.Match(LeftPath);

    public FileTypeProfileId? ProfileOverride
    {
        get => _profileOverride;
        set
        {
            if (SetProperty(ref _profileOverride, value))
            {
                RaisePropertyChanged(nameof(ActiveProfile));
                RaisePropertyChanged(nameof(ProfileBadge));
                ApplyProfileComparisonDefaultsIfNeeded();
                EditorPresentationChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private FileTypeProfileId? _profileOverride;

    public FileTypeProfile ActiveProfile =>
        ProfileOverride is FileTypeProfileId id
            ? FileTypeProfileRegistry.Get(id)
            : DetectedProfile;

    public string ProfileBadge =>
        ProfileOverride is null
            ? $"{ActiveProfile.DisplayName} (auto)"
            : $"{ActiveProfile.DisplayName} (manual)";

    public RelayCommand CompareCommand { get; }

    public RelayCommand RecompareCommand { get; }

    public RelayCommand ChooseLeftFileCommand { get; }

    public RelayCommand ChooseRightFileCommand { get; }

    public RelayCommand NextDifferenceCommand { get; }

    public RelayCommand PreviousDifferenceCommand { get; }

    /// <summary>Copies the selected block from the left document onto the right.</summary>
    public RelayCommand CopyToRightCommand { get; }

    /// <summary>Copies the selected block from the right document onto the left.</summary>
    public RelayCommand CopyToLeftCommand { get; }

    public RelayCommand SaveLeftCommand { get; }

    public RelayCommand SaveRightCommand { get; }

    public RelayCommand SaveAllCommand { get; }

    public RelayCommand ExpandAllCollapsedCommand { get; }

    public RelayCommand ExpandSelectedCollapsedCommand { get; }

    public RelayCommand FindNextCommand { get; }

    public RelayCommand FindPreviousCommand { get; }

    public RelayCommand GoToLineCommand { get; }

    public RelayCommand ShowFindPanelCommand { get; }

    public RelayCommand ToggleDifferenceRailCommand { get; }

    public RelayCommand ShowComparisonSettingsCommand { get; }

    /// <summary>Recent comparison paths for the path selector dropdowns.</summary>
    public IReadOnlyList<string> RecentPaths => _recentPathProvider?.GetRecentPaths() ?? [];

    public bool IsFindPanelExpanded
    {
        get => _isFindPanelExpanded;
        set => SetProperty(ref _isFindPanelExpanded, value);
    }

    public bool IsDifferenceRailVisible
    {
        get => _isDifferenceRailVisible;
        set => SetProperty(ref _isDifferenceRailVisible, value);
    }

    public string? StateBannerText => BuildStateBannerText();

    public bool ShowStateBanner => !string.IsNullOrWhiteSpace(StateBannerText);

    public int CollapseThreshold { get; set; } = DefaultCollapseThreshold;

    public int CollapseContextLines { get; set; } = DefaultCollapseContextLines;

    public FileComparePresentationBundle? PresentationBundle => _presentationBundle;

    public int CollapsePresentationGeneration => _collapsePresentationGeneration;

    public bool CanExpandCollapsed =>
        !IsBusy && _presentationBundle is not null && !IsComparisonStale;

    public bool CanSearch => Session is not null && !IsBusy && !string.IsNullOrWhiteSpace(SearchPattern);

    private string _searchPattern = string.Empty;

    public string SearchPattern
    {
        get => _searchPattern;
        set
        {
            if (SetProperty(ref _searchPattern, value))
            {
                InvalidateSearchMatches();
                RaisePropertyChanged(nameof(CanSearch));
                FindNextCommand.RaiseCanExecuteChanged();
                FindPreviousCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool UseRegexSearch
    {
        get => _useRegexSearch;
        set
        {
            if (SetProperty(ref _useRegexSearch, value))
            {
                InvalidateSearchMatches();
            }
        }
    }

    private bool _useRegexSearch;

    public bool IgnoreCaseSearch
    {
        get => _ignoreCaseSearch;
        set
        {
            if (SetProperty(ref _ignoreCaseSearch, value))
            {
                InvalidateSearchMatches();
            }
        }
    }

    private bool _ignoreCaseSearch;

    public bool WholeWordSearch
    {
        get => _wholeWordSearch;
        set
        {
            if (SetProperty(ref _wholeWordSearch, value))
            {
                InvalidateSearchMatches();
            }
        }
    }

    private bool _wholeWordSearch;

    public bool ChangedLinesOnlySearch
    {
        get => _changedLinesOnlySearch;
        set
        {
            if (SetProperty(ref _changedLinesOnlySearch, value))
            {
                InvalidateSearchMatches();
            }
        }
    }

    private bool _changedLinesOnlySearch;

    public SearchPaneFilter SearchPane
    {
        get => _searchPane;
        set
        {
            if (SetProperty(ref _searchPane, value))
            {
                InvalidateSearchMatches();
            }
        }
    }

    private SearchPaneFilter _searchPane = SearchPaneFilter.Both;

    public string? SearchStatusText
    {
        get => _searchStatusText;
        private set => SetProperty(ref _searchStatusText, value);
    }

    private string? _searchStatusText;

    public string GoToLineText
    {
        get => _goToLineText;
        set => SetProperty(ref _goToLineText, value);
    }

    private string _goToLineText = string.Empty;

    public ComparisonSide GoToLineSide
    {
        get => _goToLineSide;
        set => SetProperty(ref _goToLineSide, value);
    }

    private ComparisonSide _goToLineSide = ComparisonSide.Left;

    public DifferenceListEntryViewModel? SelectedDifferenceListEntry
    {
        get => _selectedDifferenceListEntry;
        set
        {
            if (!SetProperty(ref _selectedDifferenceListEntry, value) || _syncingDifferenceListSelection)
            {
                return;
            }

            if (value is not null)
            {
                NavigateToDifferenceEntry(value);
            }
        }
    }

    private DifferenceListEntryViewModel? _selectedDifferenceListEntry;

    /// <summary>The active comparison session, once files were opened successfully.</summary>
    public TwoWayComparisonSession? Session
    {
        get => _session;
        private set
        {
            if (SetProperty(ref _session, value))
            {
                _navigator = value is null ? null : new DifferenceNavigator(value);
            }
        }
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

    public string? SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    /// <summary>"Difference n of m" while a difference is selected.</summary>
    public string? NavigationText
    {
        get => _navigationText;
        private set => SetProperty(ref _navigationText, value);
    }

    public string KnownLimitationText => NoPaddingLimitationText;

    public override bool HasUnsavedChanges =>
        Session is not null && (Session.Left.IsDirty || Session.Right.IsDirty);

    public async Task CompareAsync()
    {
        string leftPath = LeftPath;
        string rightPath = RightPath;

        await RunOperationAsync("Comparing files…", async cancellationToken =>
        {
            TextComparisonOptions options = BuildPendingComparisonOptions();
            OperationResult<TwoWayComparisonSession> result =
                await _openUseCase.OpenAsync(leftPath, rightPath, options, cancellationToken);

            if (result.IsCompleted)
            {
                Session = result.Value;
                LeftPane = new DocumentPaneViewModel(Session.Left);
                RightPane = new DocumentPaneViewModel(Session.Right);
                Title = $"{Session.Left.Metadata.DisplayName} ↔ {Session.Right.Metadata.DisplayName}";
                OnComparisonApplied();
                SessionOpened?.Invoke(this, EventArgs.Empty);
                ComparisonUpdated?.Invoke(this, EventArgs.Empty);
                _sessionRecorder?.RecordRecent(new RecentComparisonEntry(
                    ComparisonSessionKinds.FilePair,
                    leftPath,
                    rightPath,
                    null,
                    DateTime.UtcNow));
                RaisePropertyChanged(nameof(RecentPaths));
            }

            return result;
        });
    }

    /// <summary>Recompares the current (possibly edited) documents of the open session.</summary>
    public async Task RecompareAsync()
    {
        TwoWayComparisonSession? session = Session;
        if (session is null)
        {
            return;
        }

        await RunOperationAsync("Comparing files…", async cancellationToken =>
        {
            TextComparisonOptions options = BuildPendingComparisonOptions();
            OperationStatus status = await session.RecompareAsync(options, cancellationToken);
            if (status == OperationStatus.Completed)
            {
                OnComparisonApplied();
                ComparisonUpdated?.Invoke(this, EventArgs.Empty);
            }

            return status switch
            {
                OperationStatus.Completed => OperationResult.Completed(),
                OperationStatus.Cancelled => OperationResult.Cancelled(),
                OperationStatus.Failed => OperationResult.Failed(session.Diagnostics),
                _ => OperationResult.Failed(session.Diagnostics),
            };
        });
    }

    /// <summary>
    /// Called by the view when a pane's editor text changes. Marks the comparison stale
    /// through the session, which disables navigation/merge until the next recompare.
    /// </summary>
    public void NotifyPaneEdited(ComparisonSide side, string newText)
    {
        if (Session is null || IsBusy)
        {
            return;
        }

        bool wasStale = IsComparisonStale;
        Session.NotifyDocumentEdited(side, newText);
        OnSessionMutated(wasStale);
    }

    /// <summary>Moves the difference selection and asks the view to scroll to it.</summary>
    public NavigationTarget? MoveToDifference(bool forward)
    {
        NavigationTarget? target = forward ? _navigator?.MoveNext() : _navigator?.MovePrevious();
        if (target is not null)
        {
            RevealBlockForNavigation(target.BlockOrdinal);
            NavigationText = $"Difference {target.DifferenceNumber} of {target.DifferenceCount}";
            SyncDifferenceListSelection(target.BlockOrdinal);
            NavigationRequested?.Invoke(this, target);
            RaiseSessionCommandsChanged();
        }

        return target;
    }

    /// <summary>Navigates to a difference block from the minimap.</summary>
    public void NavigateToBlock(int blockOrdinal)
    {
        if (Session is null || !Session.CanNavigate || Session.PresentationMap is null)
        {
            return;
        }

        IReadOnlyList<int> differences = Session.PresentationMap.DifferenceBlockOrdinals;
        int index = -1;
        for (int i = 0; i < differences.Count; i++)
        {
            if (differences[i] == blockOrdinal)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return;
        }

        Session.SelectBlock(blockOrdinal);
        RevealBlockForNavigation(blockOrdinal);
        var target = new NavigationTarget(
            blockOrdinal,
            Session.PresentationMap.GetLeftAnchorForBlock(blockOrdinal),
            Session.PresentationMap.GetRightAnchorForBlock(blockOrdinal),
            index + 1,
            differences.Count);
        NavigationText = $"Difference {target.DifferenceNumber} of {target.DifferenceCount}";
        SyncDifferenceListSelection(blockOrdinal);
        NavigationRequested?.Invoke(this, target);
        RaiseSessionCommandsChanged();
    }

    public void NavigateToDifferenceEntry(DifferenceListEntryViewModel entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (Session?.PresentationMap is null)
        {
            return;
        }

        int blockOrdinal = entry.BlockOrdinal;
        if (Session.CanNavigate)
        {
            Session.SelectBlock(blockOrdinal);
        }

        RevealBlockForNavigation(blockOrdinal);
        var target = new NavigationTarget(
            blockOrdinal,
            entry.LeftAnchorLine,
            entry.RightAnchorLine,
            entry.ListIndex,
            DifferenceList.Count);
        NavigationText = $"Difference {target.DifferenceNumber} of {target.DifferenceCount}";
        NavigationRequested?.Invoke(this, target);
        RaiseSessionCommandsChanged();
    }

    public void FindMatch(bool forward)
    {
        TwoWayComparisonSession? session = Session;
        if (session is null || string.IsNullOrWhiteSpace(SearchPattern))
        {
            return;
        }

        var options = new TextSearchOptions(
            UseRegexSearch,
            IgnoreCaseSearch,
            WholeWordSearch,
            ChangedLinesOnlySearch,
            ToSearchPane(SearchPane));

        Diagnostics.Clear();
        ReplaceDiagnostics(TextSearchService.ValidatePattern(options, SearchPattern));

        if (Diagnostics.Any(d => d.Diagnostic.Severity == DiagnosticSeverity.Error))
        {
            SearchStatusText = "Invalid search pattern.";
            return;
        }

        string searchKey = BuildSearchKey();
        if (_searchMatches is null || _searchMatchesKey != searchKey)
        {
            ChangedLinesIndex? changed = session.PresentationMap is not null
                ? ChangedLinesIndex.Build(session.PresentationMap)
                : null;
            _searchMatches = TextSearchService.Search(
                session.Left.CurrentLines,
                session.Right.CurrentLines,
                SearchPattern,
                options,
                changed?.Left,
                changed?.Right,
                CancellationToken.None);
            _searchMatchesKey = searchKey;
            _searchMatchIndex = -1;
        }

        if (_searchMatches.Count == 0)
        {
            SearchStatusText = "No matches.";
            return;
        }

        _searchMatchIndex = forward
            ? (_searchMatchIndex + 1) % _searchMatches.Count
            : (_searchMatchIndex - 1 + _searchMatches.Count) % _searchMatches.Count;

        TextSearchMatch match = _searchMatches[_searchMatchIndex];
        SearchStatusText = $"Match {_searchMatchIndex + 1} of {_searchMatches.Count}";
        LineNavigationRequested?.Invoke(this, new LineNavigationTarget(match.LineIndex, match.Side));
    }

    public void GoToLine()
    {
        TwoWayComparisonSession? session = Session;
        if (session is null || !int.TryParse(GoToLineText.Trim(), out int oneBasedLine))
        {
            return;
        }

        int lineCount = GoToLineSide == ComparisonSide.Left
            ? session.Left.CurrentLines.Count
            : session.Right.CurrentLines.Count;

        OperationResult<GoToLineTarget> result = GoToLineService.TryResolve(oneBasedLine, lineCount, GoToLineSide);
        if (!result.IsCompleted)
        {
            ReplaceDiagnostics(result.Diagnostics);
            return;
        }

        LineNavigationRequested?.Invoke(this, new LineNavigationTarget(result.Value.ZeroBasedLineIndex, result.Value.Side));
    }

    public void ExpandAllCollapsed()
    {
        if (_presentationBundle is null)
        {
            return;
        }

        _presentationBundle.Collapse.ExpandAll();
        BumpCollapsePresentation();
    }

    public void ExpandCollapsedForSelection()
    {
        if (_presentationBundle is null || Session?.SelectedBlockOrdinal is not int ordinal)
        {
            return;
        }

        RevealBlockForNavigation(ordinal);
    }

    public void RevealLineForNavigation(ComparisonSide side, int zeroBasedLine)
    {
        if (_presentationBundle is null)
        {
            return;
        }

        _presentationBundle.Collapse.EnsureLineVisible(side, zeroBasedLine);
        BumpCollapsePresentation();
    }

    public MinimapViewport CalculateMinimapViewport(int firstVisibleLine, int visibleLineCount)
        => _presentationBundle?.Minimap.CalculateViewport(firstVisibleLine, visibleLineCount)
           ?? new MinimapViewport(0, 1);

    private void RevealBlockForNavigation(int blockOrdinal)
    {
        if (_presentationBundle is null || Session?.PresentationMap is null)
        {
            return;
        }

        _presentationBundle.Collapse.EnsureBlockVisible(blockOrdinal, Session.PresentationMap);
        BumpCollapsePresentation();
    }

    private void BumpCollapsePresentation()
    {
        _collapsePresentationGeneration++;
        PresentationStateChanged?.Invoke(this, EventArgs.Empty);
        ExpandAllCollapsedCommand.RaiseCanExecuteChanged();
        ExpandSelectedCollapsedCommand.RaiseCanExecuteChanged();
    }

    /// <summary>Copies the currently selected block from <paramref name="sourceSide"/>.</summary>
    public OperationResult? CopyCurrentBlock(ComparisonSide sourceSide)
    {
        if (Session?.SelectedBlockOrdinal is not int ordinal)
        {
            return null;
        }

        bool wasStale = IsComparisonStale;
        OperationResult result = _mergeService.CopyBlock(Session, ordinal, sourceSide);
        if (result.IsCompleted)
        {
            ComparisonSide targetSide = sourceSide == ComparisonSide.Left ? ComparisonSide.Right : ComparisonSide.Left;
            DocumentTextReplaced?.Invoke(this, targetSide);
            OnSessionMutated(wasStale);
        }
        else
        {
            ReplaceDiagnostics(result.Diagnostics);
            RaiseSessionCommandsChanged();
        }

        return result;
    }

    /// <summary>Saves one document via the safe writer; a conflict offers Cancel/Reload only.</summary>
    public async Task SaveAsync(ComparisonSide side)
    {
        TextDocumentSession? document = GetDocument(side);
        if (document is null)
        {
            return;
        }

        await RunOperationAsync("Saving…", cancellationToken => _persistence.SaveAsync(document, cancellationToken));
        AfterPersistenceStateChanged();

        if (State == WorkspaceUiState.Failed
            && Diagnostics.Any(d => d.Diagnostic.Code == DocumentPersistenceService.SaveConflictCode)
            && _prompts?.AskSaveConflict(document.Metadata.DisplayName) == SaveConflictDecision.Reload)
        {
            await ReloadAsync(side);
        }
    }

    /// <summary>Save left then right; stops after a failure so diagnostics stay visible.</summary>
    public async Task SaveAllAsync()
    {
        if (CanSave(ComparisonSide.Left))
        {
            await SaveAsync(ComparisonSide.Left);
            if (State == WorkspaceUiState.Failed)
            {
                return;
            }
        }

        if (CanSave(ComparisonSide.Right))
        {
            await SaveAsync(ComparisonSide.Right);
        }
    }

    /// <summary>Reloads one document from disk, discarding its in-memory edits.</summary>
    public async Task ReloadAsync(ComparisonSide side)
    {
        TextDocumentSession? document = GetDocument(side);
        if (document is null)
        {
            return;
        }

        await RunOperationAsync("Reloading…", cancellationToken => _persistence.ReloadAsync(document, cancellationToken));
        if (State != WorkspaceUiState.Failed)
        {
            DocumentTextReplaced?.Invoke(this, side);
        }

        AfterPersistenceStateChanged();
    }

    public override async Task<bool> TrySaveAllForCloseAsync()
    {
        await SaveAllAsync();
        return !HasUnsavedChanges;
    }

    private void AfterPersistenceStateChanged()
    {
        // Save and reload bump the document revision, which makes the comparison stale.
        LeftPane?.Refresh();
        RightPane?.Refresh();
        RaisePropertyChanged(nameof(HasUnsavedChanges));
        if (State != WorkspaceUiState.Failed && IsComparisonStale)
        {
            State = WorkspaceUiState.Stale;
            SummaryText = TwoWayComparisonSession.StaleComparisonMessage
                + " Navigation and merge are disabled until then.";
            NavigationText = null;
            ClearPresentationBundle();
            ComparisonInvalidated?.Invoke(this, EventArgs.Empty);
        }

        RaiseSessionCommandsChanged();
    }

    private void OnSessionMutated(bool wasStale)
    {
        LeftPane?.Refresh();
        RightPane?.Refresh();
        RaisePropertyChanged(nameof(HasUnsavedChanges));

        if (IsComparisonStale)
        {
            State = WorkspaceUiState.Stale;
            SummaryText = TwoWayComparisonSession.StaleComparisonMessage
                + " Navigation and merge are disabled until then.";
            NavigationText = null;
            if (!wasStale)
            {
                ClearPresentationBundle();
                ComparisonInvalidated?.Invoke(this, EventArgs.Empty);
            }
        }

        RaiseSessionCommandsChanged();
    }

    private void OnComparisonApplied()
    {
        var summary = Session!.LatestResult!.Summary;
        SummaryText = summary.HasDifferences
            ? $"{summary.DifferenceBlockCount} difference block(s): {summary.ModifiedBlockCount} modified, "
              + $"{summary.InsertedBlockCount} inserted, {summary.DeletedBlockCount} deleted."
            : "No differences found.";
        NavigationText = null; // Selection was cleared by the applied result.
        RebuildPresentationBundle();
        RebuildDifferenceList();
        InvalidateSearchMatches();
        LeftPane?.Refresh();
        RightPane?.Refresh();
        RaisePropertyChanged(nameof(ActiveRulesBadge));
        RaisePropertyChanged(nameof(RulesPendingText));
        RaisePropertyChanged(nameof(HasPendingRuleChanges));
        RaisePropertyChanged(nameof(ComparisonProjectionLabel));
        RaisePropertyChanged(nameof(FormattedMergeBlockedText));
        RaiseSessionCommandsChanged();
    }

    private void RebuildPresentationBundle()
    {
        TwoWayComparisonSession? session = Session;
        if (session?.LatestResult is null || session.PresentationMap is null)
        {
            ClearPresentationBundle();
            return;
        }

        int revision = session.ComparisonRevision;
        int leftCount = session.IsFormattedProjectionActive
            ? session.FormattedDisplayLeftLines!.Count
            : session.Left.CurrentLines.Count;
        int rightCount = session.IsFormattedProjectionActive
            ? session.FormattedDisplayRightLines!.Count
            : session.Right.CurrentLines.Count;
        _presentationBundle = FileComparePresentationBundle.TryBuild(
            revision,
            session.PresentationMap.Blocks,
            leftCount,
            rightCount,
            CollapseThreshold,
            CollapseContextLines);
        BumpCollapsePresentation();
        RaisePropertyChanged(nameof(PresentationBundle));
    }

    private void ClearPresentationBundle()
    {
        _presentationBundle = null;
        _collapsePresentationGeneration++;
        DifferenceList.Clear();
        _selectedDifferenceListEntry = null;
        RaisePropertyChanged(nameof(SelectedDifferenceListEntry));
        InvalidateSearchMatches();
        RaisePropertyChanged(nameof(PresentationBundle));
        PresentationStateChanged?.Invoke(this, EventArgs.Empty);
        ExpandAllCollapsedCommand.RaiseCanExecuteChanged();
        ExpandSelectedCollapsedCommand.RaiseCanExecuteChanged();
    }

    private void RebuildDifferenceList()
    {
        DifferenceList.Clear();
        _selectedDifferenceListEntry = null;
        RaisePropertyChanged(nameof(SelectedDifferenceListEntry));

        if (Session?.PresentationMap is null)
        {
            return;
        }

        foreach (DifferenceListEntry entry in DifferenceListBuilder.Build(
            Session.PresentationMap,
            Session.MovedBlockOrdinals))
        {
            DifferenceList.Add(new DifferenceListEntryViewModel(entry));
        }
    }

    private void SyncDifferenceListSelection(int blockOrdinal)
    {
        DifferenceListEntryViewModel? entry = null;
        foreach (DifferenceListEntryViewModel candidate in DifferenceList)
        {
            if (candidate.BlockOrdinal == blockOrdinal)
            {
                entry = candidate;
                break;
            }
        }

        _syncingDifferenceListSelection = true;
        try
        {
            foreach (DifferenceListEntryViewModel item in DifferenceList)
            {
                item.IsSelected = item == entry;
            }

            _selectedDifferenceListEntry = entry;
            RaisePropertyChanged(nameof(SelectedDifferenceListEntry));
        }
        finally
        {
            _syncingDifferenceListSelection = false;
        }
    }

    private void InvalidateSearchMatches()
    {
        _searchMatches = null;
        _searchMatchesKey = null;
        _searchMatchIndex = -1;
        SearchStatusText = null;
    }

    private string BuildSearchKey()
        => $"{SearchPattern}|{UseRegexSearch}|{IgnoreCaseSearch}|{WholeWordSearch}|{ChangedLinesOnlySearch}|{SearchPane}";

    private static ComparisonSide? ToSearchPane(SearchPaneFilter filter) => filter switch
    {
        SearchPaneFilter.Left => ComparisonSide.Left,
        SearchPaneFilter.Right => ComparisonSide.Right,
        _ => null,
    };

    private void OnComparisonRulesChanged()
    {
        bool hadPending = HasPendingRuleChanges;
        RaisePropertyChanged(nameof(HasPendingRuleChanges));
        RaisePropertyChanged(nameof(RulesPendingText));
        RecompareCommand.RaiseCanExecuteChanged();

        if (Session is not null && HasPendingRuleChanges && !IsComparisonStale)
        {
            ClearPresentationBundle();
            ComparisonInvalidated?.Invoke(this, EventArgs.Empty);
        }
        else if (!hadPending && !HasPendingRuleChanges)
        {
            // no-op
        }
    }

    private void ApplyProfileComparisonDefaultsIfNeeded()
    {
        if (ActiveProfile.DefaultComparisonOptions is not TextComparisonOptions defaults)
        {
            return;
        }

        WhitespaceMode = defaults.WhitespaceMode;
        IgnoreCase = defaults.IgnoreCase;
        IgnoreEmptyLines = defaults.IgnoreEmptyLines;
        IgnoredLinePattern = defaults.IgnoredLinePattern ?? string.Empty;
        IgnoredBlockStartPattern = defaults.IgnoredBlockStartPattern ?? string.Empty;
        IgnoredBlockEndPattern = defaults.IgnoredBlockEndPattern ?? string.Empty;
        OnComparisonRulesChanged();
    }

    private TextComparisonOptions BuildPendingComparisonOptions()
        => new(
            enableIntralineDiff: true,
            whitespaceMode: WhitespaceMode,
            ignoreCase: IgnoreCase,
            ignoreEmptyLines: IgnoreEmptyLines,
            ignoredLinePattern: IgnoredLinePattern,
            ignoredBlockStartPattern: IgnoredBlockStartPattern,
            ignoredBlockEndPattern: IgnoredBlockEndPattern,
            structuredFormat: StructuredFormat,
            detectMovedBlocks: DetectMovedBlocks);

    private void RaiseSessionCommandsChanged()
    {
        RaisePropertyChanged(nameof(IsComparisonStale));
        RaisePropertyChanged(nameof(CanRecompare));
        RaisePropertyChanged(nameof(HasPendingRuleChanges));
        RaisePropertyChanged(nameof(RulesPendingText));
        RaisePropertyChanged(nameof(ActiveRulesBadge));
        RaisePropertyChanged(nameof(ComparisonProjectionLabel));
        RaisePropertyChanged(nameof(FormattedMergeBlockedText));
        RecompareCommand.RaiseCanExecuteChanged();
        NextDifferenceCommand.RaiseCanExecuteChanged();
        PreviousDifferenceCommand.RaiseCanExecuteChanged();
        CopyToRightCommand.RaiseCanExecuteChanged();
        CopyToLeftCommand.RaiseCanExecuteChanged();
        SaveLeftCommand.RaiseCanExecuteChanged();
        SaveRightCommand.RaiseCanExecuteChanged();
        SaveAllCommand.RaiseCanExecuteChanged();
        ExpandAllCollapsedCommand.RaiseCanExecuteChanged();
        ExpandSelectedCollapsedCommand.RaiseCanExecuteChanged();
        FindNextCommand.RaiseCanExecuteChanged();
        FindPreviousCommand.RaiseCanExecuteChanged();
        GoToLineCommand.RaiseCanExecuteChanged();
        RaisePropertyChanged(nameof(CanExpandCollapsed));
        RaisePropertyChanged(nameof(CanSearch));
        UpdateStateBanner();
    }

    private void UpdateStateBanner()
    {
        RaisePropertyChanged(nameof(StateBannerText));
        RaisePropertyChanged(nameof(ShowStateBanner));
    }

    private string? BuildStateBannerText()
    {
        List<string> parts = [];
        if (IsComparisonStale)
        {
            parts.Add("Comparison is stale — press F5 (Recompare) before merging or navigating differences.");
        }

        if (!string.IsNullOrWhiteSpace(FormattedMergeBlockedText))
        {
            parts.Add(FormattedMergeBlockedText);
        }

        if (!string.IsNullOrWhiteSpace(RulesPendingText))
        {
            parts.Add(RulesPendingText);
        }

        if (LeftPane?.IsDirty == true)
        {
            parts.Add("Left file has unsaved changes.");
        }

        if (RightPane?.IsDirty == true)
        {
            parts.Add("Right file has unsaved changes.");
        }

        if (LeftPane?.IsReadOnly == true)
        {
            parts.Add("Left file is read-only.");
        }

        if (RightPane?.IsReadOnly == true)
        {
            parts.Add("Right file is read-only.");
        }

        return parts.Count == 0 ? null : string.Join("  |  ", parts);
    }

    /// <summary>Direction availability: current result, selected difference, writable target.</summary>
    private bool CanCopyTo(ComparisonSide targetSide)
    {
        if (IsBusy || Session is null || !Session.CanMerge || Session.SelectedBlockOrdinal is null)
        {
            return false;
        }

        TextDocumentSession target = targetSide == ComparisonSide.Left ? Session.Left : Session.Right;
        return !target.IsReadOnly;
    }

    private bool CanSave(ComparisonSide side)
    {
        TextDocumentSession? document = GetDocument(side);
        return !IsBusy && document is { IsDirty: true, IsReadOnly: false };
    }

    private TextDocumentSession? GetDocument(ComparisonSide side)
        => side == ComparisonSide.Left ? Session?.Left : Session?.Right;

    private void ChooseFile(ComparisonSide side)
    {
        string? path = _filePicker?.PickFile(side == ComparisonSide.Left ? "Choose left file" : "Choose right file");
        if (path is null)
        {
            return;
        }

        if (side == ComparisonSide.Left)
        {
            LeftPath = path;
        }
        else
        {
            RightPath = path;
        }
    }
}
