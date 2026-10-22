using MergeTool.App.Wpf.ViewModels;
using MergeTool.Application.Sessions;
using MergeTool.Application.UseCases;
using MergeTool.Core;
using MergeTool.Text.Diff;

namespace MergeTool.App.Wpf.Tests;

[TestClass]
public sealed class ThreeWayOutputMergeTests
{
    [TestMethod]
    public async Task ApplyRegionChoice_UpdatesOutputWithoutChangingSources()
    {
        ThreeWayCompareWorkspaceViewModelTests.ThreeWayGatedReader reader = new();
        ThreeWayCompareWorkspaceViewModel workspace = ThreeWayCompareWorkspaceViewModelTests.CreateWorkspace(reader);
        workspace.BasePath = @"C:\fixtures\base.txt";
        workspace.LeftPath = @"C:\fixtures\left.txt";
        workspace.RightPath = @"C:\fixtures\right.txt";

        Task compare = workspace.CompareAsync();
        reader.Open();
        await compare;

        string baseText = workspace.Session!.Base.CurrentText;
        string leftText = workspace.Session.Left.CurrentText;
        string rightText = workspace.Session.Right.CurrentText;
        int differenceOrdinal = workspace.Session.LatestResult!.DifferenceRegionOrdinals[0];
        workspace.SelectedRegionOrdinal = differenceOrdinal;

        workspace.ApplyRegionChoice(ThreeWayOutputSourceChoice.Left);

        Assert.AreEqual("base\r\nleft\r\n", workspace.OutputMerge.Output!.CurrentText);
        Assert.AreEqual(baseText, workspace.Session.Base.CurrentText);
        Assert.AreEqual(leftText, workspace.Session.Left.CurrentText);
        Assert.AreEqual(rightText, workspace.Session.Right.CurrentText);
        Assert.AreEqual("Resolved: Left", workspace.RegionList[0].ResolutionDisplay);
    }

    [TestMethod]
    public async Task ManualOutputEdit_InvalidatesResolution()
    {
        ThreeWayCompareWorkspaceViewModelTests.ThreeWayGatedReader reader = new();
        ThreeWayCompareWorkspaceViewModel workspace = ThreeWayCompareWorkspaceViewModelTests.CreateWorkspace(reader);
        workspace.BasePath = @"C:\fixtures\base.txt";
        workspace.LeftPath = @"C:\fixtures\left.txt";
        workspace.RightPath = @"C:\fixtures\right.txt";

        Task compare = workspace.CompareAsync();
        reader.Open();
        await compare;

        workspace.SelectedRegionOrdinal = workspace.Session!.LatestResult!.DifferenceRegionOrdinals[0];
        workspace.ApplyRegionChoice(ThreeWayOutputSourceChoice.Left);
        workspace.NotifyOutputEdited("edited output\r\n");

        Assert.AreEqual("Stale (manual output edit)", workspace.RegionList[0].ResolutionDisplay);
        Assert.Contains("unresolved", workspace.OutputStatusText!, StringComparison.OrdinalIgnoreCase);
    }
}
