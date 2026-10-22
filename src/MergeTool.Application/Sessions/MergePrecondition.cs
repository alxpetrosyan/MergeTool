using System.Security.Cryptography;
using System.Text;
using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.Diff;

namespace MergeTool.Application.Sessions;

/// <summary>
/// Everything needed to prove a manual block copy is still safe at execution time:
/// the comparison revision it was planned from, both document revisions, the exact source
/// and target ranges, and content fingerprints of those ranges. Any mismatch at validation
/// time rejects the operation.
/// </summary>
public sealed record MergePrecondition(
    int ComparisonRevision,
    ComparisonSide SourceSide,
    int SourceDocumentRevision,
    int TargetDocumentRevision,
    LineRange SourceRange,
    LineRange TargetRange,
    string SourceTextFingerprint,
    string TargetTextFingerprint)
{
    /// <summary>SHA-256 fingerprint of the exact text a line range currently contains.</summary>
    public static string FingerprintRange(IReadOnlyList<TextLine> lines, LineRange range)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (range.StartIndex < 0 || range.EndExclusive > lines.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(range), range, "Range exceeds the document.");
        }

        StringBuilder builder = new();
        for (int i = range.StartIndex; i < range.EndExclusive; i++)
        {
            builder.Append(lines[i].Content);
            builder.Append(TextComposer.GetTerminatorText(lines[i].Terminator));
            builder.Append('\u0001'); // line separator sentinel; cannot occur ambiguously
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexStringLower(hash);
    }
}
