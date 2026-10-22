using System.Collections.ObjectModel;
using MergeTool.App.Wpf.Composition;
using MergeTool.App.Wpf.Services;
using MergeTool.Core.Settings;

namespace MergeTool.App.Wpf.ViewModels;

/// <summary>
/// The single shell: one window, one workspace tab strip hosting File Compare and Folder
/// Compare workspaces. No navigation framework; tabs are a plain observable collection.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase, IFileComparisonLauncher, IComparisonSessionRecorder, IRecentPathProvider
{
    private readonly Func<FileCompareWorkspaceViewModel> _fileCompareFactory;
    private readonly Func<FolderCompareWorkspaceViewModel> _folderCompareFactory;
    private readonly Func<ThreeWayFolderCompareWorkspaceViewModel> _threeWayFolderCompareFactory;
    private readonly Func<ThreeWayCompareWorkspaceViewModel> _threeWayCompareFactory;
    private readonly ComparisonSessionHistory _sessionHistory;
    private readonly IUserPromptService? _prompts;
    private WorkspaceViewModel? _activeWorkspace;
    private SessionListItemViewModel? _selectedRecent;
    private SessionListItemViewModel? _selectedPinned;
    private string? _settingsStatusText;

    public MainWindowViewModel(
        Func<FileCompareWorkspaceViewModel> fileCompareFactory,
        Func<FolderCompareWorkspaceViewModel> folderCompareFactory,
        Func<ThreeWayFolderCompareWorkspaceViewModel> threeWayFolderCompareFactory,
        Func<ThreeWayCompareWorkspaceViewModel> threeWayCompareFactory,
        IUserPromptService? prompts = null,
        ComparisonSessionHistory? sessionHistory = null)
    {
        ArgumentNullException.ThrowIfNull(fileCompareFactory);
        ArgumentNullException.ThrowIfNull(folderCompareFactory);
        ArgumentNullException.ThrowIfNull(threeWayFolderCompareFactory);
        ArgumentNullException.ThrowIfNull(threeWayCompareFactory);
        _fileCompareFactory = fileCompareFactory;
        _folderCompareFactory = folderCompareFactory;
        _threeWayFolderCompareFactory = threeWayFolderCompareFactory;
        _threeWayCompareFactory = threeWayCompareFactory;
        _prompts = prompts;
        _sessionHistory = sessionHistory ?? new ComparisonSessionHistory();

        NewFileCompareCommand = new RelayCommand(_ => OpenNewFileCompareWorkspace());
        ShowFolderCompareCommand = new RelayCommand(_ => ShowFolderCompareWorkspace());
        ShowThreeWayFolderCompareCommand = new RelayCommand(_ => ShowThreeWayFolderCompareWorkspace());
        ShowThreeWayCompareCommand = new RelayCommand(_ => ShowThreeWayCompareWorkspace());
        OpenRecentCommand = new RelayCommand(async _ => await OpenSelectedRecentAsync(), _ => CanOpenSelectedRecent);
        OpenPinnedCommand = new RelayCommand(async _ => await OpenSelectedPinnedAsync(), _ => CanOpenSelectedPinned);
        PinActiveSessionCommand = new RelayCommand(_ => PinActiveSession(), _ => CanPinActiveSession);
        ClearRecentsCommand = new RelayCommand(_ => ClearRecents());
        ResetSettingsCommand = new RelayCommand(_ => ResetSettings());
        CloseWorkspaceCommand = new RelayCommand(
            async parameter => await CloseWorkspaceAsync((WorkspaceViewModel)parameter!),
            parameter => parameter is WorkspaceViewModel { IsBusy: false });

        RefreshSessionLists();
        NotifySettingsLoadDiagnostics();

        // One active file comparison tab at launch.
        OpenNewFileCompareWorkspace();
    }

    public ObservableCollection<SessionListItemViewModel> Recents { get; } = [];

    public ObservableCollection<SessionListItemViewModel> Pinned { get; } = [];

    public SessionListItemViewModel? SelectedRecent
    {
        get => _selectedRecent;
        set
        {
            if (SetProperty(ref _selectedRecent, value))
            {
                RaiseSessionCommandsChanged();
            }
        }
    }

    public SessionListItemViewModel? SelectedPinned
    {
        get => _selectedPinned;
        set
        {
            if (SetProperty(ref _selectedPinned, value))
            {
                RaiseSessionCommandsChanged();
            }
        }
    }

    public string? SettingsStatusText
    {
        get => _settingsStatusText;
        private set => SetProperty(ref _settingsStatusText, value);
    }

    public bool CanOpenSelectedRecent => SelectedRecent?.PathsExist == true;

    public bool CanOpenSelectedPinned => SelectedPinned?.PathsExist == true;

    public bool CanPinActiveSession => TryDescribeActiveSession(out _, out _, out _, out _, out _);

    public ObservableCollection<WorkspaceViewModel> Workspaces { get; } = [];

    public WorkspaceViewModel? ActiveWorkspace
    {
        get => _activeWorkspace;
        set
        {
            if (SetProperty(ref _activeWorkspace, value))
            {
                RaiseSessionCommandsChanged();
            }
        }
    }

    public string ProductName => Core.ProductInfo.ProductName;

    public RelayCommand NewFileCompareCommand { get; }

    public RelayCommand ShowFolderCompareCommand { get; }

    public RelayCommand ShowThreeWayFolderCompareCommand { get; }

    public RelayCommand ShowThreeWayCompareCommand { get; }

    public RelayCommand OpenRecentCommand { get; }

    public RelayCommand OpenPinnedCommand { get; }

    public RelayCommand PinActiveSessionCommand { get; }

    public RelayCommand ClearRecentsCommand { get; }

    public RelayCommand ResetSettingsCommand { get; }

    public RelayCommand CloseWorkspaceCommand { get; }

    public FileCompareWorkspaceViewModel OpenNewFileCompareWorkspace()
    {
        FileCompareWorkspaceViewModel workspace = _fileCompareFactory();
        Workspaces.Add(workspace);
        ActiveWorkspace = workspace;
        return workspace;
    }

    /// <summary>Activates the folder-compare workspace, creating it on first use.</summary>
    public FolderCompareWorkspaceViewModel ShowFolderCompareWorkspace()
    {
        FolderCompareWorkspaceViewModel? existing = Workspaces.OfType<FolderCompareWorkspaceViewModel>().FirstOrDefault();
        if (existing is null)
        {
            existing = _folderCompareFactory();
            Workspaces.Add(existing);
        }

        ActiveWorkspace = existing;
        return existing;
    }

    public ThreeWayFolderCompareWorkspaceViewModel ShowThreeWayFolderCompareWorkspace()
    {
        ThreeWayFolderCompareWorkspaceViewModel? existing =
            Workspaces.OfType<ThreeWayFolderCompareWorkspaceViewModel>().FirstOrDefault();
        if (existing is null)
        {
            existing = _threeWayFolderCompareFactory();
            Workspaces.Add(existing);
        }

        ActiveWorkspace = existing;
        return existing;
    }

    public ThreeWayCompareWorkspaceViewModel ShowThreeWayCompareWorkspace()
    {
        ThreeWayCompareWorkspaceViewModel? existing =
            Workspaces.OfType<ThreeWayCompareWorkspaceViewModel>().FirstOrDefault();
        if (existing is null)
        {
            existing = _threeWayCompareFactory();
            Workspaces.Add(existing);
        }

        ActiveWorkspace = existing;
        return existing;
    }

    /// <inheritdoc />
    public async Task OpenPairedComparisonAsync(string leftPath, string rightPath)
    {
        FileCompareWorkspaceViewModel workspace = OpenNewFileCompareWorkspace();
        workspace.LeftPath = leftPath;
        workspace.RightPath = rightPath;
        await workspace.CompareAsync();
    }

    /// <inheritdoc />
    public async Task OpenThreeWayComparisonAsync(string basePath, string leftPath, string rightPath)
    {
        ThreeWayCompareWorkspaceViewModel workspace = ShowThreeWayCompareWorkspace();
        workspace.BasePath = basePath;
        workspace.LeftPath = leftPath;
        workspace.RightPath = rightPath;
        await workspace.CompareAsync();
    }

    public void RecordRecent(RecentComparisonEntry entry)
    {
        _sessionHistory.RecordRecent(entry);
        RefreshSessionLists();
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRecentPaths()
    {
        List<string> paths = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (RecentComparisonEntry entry in _sessionHistory.Recents)
        {
            foreach (string? path in (string?[])[entry.PathA, entry.PathB, entry.PathBase])
            {
                if (!string.IsNullOrWhiteSpace(path) && seen.Add(path))
                {
                    paths.Add(path);
                }
            }
        }

        return paths;
    }

    public async Task OpenSelectedRecentAsync()
    {
        if (SelectedRecent is null)
        {
            return;
        }

        if (!SelectedRecent.PathsExist)
        {
            _prompts?.ShowInformation("Cannot open recent", "One or more saved paths no longer exist.");
            return;
        }

        await SessionLauncher.OpenAsync(this, SelectedRecent);
    }

    public async Task OpenSelectedPinnedAsync()
    {
        if (SelectedPinned is null)
        {
            return;
        }

        if (!SelectedPinned.PathsExist)
        {
            _prompts?.ShowInformation("Cannot open pinned entry", "One or more saved paths no longer exist.");
            return;
        }

        await SessionLauncher.OpenAsync(this, SelectedPinned);
    }

    public void PinActiveSession()
    {
        if (!TryDescribeActiveSession(out string kind, out string pathA, out string? pathB, out string? pathBase, out string? optionsJson))
        {
            return;
        }

        string label = AppSettingsMutations.BuildDisplayLabel(kind, pathA, pathB, pathBase);
        _sessionHistory.AddPinned(new PinnedComparisonEntry(label, kind, pathA, pathB, pathBase, optionsJson));
        RefreshSessionLists();
        SettingsStatusText = "Pinned the active comparison.";
    }

    public void ClearRecents()
    {
        _sessionHistory.ResetRecents();
        RefreshSessionLists();
        SettingsStatusText = "Recent list cleared.";
    }

    public void ResetSettings()
    {
        _sessionHistory.ResetAll();
        RefreshSessionLists();
        SettingsStatusText = "Settings reset to defaults.";
    }

    private void RefreshSessionLists()
    {
        Recents.Clear();
        foreach (RecentComparisonEntry entry in _sessionHistory.Recents)
        {
            Recents.Add(CreateRecentItem(entry));
        }

        Pinned.Clear();
        foreach (PinnedComparisonEntry entry in _sessionHistory.Pinned)
        {
            Pinned.Add(CreatePinnedItem(entry));
        }

        SelectedRecent = Recents.FirstOrDefault();
        SelectedPinned = Pinned.FirstOrDefault();
        RaiseSessionCommandsChanged();
    }

    private void NotifySettingsLoadDiagnostics()
    {
        if (_sessionHistory.UsedFallbackDefaults)
        {
            string message = _sessionHistory.LoadDiagnostics.FirstOrDefault()?.Message
                ?? "Settings could not be read.";
            SettingsStatusText = message;
            _prompts?.ShowInformation("Settings", message);
        }
    }

    private static SessionListItemViewModel CreateRecentItem(RecentComparisonEntry entry)
        => new(
            AppSettingsMutations.BuildDisplayLabel(entry.Kind, entry.PathA, entry.PathB, entry.PathBase),
            entry.Kind,
            entry.PathA,
            entry.PathB,
            entry.PathBase,
            AppSettingsMutations.PathsExist(entry),
            entry.OptionsJson);

    private static SessionListItemViewModel CreatePinnedItem(PinnedComparisonEntry entry)
        => new(
            entry.Label,
            entry.Kind,
            entry.PathA,
            entry.PathB,
            entry.PathBase,
            AppSettingsMutations.PathsExist(entry),
            entry.OptionsJson,
            entry.Label);

    private bool TryDescribeActiveSession(
        out string kind,
        out string pathA,
        out string? pathB,
        out string? pathBase,
        out string? optionsJson)
    {
        kind = string.Empty;
        pathA = string.Empty;
        pathB = null;
        pathBase = null;
        optionsJson = null;

        switch (ActiveWorkspace)
        {
            case FileCompareWorkspaceViewModel file when file.CanCompare:
                kind = ComparisonSessionKinds.FilePair;
                pathA = file.LeftPath;
                pathB = file.RightPath;
                return true;
            case FolderCompareWorkspaceViewModel folder when folder.CanCompare:
                kind = ComparisonSessionKinds.FolderPair;
                pathA = folder.LeftRoot;
                pathB = folder.RightRoot;
                optionsJson = SessionFolderOptionsApplier.BuildOptionsJson(folder.ComparisonMode, folder.TimestampToleranceSeconds);
                return true;
            case ThreeWayCompareWorkspaceViewModel threeWayFile when threeWayFile.CanCompare:
                kind = ComparisonSessionKinds.FileThreeWay;
                pathBase = threeWayFile.BasePath;
                pathA = threeWayFile.LeftPath;
                pathB = threeWayFile.RightPath;
                return true;
            case ThreeWayFolderCompareWorkspaceViewModel threeWayFolder when threeWayFolder.CanCompare:
                kind = ComparisonSessionKinds.FolderThreeWay;
                pathBase = threeWayFolder.BaseRoot;
                pathA = threeWayFolder.LeftRoot;
                pathB = threeWayFolder.RightRoot;
                optionsJson = SessionFolderOptionsApplier.BuildOptionsJson(
                    threeWayFolder.ComparisonMode,
                    threeWayFolder.TimestampToleranceSeconds);
                return true;
            default:
                return false;
        }
    }

    private void RaiseSessionCommandsChanged()
    {
        RaisePropertyChanged(nameof(CanOpenSelectedRecent));
        RaisePropertyChanged(nameof(CanOpenSelectedPinned));
        RaisePropertyChanged(nameof(CanPinActiveSession));
        OpenRecentCommand.RaiseCanExecuteChanged();
        OpenPinnedCommand.RaiseCanExecuteChanged();
        PinActiveSessionCommand.RaiseCanExecuteChanged();
    }

    /// <summary>Closes a workspace tab. Busy or unsaved workspaces refuse to close.</summary>
    public bool CloseWorkspace(WorkspaceViewModel workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        if (!workspace.CanClose)
        {
            return false;
        }

        return RemoveWorkspace(workspace);
    }

    /// <summary>
    /// Closes a workspace tab, asking for a safe decision (Save/Discard/Cancel) when it has
    /// unsaved changes. Busy workspaces always refuse to close.
    /// </summary>
    public async Task<bool> CloseWorkspaceAsync(WorkspaceViewModel workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        if (workspace.IsBusy)
        {
            return false;
        }

        if (workspace.HasUnsavedChanges)
        {
            DirtyCloseDecision decision = _prompts?.AskCloseDirtyWorkspace(workspace.Title)
                ?? DirtyCloseDecision.Cancel;
            switch (decision)
            {
                case DirtyCloseDecision.Cancel:
                    return false;
                case DirtyCloseDecision.Save:
                    if (!await workspace.TrySaveAllForCloseAsync())
                    {
                        return false; // Save failed or conflicted; keep the tab open.
                    }

                    break;
                case DirtyCloseDecision.Discard:
                    break;
            }
        }

        return RemoveWorkspace(workspace);
    }

    /// <summary>
    /// Application-exit gate: true when nothing is dirty or the user explicitly agrees to
    /// discard all unsaved changes. Never saves implicitly on exit.
    /// </summary>
    public bool ConfirmApplicationExit()
    {
        int dirtyCount = Workspaces.Count(w => w.HasUnsavedChanges);
        if (dirtyCount == 0)
        {
            return true;
        }

        return _prompts?.ConfirmDiscardAllOnExit(dirtyCount) ?? false;
    }

    private bool RemoveWorkspace(WorkspaceViewModel workspace)
    {
        int index = Workspaces.IndexOf(workspace);
        if (index < 0)
        {
            return false;
        }

        Workspaces.RemoveAt(index);
        if (ReferenceEquals(ActiveWorkspace, workspace))
        {
            ActiveWorkspace = Workspaces.Count > 0 ? Workspaces[Math.Min(index, Workspaces.Count - 1)] : null;
        }

        return true;
    }

    public static MainWindowViewModel CreateDefault() => AppCompositionRoot.CreateShell();
}
