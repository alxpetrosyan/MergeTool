using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class ComparisonPresentationMapTests
{
    private static TwoWayTextComparisonResult Result(params DiffBlock[] blocks)
        => TwoWayTextComparisonResult.Create(DocumentSessionId.New(), DocumentSessionId.New(), 0, 0, blocks);

    // Layout used by most tests:
    //   block 0 Equal     L[0,2)  R[0,2)
    //   block 1 Modified  L[2,3)  R[2,4)
    //   block 2 Equal     L[3,5)  R[4,6)
    //   block 3 Deleted   L[5,7)  R[6,6) (empty, anchor 6)
    //   block 4 Equal     L[7,8)  R[6,7)
    //   block 5 Inserted  L[8,8) (empty, anchor 8=end) R[7,9)
    private static readonly DiffBlock[] Layout =
    [
        new DiffBlock(0, DiffBlockKind.Equal, new LineRange(0, 2), new LineRange(0, 2)),
        new DiffBlock(1, DiffBlockKind.Modified, new LineRange(2, 1), new LineRange(2, 2)),
        new DiffBlock(2, DiffBlockKind.Equal, new LineRange(3, 2), new LineRange(4, 2)),
        new DiffBlock(3, DiffBlockKind.Deleted, new LineRange(5, 2), LineRange.Empty(6)),
        new DiffBlock(4, DiffBlockKind.Equal, new LineRange(7, 1), new LineRange(6, 1)),
        new DiffBlock(5, DiffBlockKind.Inserted, LineRange.Empty(8), new LineRange(7, 2)),
    ];

    private static ComparisonPresentationMap CreateMap()
        => ComparisonPresentationMap.Create(Result(Layout), leftLineCount: 8, rightLineCount: 9);

    [TestMethod]
    public void LinesMapToTheirBlocks_ForEveryKind()
    {
        ComparisonPresentationMap map = CreateMap();

        Assert.AreEqual(0, map.GetBlockForLeftLine(0)!.Ordinal);   // Equal
        Assert.AreEqual(1, map.GetBlockForLeftLine(2)!.Ordinal);   // Modified
        Assert.AreEqual(1, map.GetBlockForRightLine(3)!.Ordinal);  // Modified (right side)
        Assert.AreEqual(3, map.GetBlockForLeftLine(5)!.Ordinal);   // Deleted (left lines)
        Assert.AreEqual(3, map.GetBlockForLeftLine(6)!.Ordinal);
        Assert.AreEqual(5, map.GetBlockForRightLine(7)!.Ordinal);  // Inserted (right lines)
        Assert.AreEqual(5, map.GetBlockForRightLine(8)!.Ordinal);
    }

    [TestMethod]
    public void BlockAnchors_AreValidLineIndices_ForEveryKind()
    {
        ComparisonPresentationMap map = CreateMap();

        // Equal
        Assert.AreEqual(0, map.GetLeftAnchorForBlock(0));
        Assert.AreEqual(0, map.GetRightAnchorForBlock(0));
        // Modified
        Assert.AreEqual(2, map.GetLeftAnchorForBlock(1));
        Assert.AreEqual(2, map.GetRightAnchorForBlock(1));
        // Deleted: right side empty; anchor is the boundary line 6 (valid).
        Assert.AreEqual(5, map.GetLeftAnchorForBlock(3));
        Assert.AreEqual(6, map.GetRightAnchorForBlock(3));
        // Inserted at left end: boundary index 8 clamps to last valid left line 7.
        Assert.AreEqual(7, map.GetLeftAnchorForBlock(5));
        Assert.AreEqual(7, map.GetRightAnchorForBlock(5));
    }

    [TestMethod]
    public void ScrollMapping_UsesRelativeOffsetWithinBlock()
    {
        ComparisonPresentationMap map = CreateMap();

        Assert.AreEqual(1, map.MapLeftLineToRightAnchor(1));  // Equal block offset 1
        Assert.AreEqual(2, map.MapLeftLineToRightAnchor(2));  // Modified: L2 -> R2
        Assert.AreEqual(2, map.MapRightLineToLeftAnchor(3));  // Modified: R3 offset 1 clamps to left length 1 -> L2
        Assert.AreEqual(4, map.MapRightLineToLeftAnchor(5));  // Equal block 2: R5 offset 1 -> L4
    }

    [TestMethod]
    public void ScrollMapping_AtInsertDeleteBoundaries_ClampsToValidLines()
    {
        ComparisonPresentationMap map = CreateMap();

        // Deleted block: left lines 5-6 have no right counterpart; anchor to boundary 6.
        Assert.AreEqual(6, map.MapLeftLineToRightAnchor(5));
        Assert.AreEqual(6, map.MapLeftLineToRightAnchor(6));
        // Inserted block: right lines 7-8 have no left counterpart; boundary 8 clamps to 7.
        Assert.AreEqual(7, map.MapRightLineToLeftAnchor(7));
        Assert.AreEqual(7, map.MapRightLineToLeftAnchor(8));
    }

    [TestMethod]
    public void OutOfRangeInput_IsClampedNotThrown()
    {
        ComparisonPresentationMap map = CreateMap();

        // Left line 999 clamps to the last left line (7), which belongs to Equal block 4;
        // the Inserted block 5 has no left lines of its own.
        Assert.AreEqual(4, map.GetBlockForLeftLine(999)!.Ordinal);
        Assert.AreEqual(0, map.GetBlockForLeftLine(-5)!.Ordinal);
        Assert.AreEqual(6, map.MapLeftLineToRightAnchor(999));
    }

    [TestMethod]
    public void DifferenceBlockOrdinals_ListsOnlyDifferencesInOrder()
    {
        ComparisonPresentationMap map = CreateMap();

        CollectionAssert.AreEqual(new[] { 1, 3, 5 }, map.DifferenceBlockOrdinals.ToArray());
    }

    [TestMethod]
    public void EmptyDocuments_ProduceSafeDefaults()
    {
        ComparisonPresentationMap map = ComparisonPresentationMap.Create(Result(), 0, 0);

        Assert.IsNull(map.GetBlockForLeftLine(0));
        Assert.AreEqual(0, map.MapLeftLineToRightAnchor(0));
        Assert.AreEqual(0, map.MapRightLineToLeftAnchor(0));
        Assert.IsEmpty(map.DifferenceBlockOrdinals);
    }

    [TestMethod]
    public void EmptyLeftDocument_AnchorsToLineZero()
    {
        DiffBlock inserted = new(0, DiffBlockKind.Inserted, LineRange.Empty(0), new LineRange(0, 3));
        ComparisonPresentationMap map = ComparisonPresentationMap.Create(Result(inserted), 0, 3);

        Assert.AreEqual(0, map.GetLeftAnchorForBlock(0));
        Assert.AreEqual(0, map.MapRightLineToLeftAnchor(2));
        Assert.AreEqual(0, map.GetBlockForRightLine(1)!.Ordinal);
    }
}
