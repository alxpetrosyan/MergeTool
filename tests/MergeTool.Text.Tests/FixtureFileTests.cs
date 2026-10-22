using MergeTool.Core;
using MergeTool.Text;

namespace MergeTool.Text.Tests;

/// <summary>
/// Loads the committed fixture files in tests/TestData/Text through a byte-array reader,
/// proving the decoder policy against real on-disk fixtures.
/// </summary>
[TestClass]
public sealed class FixtureFileTests
{
    private static string FixtureDirectory
    {
        get
        {
            // tests/MergeTool.Text.Tests/bin/<config>/net10.0 -> tests/TestData/Text
            string testsRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            return Path.Combine(testsRoot, "TestData", "Text");
        }
    }

    private static TextDecodingResult DecodeFixture(string fileName)
        => TextDecoder.Decode(File.ReadAllBytes(Path.Combine(FixtureDirectory, fileName)));

    [TestMethod]
    public void Utf8BomFixture_Decodes()
    {
        TextDecodingResult result = DecodeFixture("utf8_bom_crlf.txt");
        Assert.AreEqual(TextEncodings.Utf8, result.EncodingWebName);
        Assert.IsTrue(result.HasByteOrderMark);
        Assert.AreEqual("alpha\r\nbeta\r\n", result.Text);
    }

    [TestMethod]
    public void Utf8NoBomFixture_Decodes()
    {
        TextDecodingResult result = DecodeFixture("utf8_nobom_lf.txt");
        Assert.AreEqual(TextEncodings.Utf8, result.EncodingWebName);
        Assert.IsFalse(result.HasByteOrderMark);
        Assert.AreEqual("alpha\nbeta\n", result.Text);
    }

    [TestMethod]
    public void Utf16Fixtures_Decode()
    {
        TextDecodingResult le = DecodeFixture("utf16le_bom_crlf.txt");
        Assert.AreEqual(TextEncodings.Utf16LittleEndian, le.EncodingWebName);
        Assert.AreEqual("alpha\r\nbeta\r\n", le.Text);

        TextDecodingResult be = DecodeFixture("utf16be_bom_crlf.txt");
        Assert.AreEqual(TextEncodings.Utf16BigEndian, be.EncodingWebName);
        Assert.AreEqual("alpha\r\nbeta\r\n", be.Text);
    }

    [TestMethod]
    public void AnsiFixture_UsesDocumentedFallbackWithWarning()
    {
        TextDecodingResult result = DecodeFixture("ansi_windows1252.txt");
        Assert.AreEqual(TextEncodings.Windows1252, result.EncodingWebName);
        Assert.AreEqual("q\u201Cw\u201D\r\ne", result.Text);
        Assert.HasCount(1, result.Diagnostics);
    }

    [TestMethod]
    public void LineEndingFixtures_AnalyzeCorrectly()
    {
        TextLineAnalysis crOnly = TextLineSplitter.Split(DecodeFixture("cr_only.txt").Text);
        Assert.AreEqual(LineEndingStyle.Cr, crOnly.DominantLineEnding);

        TextLineAnalysis mixed = TextLineSplitter.Split(DecodeFixture("mixed_endings_no_final_newline.txt").Text);
        Assert.IsTrue(mixed.HasMixedLineEndings);
        Assert.IsFalse(mixed.HasFinalNewline);
    }

    [TestMethod]
    public void EmptyAndNulFixtures_ClassifyCorrectly()
    {
        TextDecodingResult empty = DecodeFixture("empty.txt");
        Assert.AreEqual(TextClassification.Text, empty.Classification);
        Assert.AreEqual(string.Empty, empty.Text);

        TextDecodingResult nulHeavy = DecodeFixture("nul_heavy_not_text.bin");
        Assert.AreEqual(TextClassification.NotTextForThisProduct, nulHeavy.Classification);
    }
}
