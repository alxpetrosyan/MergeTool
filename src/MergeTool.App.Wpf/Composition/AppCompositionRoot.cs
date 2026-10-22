using MergeTool.App.Wpf.Services;
using MergeTool.App.Wpf.ViewModels;
using MergeTool.Application.UseCases;
using MergeTool.FileSystem;
using MergeTool.Text.Diff;

namespace MergeTool.App.Wpf.Composition;

/// <summary>
/// Single composition root for the desktop shell. Keeps infrastructure wiring out of
/// view models except for constructor injection of already-built use cases.
/// </summary>
public static class AppCompositionRoot
{
    public static MainWindowViewModel CreateShell()
    {
        PhysicalFileReader reader = new();
        OpenFileComparisonUseCase openUseCase = new(reader, new PatienceLineDiffEngine());
        DocumentPersistenceService persistence = new(reader);
        RunFolderComparisonUseCase folderCompareUseCase = new();
        FindFolderRenameCandidatesUseCase renameCandidatesUseCase = new();
        WpfFilePickerService filePicker = new();
        WpfFolderPickerService folderPicker = new();
        WpfUserPromptService prompts = new();
        OpenThreeWayFileComparisonUseCase threeWayOpenUseCase = new(reader, new PatienceLineDiffEngine());
        ThreeWayOutputPersistenceService threeWayOutputPersistence = new(reader);
        WpfFolderOperationPromptService operationPrompts = new();

        MainWindowViewModel? shell = null;
        shell = new MainWindowViewModel(
            () => new FileCompareWorkspaceViewModel(
                openUseCase, persistence, filePicker, prompts, shell, recentPathProvider: shell),
            () => new FolderCompareWorkspaceViewModel(
                folderCompareUseCase,
                folderPicker,
                prompts,
                shell,
                operationPrompts,
                sessionRecorder: shell,
                renameCandidatesUseCase: renameCandidatesUseCase,
                recentPathProvider: shell),
            () => new ThreeWayFolderCompareWorkspaceViewModel(
                folderCompareUseCase,
                folderPicker,
                prompts,
                shell,
                sessionRecorder: shell),
            () => new ThreeWayCompareWorkspaceViewModel(
                threeWayOpenUseCase,
                threeWayOutputPersistence,
                filePicker,
                prompts,
                shell),
            prompts);

        return shell;
    }
}
