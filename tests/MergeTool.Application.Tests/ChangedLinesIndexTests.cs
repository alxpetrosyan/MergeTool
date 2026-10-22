using MergeTool.Application.Presentation;
using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class ChangedLinesIndexTests
{
    [TestMethod]
    public void Build_CollectsChangedLinesPerSide()
    {
        var blocks = new List<DiffBlock>
        {
            new(0, DiffBlockKind.Equal, new LineRange(0, 2), new LineRange(0, 2)),
            new(1, DiffBlockKind.Modified, new LineRange(2, 2), new LineRange(2, 1)),
            new(2, DiffBlockKind.Inserted, LineRange.Empty(4), new LineRange(4, 1)),
        };

        var map = ComparisonPresentationMap.Create(
            TwoWayTextComparisonResult.Create(DocumentSessionId.New(), DocumentSessionId.New(), 0, 0, blocks),
            5,
            5);

        ChangedLinesIndex index = ChangedLinesIndex.Build(map);
        Assert.Contains(2, index.Left);
        Assert.Contains(3, index.Left);
        Assert.DoesNotContain(0, index.Left);
        Assert.Contains(2, index.Right);
        Assert.Contains(4, index.Right);
    }
}
