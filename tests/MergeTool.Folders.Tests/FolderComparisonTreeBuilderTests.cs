using MergeTool.Folders;

namespace MergeTool.Folders.Tests;

[TestClass]
public sealed class FolderComparisonTreeBuilderTests
{
    private static readonly DateTime Time = new(2026, 7, 9, 8, 0, 0, DateTimeKind.Utc);

    private static FolderComparisonRow FileRow(string relativePath, FolderRowState state,
        bool leftExists = true, bool rightExists = true)
        => new(
            relativePath,
            state,
            leftExists ? @"C:\left\" + relativePath : null,
            rightExists ? @"C:\right\" + relativePath : null,
            leftExists ? 10 : null,
            rightExists ? 10 : null,
            leftExists ? Time : null,
            rightExists ? Time : null,
            isDirectory: false);

    private static FolderComparisonRow DirRow(string relativePath, FolderRowState state = FolderRowState.MetadataMatch)
        => new(relativePath, state, @"C:\left\" + relativePath, @"C:\right\" + relativePath,
            null, null, null, null, isDirectory: true);

    [TestMethod]
    public void Build_NestedPaths_SynthesizesIntermediateDirectories()
    {
        IReadOnlyList<FolderComparisonTreeNode> roots = FolderComparisonTreeBuilder.Build(
        [
            FileRow(@"src\app\main.cs", FolderRowState.MetadataDifferent),
            FileRow(@"src\readme.txt", FolderRowState.MetadataMatch),
            FileRow(@"root.txt", FolderRowState.MetadataMatch),
        ]);

        Assert.HasCount(2, roots);
        FolderComparisonTreeNode src = roots[0];
        Assert.AreEqual("src", src.Name);
        Assert.IsTrue(src.IsDirectory);
        Assert.IsNull(src.Row, "Intermediate directory has no scan row.");
        Assert.HasCount(2, src.Children);
        Assert.AreEqual("app", src.Children[0].Name, "Directories sort before files.");
        Assert.AreEqual(@"src\app\main.cs", src.Children[0].Children[0].RelativePath);
        Assert.AreEqual("root.txt", roots[1].Name);
    }

    [TestMethod]
    public void Build_DirectoryWithChangedDescendant_SetsDifferenceFlag()
    {
        IReadOnlyList<FolderComparisonTreeNode> roots = FolderComparisonTreeBuilder.Build(
        [
            FileRow(@"a\changed.txt", FolderRowState.MetadataDifferent),
            FileRow(@"b\same.txt", FolderRowState.MetadataMatch),
        ]);

        FolderComparisonTreeNode dirA = roots.Single(n => n.Name == "a");
        FolderComparisonTreeNode dirB = roots.Single(n => n.Name == "b");
        Assert.IsTrue(dirA.HasDifferenceDescendant);
        Assert.IsFalse(dirB.HasDifferenceDescendant);
        Assert.IsFalse(dirB.HasErrorDescendant);
    }

    [TestMethod]
    public void Build_LeftOnlyAndRightOnly_AreDifferences()
    {
        IReadOnlyList<FolderComparisonTreeNode> roots = FolderComparisonTreeBuilder.Build(
        [
            FileRow("left.txt", FolderRowState.LeftOnly, leftExists: true, rightExists: false),
            FileRow("right.txt", FolderRowState.RightOnly, leftExists: false, rightExists: true),
        ]);

        Assert.IsTrue(roots.All(n => n.HasDifferenceDescendant));
        Assert.AreEqual(FolderRowState.LeftOnly, roots.Single(n => n.Name == "left.txt").State);
        Assert.AreEqual(FolderRowState.RightOnly, roots.Single(n => n.Name == "right.txt").State);
    }

    [TestMethod]
    public void Build_ErrorRow_SetsErrorFlagUpTheChain()
    {
        IReadOnlyList<FolderComparisonTreeNode> roots = FolderComparisonTreeBuilder.Build(
        [
            FileRow(@"deep\nested\broken.txt", FolderRowState.Error),
        ]);

        Assert.IsTrue(roots[0].HasErrorDescendant);
        Assert.IsTrue(roots[0].Children[0].HasErrorDescendant);
    }

    [TestMethod]
    public void Build_ExplicitDirectoryRow_IsAttachedToNode()
    {
        IReadOnlyList<FolderComparisonTreeNode> roots = FolderComparisonTreeBuilder.Build(
        [
            DirRow("emptydir"),
            FileRow("file.txt", FolderRowState.MetadataMatch),
        ]);

        FolderComparisonTreeNode dir = roots.Single(n => n.Name == "emptydir");
        Assert.IsTrue(dir.IsDirectory);
        Assert.IsNotNull(dir.Row);
        Assert.IsEmpty(dir.Children);
    }

    [TestMethod]
    public void Build_MetadataMatchState_IsNotLabelledAsDifference()
    {
        Assert.IsFalse(FolderRowStateClassification.IsDifference(FolderRowState.MetadataMatch));
        Assert.IsFalse(FolderRowStateClassification.IsDifference(FolderRowState.ContentMatch));
        Assert.IsTrue(FolderRowStateClassification.IsDifference(FolderRowState.ContentDifferent));
        Assert.IsTrue(FolderRowStateClassification.IsDifference(FolderRowState.MetadataDifferent));
    }

    [TestMethod]
    public void Build_IsDeterministic_ForSameInput()
    {
        FolderComparisonRow[] rows =
        [
            FileRow(@"z\file2.txt", FolderRowState.MetadataMatch),
            FileRow(@"a\file1.txt", FolderRowState.MetadataDifferent),
        ];

        IReadOnlyList<FolderComparisonTreeNode> first = FolderComparisonTreeBuilder.Build(rows);
        IReadOnlyList<FolderComparisonTreeNode> second = FolderComparisonTreeBuilder.Build(rows);

        Assert.HasCount(first.Count, second);
        Assert.AreEqual(first[0].Name, second[0].Name);
        Assert.AreEqual("a", first[0].Name, "Sorted ordinal ignore-case.");
    }
}
