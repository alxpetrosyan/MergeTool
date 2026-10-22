using MergeTool.Text;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class TextLineSplitterTests
{
    [TestMethod]
    public void CrLfDocument_SplitsWithTerminators()
    {
        TextLineAnalysis analysis = TextLineSplitter.Split("one\r\ntwo\r\n");

        Assert.HasCount(2, analysis.Lines);
        Assert.AreEqual(new TextLine(0, "one", LineTerminator.CrLf), analysis.Lines[0]);
        Assert.AreEqual(new TextLine(1, "two", LineTerminator.CrLf), analysis.Lines[1]);
        Assert.AreEqual(LineEndingStyle.CrLf, analysis.DominantLineEnding);
        Assert.IsFalse(analysis.HasMixedLineEndings);
        Assert.IsTrue(analysis.HasFinalNewline);
    }

    [TestMethod]
    public void LfDocument_IsDetected()
    {
        TextLineAnalysis analysis = TextLineSplitter.Split("one\ntwo\n");

        Assert.AreEqual(LineEndingStyle.Lf, analysis.DominantLineEnding);
        Assert.AreEqual(LineTerminator.Lf, analysis.Lines[0].Terminator);
        Assert.IsFalse(analysis.HasMixedLineEndings);
    }

    [TestMethod]
    public void CrOnlyDocument_IsDetected()
    {
        TextLineAnalysis analysis = TextLineSplitter.Split("one\rtwo\r");

        Assert.AreEqual(LineEndingStyle.Cr, analysis.DominantLineEnding);
        Assert.AreEqual(LineTerminator.Cr, analysis.Lines[0].Terminator);
        Assert.IsTrue(analysis.HasFinalNewline);
    }

    [TestMethod]
    public void MixedEndings_AreFlagged_AndDominantIsMostFrequent()
    {
        TextLineAnalysis analysis = TextLineSplitter.Split("a\nb\nc\r\nd\n");

        Assert.IsTrue(analysis.HasMixedLineEndings);
        Assert.AreEqual(LineEndingStyle.Lf, analysis.DominantLineEnding);
    }

    [TestMethod]
    public void TiedCounts_PreferCrLf()
    {
        TextLineAnalysis analysis = TextLineSplitter.Split("a\r\nb\n");

        Assert.AreEqual(LineEndingStyle.CrLf, analysis.DominantLineEnding);
    }

    [TestMethod]
    public void NoFinalNewline_ProducesUnterminatedLastLine()
    {
        TextLineAnalysis analysis = TextLineSplitter.Split("one\r\ntwo");

        Assert.HasCount(2, analysis.Lines);
        Assert.AreEqual(new TextLine(1, "two", LineTerminator.None), analysis.Lines[1]);
        Assert.IsFalse(analysis.HasFinalNewline);
    }

    [TestMethod]
    public void EmptyText_ProducesNoLines()
    {
        TextLineAnalysis analysis = TextLineSplitter.Split(string.Empty);

        Assert.IsEmpty(analysis.Lines);
        Assert.IsFalse(analysis.HasFinalNewline);
        Assert.AreEqual(LineEndingStyle.CrLf, analysis.DominantLineEnding);
        Assert.IsFalse(analysis.HasMixedLineEndings);
    }

    [TestMethod]
    public void CrFollowedByLfAcrossLines_IsSingleCrLf()
    {
        // "a\r\n" must be one CRLF, not CR + LF.
        TextLineAnalysis analysis = TextLineSplitter.Split("a\r\nb");

        Assert.HasCount(2, analysis.Lines);
        Assert.AreEqual(LineTerminator.CrLf, analysis.Lines[0].Terminator);
        Assert.IsFalse(analysis.HasMixedLineEndings);
    }

    [TestMethod]
    public void EmptyLines_ArePreserved()
    {
        TextLineAnalysis analysis = TextLineSplitter.Split("\r\n\r\nx\r\n");

        Assert.HasCount(3, analysis.Lines);
        Assert.AreEqual(string.Empty, analysis.Lines[0].Content);
        Assert.AreEqual(string.Empty, analysis.Lines[1].Content);
        Assert.AreEqual("x", analysis.Lines[2].Content);
    }

    [TestMethod]
    public void SplitAndCompose_RoundTripsExactly()
    {
        string[] samples =
        [
            "one\r\ntwo\r\n",
            "one\ntwo",
            "a\rb\r\nc\nd",
            "",
            "single line no newline",
            "\n\n\n",
            "trailing\r",
        ];

        foreach (string sample in samples)
        {
            TextLineAnalysis analysis = TextLineSplitter.Split(sample);
            Assert.AreEqual(sample, TextComposer.Compose(analysis.Lines), $"Roundtrip failed for {sample.Length}-char sample.");
        }
    }
}
