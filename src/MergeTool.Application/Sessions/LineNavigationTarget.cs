using MergeTool.Core;

namespace MergeTool.Application.Sessions;

/// <summary>Scroll a single pane to a source line (0-based index).</summary>
public sealed record LineNavigationTarget(int ZeroBasedLineIndex, ComparisonSide Side);
