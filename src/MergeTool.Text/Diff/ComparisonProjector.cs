using System.Text.RegularExpressions;
using MergeTool.Core;

namespace MergeTool.Text.Diff;

/// <summary>Result of building a comparison projection; carries diagnostics on validation failure.</summary>
public sealed class ComparisonProjectionBuildResult
{
    private ComparisonProjectionBuildResult(ComparisonProjection? projection, IReadOnlyList<Diagnostic> diagnostics)
    {
        Projection = projection;
        Diagnostics = diagnostics;
    }

    public ComparisonProjection? Projection { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public bool IsSuccess => Projection is not null && Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error);

    public static ComparisonProjectionBuildResult Succeeded(ComparisonProjection projection)
        => new(projection, []);

    public static ComparisonProjectionBuildResult Failed(IReadOnlyList<Diagnostic> diagnostics)
        => new(null, diagnostics);

    public static ComparisonProjectionBuildResult Failed(Diagnostic diagnostic)
        => new(null, [diagnostic]);
}

/// <summary>
/// Builds projection-safe comparison lines: excluded lines are omitted, remaining lines carry
/// normalized comparison content while mapping back to original indices.
/// </summary>
public static class ComparisonProjector
{
    public const string InvalidRegexCode = "MT-CMP-004";

    public const string OverlappingIgnoredBlocksCode = "MT-CMP-005";

    public const string UnclosedIgnoredBlockCode = "MT-CMP-006";

    public static ComparisonProjectionBuildResult Build(
        IReadOnlyList<TextLine> leftLines,
        IReadOnlyList<TextLine> rightLines,
        TextComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(leftLines);
        ArgumentNullException.ThrowIfNull(rightLines);
        ArgumentNullException.ThrowIfNull(options);

        RegexBuildResult ignoredLine = TryCompile(options.IgnoredLinePattern, "Ignored line");
        if (!ignoredLine.IsSuccess)
        {
            return ComparisonProjectionBuildResult.Failed(ignoredLine.Diagnostic!);
        }

        RegexBuildResult blockStart = TryCompile(options.IgnoredBlockStartPattern, "Ignored block start");
        if (!blockStart.IsSuccess)
        {
            return ComparisonProjectionBuildResult.Failed(blockStart.Diagnostic!);
        }

        RegexBuildResult blockEnd = TryCompile(options.IgnoredBlockEndPattern, "Ignored block end");
        if (!blockEnd.IsSuccess)
        {
            return ComparisonProjectionBuildResult.Failed(blockEnd.Diagnostic!);
        }

        if ((blockStart.Regex is null) != (blockEnd.Regex is null))
        {
            return ComparisonProjectionBuildResult.Failed(Diagnostic.Error(
                InvalidRegexCode,
                "Ignored block start and end patterns must both be set or both be empty."));
        }

        SideProjectionBuildResult left = BuildSide(
            leftLines,
            options,
            ignoredLine.Regex,
            blockStart.Regex,
            blockEnd.Regex);
        if (!left.IsSuccess)
        {
            return ComparisonProjectionBuildResult.Failed(left.Diagnostics);
        }

        SideProjectionBuildResult right = BuildSide(
            rightLines,
            options,
            ignoredLine.Regex,
            blockStart.Regex,
            blockEnd.Regex);
        if (!right.IsSuccess)
        {
            return ComparisonProjectionBuildResult.Failed(right.Diagnostics);
        }

        return ComparisonProjectionBuildResult.Succeeded(new ComparisonProjection(
            left.ProjectedLines!,
            right.ProjectedLines!,
            left.ProjectedToOriginalLine!,
            right.ProjectedToOriginalLine!));
    }

    private static SideProjectionBuildResult BuildSide(
        IReadOnlyList<TextLine> originalLines,
        TextComparisonOptions options,
        Regex? ignoredLineRegex,
        Regex? ignoredBlockStartRegex,
        Regex? ignoredBlockEndRegex)
    {
        var projectedLines = new List<TextLine>();
        var projectedToOriginal = new List<int>();
        bool insideIgnoredBlock = false;

        for (int originalIndex = 0; originalIndex < originalLines.Count; originalIndex++)
        {
            TextLine originalLine = originalLines[originalIndex];
            string content = originalLine.Content;

            if (ignoredBlockStartRegex is not null && ignoredBlockStartRegex.IsMatch(content))
            {
                if (insideIgnoredBlock)
                {
                    return SideProjectionBuildResult.Failed(Diagnostic.Error(
                        OverlappingIgnoredBlocksCode,
                        "Ignored block start matched before the previous block ended.",
                        technicalDetail: $"Line {originalIndex + 1}: '{content}'"));
                }

                insideIgnoredBlock = true;
                continue;
            }

            if (insideIgnoredBlock)
            {
                if (ignoredBlockEndRegex!.IsMatch(content))
                {
                    insideIgnoredBlock = false;
                }

                continue;
            }

            if (options.IgnoreEmptyLines && string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            if (ignoredLineRegex is not null && ignoredLineRegex.IsMatch(content))
            {
                continue;
            }

            string normalized = LineComparisonNormalizer.Normalize(content, options);
            projectedLines.Add(new TextLine(projectedLines.Count, normalized, originalLine.Terminator));
            projectedToOriginal.Add(originalIndex);
        }

        if (insideIgnoredBlock)
        {
            return SideProjectionBuildResult.Failed(Diagnostic.Error(
                UnclosedIgnoredBlockCode,
                "An ignored block was not closed before the end of the document."));
        }

        return SideProjectionBuildResult.Succeeded(projectedLines, projectedToOriginal);
    }

    private static RegexBuildResult TryCompile(string? pattern, string label)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return RegexBuildResult.Success(null);
        }

        try
        {
            return RegexBuildResult.Success(new Regex(pattern, RegexOptions.CultureInvariant));
        }
        catch (Exception exception)
        {
            return RegexBuildResult.Failure(Diagnostic.Error(
                InvalidRegexCode,
                $"{label} pattern is not a valid regular expression.",
                technicalDetail: exception.Message));
        }
    }

    private sealed class SideProjectionBuildResult
    {
        private SideProjectionBuildResult(
            IReadOnlyList<TextLine>? projectedLines,
            IReadOnlyList<int>? projectedToOriginalLine,
            IReadOnlyList<Diagnostic> diagnostics)
        {
            ProjectedLines = projectedLines;
            ProjectedToOriginalLine = projectedToOriginalLine;
            Diagnostics = diagnostics;
        }

        public IReadOnlyList<TextLine>? ProjectedLines { get; }

        public IReadOnlyList<int>? ProjectedToOriginalLine { get; }

        public IReadOnlyList<Diagnostic> Diagnostics { get; }

        public bool IsSuccess => ProjectedLines is not null && Diagnostics.Count == 0;

        public static SideProjectionBuildResult Succeeded(
            IReadOnlyList<TextLine> projectedLines,
            IReadOnlyList<int> projectedToOriginalLine)
            => new(projectedLines, projectedToOriginalLine, []);

        public static SideProjectionBuildResult Failed(Diagnostic diagnostic)
            => new(null, null, [diagnostic]);
    }

    private sealed class RegexBuildResult
    {
        private RegexBuildResult(Regex? regex, Diagnostic? diagnostic)
        {
            Regex = regex;
            Diagnostic = diagnostic;
        }

        public Regex? Regex { get; }

        public Diagnostic? Diagnostic { get; }

        public bool IsSuccess => Diagnostic is null;

        public static RegexBuildResult Success(Regex? regex) => new(regex, null);

        public static RegexBuildResult Failure(Diagnostic diagnostic) => new(null, diagnostic);
    }
}
