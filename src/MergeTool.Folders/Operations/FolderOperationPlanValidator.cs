using MergeTool.Core;

namespace MergeTool.Folders.Operations;

public sealed record FolderOperationPlanValidationResult(
    bool IsValid,
    IReadOnlyList<Diagnostic> Diagnostics);

public static class FolderOperationPlanValidator
{
    public static FolderOperationPlanValidationResult Validate(
        FolderOperationPlan plan,
        string leftRoot,
        string rightRoot,
        bool verifySourcePreconditions = true)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var diagnostics = new List<Diagnostic>();

        string normalizedLeftRoot;
        string normalizedRightRoot;
        try
        {
            normalizedLeftRoot = PathNormalization.NormalizeFullPath(leftRoot);
            normalizedRightRoot = PathNormalization.NormalizeFullPath(rightRoot);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            diagnostics.Add(Diagnostic.Error(
                "MT-FOL-020",
                "Folder roots are invalid.",
                technicalDetail: exception.Message));
            return new FolderOperationPlanValidationResult(false, diagnostics);
        }

        if (plan.Items.Count == 0)
        {
            diagnostics.Add(Diagnostic.Warning(
                "MT-FOL-021",
                "No folder operations were planned for the selected rows."));
            return new FolderOperationPlanValidationResult(false, diagnostics);
        }

        foreach (FolderOperationItem item in plan.Items)
        {
            ValidateItem(item, normalizedLeftRoot, normalizedRightRoot, verifySourcePreconditions, diagnostics);
        }

        bool hasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        return new FolderOperationPlanValidationResult(!hasErrors, diagnostics);
    }

    private static void ValidateItem(
        FolderOperationItem item,
        string normalizedLeftRoot,
        string normalizedRightRoot,
        bool verifySourcePreconditions,
        List<Diagnostic> diagnostics)
    {
        if (!FolderPathSafety.IsSafeRelativePath(item.RelativePath))
        {
            diagnostics.Add(Diagnostic.Error(
                "MT-FOL-022",
                $"Unsafe relative path '{item.RelativePath}'.",
                path: item.SourceFullPath));
            return;
        }

        if (!File.Exists(item.SourceFullPath))
        {
            diagnostics.Add(Diagnostic.Error(
                "MT-FOL-023",
                $"Source file is missing for '{item.RelativePath}'.",
                path: item.SourceFullPath));
            return;
        }

        string sourceRoot = item.Kind is FolderOperationKind.CopyLeftToRight or FolderOperationKind.DeleteLeft
            ? normalizedLeftRoot
            : normalizedRightRoot;

        if (!FolderPathSafety.IsPathUnderRoot(item.SourceFullPath, sourceRoot))
        {
            diagnostics.Add(Diagnostic.Error(
                "MT-FOL-024",
                $"Source path escapes its folder root for '{item.RelativePath}'.",
                path: item.SourceFullPath));
        }

        if (item.TargetFullPath is not null)
        {
            string targetRoot = item.Kind == FolderOperationKind.CopyLeftToRight
                ? normalizedRightRoot
                : normalizedLeftRoot;

            if (!FolderPathSafety.IsPathUnderRoot(item.TargetFullPath, targetRoot))
            {
                diagnostics.Add(Diagnostic.Error(
                    "MT-FOL-025",
                    $"Target path escapes its folder root for '{item.RelativePath}'.",
                    path: item.TargetFullPath));
            }
        }

        if (verifySourcePreconditions
            && item.ExpectedSourcePrecondition is FolderOperationPrecondition expected
            && !FolderOperationPrecondition.MatchesFile(item.SourceFullPath, expected))
        {
            diagnostics.Add(Diagnostic.Error(
                "MT-FOL-026",
                $"Source file changed since planning for '{item.RelativePath}'. Recompare and try again.",
                path: item.SourceFullPath));
        }
    }
}
