using MergeTool.Text.ThreeWay;

namespace MergeTool.Application.Sessions;

public sealed record ThreeWayNavigationTarget(
    int RegionOrdinal,
    int BaseAnchorLine,
    int LeftAnchorLine,
    int RightAnchorLine,
    int DifferenceNumber,
    int DifferenceCount);

/// <summary>Moves selection across non-unchanged 3-way regions without wrapping.</summary>
public sealed class ThreeWayRegionNavigator
{
    private readonly ThreeWayComparisonSession _session;

    public ThreeWayRegionNavigator(ThreeWayComparisonSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private IReadOnlyList<int> DifferenceOrdinals =>
        _session.CanNavigate ? _session.LatestResult?.DifferenceRegionOrdinals ?? [] : [];

    public int DifferenceCount => DifferenceOrdinals.Count;

    public bool CanMoveNext
    {
        get
        {
            IReadOnlyList<int> differences = DifferenceOrdinals;
            if (differences.Count == 0)
            {
                return false;
            }

            return _session.SelectedRegionOrdinal is not int current || current < differences[^1];
        }
    }

    public bool CanMovePrevious
    {
        get
        {
            IReadOnlyList<int> differences = DifferenceOrdinals;
            if (differences.Count == 0)
            {
                return false;
            }

            return _session.SelectedRegionOrdinal is not int current || current > differences[0];
        }
    }

    public ThreeWayNavigationTarget? MoveNext()
    {
        IReadOnlyList<int> differences = DifferenceOrdinals;
        if (differences.Count == 0)
        {
            return null;
        }

        int index;
        if (_session.SelectedRegionOrdinal is int current)
        {
            index = FirstIndexAfter(differences, current);
            if (index >= differences.Count)
            {
                return null;
            }
        }
        else
        {
            index = 0;
        }

        return Select(differences, index);
    }

    public ThreeWayNavigationTarget? MovePrevious()
    {
        IReadOnlyList<int> differences = DifferenceOrdinals;
        if (differences.Count == 0)
        {
            return null;
        }

        int index;
        if (_session.SelectedRegionOrdinal is int current)
        {
            index = LastIndexBefore(differences, current);
            if (index < 0)
            {
                return null;
            }
        }
        else
        {
            index = differences.Count - 1;
        }

        return Select(differences, index);
    }

    private ThreeWayNavigationTarget Select(IReadOnlyList<int> differences, int index)
    {
        int ordinal = differences[index];
        _session.SelectRegion(ordinal);
        return CreateTarget(differences, index);
    }

    private ThreeWayNavigationTarget CreateTarget(IReadOnlyList<int> differences, int index)
    {
        ThreeWayRegion region = _session.LatestResult!.Regions[differences[index]];
        return new ThreeWayNavigationTarget(
            region.Ordinal,
            region.BaseRange.StartIndex,
            region.LeftRange.StartIndex,
            region.RightRange.StartIndex,
            index + 1,
            differences.Count);
    }

    private static int FirstIndexAfter(IReadOnlyList<int> ordinals, int current)
    {
        for (int i = 0; i < ordinals.Count; i++)
        {
            if (ordinals[i] > current)
            {
                return i;
            }
        }

        return ordinals.Count;
    }

    private static int LastIndexBefore(IReadOnlyList<int> ordinals, int current)
    {
        for (int i = ordinals.Count - 1; i >= 0; i--)
        {
            if (ordinals[i] < current)
            {
                return i;
            }
        }

        return -1;
    }
}
