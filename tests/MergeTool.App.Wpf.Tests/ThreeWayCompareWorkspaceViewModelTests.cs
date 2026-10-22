using MergeTool.App.Wpf.ViewModels;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.Diff;
using System.IO;

namespace MergeTool.App.Wpf.Tests;

[TestClass]
public sealed class ThreeWayCompareWorkspaceViewModelTests
{
    [TestMethod]
    public void CompareCommand_RequiresAllThreePaths()
    {
        ThreeWayCompareWorkspaceViewModel workspace = CreateWorkspace();
        Assert.IsFalse(workspace.CanCompare);

        workspace.BasePath = @"C:\base.txt";
        workspace.LeftPath = @"C:\left.txt";
        Assert.IsFalse(workspace.CanCompare);

        workspace.RightPath = @"C:\right.txt";
        Assert.IsTrue(workspace.CanCompare);
    }

    [TestMethod]
    public async Task Compare_LoadsReadOnlySession_AndDoesNotMutateDocuments()
    {
        ThreeWayGatedReader reader = new();
        ThreeWayCompareWorkspaceViewModel workspace = CreateWorkspace(reader);
        workspace.BasePath = @"C:\fixtures\base.txt";
        workspace.LeftPath = @"C:\fixtures\left.txt";
        workspace.RightPath = @"C:\fixtures\right.txt";

        Task compare = workspace.CompareAsync();
        reader.Open();
        await compare;

        Assert.AreEqual(WorkspaceUiState.Completed, workspace.State);
        Assert.IsNotNull(workspace.Session);
        Assert.AreEqual("base\r\nshared\r\n", workspace.Session.Base.CurrentText);
        Assert.AreEqual("base\r\nleft\r\n", workspace.Session.Left.CurrentText);
        Assert.AreEqual("base\r\nshared\r\n", workspace.Session.Right.CurrentText);
        Assert.IsFalse(workspace.Session.Base.IsDirty);
        Assert.IsFalse(workspace.Session.Left.IsDirty);
        Assert.IsFalse(workspace.Session.Right.IsDirty);
        Assert.IsNotNull(workspace.SummaryText);
        Assert.IsTrue(workspace.RegionList.Any(entry => entry.KindDisplay == "Left only"));
    }

    internal static ThreeWayCompareWorkspaceViewModel CreateWorkspace(ThreeWayGatedReader? reader = null)
        => new(
            new OpenThreeWayFileComparisonUseCase(
                reader ?? new ThreeWayGatedReader(),
                new PatienceLineDiffEngine()),
            new ThreeWayOutputPersistenceService(reader ?? new ThreeWayGatedReader()));

    internal sealed class ThreeWayGatedReader : IRawFileReader
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Open() => _gate.TrySetResult();

        public async Task<RawFileContent> ReadAsync(string path, CancellationToken cancellationToken)
        {
            await _gate.Task.WaitAsync(cancellationToken);
            string fileName = Path.GetFileName(path).ToLowerInvariant();
            string text = fileName switch
            {
                "base.txt" => "base\r\nshared\r\n",
                "left.txt" => "base\r\nleft\r\n",
                _ => "base\r\nshared\r\n",
            };

            return new RawFileContent(
                path,
                System.Text.Encoding.UTF8.GetBytes(text),
                new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc),
                IsReadOnly: false);
        }
    }
}
