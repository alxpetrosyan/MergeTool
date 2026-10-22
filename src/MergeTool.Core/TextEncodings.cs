using System.Text;

namespace MergeTool.Core;

/// <summary>
/// Maps the product's supported encoding names to concrete <see cref="Encoding"/> instances.
/// Supported names: "utf-8", "utf-16" (LE), "utf-16BE", "windows-1252".
/// Windows-1252 is implemented internally (see <see cref="Windows1252Encoding"/>) so the
/// product needs no code-pages package; encoding failures throw instead of silently
/// substituting characters.
/// </summary>
public static class TextEncodings
{
    public const string Utf8 = "utf-8";
    public const string Utf16LittleEndian = "utf-16";
    public const string Utf16BigEndian = "utf-16BE";
    public const string Windows1252 = "windows-1252";

    /// <summary>
    /// Returns an encoding for writing. The returned encoding never emits a BOM itself;
    /// callers prepend the preamble explicitly when the policy requires one.
    /// </summary>
    public static Encoding GetEncoding(string webName) => webName switch
    {
        Utf8 => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        Utf16LittleEndian => new UnicodeEncoding(bigEndian: false, byteOrderMark: false),
        Utf16BigEndian => new UnicodeEncoding(bigEndian: true, byteOrderMark: false),
        Windows1252 => Windows1252Encoding.Instance,
        _ => throw new ArgumentOutOfRangeException(nameof(webName), webName, "Unsupported encoding name."),
    };

    /// <summary>The BOM bytes for the given encoding name (empty when it has no BOM concept).</summary>
    public static byte[] GetByteOrderMark(string webName) => webName switch
    {
        Utf8 => [0xEF, 0xBB, 0xBF],
        Utf16LittleEndian => [0xFF, 0xFE],
        Utf16BigEndian => [0xFE, 0xFF],
        Windows1252 => [],
        _ => throw new ArgumentOutOfRangeException(nameof(webName), webName, "Unsupported encoding name."),
    };
}

/// <summary>
/// Minimal Windows-1252 codec. Decoding maps every byte deterministically (the five
/// undefined bytes map to their C1 control code points, matching Windows behavior).
/// Encoding throws <see cref="EncoderFallbackException"/> for unmappable characters so a
/// save can never silently lose data.
/// </summary>
public sealed class Windows1252Encoding : Encoding
{
    public static Windows1252Encoding Instance { get; } = new();

    // Code points for bytes 0x80-0x9F; all other bytes map 1:1 to the same code point.
    private static readonly char[] HighRange =
    [
        '\u20AC', '\u0081', '\u201A', '\u0192', '\u201E', '\u2026', '\u2020', '\u2021',
        '\u02C6', '\u2030', '\u0160', '\u2039', '\u0152', '\u008D', '\u017D', '\u008F',
        '\u0090', '\u2018', '\u2019', '\u201C', '\u201D', '\u2022', '\u2013', '\u2014',
        '\u02DC', '\u2122', '\u0161', '\u203A', '\u0153', '\u009D', '\u017E', '\u0178',
    ];

    private static readonly Dictionary<char, byte> ReverseHighRange = BuildReverse();

    private static Dictionary<char, byte> BuildReverse()
    {
        Dictionary<char, byte> map = new(HighRange.Length);
        for (int i = 0; i < HighRange.Length; i++)
        {
            map[HighRange[i]] = (byte)(0x80 + i);
        }

        return map;
    }

    public override string WebName => TextEncodings.Windows1252;

    private static byte EncodeChar(char c)
    {
        if (c < '\u0080' || (c >= '\u00A0' && c <= '\u00FF'))
        {
            return (byte)c;
        }

        if (ReverseHighRange.TryGetValue(c, out byte b))
        {
            return b;
        }

        throw new EncoderFallbackException(
            $"Character U+{(int)c:X4} cannot be encoded as {TextEncodings.Windows1252}.");
    }

    private static char DecodeByte(byte b)
        => b is >= 0x80 and <= 0x9F ? HighRange[b - 0x80] : (char)b;

    public override int GetByteCount(char[] chars, int index, int count) => count;

    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        for (int i = 0; i < charCount; i++)
        {
            bytes[byteIndex + i] = EncodeChar(chars[charIndex + i]);
        }

        return charCount;
    }

    public override int GetCharCount(byte[] bytes, int index, int count) => count;

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        for (int i = 0; i < byteCount; i++)
        {
            chars[charIndex + i] = DecodeByte(bytes[byteIndex + i]);
        }

        return byteCount;
    }

    public override int GetMaxByteCount(int charCount) => charCount;

    public override int GetMaxCharCount(int byteCount) => byteCount;
}
