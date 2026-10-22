using System.Reflection;

namespace MergeTool.Folders.Tests;

[TestClass]
public sealed class SmokeTests
{
    [TestMethod]
    public void FoldersAssembly_LoadsAndReferencesCoreOnly()
    {
        Assembly assembly = Assembly.Load("MergeTool.Folders");

        Assert.AreEqual("MergeTool.Folders", assembly.GetName().Name);
        foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
        {
            string name = reference.Name ?? string.Empty;
            if (name.StartsWith("MergeTool", StringComparison.Ordinal))
            {
                Assert.AreEqual("MergeTool.Core", name, "MergeTool.Folders may reference MergeTool.Core only.");
            }
        }
    }
}
