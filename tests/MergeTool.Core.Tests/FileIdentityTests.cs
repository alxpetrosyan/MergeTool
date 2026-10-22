using MergeTool.Core;

namespace MergeTool.Core.Tests;

[TestClass]
public sealed class FileIdentityTests
{
    private static readonly DateTime SampleUtc = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void VersionTokens_WithSameData_AreEqual()
    {
        FileVersionToken first = new(100, SampleUtc, "ABCDEF");
        FileVersionToken second = new(100, SampleUtc, "abcdef");

        Assert.AreEqual(first, second, "Hash comparison must be case-insensitive via normalization.");
    }

    [TestMethod]
    public void VersionTokens_WithDifferentData_AreNotEqual()
    {
        FileVersionToken baseline = new(100, SampleUtc, "aa");

        Assert.AreNotEqual(baseline, new FileVersionToken(101, SampleUtc, "aa"));
        Assert.AreNotEqual(baseline, new FileVersionToken(100, SampleUtc.AddSeconds(1), "aa"));
        Assert.AreNotEqual(baseline, new FileVersionToken(100, SampleUtc, "bb"));
    }

    [TestMethod]
    public void VersionToken_RejectsNonUtcTimestamp()
    {
        DateTime local = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Local);

        Assert.ThrowsExactly<ArgumentException>(() => _ = new FileVersionToken(1, local, "aa"));
    }

    [TestMethod]
    public void VersionToken_RejectsNegativeLengthAndBlankHash()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new FileVersionToken(-1, SampleUtc, "aa"));
        Assert.ThrowsExactly<ArgumentException>(() => _ = new FileVersionToken(1, SampleUtc, " "));
    }

    [TestMethod]
    public void FileIdentity_RequiresPathAndVersion()
    {
        FileVersionToken version = new(1, SampleUtc, "aa");

        Assert.ThrowsExactly<ArgumentException>(() => _ = new FileIdentity(" ", version));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new FileIdentity(@"C:\data\a.txt", null!));
    }

    [TestMethod]
    public void SessionIds_AreOpaqueAndUnique()
    {
        DocumentSessionId docA = DocumentSessionId.New();
        DocumentSessionId docB = DocumentSessionId.New();
        ComparisonSessionId cmp = ComparisonSessionId.New();

        Assert.AreNotEqual(docA, docB);
        Assert.AreEqual(docA, new DocumentSessionId(docA.Value));
        Assert.AreNotEqual(Guid.Empty, cmp.Value);
    }
}
