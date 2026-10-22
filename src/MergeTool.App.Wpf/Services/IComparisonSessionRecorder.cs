using MergeTool.Core.Settings;

namespace MergeTool.App.Wpf.Services;

/// <summary>Records successful comparison sessions into local settings.</summary>
public interface IComparisonSessionRecorder
{
    void RecordRecent(RecentComparisonEntry entry);
}
