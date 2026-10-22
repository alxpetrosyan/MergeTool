using System.Text;
using MergeTool.Application.Sessions;
using MergeTool.Core;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class TextDocumentSessionTests
{
    [TestMethod]
    public void NewSession_StartsCleanAtRevisionZero()
    {
        TextDocumentSession session = SessionTestHelper.SessionFromText("a\r\nb\r\n");

        Assert.AreEqual(0, session.DocumentRevision);
        Assert.IsFalse(session.IsDirty);
        Assert.AreEqual("a\r\nb\r\n", session.CurrentText);
        Assert.HasCount(2, session.CurrentLines);
    }

    [TestMethod]
    public void ReplaceText_IncrementsRevisionAndMarksDirty()
    {
        TextDocumentSession session = SessionTestHelper.SessionFromText("a\r\n");

        session.ReplaceText("a\r\nb\r\n");

        Assert.AreEqual(1, session.DocumentRevision);
        Assert.IsTrue(session.IsDirty);
        Assert.HasCount(2, session.CurrentLines);
    }

    [TestMethod]
    public void ReplaceText_WithIdenticalContent_DoesNotMoveRevision()
    {
        TextDocumentSession session = SessionTestHelper.SessionFromText("a\r\n");

        session.ReplaceText("a\r\n");

        Assert.AreEqual(0, session.DocumentRevision);
        Assert.IsFalse(session.IsDirty);
    }

    [TestMethod]
    public void ReplaceText_OnReadOnlyDocument_Throws()
    {
        TextDocumentSession session = SessionTestHelper.SessionFromText("a\r\n", isReadOnly: true);

        Assert.ThrowsExactly<InvalidOperationException>(() => session.ReplaceText("b\r\n"));
        Assert.AreEqual(0, session.DocumentRevision);
    }

    [TestMethod]
    public void MarkSaved_ClearsDirtyAndMovesRevisionAndIdentity()
    {
        TextDocumentSession session = SessionTestHelper.SessionFromText("a\r\n");
        session.ReplaceText("edited\r\n");
        DateTime savedUtc = new(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc);
        FileIdentity newIdentity = new(
            session.Metadata.NormalizedFullPath,
            FileVersionToken.FromBytes(Encoding.UTF8.GetBytes("edited\r\n"), savedUtc));

        session.MarkSaved(newIdentity);

        Assert.AreEqual(2, session.DocumentRevision);
        Assert.IsFalse(session.IsDirty);
        Assert.AreSame(newIdentity, session.Identity);
    }

    [TestMethod]
    public void ReloadFrom_ReplacesContentAndMovesRevision()
    {
        TextDocumentSession session = SessionTestHelper.SessionFromText("a\r\n");
        session.ReplaceText("edited\r\n");

        session.ReloadFrom(SessionTestHelper.SnapshotFromText("reloaded\r\n"));

        Assert.AreEqual(2, session.DocumentRevision);
        Assert.IsFalse(session.IsDirty);
        Assert.AreEqual("reloaded\r\n", session.CurrentText);
    }
}
