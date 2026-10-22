using System.Text;
using MergeTool.Core;

namespace MergeTool.Text;

/// <summary>Result of decoding raw bytes according to the MVP decoding policy.</summary>
public sealed record TextDecodingResult(
    string Text,
    string EncodingWebName,
    bool HasByteOrderMark,
    TextClassification Classification,
    IReadOnlyList<Diagnostic> Diagnostics);

/// <summary>
/// Deterministic MVP decoding policy (00_ARCHITECTURE_GUARDRAILS.txt):
/// 1. Detect BOM for UTF-8 / UTF-16 LE / UTF-16 BE.
/// 2. Without BOM, attempt strict UTF-8.
/// 3. If strict UTF-8 fails, use the documented Windows ANSI fallback (windows-1252) and add
///    a warning diagnostic that decoding was heuristic.
/// 4. NUL-heavy/undecodable content is classified NotTextForThisProduct.
/// </summary>
public static class TextDecoder
{
    public const string HeuristicDecodingCode = "MT-TXT-001";
    public const string NotTextCode = "MT-TXT-002";

    public static TextDecodingResult Decode(ReadOnlySpan<byte> bytes)
    {
        if (TryDetectByteOrderMark(bytes, out string bomEncoding, out int bomLength))
        {
            ReadOnlySpan<byte> payload = bytes[bomLength..];
            Encoding encoding = TextEncodings.GetEncoding(bomEncoding);

            if (bomEncoding == TextEncodings.Utf8)
            {
                Encoding strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
                try
                {
                    string decoded = strictUtf8.GetString(payload);
                    return new TextDecodingResult(decoded, bomEncoding, true, TextClassification.Text, []);
                }
                catch (DecoderFallbackException)
                {
                    return NotText("The file has a UTF-8 byte order mark but contains invalid UTF-8 data.");
                }
            }

            // UTF-16: even byte count is required; NUL characters in text are not.
            if (payload.Length % 2 != 0)
            {
                return NotText("The file has a UTF-16 byte order mark but an odd byte length.");
            }

            string utf16Text = encoding.GetString(payload);
            if (utf16Text.Contains('\0'))
            {
                return NotText("The file contains NUL characters and cannot be compared as text.");
            }

            return new TextDecodingResult(utf16Text, bomEncoding, true, TextClassification.Text, []);
        }

        // No BOM. NUL bytes mean this is not text for this product.
        if (bytes.IndexOf((byte)0) >= 0)
        {
            return NotText("The file contains NUL bytes and cannot be compared as text.");
        }

        Encoding strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        try
        {
            string decoded = strict.GetString(bytes);
            return new TextDecodingResult(decoded, TextEncodings.Utf8, false, TextClassification.Text, []);
        }
        catch (DecoderFallbackException)
        {
            // Documented Windows ANSI fallback: windows-1252, every byte maps deterministically.
            string fallbackText = TextEncodings.GetEncoding(TextEncodings.Windows1252).GetString(bytes);
            Diagnostic warning = Diagnostic.Warning(
                HeuristicDecodingCode,
                "The file is not valid UTF-8. It was decoded using the Windows-1252 fallback; verify the text looks correct.");
            return new TextDecodingResult(fallbackText, TextEncodings.Windows1252, false, TextClassification.Text, [warning]);
        }

        static TextDecodingResult NotText(string message) => new(
            string.Empty,
            TextEncodings.Utf8,
            false,
            TextClassification.NotTextForThisProduct,
            [Diagnostic.Error(NotTextCode, message)]);
    }

    private static bool TryDetectByteOrderMark(ReadOnlySpan<byte> bytes, out string encodingWebName, out int bomLength)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            encodingWebName = TextEncodings.Utf8;
            bomLength = 3;
            return true;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            encodingWebName = TextEncodings.Utf16LittleEndian;
            bomLength = 2;
            return true;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            encodingWebName = TextEncodings.Utf16BigEndian;
            bomLength = 2;
            return true;
        }

        encodingWebName = string.Empty;
        bomLength = 0;
        return false;
    }
}
