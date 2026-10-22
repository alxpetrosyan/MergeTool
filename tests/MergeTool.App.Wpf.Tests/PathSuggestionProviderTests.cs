using MergeTool.App.Wpf.Controls;
using System.IO;

namespace MergeTool.App.Wpf.Tests;

/// <summary>UI-REDESIGN-01: path selector suggestion and validation logic.</summary>
[TestClass]
public sealed class PathSuggestionProviderTests
{
    [TestMethod]
    public void EmptyText_ReturnsRecentPaths_MostRecentFirst_Distinct()
    {
        IReadOnlyList<string> suggestions = PathSuggestionProvider.GetSuggestions(
            string.Empty,
            [@"C:\a.txt", @"C:\b.txt", @"C:\A.TXT"]);

        Assert.HasCount(2, suggestions, "Duplicates differing only by case are collapsed.");
        Assert.AreEqual(@"C:\a.txt", suggestions[0]);
        Assert.AreEqual(@"C:\b.txt", suggestions[1]);
    }

    [TestMethod]
    public void TypedPrefix_FiltersRecentPaths()
    {
        IReadOnlyList<string> suggestions = PathSuggestionProvider.GetSuggestions(
            @"C:\proj",
            [@"C:\projects\left.txt", @"D:\other\file.txt"]);

        Assert.HasCount(1, suggestions);
        Assert.AreEqual(@"C:\projects\left.txt", suggestions[0]);
    }

    [TestMethod]
    public void TextEndingWithSeparator_ListsChildFoldersAndFiles()
    {
        using TempDir temp = TempDir.Create();
        Directory.CreateDirectory(Path.Combine(temp.Root, "childdir"));
        File.WriteAllText(Path.Combine(temp.Root, "childfile.txt"), "x");

        IReadOnlyList<string> suggestions = PathSuggestionProvider.GetSuggestions(temp.Root + "\\", []);

        Assert.HasCount(2, suggestions);
        Assert.AreEqual(Path.Combine(temp.Root, "childdir"), suggestions[0], "Directories come first.");
        Assert.AreEqual(Path.Combine(temp.Root, "childfile.txt"), suggestions[1]);
    }

    [TestMethod]
    public void PartialLastSegment_FiltersChildrenByPrefix()
    {
        using TempDir temp = TempDir.Create();
        Directory.CreateDirectory(Path.Combine(temp.Root, "alpha"));
        Directory.CreateDirectory(Path.Combine(temp.Root, "beta"));

        IReadOnlyList<string> suggestions = PathSuggestionProvider.GetSuggestions(
            Path.Combine(temp.Root, "al"), []);

        Assert.HasCount(1, suggestions);
        Assert.AreEqual(Path.Combine(temp.Root, "alpha"), suggestions[0]);
    }

    [TestMethod]
    public void InvalidCharactersOrMissingDrive_DoNotThrow()
    {
        IReadOnlyList<string> suggestions = PathSuggestionProvider.GetSuggestions("Z:\\nope\\\"bad|chars<>\\", []);
        Assert.IsEmpty(suggestions);
    }

    [TestMethod]
    public void IsInvalid_EmptyIsNeutral_MissingIsInvalid_ExistingIsValid()
    {
        using TempDir temp = TempDir.Create();
        string file = Path.Combine(temp.Root, "exists.txt");
        File.WriteAllText(file, "x");

        Assert.IsFalse(PathSuggestionProvider.IsInvalid(string.Empty, PathSelectorKind.File));
        Assert.IsFalse(PathSuggestionProvider.IsInvalid("   ", PathSelectorKind.Folder));

        Assert.IsFalse(PathSuggestionProvider.IsInvalid(file, PathSelectorKind.File));
        Assert.IsFalse(PathSuggestionProvider.IsInvalid(temp.Root, PathSelectorKind.Folder));

        Assert.IsTrue(PathSuggestionProvider.IsInvalid(Path.Combine(temp.Root, "missing.txt"), PathSelectorKind.File));
        Assert.IsTrue(PathSuggestionProvider.IsInvalid(file, PathSelectorKind.Folder), "A file is not a valid folder.");
        Assert.IsTrue(PathSuggestionProvider.IsInvalid(temp.Root, PathSelectorKind.File), "A folder is not a valid file.");
    }

    [TestMethod]
    public void EndsWithSeparator_DetectsBothSeparatorStyles()
    {
        Assert.IsTrue(PathSuggestionProvider.EndsWithSeparator(@"C:\dir\"));
        Assert.IsTrue(PathSuggestionProvider.EndsWithSeparator("C:/dir/"));
        Assert.IsFalse(PathSuggestionProvider.EndsWithSeparator(@"C:\dir"));
        Assert.IsFalse(PathSuggestionProvider.EndsWithSeparator(string.Empty));
        Assert.IsFalse(PathSuggestionProvider.EndsWithSeparator(null));
    }

    private sealed class TempDir : IDisposable
    {
        public string Root { get; }

        private TempDir(string root) => Root = root;

        public static TempDir Create()
        {
            string root = Path.Combine(Path.GetTempPath(), "MergeTool.PathTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TempDir(root);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
