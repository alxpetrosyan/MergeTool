using MergeTool.Core;
using MergeTool.Text;
using MergeTool.Text.ThreeWay;

namespace MergeTool.Application.Sessions;

/// <summary>
/// Tracks per-region resolutions and the editable output document initialized from base.
/// Sources are never mutated; output is rebuilt from explicit choices.
/// </summary>
public sealed class ThreeWayOutputMergeSession
{
    public const string StaleComparisonCode = "MT-3WM-001";

    public const string UnresolvedRegionsCode = "MT-3WM-002";

    public const string ManualEditInvalidatesCode = "MT-3WM-003";

    private ThreeWayRegionResolution[] _resolutions = [];

    public TextDocumentSession? Output { get; private set; }

    public string? OutputSavePath { get; private set; }

    public bool HasManualOutputEdit { get; private set; }

    public int ComparisonRevisionWhenInitialized { get; private set; } = -1;

    public IReadOnlyList<ThreeWayRegionResolution> Resolutions => _resolutions;

    public void Initialize(ThreeWayComparisonSession comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        if (comparison.LatestResult is null)
        {
            throw new InvalidOperationException("A 3-way comparison result is required before initializing output.");
        }

        Output = CreateOutputSession(comparison.Base);
        _resolutions = CreateInitialResolutions(comparison.LatestResult);
        ComparisonRevisionWhenInitialized = comparison.ComparisonRevision;
        HasManualOutputEdit = false;
        OutputSavePath = null;
        RebuildOutput(comparison);
    }

    public OperationResult ApplyRegionChoice(
        ThreeWayComparisonSession comparison,
        int regionOrdinal,
        ThreeWayOutputSourceChoice choice)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        if (!comparison.CanNavigate || comparison.LatestResult is null || Output is null)
        {
            return OperationResult.Failed(Diagnostic.Error(
                StaleComparisonCode,
                ThreeWayComparisonSession.StaleComparisonMessage));
        }

        if (regionOrdinal < 0 || regionOrdinal >= comparison.LatestResult.Regions.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(regionOrdinal));
        }

        ThreeWayRegion region = comparison.LatestResult.Regions[regionOrdinal];
        if (!region.IsDifference)
        {
            return OperationResult.Failed(Diagnostic.Warning(
                "MT-3WM-004",
                "Unchanged regions already use base content in the output."));
        }

        _resolutions[regionOrdinal] = new ThreeWayRegionResolution(
            regionOrdinal,
            ThreeWayRegionResolutionState.Resolved,
            choice);
        HasManualOutputEdit = false;
        RebuildOutput(comparison);
        return OperationResult.Completed();
    }

    public void NotifyManualOutputEdit(ThreeWayComparisonSession comparison, string newText)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        if (Output is null || comparison.LatestResult is null)
        {
            return;
        }

        Output.ReplaceText(newText);
        HasManualOutputEdit = true;
        for (int i = 0; i < _resolutions.Length; i++)
        {
            if (!comparison.LatestResult.Regions[i].IsDifference)
            {
                continue;
            }

            _resolutions[i] = new ThreeWayRegionResolution(
                i,
                ThreeWayRegionResolutionState.Stale,
                null);
        }
    }

    public void RefreshAfterRecompare(ThreeWayComparisonSession comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        if (comparison.LatestResult is null)
        {
            return;
        }

        if (Output is null)
        {
            Initialize(comparison);
            return;
        }

        _resolutions = CreateInitialResolutions(comparison.LatestResult);
        ComparisonRevisionWhenInitialized = comparison.ComparisonRevision;
        HasManualOutputEdit = false;
        RebuildOutput(comparison);
    }

    public bool IsOutputStale(ThreeWayComparisonSession comparison)
        => comparison.ComparisonRevision != ComparisonRevisionWhenInitialized;

    public IReadOnlyList<int> UnresolvedDifferenceRegionOrdinals(ThreeWayComparisonResult comparison)
    {
        var ordinals = new List<int>();
        for (int i = 0; i < _resolutions.Length; i++)
        {
            if (!comparison.Regions[i].IsDifference)
            {
                continue;
            }

            ThreeWayRegionResolution resolution = _resolutions[i];
            if (resolution.State is ThreeWayRegionResolutionState.Unresolved or ThreeWayRegionResolutionState.Stale)
            {
                ordinals.Add(i);
            }
        }

        return ordinals;
    }

    public void SetOutputSavePath(string path) => OutputSavePath = path;

    private void RebuildOutput(ThreeWayComparisonSession comparison)
    {
        if (Output is null || comparison.LatestResult is null)
        {
            return;
        }

        string rebuilt = ThreeWayOutputTextBuilder.Build(
            comparison.LatestResult,
            _resolutions,
            comparison.Base,
            comparison.Left,
            comparison.Right);
        Output.ReplaceText(rebuilt);
    }

    private static ThreeWayRegionResolution[] CreateInitialResolutions(ThreeWayComparisonResult comparison)
    {
        var resolutions = new ThreeWayRegionResolution[comparison.Regions.Count];
        for (int i = 0; i < comparison.Regions.Count; i++)
        {
            ThreeWayRegion region = comparison.Regions[i];
            resolutions[i] = region.IsDifference
                ? new ThreeWayRegionResolution(i, ThreeWayRegionResolutionState.Unresolved, null)
                : new ThreeWayRegionResolution(i, ThreeWayRegionResolutionState.Resolved, ThreeWayOutputSourceChoice.Base);
        }

        return resolutions;
    }

    private static TextDocumentSession CreateOutputSession(TextDocumentSession baseDocument)
    {
        string directory = Path.GetDirectoryName(baseDocument.Metadata.NormalizedFullPath)
            ?? Environment.CurrentDirectory;
        string outputPath = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(baseDocument.Metadata.DisplayName)}.merged.txt");

        TextDocumentMetadata metadata = baseDocument.Metadata with
        {
            NormalizedFullPath = outputPath,
            DisplayName = "Output (unsaved)",
            IsReadOnly = false,
        };

        TextDocumentSnapshot snapshot = new(
            metadata,
            new FileIdentity(outputPath, baseDocument.Identity.Version),
            baseDocument.CurrentText,
            baseDocument.CurrentLines);
        return TextDocumentSession.FromSnapshot(snapshot);
    }
}
