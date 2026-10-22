using System.Text;
using MergeTool.Application.Sessions;
using MergeTool.Core;
using MergeTool.Text;

namespace MergeTool.Application.Tests;

internal static class SessionTestHelper
{
    private static readonly DateTime SampleUtc = new(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc);

    public static TextDocumentSession SessionFromText(string text, bool isReadOnly = false, string fileName = "doc.txt")
    {
        TextLineAnalysis analysis = TextLineSplitter.Split(text);
        TextDocumentMetadata metadata = new()
        {
            NormalizedFullPath = @"C:\fixtures\" + fileName,
            DisplayName = fileName,
            EncodingWebName = TextEncodings.Utf8,
            HasByteOrderMark = false,
            DominantLineEnding = analysis.DominantLineEnding,
            HasMixedLineEndings = analysis.HasMixedLineEndings,
            HasFinalNewline = analysis.HasFinalNewline,
            Length = Encoding.UTF8.GetByteCount(text),
            LastWriteTimeUtc = SampleUtc,
            IsReadOnly = isReadOnly,
            Classification = TextClassification.Text,
            LoadDiagnostics = [],
        };
        FileIdentity identity = new(
            metadata.NormalizedFullPath,
            FileVersionToken.FromBytes(Encoding.UTF8.GetBytes(text), SampleUtc));
        return TextDocumentSession.FromSnapshot(new TextDocumentSnapshot(metadata, identity, text, analysis.Lines));
    }

    public static TextDocumentSnapshot SnapshotFromText(string text, string fileName = "doc.txt")
        => SessionFromText(text, fileName: fileName).OriginalSnapshot;
}
