using MergeTool.Application.Progress;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class ThrottledProgressTests
{
    [TestMethod]
    public void Report_BatchesUpdatesUntilFlush()
    {
        List<int> values = [];
        IProgress<int> inner = new ImmediateProgress<int>(values);
        ThrottledProgress<int> progress = new(inner, TimeSpan.FromMilliseconds(500));

        progress.Report(1);
        progress.Report(2);
        progress.Report(3);

        Assert.HasCount(1, values);
        Assert.AreEqual(1, values[0]);

        progress.Flush();

        Assert.HasCount(2, values);
        Assert.AreEqual(3, values[1]);
    }

    private sealed class ImmediateProgress<T>(List<T> values) : IProgress<T>
    {
        public void Report(T value) => values.Add(value);
    }
}
