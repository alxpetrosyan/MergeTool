using MergeTool.Core;
using MergeTool.Folders;

namespace MergeTool.App.Wpf.ViewModels;

/// <summary>One row in the folder comparison grid.</summary>
public sealed class FolderCompareRowViewModel
{
    public FolderCompareRowViewModel(FolderComparisonRow row)
    {
        Source = row;
        RelativePath = row.RelativePath;
        State = row.State;
        StateDisplay = FolderRowStateLabels.GetDisplayName(row.State);
        BaseSizeDisplay = FormatSize(row.BaseSizeBytes);
        LeftSizeDisplay = FormatSize(row.LeftSizeBytes);
        RightSizeDisplay = FormatSize(row.RightSizeBytes);
        BaseTimeDisplay = FormatUtc(row.BaseLastWriteTimeUtc);
        LeftTimeDisplay = FormatUtc(row.LeftLastWriteTimeUtc);
        RightTimeDisplay = FormatUtc(row.RightLastWriteTimeUtc);
        DiagnosticsDisplay = row.RowDiagnostics.Count == 0
            ? string.Empty
            : string.Join("; ", row.RowDiagnostics.Select(d => d.Message));
    }

    public FolderComparisonRow Source { get; }

    public string RelativePath { get; }

    public FolderRowState State { get; }

    public string StateDisplay { get; }

    public string BaseSizeDisplay { get; }

    public string LeftSizeDisplay { get; }

    public string RightSizeDisplay { get; }

    public string BaseTimeDisplay { get; }

    public string LeftTimeDisplay { get; }

    public string RightTimeDisplay { get; }

    public string DiagnosticsDisplay { get; }

    public bool CanOpenPairedTextComparison => Source.HasPairedFiles;

    public bool CanOpenThreeWayTextComparison => Source.HasTripledFiles;

    private static string FormatSize(long? bytes)
        => bytes is null ? string.Empty : bytes.Value.ToString("N0");

    private static string FormatUtc(DateTime? utc)
        => utc is null ? string.Empty : utc.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
}
