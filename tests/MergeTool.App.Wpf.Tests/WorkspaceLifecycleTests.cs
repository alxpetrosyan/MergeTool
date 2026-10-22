using System.Text;
using MergeTool.App.Wpf.ViewModels;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.Core.Settings;
using MergeTool.Text.Diff;

namespace MergeTool.App.Wpf.Tests;

/// <summary>
/// A file reader whose reads block until the test opens the gate, so busy/cancel/supersede
/// behavior can be exercised deterministically without touching the file system.
/// </summary>
internal sealed class GatedReader : IRawFileReader
{
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _startedReads;

    public int StartedReads => Volatile.Read(ref _startedReads);

    public void Open() => _gate.TrySetResult();

    public async Task<RawFileContent> ReadAsync(string path, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _startedReads);
        await _gate.Task.WaitAsync(cancellationToken);
        return new RawFileContent(
            @"C:\fixtures\" + System.IO.Path.GetFileName(path),
            Encoding.UTF8.GetBytes(path.Contains("left", StringComparison.OrdinalIgnoreCase) ? "a\r\nx\r\n" : "a\r\ny\r\n"),
            new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc),
            IsReadOnly: false);
    }
}

internal static class WorkspaceTestFactory
{
    public static FileCompareWorkspaceViewModel CreateFileCompare(
        IRawFileReader reader,
        MergeTool.App.Wpf.Services.IUserPromptService? prompts = null,
        MergeTool.App.Wpf.Services.IComparisonSessionRecorder? sessionRecorder = null)
        => new(
            new OpenFileComparisonUseCase(reader, new PatienceLineDiffEngine()),
            new DocumentPersistenceService(reader),
            filePicker: null,
            prompts,
            sessionRecorder);

    public static MainWindowViewModel CreateShell(
        IRawFileReader reader,
        MergeTool.App.Wpf.Services.IUserPromptService? prompts = null,
        ComparisonSessionHistory? sessionHistory = null)
    {
        RunFolderComparisonUseCase folderCompare = new();
        MainWindowViewModel? shell = null;
        shell = new MainWindowViewModel(
            () => CreateFileCompare(reader, prompts, shell),
            () => new FolderCompareWorkspaceViewModel(folderCompare, prompts: prompts, fileComparisonLauncher: shell, sessionRecorder: shell),
            () => new ThreeWayFolderCompareWorkspaceViewModel(folderCompare, prompts: prompts, fileComparisonLauncher: shell, sessionRecorder: shell),
            () => new ThreeWayCompareWorkspaceViewModel(
                new OpenThreeWayFileComparisonUseCase(reader, new PatienceLineDiffEngine()),
                new ThreeWayOutputPersistenceService(reader),
                sessionRecorder: shell),
            prompts,
            sessionHistory);
        return shell;
    }
}

[TestClass]
public sealed class WorkspaceLifecycleTests
{
    [TestMethod]
    public void Shell_StartsWithOneActiveFileCompareTab()
    {
        MainWindowViewModel shell = WorkspaceTestFactory.CreateShell(new GatedReader());

        Assert.HasCount(1, shell.Workspaces);
        Assert.IsInstanceOfType<FileCompareWorkspaceViewModel>(shell.Workspaces[0]);
        Assert.AreSame(shell.Workspaces[0], shell.ActiveWorkspace);
    }

    [TestMethod]
    public void NewFileCompare_AddsAndActivatesTab()
    {
        MainWindowViewModel shell = WorkspaceTestFactory.CreateShell(new GatedReader());

        FileCompareWorkspaceViewModel added = shell.OpenNewFileCompareWorkspace();

        Assert.HasCount(2, shell.Workspaces);
        Assert.AreSame(added, shell.ActiveWorkspace);
    }

    [TestMethod]
    public void FolderCompare_IsCreatedOnceAndReused()
    {
        MainWindowViewModel shell = WorkspaceTestFactory.CreateShell(new GatedReader());

        FolderCompareWorkspaceViewModel first = shell.ShowFolderCompareWorkspace();
        shell.ActiveWorkspace = shell.Workspaces[0];
        FolderCompareWorkspaceViewModel second = shell.ShowFolderCompareWorkspace();

        Assert.AreSame(first, second);
        Assert.HasCount(2, shell.Workspaces);
        Assert.AreSame(first, shell.ActiveWorkspace);
    }

    [TestMethod]
    public void CloseWorkspace_WithNoChanges_IsSafeAndReactivatesNeighbor()
    {
        MainWindowViewModel shell = WorkspaceTestFactory.CreateShell(new GatedReader());
        WorkspaceViewModel first = shell.Workspaces[0];
        FileCompareWorkspaceViewModel second = shell.OpenNewFileCompareWorkspace();

        Assert.IsTrue(shell.CloseWorkspace(second));
        Assert.HasCount(1, shell.Workspaces);
        Assert.AreSame(first, shell.ActiveWorkspace);

        Assert.IsTrue(shell.CloseWorkspace(first));
        Assert.IsEmpty(shell.Workspaces);
        Assert.IsNull(shell.ActiveWorkspace);
    }

    [TestMethod]
    public async Task CloseWorkspace_WhileBusy_IsRefused()
    {
        GatedReader reader = new();
        MainWindowViewModel shell = WorkspaceTestFactory.CreateShell(reader);
        FileCompareWorkspaceViewModel workspace = (FileCompareWorkspaceViewModel)shell.Workspaces[0];
        workspace.LeftPath = @"C:\fixtures\left.txt";
        workspace.RightPath = @"C:\fixtures\right.txt";

        Task compare = workspace.CompareAsync();

        Assert.IsTrue(workspace.IsBusy);
        Assert.IsFalse(workspace.CanClose);
        Assert.IsFalse(shell.CloseWorkspace(workspace));
        Assert.HasCount(1, shell.Workspaces);

        reader.Open();
        await compare;

        Assert.IsTrue(workspace.CanClose);
        Assert.IsTrue(shell.CloseWorkspace(workspace));
    }
}
