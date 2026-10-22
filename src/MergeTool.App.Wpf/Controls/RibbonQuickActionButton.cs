using System.Windows;
using System.Windows.Controls;

namespace MergeTool.App.Wpf.Controls;

/// <summary>
/// Compact ribbon quick-action button: a glyph over a short label. Styled app-wide; disabled
/// automatically when no command is attached (e.g. the active workspace lacks that action).
/// </summary>
public sealed class RibbonQuickActionButton : Button
{
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(RibbonQuickActionButton), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(RibbonQuickActionButton), new PropertyMetadata(string.Empty));

    /// <summary>Segoe MDL2 Assets glyph character (system icon font shipped with Windows).</summary>
    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
}
