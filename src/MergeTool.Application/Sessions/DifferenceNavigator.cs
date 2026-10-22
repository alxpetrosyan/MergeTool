namespace MergeTool.Application.Sessions;

/// <summary>
/// One navigation step's outcome: the selected block and safe scroll anchors for both panes
/// (generated through the presentation map), plus the 1-based position for display.
/// </summary>
public sealed record NavigationTarget(
    int BlockOrdinal,
    int LeftAnchorLine,
    int RightAnchorLine,
    int DifferenceNumber,
    int DifferenceCount);

/// <summary>
/// Moves the current difference selection over the non-equal blocks of the latest applied
/// result. Block ordinals are stable within one result (results are immutable). Navigation
/// deliberately does not wrap: MoveNext at the last difference and MovePrevious at the first
/// return null and leave the selection unchanged. A stale or running comparison disables all
/// movement.
/// </summary>
public sealed class DifferenceNavigator
{
    private readonly TwoWayComparisonSession _session;

    public DifferenceNavigator(TwoWayComparisonSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private IReadOnlyList<int> DifferenceOrdinals =>
        _session.CanNavigate ? _session.PresentationMap?.DifferenceBlockOrdinals ?? [] : [];

    public int DifferenceCount => DifferenceOrdinals.Count;

    /// <summary>With no selection, MoveNext targets the first difference.</summary>
    public bool CanMoveNext
    {
        get
        {
            IReadOnlyList<int> differences = DifferenceOrdinals;
            if (differences.Count == 0)
            {
                return false;
            }

            return _session.SelectedBlockOrdinal is not int current || current < differences[^1];
        }
    }

    /// <summary>With no selection, MovePrevious targets the last difference.</summary>
    public bool CanMovePrevious
    {
        get
        {
            IReadOnlyList<int> differences = DifferenceOrdinals;
            if (differences.Count == 0)
            {
                return false;
            }

            return _session.SelectedBlockOrdinal is not int current || current > differences[0];
        }
    }

    public NavigationTarget? MoveNext()
    {
        IReadOnlyList<int> differences = DifferenceOrdinals;
        if (differences.Count == 0)
        {
            return null;
        }

        int index;
        if (_session.SelectedBlockOrdinal is int current)
        {
            index = FirstIndexAfter(differences, current);
            if (index >= differences.Count)
            {
                return null; // No wrap: stay at the last difference.
            }
        }
        else
        {
            index = 0;
        }

        return Select(differences, index);
    }

    public NavigationTarget? MovePrevious()
    {
        IReadOnlyList<int> differences = DifferenceOrdinals;
        if (differences.Count == 0)
        {
            return null;
        }

        int index;
        if (_session.SelectedBlockOrdinal is int current)
        {
            index = LastIndexBefore(differences, current);
            if (index < 0)
            {
                return null; // No wrap: stay at the first difference.
            }
        }
        else
        {
            index = differences.Count - 1;
        }

        return Select(differences, index);
    }

    /// <summary>The target for the currently selected difference, if any.</summary>
    public NavigationTarget? CurrentTarget()
    {
        IReadOnlyList<int> differences = DifferenceOrdinals;
        if (_session.SelectedBlockOrdinal is not int current)
        {
            return null;
        }

        for (int i = 0; i < differences.Count; i++)
        {
            if (differences[i] == current)
            {
                return CreateTarget(differences, i);
            }
        }

        return null;
    }

    private NavigationTarget Select(IReadOnlyList<int> differences, int index)
    {
        _session.SelectBlock(differences[index]);
        return CreateTarget(differences, index);
    }

    private NavigationTarget CreateTarget(IReadOnlyList<int> differences, int index)
    {
        ComparisonPresentationMap map = _session.PresentationMap!;
        int ordinal = differences[index];
        return new NavigationTarget(
            ordinal,
            map.GetLeftAnchorForBlock(ordinal),
            map.GetRightAnchorForBlock(ordinal),
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
