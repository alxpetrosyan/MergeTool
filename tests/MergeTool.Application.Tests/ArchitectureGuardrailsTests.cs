using System.Reflection;
using MergeTool.Core;
using MergeTool.FileSystem;
using MergeTool.Folders;
using MergeTool.Text;

namespace MergeTool.Application.Tests;

/// <summary>
/// Characterization tests that enforce solution dependency guardrails from POL-01.
/// </summary>
[TestClass]
public sealed class ArchitectureGuardrailsTests
{
    private static readonly string[] ForbiddenReferencePrefixes =
    [
        "System.Windows",
        "PresentationCore",
        "PresentationFramework",
        "WindowsBase",
        "ICSharpCode.AvalonEdit",
    ];

    public static IEnumerable<object[]> NonWpfAssemblies =>
    [
        [typeof(Diagnostic).Assembly],
        [typeof(TextLine).Assembly],
        [typeof(FolderComparisonRow).Assembly],
        [typeof(PhysicalFileReader).Assembly],
        [typeof(Sessions.TwoWayComparisonSession).Assembly],
    ];

    [TestMethod]
    [DynamicData(nameof(NonWpfAssemblies))]
    public void NonWpfAssembly_DoesNotReferenceWpfOrAvalonEdit(Assembly assembly)
    {
        foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
        {
            string name = reference.Name ?? string.Empty;
            foreach (string forbidden in ForbiddenReferencePrefixes)
            {
                Assert.IsFalse(
                    name.StartsWith(forbidden, StringComparison.Ordinal),
                    $"{assembly.GetName().Name} must not reference {name}.");
            }
        }
    }

    [TestMethod]
    public void Application_DoesNotReferenceAppWpf()
    {
        Assembly application = typeof(Sessions.TwoWayComparisonSession).Assembly;
        bool referencesWpf = application.GetReferencedAssemblies()
            .Any(reference => reference.Name?.Equals("MergeTool.App.Wpf", StringComparison.Ordinal) == true);

        Assert.IsFalse(referencesWpf, "Application must not reference the WPF shell.");
    }

    [TestMethod]
    public void TextFoldersFileSystem_ReferenceCoreOnly()
    {
        Assembly[] domainAssemblies =
        [
            typeof(TextLine).Assembly,
            typeof(FolderComparisonRow).Assembly,
            typeof(PhysicalFileReader).Assembly,
        ];

        foreach (Assembly assembly in domainAssemblies)
        {
            foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
            {
                string name = reference.Name ?? string.Empty;
                Assert.IsTrue(
                    name.Equals("MergeTool.Core", StringComparison.Ordinal)
                    || name.StartsWith("System.", StringComparison.Ordinal)
                    || name.StartsWith("Microsoft.", StringComparison.Ordinal)
                    || name.Equals("netstandard", StringComparison.Ordinal),
                    $"{assembly.GetName().Name} has unexpected reference {name}.");
            }
        }
    }
}
