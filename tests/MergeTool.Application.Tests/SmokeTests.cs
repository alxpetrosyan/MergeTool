using System.Reflection;

namespace MergeTool.Application.Tests;

[TestClass]
public sealed class SmokeTests
{
    private static readonly string[] AllowedReferences =
    [
        "MergeTool.Core",
        "MergeTool.Text",
        "MergeTool.Folders",
        "MergeTool.FileSystem",
    ];

    [TestMethod]
    public void ApplicationAssembly_LoadsAndFollowsDependencyDirection()
    {
        Assembly assembly = Assembly.Load("MergeTool.Application");

        Assert.AreEqual("MergeTool.Application", assembly.GetName().Name);
        foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
        {
            string name = reference.Name ?? string.Empty;
            if (name.StartsWith("MergeTool", StringComparison.Ordinal))
            {
                CollectionAssert.Contains(
                    AllowedReferences,
                    name,
                    $"MergeTool.Application must not reference '{name}'.");
            }
        }
    }
}
