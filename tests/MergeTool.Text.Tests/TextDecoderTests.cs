using System.Text;
using MergeTool.Core;
using MergeTool.Text;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class TextDecoderTests
{
    private static byte[] Utf8Bom => [0xEF, 0xBB, 0xBF];
    private static byte[] Utf16LeBom => [0xFF, 0xFE];
    private static byte[] Utf16BeBom => [0xFE, 0xFF];

    [TestMethod]
    public void Utf8WithBom_IsDetected()
    {
        byte[] bytes = [.. Utf8Bom, .. Encoding.UTF8.GetBytes("héllo")];

        TextDecodingResult result = TextDecoder.Decode(bytes);

        Assert.AreEqual("héllo", result.Text);
        Assert.AreEqual(TextEncodings.Utf8, result.EncodingWebName);
        Assert.IsTrue(result.HasByteOrderMark);
        Assert.AreEqual(TextClassification.Text, result.Classification);
        Assert.IsEmpty(result.Diagnostics);
    }

    [TestMethod]
    public void Utf8WithoutBom_UsesStrictUtf8()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("plain ascii and ünïcode");

        TextDecodingResult result = TextDecoder.Decode(bytes);

        Assert.AreEqual("plain ascii and ünïcode", result.Text);
        Assert.AreEqual(TextEncodings.Utf8, result.EncodingWebName);
        Assert.IsFalse(result.HasByteOrderMark);
        Assert.IsEmpty(result.Diagnostics);
    }

    [TestMethod]
    public void Utf16LittleEndianWithBom_IsDetected()
    {
        byte[] bytes = [.. Utf16LeBom, .. Encoding.Unicode.GetBytes("abc")];

        TextDecodingResult result = TextDecoder.Decode(bytes);

        Assert.AreEqual("abc", result.Text);
        Assert.AreEqual(TextEncodings.Utf16LittleEndian, result.EncodingWebName);
        Assert.IsTrue(result.HasByteOrderMark);
    }

    [TestMethod]
    public void Utf16BigEndianWithBom_IsDetected()
    {
        byte[] bytes = [.. Utf16BeBom, .. Encoding.BigEndianUnicode.GetBytes("abc")];

        TextDecodingResult result = TextDecoder.Decode(bytes);

        Assert.AreEqual("abc", result.Text);
        Assert.AreEqual(TextEncodings.Utf16BigEndian, result.EncodingWebName);
        Assert.IsTrue(result.HasByteOrderMark);
    }

    [TestMethod]
    public void InvalidUtf8_FallsBackToWindows1252_WithHeuristicWarning()
    {
        // 0x93/0x94 are curly quotes in windows-1252 and invalid as UTF-8 lead bytes.
        byte[] bytes = [(byte)'a', 0x93, (byte)'b', 0x94];

        TextDecodingResult result = TextDecoder.Decode(bytes);

        Assert.AreEqual("a\u201Cb\u201D", result.Text);
        Assert.AreEqual(TextEncodings.Windows1252, result.EncodingWebName);
        Assert.AreEqual(TextClassification.Text, result.Classification);
        Assert.HasCount(1, result.Diagnostics);
        Assert.AreEqual(TextDecoder.HeuristicDecodingCode, result.Diagnostics[0].Code);
        Assert.AreEqual(DiagnosticSeverity.Warning, result.Diagnostics[0].Severity);
    }

    [TestMethod]
    public void NulHeavyData_IsNotTextForThisProduct()
    {
        byte[] bytes = [0x4D, 0x5A, 0x00, 0x00, 0x01, 0x00, 0xFF];

        TextDecodingResult result = TextDecoder.Decode(bytes);

        Assert.AreEqual(TextClassification.NotTextForThisProduct, result.Classification);
        Assert.HasCount(1, result.Diagnostics);
        Assert.AreEqual(TextDecoder.NotTextCode, result.Diagnostics[0].Code);
    }

    [TestMethod]
    public void Utf16WithEmbeddedNulCharacters_IsNotTextForThisProduct()
    {
        byte[] payload = Encoding.Unicode.GetBytes("a\0b");
        byte[] bytes = [.. Utf16LeBom, .. payload];

        TextDecodingResult result = TextDecoder.Decode(bytes);

        Assert.AreEqual(TextClassification.NotTextForThisProduct, result.Classification);
    }

    [TestMethod]
    public void Utf16BomWithOddLength_IsNotTextForThisProduct()
    {
        byte[] bytes = [.. Utf16LeBom, 0x61, 0x00, 0x62];

        TextDecodingResult result = TextDecoder.Decode(bytes);

        Assert.AreEqual(TextClassification.NotTextForThisProduct, result.Classification);
    }

    [TestMethod]
    public void Utf8BomWithInvalidPayload_IsNotTextForThisProduct()
    {
        byte[] bytes = [.. Utf8Bom, 0xC3, 0x28];

        TextDecodingResult result = TextDecoder.Decode(bytes);

        Assert.AreEqual(TextClassification.NotTextForThisProduct, result.Classification);
    }

    [TestMethod]
    public void EmptyFile_IsEmptyUtf8Text()
    {
        TextDecodingResult result = TextDecoder.Decode([]);

        Assert.AreEqual(string.Empty, result.Text);
        Assert.AreEqual(TextClassification.Text, result.Classification);
        Assert.AreEqual(TextEncodings.Utf8, result.EncodingWebName);
        Assert.IsFalse(result.HasByteOrderMark);
    }

    [TestMethod]
    public void Windows1252RoundTrip_PreservesEveryDecodedByte()
    {
        byte[] allBytes = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            allBytes[i] = (byte)i;
        }

        Encoding codec = TextEncodings.GetEncoding(TextEncodings.Windows1252);
        string decoded = codec.GetString(allBytes);
        byte[] reencoded = codec.GetBytes(decoded);

        CollectionAssert.AreEqual(allBytes, reencoded);
    }

    [TestMethod]
    public void Windows1252Encoding_ThrowsForUnmappableCharacter()
    {
        Encoding codec = TextEncodings.GetEncoding(TextEncodings.Windows1252);

        Assert.ThrowsExactly<EncoderFallbackException>(() => _ = codec.GetBytes("\u4E2D"));
    }
}
