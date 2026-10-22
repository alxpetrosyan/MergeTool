namespace MergeTool.Application.Sessions;

/// <summary>Explicit source selection for a 3-way output region.</summary>
public enum ThreeWayOutputSourceChoice
{
    Base = 0,
    Left = 1,
    Right = 2,
}

public enum ThreeWayRegionResolutionState
{
    Unresolved = 0,
    Resolved = 1,
    Stale = 2,
}

public sealed record ThreeWayRegionResolution(
    int RegionOrdinal,
    ThreeWayRegionResolutionState State,
    ThreeWayOutputSourceChoice? ChosenSource);
