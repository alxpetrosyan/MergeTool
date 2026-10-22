using System.Reflection;
using MergeTool.Core;

namespace MergeTool.Core.Tests;

[TestClass]
public sealed class SmokeTests
{
    [TestMethod]
    public void CoreAssembly_LoadsAndHasNoMergeToolDependencies()
    {
        Assembly assembly = typeof(ProductInfo).Assembly;

        Assert.AreEqual("MergeTool.Core", assembly.GetName().Name);
        foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
        {
            string name = reference.Name ?? string.Empty;
            Assert.IsFalse(
                name.StartsWith("MergeTool", StringComparison.Ordinal),
                "MergeTool.Core must not reference other MergeTool assemblies.");
        }
    }
}
