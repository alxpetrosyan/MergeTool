using MergeTool.App.Wpf.ViewModels;
using MergeTool.Application.Presentation;
using MergeTool.Application.Sessions;
using MergeTool.Core;

namespace MergeTool.App.Wpf.Tests;

[TestClass]
public sealed class FileComparePresentationTests
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
    public async Task Compare_BuildsPresentationBundleWithMinimapMarkers()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        FileComparePresentationBundle? bundle = workspace.PresentationBundle;
        Assert.IsNotNull(bundle);
        Assert.IsGreaterThan(0, bundle.Minimap.Markers.Count);
        Assert.AreEqual(workspace.Session!.ComparisonRevision, bundle.ComparisonRevision);
    }

    [TestMethod]
    public async Task StaleComparison_ClearsPresentationBundle()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        Assert.IsNotNull(workspace.PresentationBundle);

        workspace.NotifyPaneEdited(ComparisonSide.Left, "edited\r\ncontent\r\n");

        Assert.IsNull(workspace.PresentationBundle);
        Assert.IsFalse(workspace.CanExpandCollapsed);
    }

    [TestMethod]
    public async Task Recompare_RebuildsPresentationBundle()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        int revisionBefore = workspace.PresentationBundle!.ComparisonRevision;

        workspace.NotifyPaneEdited(ComparisonSide.Left, "a\r\nedited\r\n");
        await workspace.RecompareAsync();

        Assert.IsNotNull(workspace.PresentationBundle);
        Assert.IsGreaterThan(revisionBefore, workspace.PresentationBundle.ComparisonRevision);
    }

    [TestMethod]
    public async Task NextDifference_ExpandsCollapsedRegionBeforeNavigation()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        int generationBefore = workspace.CollapsePresentationGeneration;

        NavigationTarget? target = workspace.MoveToDifference(forward: true);
        Assert.IsNotNull(target);
        Assert.IsNotNull(workspace.NavigationText);
        Assert.IsGreaterThan(generationBefore, workspace.CollapsePresentationGeneration);
    }

    [TestMethod]
    public async Task NavigateToBlock_SelectsBlockAndExpandsCollapsedContext()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        int blockOrdinal = workspace.Session!.PresentationMap!.DifferenceBlockOrdinals[0];
        int generationBefore = workspace.CollapsePresentationGeneration;

        workspace.NavigateToBlock(blockOrdinal);

        Assert.AreEqual(blockOrdinal, workspace.Session.SelectedBlockOrdinal);
        Assert.IsNotNull(workspace.NavigationText);
        Assert.IsGreaterThan(generationBefore, workspace.CollapsePresentationGeneration);
    }

    [TestMethod]
    public async Task ExpandAllCollapsed_RemovesActiveFolds()
    {
        FileCompareWorkspaceViewModel workspace = await CreateComparedWorkspaceAsync();
        int generationBefore = workspace.CollapsePresentationGeneration;

        workspace.ExpandAllCollapsed();

        Assert.IsEmpty(workspace.PresentationBundle!.Collapse.GetActiveFolds(ComparisonSide.Left));
        Assert.IsGreaterThan(generationBefore, workspace.CollapsePresentationGeneration);
    }
}
