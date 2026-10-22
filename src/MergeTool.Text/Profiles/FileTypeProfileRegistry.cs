using MergeTool.Text.Diff;

namespace MergeTool.Text.Profiles;

/// <summary>Built-in profile catalog matched by file extension.</summary>
public static class FileTypeProfileRegistry
{
    public static FileTypeProfile PlainText { get; } = Create(
        FileTypeProfileId.PlainText,
        "Plain Text",
        [".txt", ".log", ".text"],
        tabSize: 4,
        highlighting: null);

    public static FileTypeProfile DelphiPascal { get; } = Create(
        FileTypeProfileId.DelphiPascal,
        "Delphi/Pascal",
        [".pas", ".dpr", ".dpk", ".inc"],
        tabSize: 2,
        highlighting: null);

    public static FileTypeProfile CSharp { get; } = Create(
        FileTypeProfileId.CSharp,
        "C#",
        [".cs", ".csx"],
        tabSize: 4,
        highlighting: "C#");

    public static FileTypeProfile Sql { get; } = Create(
        FileTypeProfileId.Sql,
        "SQL",
        [".sql"],
        tabSize: 4,
        highlighting: null);

    public static FileTypeProfile Xml { get; } = Create(
        FileTypeProfileId.Xml,
        "XML",
        [".xml", ".xaml", ".config", ".csproj", ".props", ".targets", ".svg"],
        tabSize: 2,
        highlighting: "XML");

    public static FileTypeProfile Json { get; } = Create(
        FileTypeProfileId.Json,
        "JSON",
        [".json", ".jsonl"],
        tabSize: 2,
        highlighting: "JavaScript",
        comparisonOptions: new TextComparisonOptions(ignoreEmptyLines: false));

    public static FileTypeProfile Html { get; } = Create(
        FileTypeProfileId.Html,
        "HTML",
        [".html", ".htm", ".xhtml"],
        tabSize: 2,
        highlighting: "HTML");

    public static FileTypeProfile Css { get; } = Create(
        FileTypeProfileId.Css,
        "CSS",
        [".css"],
        tabSize: 2,
        highlighting: "HTML");

    public static FileTypeProfile JavaScript { get; } = Create(
        FileTypeProfileId.JavaScript,
        "JavaScript",
        [".js", ".mjs", ".cjs"],
        tabSize: 2,
        highlighting: "JavaScript");

    public static FileTypeProfile TypeScript { get; } = Create(
        FileTypeProfileId.TypeScript,
        "TypeScript",
        [".ts", ".tsx"],
        tabSize: 2,
        highlighting: "JavaScript");

    public static FileTypeProfile Markdown { get; } = Create(
        FileTypeProfileId.Markdown,
        "Markdown",
        [".md", ".markdown"],
        tabSize: 2,
        highlighting: null);

    public static FileTypeProfile IniConfig { get; } = Create(
        FileTypeProfileId.IniConfig,
        "INI/Config",
        [".ini", ".cfg", ".conf", ".properties"],
        tabSize: 4,
        highlighting: null);

    private static readonly FileTypeProfile[] All =
    [
        CSharp, TypeScript, JavaScript, Json, Xml, Html, Css, Sql,
        DelphiPascal, Markdown, IniConfig, PlainText,
    ];

    public static IReadOnlyList<FileTypeProfile> Profiles => All;

    public static FileTypeProfile Get(FileTypeProfileId id)
        => All.First(p => p.Id == id);

    private static FileTypeProfile Create(
        FileTypeProfileId id,
        string displayName,
        IReadOnlyList<string> extensionMasks,
        int tabSize,
        string? highlighting,
        TextComparisonOptions? comparisonOptions = null)
        => new(id, displayName, extensionMasks, tabSize, comparisonOptions, highlighting);
}
