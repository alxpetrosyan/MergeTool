namespace MergeTool.Text.Diff;

/// <summary>Associates intraline data with one modified block ordinal and line pair.</summary>
public sealed record ModifiedLineInlineDiff(int BlockOrdinal, InlineDiffLinePairResult LinePair);
