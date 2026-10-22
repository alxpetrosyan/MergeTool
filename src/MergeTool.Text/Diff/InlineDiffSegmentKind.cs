namespace MergeTool.Text.Diff;

/// <summary>Semantic kind of one UTF-16 span inside a modified line pair.</summary>
public enum InlineDiffSegmentKind
{
    Unchanged = 0,
    Replacement = 1,
    Insertion = 2,
    Deletion = 3,
}
