using MergeTool.Text.Profiles;

namespace MergeTool.Text.Tests;

[TestClass]
public sealed class FileTypeProfileMatcherTests
{
    [TestMethod]
    public void Match_CSharpExtension_ReturnsCSharpProfile()
    {
        FileTypeProfile profile = FileTypeProfileMatcher.Match(@"C:\src\Program.cs");
        Assert.AreEqual(FileTypeProfileId.CSharp, profile.Id);
        Assert.AreEqual("C#", profile.AvalonEditHighlightingName);
    }

    [TestMethod]
    public void Match_UnknownExtension_ReturnsPlainText()
    {
        FileTypeProfile profile = FileTypeProfileMatcher.Match(@"C:\data\unknown.xyz");
        Assert.AreEqual(FileTypeProfileId.PlainText, profile.Id);
    }

    [TestMethod]
    public void Registry_ContainsAllRequiredProfiles()
    {
        var ids = FileTypeProfileRegistry.Profiles.Select(p => p.Id).ToHashSet();
        Assert.Contains(FileTypeProfileId.DelphiPascal, ids);
        Assert.Contains(FileTypeProfileId.Json, ids);
        Assert.Contains(FileTypeProfileId.IniConfig, ids);
        Assert.HasCount(12, FileTypeProfileRegistry.Profiles);
    }
}
