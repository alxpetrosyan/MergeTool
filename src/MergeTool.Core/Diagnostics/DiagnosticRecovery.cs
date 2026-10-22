namespace MergeTool.Core.Diagnostics;

using MergeTool.Core.Settings;

/// <summary>
/// User-facing recovery hints keyed by stable diagnostic codes. Hints are safe to show in the UI
/// and describe what the user can try next; they never contain exception text.
/// </summary>
public static class DiagnosticRecovery
{
    private static readonly IReadOnlyDictionary<string, string> Hints = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [AppSettingsMutations.CorruptSettingsCode] = "Close MergeTool, fix or delete the settings file, then restart. Reset to defaults from Settings if the file cannot be repaired.",
        [OperationDiagnostics.CancelledCode] = "Start the operation again when ready. The last completed comparison or folder scan is unchanged.",
        [OperationDiagnostics.UnexpectedFailureCode] = "Try the action again. If it keeps failing, copy the technical details and report the problem.",

        ["MT-CMP-001"] = "Run Compare again, then retry the merge or navigation action.",
        ["MT-CMP-002"] = "Choose text files on both sides, or switch to a workspace that supports binary comparison.",
        ["MT-CMP-003"] = "Pick two different files to compare.",
        ["MT-CMP-004"] = "Fix the regular expression pattern in comparison or search options, then try again.",
        ["MT-CMP-005"] = "Adjust ignored-block rules so regions do not overlap.",
        ["MT-CMP-006"] = "Close every ignored-block region with a matching end marker.",
        ["MT-CMP-012"] = "Choose text files for three-way comparison.",
        ["MT-CMP-013"] = "Pick three different file paths (base, left, right).",

        ["MT-FMT-001"] = "Repair the file so it is valid for the selected format, or switch comparison projection to Raw.",
        ["MT-FMT-002"] = "Repair the file so it is valid for the selected format, or switch comparison projection to Raw.",

        ["MT-FLD-001"] = "Browse to an existing left folder, or create it on disk first.",
        ["MT-FLD-002"] = "Browse to an existing right folder, or create it on disk first.",
        ["MT-FLD-003"] = "The left path must be a folder, not a file.",
        ["MT-FLD-004"] = "The right path must be a folder, not a file.",
        ["MT-FLD-005"] = "Choose two different folder roots to compare.",
        ["MT-FLD-006"] = "Check that the folder path exists and you have permission to read it.",
        ["MT-FLD-007"] = "Browse to an existing base folder, or create it on disk first.",
        ["MT-FLD-008"] = "The base path must be a folder, not a file.",
        ["MT-FLD-009"] = "Choose three different folder roots (base, left, right).",
        ["MT-FLD-010"] = "Check folder permissions and that no file is locked by another program.",
        ["MT-FLD-011"] = "Check folder permissions and that no file is locked by another program.",
        ["MT-FLD-012"] = "Retry the scan, or switch folder comparison to metadata-only mode.",

        ["MT-FOL-010"] = "Review the failed item, fix permissions or conflicts, then retry the folder operation.",
        ["MT-FOL-020"] = "Refresh the folder comparison and rebuild the operation plan.",
        ["MT-FOL-021"] = "Refresh the folder comparison and rebuild the operation plan.",
        ["MT-FOL-022"] = "Choose a destination that does not already contain the target file.",
        ["MT-FOL-023"] = "Ensure the source file still exists, then refresh and try again.",
        ["MT-FOL-024"] = "Resolve path conflicts in the operation plan before confirming.",
        ["MT-FOL-025"] = "Remove or replace items that point outside the allowed folder roots.",
        ["MT-FOL-026"] = "Wait for the in-progress operation to finish, or cancel it before starting another.",

        ["MT-IO-001"] = "Check that the file exists, is not locked, and you have read permission.",
        ["MT-IO-002"] = "Reload the file from disk, merge your changes again, or save to a different path.",
        ["MT-IO-003"] = "Check disk space, permissions, and that the target is not read-only.",
        ["MT-IO-004"] = "Clear the read-only attribute on the file, or save to a different location.",
        ["MT-IO-005"] = "Check disk space, permissions, and that the output path is writable.",

        ["MT-MRG-001"] = "Save or copy to a writable file, or clear the read-only flag on the merge target.",

        ["MT-GTL-001"] = "Enter a line number within the document range shown in the pane.",
        ["MT-GTL-002"] = "Enter a positive integer line number.",

        ["MT-TXT-001"] = "Confirm the encoding looks correct; pick a specific encoding if needed.",
        ["MT-TXT-002"] = "Open a text file, or use a tool that supports binary comparison.",

        ["MT-SRC-001"] = "Fix the search regular expression, or turn off regex mode.",

        ["MT-3WM-001"] = "Run Compare again, then retry resolving or saving output.",
        ["MT-3WM-002"] = "Resolve every highlighted region before saving merged output.",
        ["MT-3WM-003"] = "Run Compare again after manual edits, then continue resolving regions.",
        ["MT-3WM-004"] = "Fix the output document and run Compare again before saving.",

        ["MT-SET-001"] = "Reset settings to defaults or repair the settings file, then restart MergeTool.",
    };

    /// <summary>Returns a recovery hint for <paramref name="code"/>, or <see langword="null"/> when none is defined.</summary>
    public static string? TryGetRecoveryHint(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return Hints.TryGetValue(code, out string? hint) ? hint : null;
    }
}
