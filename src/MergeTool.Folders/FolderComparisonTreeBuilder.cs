namespace MergeTool.Folders;

/// <summary>
/// One node in a hierarchical folder comparison tree. Directory nodes may be synthesized from
/// relative path segments when the scan produced no explicit row for them (non-empty
/// directories); such nodes carry <see langword="null"/> in <see cref="Row"/>.
/// </summary>
public sealed class FolderComparisonTreeNode
{
    public FolderComparisonTreeNode(
        string name,
        string relativePath,
        bool isDirectory,
        FolderComparisonRow? row,
        IReadOnlyList<FolderComparisonTreeNode> children,
        bool hasDifferenceDescendant,
        bool hasErrorDescendant)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(children);

        Name = name;
        RelativePath = relativePath;
        IsDirectory = isDirectory;
        Row = row;
        Children = children;
        HasDifferenceDescendant = hasDifferenceDescendant;
        HasErrorDescendant = hasErrorDescendant;
    }

    /// <summary>Last path segment (file or directory name).</summary>
    public string Name { get; }

    public string RelativePath { get; }

    public bool IsDirectory { get; }

    /// <summary>The scanned row, or <see langword="null"/> for a synthesized directory node.</summary>
    public FolderComparisonRow? Row { get; }

    /// <summary>Children ordered directories-first, then by name (ordinal, case-insensitive).</summary>
    public IReadOnlyList<FolderComparisonTreeNode> Children { get; }

    /// <summary>True when this node or any descendant is a difference (see <see cref="FolderRowStateClassification.IsDifference"/>).</summary>
    public bool HasDifferenceDescendant { get; }

    /// <summary>True when this node or any descendant is an error or skipped row.</summary>
    public bool HasErrorDescendant { get; }

    /// <summary>State of the underlying row; metadata match for synthesized directories.</summary>
    public FolderRowState State => Row?.State ?? FolderRowState.MetadataMatch;
}

/// <summary>Shared classification of folder row states used by tree aggregation and filters.</summary>
public static class FolderRowStateClassification
{
    /// <summary>Anything that is not a plain metadata/content match.</summary>
    public static bool IsDifference(FolderRowState state) => state
        is FolderRowState.LeftOnly
        or FolderRowState.RightOnly
        or FolderRowState.BaseOnly
        or FolderRowState.BaseLeftOnly
        or FolderRowState.BaseRightOnly
        or FolderRowState.LeftRightOnly
        or FolderRowState.MetadataDifferent
        or FolderRowState.ContentDifferent
        or FolderRowState.Error
        or FolderRowState.SkippedReparsePoint;

    public static bool IsErrorOrSkipped(FolderRowState state) => state
        is FolderRowState.Error
        or FolderRowState.SkippedReparsePoint;
}

/// <summary>
/// Builds a hierarchical tree from the flat recursive scan rows. Pure transformation:
/// no file IO, deterministic ordering, safe for any row set including missing intermediate
/// directory rows.
/// </summary>
public static class FolderComparisonTreeBuilder
{
    private static readonly char[] Separators = ['\\', '/'];

    public static IReadOnlyList<FolderComparisonTreeNode> Build(IReadOnlyList<FolderComparisonRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        MutableNode root = new(name: string.Empty, relativePath: string.Empty, isDirectory: true);

        foreach (FolderComparisonRow row in rows)
        {
            string[] segments = row.RelativePath.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                continue;
            }

            MutableNode current = root;
            for (int index = 0; index < segments.Length; index++)
            {
                bool isLeaf = index == segments.Length - 1;
                string segment = segments[index];
                string relative = current.RelativePath.Length == 0
                    ? segment
                    : current.RelativePath + "\\" + segment;

                MutableNode child = current.GetOrAddChild(segment, relative, isDirectory: !isLeaf || row.IsDirectory);
                if (isLeaf)
                {
                    child.Row = row;
                    if (row.IsDirectory)
                    {
                        child.IsDirectory = true;
                    }
                }

                current = child;
            }
        }

        return root.Children.Values
            .OrderBy(node => node.IsDirectory ? 0 : 1)
            .ThenBy(node => node.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToImmutable)
            .ToList();
    }

    private static FolderComparisonTreeNode ToImmutable(MutableNode node)
    {
        List<FolderComparisonTreeNode> children = node.Children.Values
            .OrderBy(child => child.IsDirectory ? 0 : 1)
            .ThenBy(child => child.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToImmutable)
            .ToList();

        bool hasDifference = (node.Row is not null && FolderRowStateClassification.IsDifference(node.Row.State))
            || children.Any(child => child.HasDifferenceDescendant);
        bool hasError = (node.Row is not null && FolderRowStateClassification.IsErrorOrSkipped(node.Row.State))
            || children.Any(child => child.HasErrorDescendant);

        return new FolderComparisonTreeNode(
            node.Name,
            node.RelativePath,
            node.IsDirectory,
            node.Row,
            children,
            hasDifference,
            hasError);
    }

    private sealed class MutableNode(string name, string relativePath, bool isDirectory)
    {
        public string Name { get; } = name;

        public string RelativePath { get; } = relativePath;

        public bool IsDirectory { get; set; } = isDirectory;

        public FolderComparisonRow? Row { get; set; }

        public Dictionary<string, MutableNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);

        public MutableNode GetOrAddChild(string name, string relativePath, bool isDirectory)
        {
            if (!Children.TryGetValue(name, out MutableNode? child))
            {
                child = new MutableNode(name, relativePath, isDirectory);
                Children[name] = child;
            }
            else if (isDirectory)
            {
                child.IsDirectory = true;
            }

            return child;
        }
    }
}
