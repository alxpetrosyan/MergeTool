using System.Reflection;

namespace MergeTool.App.Wpf.Tests;

[TestClass]
public sealed class SmokeTests
{
    [TestMethod]
    public void WpfAssembly_Loads()
    {
        Assembly assembly = Assembly.Load("MergeTool.App.Wpf");

        Assert.AreEqual("MergeTool.App.Wpf", assembly.GetName().Name);
    }
}
