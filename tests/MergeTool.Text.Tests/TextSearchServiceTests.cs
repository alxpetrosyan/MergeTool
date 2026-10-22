using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.Search;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class TextSearchServiceTests
{
    [TestMethod]
    public void Search_Literal_FindsMatch()
    {
        var lines = TextLineSplitter.Split("alpha\r\nbeta\r\n").Lines;
        IReadOnlyList<TextSearchMatch> matches = TextSearchService.Search(
            lines, lines, "beta", new TextSearchOptions(), null, null, CancellationToken.None);
        Assert.HasCount(2, matches);
    }

    [TestMethod]
    public void ValidatePattern_InvalidRegex_ReturnsDiagnostic()
    {
        IReadOnlyList<Diagnostic> diagnostics = TextSearchService.ValidatePattern(new TextSearchOptions(UseRegex: true), "[");
        Assert.AreEqual(TextSearchService.InvalidRegexCode, diagnostics.Single().Code);
    }

    [TestMethod]
    public void Search_IgnoreCase_FindsMatch()
    {
        var lines = TextLineSplitter.Split("Alpha\r\n").Lines;
        IReadOnlyList<TextSearchMatch> matches = TextSearchService.Search(
            lines,
            TextLineSplitter.Split("\r\n").Lines,
            "alpha",
            new TextSearchOptions(IgnoreCase: true, Pane: ComparisonSide.Left),
            null,
            null,
            CancellationToken.None);
        Assert.HasCount(1, matches);
    }

    [TestMethod]
    public void Search_WholeWord_SkipsPartialMatch()
    {
        var lines = TextLineSplitter.Split("alphabet\r\n").Lines;
        IReadOnlyList<TextSearchMatch> matches = TextSearchService.Search(
            lines,
            lines,
            "alpha",
            new TextSearchOptions(WholeWord: true),
            null,
            null,
            CancellationToken.None);
        Assert.IsEmpty(matches);
    }

    [TestMethod]
    public void Search_ChangedLinesOnly_FiltersToChangedSet()
    {
        var lines = TextLineSplitter.Split("same\r\nchanged\r\n").Lines;
        IReadOnlySet<int> changed = new HashSet<int> { 1 };
        IReadOnlyList<TextSearchMatch> matches = TextSearchService.Search(
            lines,
            lines,
            "same",
            new TextSearchOptions(ChangedLinesOnly: true),
            changed,
            changed,
            CancellationToken.None);
        Assert.IsEmpty(matches);
    }

    [TestMethod]
    public void Search_PaneFilter_SearchesOneSideOnly()
    {
        var left = TextLineSplitter.Split("left-only\r\n").Lines;
        var right = TextLineSplitter.Split("right-only\r\n").Lines;
        IReadOnlyList<TextSearchMatch> matches = TextSearchService.Search(
            left,
            right,
            "only",
            new TextSearchOptions(Pane: ComparisonSide.Right),
            null,
            null,
            CancellationToken.None);
        Assert.HasCount(1, matches);
        Assert.AreEqual(ComparisonSide.Right, matches[0].Side);
    }

    [TestMethod]
    public void Search_Cancellation_Throws()
    {
        var lines = TextLineSplitter.Split("a\r\nb\r\nc\r\n").Lines;
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() =>
            TextSearchService.Search(lines, lines, "a", new TextSearchOptions(), null, null, cts.Token));
    }
}
