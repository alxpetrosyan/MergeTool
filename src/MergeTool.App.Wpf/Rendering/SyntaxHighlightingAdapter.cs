using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using MergeTool.Text.Profiles;

namespace MergeTool.App.Wpf.Rendering;

/// <summary>Maps file-type profiles to AvalonEdit syntax highlighting and tab size.</summary>
public static class SyntaxHighlightingAdapter
{
    public static void Apply(TextEditor editor, FileTypeProfile profile)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(profile);

        editor.Options.IndentationSize = profile.TabSize;
        editor.Options.ConvertTabsToSpaces = true;

        if (string.IsNullOrWhiteSpace(profile.AvalonEditHighlightingName))
        {
            editor.SyntaxHighlighting = null;
            return;
        }

        editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(profile.AvalonEditHighlightingName)
            ?? HighlightingManager.Instance.GetDefinition("XML");
    }
}
