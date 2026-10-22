using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.Diff;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class ComparisonProjectorTests
{
    private static IReadOnlyList<TextLine> Lines(params string[] contents)
        => TextLineSplitter.Split(string.Join("\r\n", contents) + "\r\n").Lines;

    [TestMethod]
    public void IgnoreCase_MapsBackToOriginalLineIndices()
    {
        var left = Lines("Alpha", "beta");
        var right = Lines("alpha", "BETA");
        var options = new TextComparisonOptions(ignoreCase: true);

        ComparisonProjectionBuildResult built = ComparisonProjector.Build(left, right, options);

        Assert.IsTrue(built.IsSuccess);
        IReadOnlyList<DiffBlock> projected = new PatienceLineDiffEngine().Compare(
            built.Projection!.ProjectedLeftLines,
            built.Projection.ProjectedRightLines,
            options,
            CancellationToken.None);
        IReadOnlyList<DiffBlock> original = ComparisonProjectionMapper.MapToOriginal(projected, built.Projection);

        Assert.HasCount(1, original);
        Assert.AreEqual(DiffBlockKind.Equal, original[0].Kind);
        Assert.AreEqual(2, original[0].LeftRange.Length);
        Assert.AreEqual(2, original[0].RightRange.Length);
    }

    [TestMethod]
    public void IgnoreEmptyLines_ExcludesBlankLinesFromProjection()
    {
        var left = Lines("a", "", "b");
        var right = Lines("a", "b");
        var options = new TextComparisonOptions(ignoreEmptyLines: true);

        ComparisonProjectionBuildResult built = ComparisonProjector.Build(left, right, options);

        Assert.IsTrue(built.IsSuccess);
        Assert.HasCount(2, built.Projection!.ProjectedLeftLines);
        Assert.AreEqual(0, built.Projection.LeftProjectedToOriginalLine[0]);
        Assert.AreEqual(2, built.Projection.LeftProjectedToOriginalLine[1]);
    }

    [TestMethod]
    public void TrimAll_TreatsPaddedLinesAsEqual()
    {
        var left = Lines(" value ");
        var right = Lines("value");
        var options = new TextComparisonOptions(whitespaceMode: WhitespaceComparisonMode.TrimAll);

        ComparisonProjectionBuildResult built = ComparisonProjector.Build(left, right, options);
        IReadOnlyList<DiffBlock> projected = new PatienceLineDiffEngine().Compare(
            built.Projection!.ProjectedLeftLines,
            built.Projection.ProjectedRightLines,
            options,
            CancellationToken.None);

        Assert.HasCount(1, projected);
        Assert.AreEqual(DiffBlockKind.Equal, projected[0].Kind);
    }

    [TestMethod]
    public void IgnoredLineRegex_ExcludesMatchingLines()
    {
        var left = Lines("// comment", "code");
        var right = Lines("code");
        var options = new TextComparisonOptions(ignoredLinePattern: @"^\s*//");

        ComparisonProjectionBuildResult built = ComparisonProjector.Build(left, right, options);

        Assert.IsTrue(built.IsSuccess);
        Assert.HasCount(1, built.Projection!.ProjectedLeftLines);
        Assert.AreEqual(1, built.Projection.LeftProjectedToOriginalLine[0]);
    }

    [TestMethod]
    public void IgnoredBlock_ExcludesLinesBetweenMarkers()
    {
        var left = Lines("keep", "#if IGNORE", "hidden", "#endif", "tail");
        var right = Lines("keep", "tail");
        var options = new TextComparisonOptions(
            ignoredBlockStartPattern: @"^#if\b",
            ignoredBlockEndPattern: @"^#endif\b");

        ComparisonProjectionBuildResult built = ComparisonProjector.Build(left, right, options);
        IReadOnlyList<DiffBlock> projected = new PatienceLineDiffEngine().Compare(
            built.Projection!.ProjectedLeftLines,
            built.Projection.ProjectedRightLines,
            options,
            CancellationToken.None);
        IReadOnlyList<DiffBlock> original = ComparisonProjectionMapper.MapToOriginal(projected, built.Projection);

        Assert.HasCount(2, original.Where(b => b.Kind == DiffBlockKind.Equal).ToList());
        Assert.IsTrue(original.Any(b => b.LeftRange.StartIndex == 0));
        Assert.IsTrue(original.Any(b => b.LeftRange.StartIndex == 4));
    }

    [TestMethod]
    public void InvalidRegex_ReturnsDiagnostic()
    {
        var lines = Lines("a");
        var options = new TextComparisonOptions(ignoredLinePattern: "[");

        ComparisonProjectionBuildResult built = ComparisonProjector.Build(lines, lines, options);

        Assert.IsFalse(built.IsSuccess);
        Assert.IsNull(built.Projection);
        Diagnostic diagnostic = built.Diagnostics.Single();
        Assert.AreEqual(ComparisonProjector.InvalidRegexCode, diagnostic.Code);
    }

    [TestMethod]
    public void OverlappingIgnoredBlocks_ReturnsDiagnostic()
    {
        var lines = Lines("#if A", "#if B", "#endif", "#endif");
        var options = new TextComparisonOptions(
            ignoredBlockStartPattern: @"^#if\b",
            ignoredBlockEndPattern: @"^#endif\b");

        ComparisonProjectionBuildResult built = ComparisonProjector.Build(lines, lines, options);

        Assert.IsFalse(built.IsSuccess);
        Assert.AreEqual(ComparisonProjector.OverlappingIgnoredBlocksCode, built.Diagnostics.Single().Code);
    }

    [TestMethod]
    public void UnclosedIgnoredBlock_ReturnsDiagnostic()
    {
        var lines = Lines("a", "#if X", "hidden");
        var options = new TextComparisonOptions(
            ignoredBlockStartPattern: @"^#if\b",
            ignoredBlockEndPattern: @"^#endif\b");

        ComparisonProjectionBuildResult built = ComparisonProjector.Build(lines, lines, options);

        Assert.IsFalse(built.IsSuccess);
        Assert.AreEqual(ComparisonProjector.UnclosedIgnoredBlockCode, built.Diagnostics.Single().Code);
    }
}
