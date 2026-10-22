using MergeTool.App.Wpf.ViewModels;
using MergeTool.Application.Sessions;
using MergeTool.Core;

namespace MergeTool.App.Wpf.Tests;

[TestClass]
public sealed class FileCompareSearchTests
{
    private static async Task<FileCompareWorkspaceViewModel> CreateComparedWorkspaceAsync()
    {
        GatedReader reader = new();
        reader.Open();
        FileCompareWorkspaceViewModel workspace = WorkspaceTestFactory.CreateFileCompare(reader);
        workspace.LeftPath = @"C:\fixtures\left.txt";
        workspace.RightPath = @"C:\fixtures\right.txt";
        await workspace.CompareAsync();
        return workspace;
    }

    [TestMethod]
    public async Task Compare_BuildsDifferenceList()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        Assert.IsGreaterThan(0, workspace.DifferenceList.Count);
    }

    [TestMethod]
    public async Task FindNext_RaisesLineNavigation_ForLiteralMatch()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        LineNavigationTarget? target = null;
        workspace.LineNavigationRequested += (_, t) => target = t;

        workspace.SearchPattern = "x";
        workspace.FindMatch(forward: true);

        Assert.IsNotNull(target);
        Assert.AreEqual(ComparisonSide.Left, target.Side);
        Assert.AreEqual(1, target.ZeroBasedLineIndex);
    }

    [TestMethod]
    public async Task FindNext_InvalidRegex_ShowsDiagnostic()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        workspace.UseRegexSearch = true;
        workspace.SearchPattern = "[";

        workspace.FindMatch(forward: true);

        Assert.HasCount(1, workspace.Diagnostics);
    }

    [TestMethod]
    public async Task GoToLine_OutOfRange_ShowsDiagnostic()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        workspace.GoToLineText = "99";

        workspace.GoToLine();

        Assert.HasCount(1, workspace.Diagnostics);
    }

    [TestMethod]
    public async Task GoToLine_ValidLine_RaisesNavigation()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        LineNavigationTarget? target = null;
        workspace.LineNavigationRequested += (_, t) => target = t;
        workspace.GoToLineText = "2";

        workspace.GoToLine();

        Assert.IsNotNull(target);
        Assert.AreEqual(1, target.ZeroBasedLineIndex);
    }

    [TestMethod]
    public async Task DifferenceListSelection_NavigatesToBlock()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        NavigationTarget? target = null;
        workspace.NavigationRequested += (_, t) => target = t;

        DifferenceListEntryViewModel entry = workspace.DifferenceList[0];
        workspace.SelectedDifferenceListEntry = entry;

        Assert.IsNotNull(target);
        Assert.AreEqual(entry.BlockOrdinal, target.BlockOrdinal);
    }

    [TestMethod]
    public async Task NextDifference_SyncsDifferenceListSelection()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        workspace.MoveToDifference(forward: true);

        Assert.IsNotNull(workspace.SelectedDifferenceListEntry);
        Assert.IsTrue(workspace.DifferenceList.Any(d => d.IsSelected));
    }
}
