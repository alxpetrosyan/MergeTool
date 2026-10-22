namespace MergeTool.Folders.Operations;

public static class FolderOperationPlanPreviewFormatter
{
    public static IReadOnlyList<string> FormatActionLines(FolderOperationPlan plan)
        => plan.Items.Select(FormatItem).ToList();

    public static string FormatSummary(FolderOperationPlan plan)
    {
        int copyCount = plan.Items.Count(item =>
            item.Kind is FolderOperationKind.CopyLeftToRight or FolderOperationKind.CopyRightToLeft);
        int deleteCount = plan.Items.Count - copyCount;
        int overwriteCount = plan.Items.Count(item => item.WillOverwriteTarget);

        return $"{plan.Items.Count} action(s): {copyCount} copy, {deleteCount} delete"
            + (overwriteCount > 0 ? $", {overwriteCount} overwrite" : string.Empty) + ".";
    }

    public static string? FormatWarning(FolderOperationPlan plan)
    {
        if (plan.HasDeleteOperations && !RecycleBinSupport.IsAvailable)
        {
            return "Recycle Bin is unavailable on this platform. Deletes will be permanent after an extra confirmation.";
        }

        if (plan.HasOverwriteTargets)
        {
            return "One or more copy actions will overwrite existing files.";
        }

        return plan.HasDeleteOperations
            ? "Deleted files are sent to the Recycle Bin when available."
            : null;
    }

    private static string FormatItem(FolderOperationItem item)
    {
        string verb = item.Kind switch
        {
            FolderOperationKind.CopyLeftToRight => "Copy left → right",
            FolderOperationKind.CopyRightToLeft => "Copy right → left",
            FolderOperationKind.DeleteLeft => "Delete left",
            FolderOperationKind.DeleteRight => "Delete right",
            _ => item.Kind.ToString(),
        };

        string overwrite = item.WillOverwriteTarget ? " [OVERWRITE]" : string.Empty;
        return $"{verb}: {item.RelativePath}{overwrite}";
    }
}
