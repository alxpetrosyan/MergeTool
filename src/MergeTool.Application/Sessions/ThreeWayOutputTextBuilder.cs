using MergeTool.Text;
using MergeTool.Text.Diff;
using MergeTool.Text.ThreeWay;

namespace MergeTool.Application.Sessions;

/// <summary>Builds merged output text from region resolutions without mutating sources.</summary>
public static class ThreeWayOutputTextBuilder
{
    public static string Build(
        ThreeWayComparisonResult comparison,
        IReadOnlyList<ThreeWayRegionResolution> resolutions,
        TextDocumentSession baseDocument,
        TextDocumentSession leftDocument,
        TextDocumentSession rightDocument)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        ArgumentNullException.ThrowIfNull(resolutions);
        ArgumentNullException.ThrowIfNull(baseDocument);
        ArgumentNullException.ThrowIfNull(leftDocument);
        ArgumentNullException.ThrowIfNull(rightDocument);

        LineEndingStyle convention = baseDocument.Metadata.DominantLineEnding;
        var parts = new List<string>(comparison.Regions.Count);

        foreach (ThreeWayRegion region in comparison.Regions)
        {
            ThreeWayRegionResolution resolution = resolutions[region.Ordinal];
            ThreeWayOutputSourceChoice source = ResolveSource(region, resolution);
            IReadOnlyList<TextLine> lines = source switch
            {
                ThreeWayOutputSourceChoice.Base => baseDocument.CurrentLines,
                ThreeWayOutputSourceChoice.Left => leftDocument.CurrentLines,
                ThreeWayOutputSourceChoice.Right => rightDocument.CurrentLines,
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
            };

            LineRange range = source switch
            {
                ThreeWayOutputSourceChoice.Base => region.BaseRange,
                ThreeWayOutputSourceChoice.Left => region.LeftRange,
                ThreeWayOutputSourceChoice.Right => region.RightRange,
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
            };

            string segment = ExtractRegionText(lines, range, convention);
            if (segment.Length > 0)
            {
                parts.Add(segment);
            }
        }

        if (parts.Count == 0)
        {
            return string.Empty;
        }

        string separator = TextComposer.GetLineEndingText(convention);
        string joined = string.Join(separator, parts);
        if (baseDocument.Metadata.HasFinalNewline && parts.Count > 0)
        {
            joined += separator;
        }

        return joined;
    }

    private static ThreeWayOutputSourceChoice ResolveSource(
        ThreeWayRegion region,
        ThreeWayRegionResolution resolution)
    {
        if (!region.IsDifference)
        {
            return ThreeWayOutputSourceChoice.Base;
        }

        return resolution.State == ThreeWayRegionResolutionState.Resolved && resolution.ChosenSource is ThreeWayOutputSourceChoice choice
            ? choice
            : ThreeWayOutputSourceChoice.Base;
    }

    internal static string ExtractRegionText(
        IReadOnlyList<TextLine> lines,
        LineRange range,
        LineEndingStyle convention)
    {
        if (range.IsEmpty)
        {
            return string.Empty;
        }

        string separator = TextComposer.GetLineEndingText(convention);
        var parts = new List<string>(range.Length);
        int end = Math.Min(range.EndExclusive, lines.Count);
        for (int line = range.StartIndex; line < end; line++)
        {
            parts.Add(lines[line].Content);
        }

        return string.Join(separator, parts);
    }
}
