using System.Reflection;

namespace MergeTool.Application.Tests;

// The architecture guardrails define no dedicated MergeTool.FileSystem.Tests project,
// so the FileSystem smoke test lives here where the assembly is already deployed.
[TestClass]
public sealed class FileSystemSmokeTests
{
    [TestMethod]
    public void FileSystemAssembly_LoadsAndReferencesCoreOnly()
    {
        Assembly assembly = Assembly.Load("MergeTool.FileSystem");

        Assert.AreEqual("MergeTool.FileSystem", assembly.GetName().Name);
        foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
        {
            string name = reference.Name ?? string.Empty;
            if (name.StartsWith("MergeTool", StringComparison.Ordinal))
            {
                Assert.AreEqual("MergeTool.Core", name, "MergeTool.FileSystem may reference MergeTool.Core only.");
            }
        }
    }
}
