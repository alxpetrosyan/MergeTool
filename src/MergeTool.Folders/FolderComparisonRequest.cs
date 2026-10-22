namespace MergeTool.Folders;

/// <summary>Request to compare two or three folder roots using quick metadata comparison.</summary>
public sealed record FolderComparisonRequest
{
    public FolderComparisonRequest(string leftRoot, string rightRoot, FolderComparisonOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leftRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(rightRoot);

        LeftRoot = leftRoot;
        RightRoot = rightRoot;
        Options = options ?? new FolderComparisonOptions();
    }

    public FolderComparisonRequest(
        string baseRoot,
        string leftRoot,
        string rightRoot,
        FolderComparisonOptions? options = null)
        : this(leftRoot, rightRoot, options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseRoot);

        BaseRoot = baseRoot;
    }

    public string? BaseRoot { get; }

    public string LeftRoot { get; }

    public string RightRoot { get; }

    public FolderComparisonOptions Options { get; }

    public bool IsThreeWay => BaseRoot is not null;
}
