using System.Reflection;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class SmokeTests
{
    [TestMethod]
    public void TextAssembly_LoadsAndReferencesCoreOnly()
    {
        Assembly assembly = Assembly.Load("MergeTool.Text");

        Assert.AreEqual("MergeTool.Text", assembly.GetName().Name);
        AssertReferencesOnlyCore(assembly);
    }

    private static void AssertReferencesOnlyCore(Assembly assembly)
    {
        foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
        {
            string name = reference.Name ?? string.Empty;
            if (name.StartsWith("MergeTool", StringComparison.Ordinal))
            {
                Assert.AreEqual("MergeTool.Core", name, "MergeTool.Text may reference MergeTool.Core only.");
            }
        }
    }
}
