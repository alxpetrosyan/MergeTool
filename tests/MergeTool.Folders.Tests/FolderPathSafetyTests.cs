using MergeTool.Core;
using MergeTool.Folders.Operations;

namespace MergeTool.Folders.Tests;

[TestClass]
public sealed class FolderPathSafetyTests
{
    [TestMethod]
    public void IsSafeRelativePath_RejectsTraversalAndRootedPaths()
    {
        Assert.IsFalse(FolderPathSafety.IsSafeRelativePath("../secret.txt"));
        Assert.IsFalse(FolderPathSafety.IsSafeRelativePath(@"C:\absolute.txt"));
        Assert.IsFalse(FolderPathSafety.IsSafeRelativePath(@"folder\..\escape.txt"));
        Assert.IsTrue(FolderPathSafety.IsSafeRelativePath(@"folder\file.txt"));
    }

    [TestMethod]
    public void IsPathUnderRoot_DetectsEscape()
    {
        string root = PathNormalization.NormalizeFullPath(@"C:\work\root");
        Assert.IsTrue(FolderPathSafety.IsPathUnderRoot(Path.Combine(root, "a.txt"), root));
        Assert.IsFalse(FolderPathSafety.IsPathUnderRoot(@"C:\work\other\a.txt", root));
    }
}
