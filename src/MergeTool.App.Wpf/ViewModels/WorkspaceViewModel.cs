using System.Collections.ObjectModel;
using MergeTool.Application.Operations;
using MergeTool.Core;
using MergeTool.Core.Diagnostics;

namespace MergeTool.App.Wpf.ViewModels;

/// <summary>UI state of a workspace, including the guardrail-mandated Stale state.</summary>
public enum WorkspaceUiState
{
    Idle = 0,
    Working = 1,
    Completed = 2,
    Cancelled = 3,
    Failed = 4,
    Stale = 5,
}

/// <summary>
/// Base for workspace tabs. Owns the busy/progress/cancel lifecycle for one operation slot:
/// starting a new operation supersedes (cancels) the previous one, and only the newest
/// operation's outcome may update the workspace state.
/// </summary>
public abstract class WorkspaceViewModel : ViewModelBase
{
    private readonly OperationRequestCoordinator _coordinator = new();
    private CancellationTokenSource? _activeCts;

    private string _title = string.Empty;
    private WorkspaceUiState _state = WorkspaceUiState.Idle;
    private string? _busyText;

    protected WorkspaceViewModel()
    {
        CancelCommand = new RelayCommand(_ => CancelActiveOperation(), _ => IsBusy);
    }

    public string Title
    {
        get => _title;
        protected set => SetProperty(ref _title, value);
    }

    public WorkspaceUiState State
    {
        get => _state;
        protected set
        {
            if (SetProperty(ref _state, value))
            {
                RaisePropertyChanged(nameof(IsBusy));
                CancelCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsBusy => State == WorkspaceUiState.Working;

    public string? BusyText
    {
        get => _busyText;
        private set => SetProperty(ref _busyText, value);
    }

    public ObservableCollection<DiagnosticViewModel> Diagnostics { get; } = [];

    public bool HasDiagnostics => Diagnostics.Count > 0;

    public RelayCommand CancelCommand { get; }

    /// <summary>Whether the tab can close right now (never while busy or with unsaved edits).</summary>
    public virtual bool CanClose => !IsBusy && !HasUnsavedChanges;

    public virtual bool HasUnsavedChanges => false;

    /// <summary>
    /// Saves everything this workspace owns as part of a close request. Returns true when
    /// nothing unsaved remains afterwards. The default has nothing to save.
    /// </summary>
    public virtual Task<bool> TrySaveAllForCloseAsync() => Task.FromResult(!HasUnsavedChanges);

    public void CancelActiveOperation() => _activeCts?.Cancel();

    /// <summary>Replaces the diagnostics list from an operation result outside <see cref="RunOperationAsync"/>.</summary>
    protected void ReplaceDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        Diagnostics.Clear();
        foreach (Diagnostic diagnostic in diagnostics)
        {
            Diagnostics.Add(new DiagnosticViewModel(diagnostic));
        }

        NotifyDiagnosticsChanged();
    }

    private void NotifyDiagnosticsChanged() => RaisePropertyChanged(nameof(HasDiagnostics));

    /// <summary>
    /// Runs one long operation through the application layer. A newer call supersedes the
    /// previous one; superseded completions never change workspace state.
    /// </summary>
    protected async Task RunOperationAsync(string busyText, Func<CancellationToken, Task<OperationResult>> operation)
    {
        _activeCts?.Cancel();
        CancellationTokenSource cts = new();
        _activeCts = cts;
        ComparisonRequestId requestId = _coordinator.BeginRequest();

        BusyText = busyText;
        State = WorkspaceUiState.Working;
        Diagnostics.Clear();
        NotifyDiagnosticsChanged();

        try
        {
            OperationResult result = await operation(cts.Token);
            if (!_coordinator.IsCurrent(requestId))
            {
                return; // Superseded: discard entirely.
            }

            IReadOnlyList<Diagnostic> diagnostics = result.Diagnostics;
            if (result.Status == OperationStatus.Cancelled && diagnostics.Count == 0)
            {
                diagnostics = [OperationDiagnostics.OperationCancelled()];
            }

            ReplaceDiagnostics(diagnostics);

            State = result.Status switch
            {
                OperationStatus.Completed => WorkspaceUiState.Completed,
                OperationStatus.Cancelled => WorkspaceUiState.Cancelled,
                _ => WorkspaceUiState.Failed,
            };
        }
        catch (OperationCanceledException)
        {
            if (_coordinator.IsCurrent(requestId))
            {
                ReplaceDiagnostics([OperationDiagnostics.OperationCancelled()]);
                State = WorkspaceUiState.Cancelled;
            }
        }
        catch (Exception exception)
        {
            if (_coordinator.IsCurrent(requestId))
            {
                ReplaceDiagnostics([OperationDiagnostics.UnexpectedFailure(exception)]);
                State = WorkspaceUiState.Failed;
            }
        }
        finally
        {
            if (_coordinator.IsCurrent(requestId))
            {
                BusyText = null;
                _activeCts = null;
            }

            cts.Dispose();
        }
    }
}
