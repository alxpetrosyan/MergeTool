namespace MergeTool.Folders;

/// <summary>Folder comparison options. Defaults preserve MVP quick-metadata behavior.</summary>
public sealed record FolderComparisonOptions
{
    public FolderComparisonOptions(
        FolderComparisonMode mode = FolderComparisonMode.QuickMetadata,
        TimeSpan metadataTimestampTolerance = default)
    {
        if (metadataTimestampTolerance < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(metadataTimestampTolerance));
        }

        Mode = mode;
        MetadataTimestampTolerance = metadataTimestampTolerance;
    }

    public FolderComparisonMode Mode { get; init; }

    /// <summary>Used only in <see cref="FolderComparisonMode.QuickMetadata"/> mode.</summary>
    public TimeSpan MetadataTimestampTolerance { get; init; }

    public static FolderComparisonOptions QuickMetadata { get; } = new();
}
