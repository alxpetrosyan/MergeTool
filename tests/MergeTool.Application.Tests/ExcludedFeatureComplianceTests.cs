using System.Xml.Linq;

namespace MergeTool.Application.Tests;

/// <summary>Guards against scope drift: excluded commercial features and unapproved packages.</summary>
[TestClass]
public sealed class ExcludedFeatureComplianceTests
{
    [TestMethod]
    public void DirectNuGetPackages_AreOnlyApproved()
    {
        string repoRoot = FindRepoRoot();
        string[] allowed = ["AvalonEdit", "MSTest"];
        IEnumerable<string> packageRefs = Directory.EnumerateFiles(repoRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(path => XDocument.Load(path).Descendants("PackageReference"))
            .Select(element => element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (string package in packageRefs)
        {
            Assert.IsTrue(
                allowed.Contains(package, StringComparer.OrdinalIgnoreCase),
                $"Unexpected direct package reference: {package}");
        }
    }

    [TestMethod]
    public void Source_DoesNotReferenceExcludedProductFeatures()
    {
        string repoRoot = FindRepoRoot();
        string[] forbidden =
        [
            "ApplicationInsights",
            "Sentry",
            "Bugsnag",
            "AutoUpdater",
            "MSIX",
            "WiX",
            "DiffPlex",
        ];

        IEnumerable<string> sourceFiles = Directory.EnumerateFiles(
            Path.Combine(repoRoot, "src"),
            "*.*",
            SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase));

        foreach (string file in sourceFiles)
        {
            string text = File.ReadAllText(file);
            foreach (string token in forbidden)
            {
                Assert.IsFalse(
                    text.Contains(token, StringComparison.OrdinalIgnoreCase),
                    $"Forbidden token '{token}' found in {file}");
            }
        }
    }

    private static string FindRepoRoot()
    {
        string? current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "Directory.Packages.props")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
