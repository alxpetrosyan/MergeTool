using MergeTool.Core;

namespace MergeTool.Core.Tests;

[TestClass]
public sealed class ComparisonRequestIdTests
{
    [TestMethod]
    public void LargerValue_IsDeterministicallyNewer()
    {
        ComparisonRequestId older = new(1);
        ComparisonRequestId newer = new(2);

        Assert.IsTrue(newer.IsNewerThan(older));
        Assert.IsFalse(older.IsNewerThan(newer));
        Assert.IsFalse(older.IsNewerThan(older));
        Assert.IsTrue(newer > older);
        Assert.IsTrue(older < newer);
        Assert.IsTrue(older <= new ComparisonRequestId(1));
        Assert.AreEqual(1, newer.CompareTo(older));
    }

    [TestMethod]
    public void Ids_AreValueEqual()
    {
        Assert.AreEqual(new ComparisonRequestId(7), new ComparisonRequestId(7));
        Assert.AreNotEqual(new ComparisonRequestId(7), new ComparisonRequestId(8));
        Assert.AreEqual(ComparisonRequestId.None, new ComparisonRequestId(0));
    }
}
