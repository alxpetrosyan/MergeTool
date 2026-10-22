using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MergeTool.App.Wpf.Controls;

/// <summary>
/// Reusable editable path field: manual typing, browse button, recent-path dropdown, child
/// folder/file completion after a separator, and a visible invalid-path marker. Used by both
/// file and folder comparison workspaces.
/// </summary>
public partial class PathComboSelector : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(PathComboSelector),
        new FrameworkPropertyMetadata(string.Empty,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    public static readonly DependencyProperty LabelTextProperty = DependencyProperty.Register(
        nameof(LabelText), typeof(string), typeof(PathComboSelector), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AutomationNameProperty = DependencyProperty.Register(
        nameof(AutomationName), typeof(string), typeof(PathComboSelector), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty BrowseAutomationNameProperty = DependencyProperty.Register(
        nameof(BrowseAutomationName), typeof(string), typeof(PathComboSelector), new PropertyMetadata("Browse"));

    public static readonly DependencyProperty DropDownAutomationNameProperty = DependencyProperty.Register(
        nameof(DropDownAutomationName), typeof(string), typeof(PathComboSelector),
        new PropertyMetadata("Path suggestions"));

    public static readonly DependencyProperty BrowseCommandProperty = DependencyProperty.Register(
        nameof(BrowseCommand), typeof(ICommand), typeof(PathComboSelector), new PropertyMetadata(null));

    public static readonly DependencyProperty RecentPathsProperty = DependencyProperty.Register(
        nameof(RecentPaths), typeof(IEnumerable), typeof(PathComboSelector), new PropertyMetadata(null));

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(PathSelectorKind), typeof(PathComboSelector),
        new PropertyMetadata(PathSelectorKind.Any, OnKindChanged));

    public static readonly DependencyProperty IsPathInvalidProperty = DependencyProperty.Register(
        nameof(IsPathInvalid), typeof(bool), typeof(PathComboSelector), new PropertyMetadata(false));

    public PathComboSelector()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    /// <summary>Automation name applied to the inner text box (UI tests set values through it).</summary>
    public string AutomationName
    {
        get => (string)GetValue(AutomationNameProperty);
        set => SetValue(AutomationNameProperty, value);
    }

    public string BrowseAutomationName
    {
        get => (string)GetValue(BrowseAutomationNameProperty);
        set => SetValue(BrowseAutomationNameProperty, value);
    }

    public string DropDownAutomationName
    {
        get => (string)GetValue(DropDownAutomationNameProperty);
        set => SetValue(DropDownAutomationNameProperty, value);
    }

    public ICommand? BrowseCommand
    {
        get => (ICommand?)GetValue(BrowseCommandProperty);
        set => SetValue(BrowseCommandProperty, value);
    }

    public IEnumerable? RecentPaths
    {
        get => (IEnumerable?)GetValue(RecentPathsProperty);
        set => SetValue(RecentPathsProperty, value);
    }

    public PathSelectorKind Kind
    {
        get => (PathSelectorKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public bool IsPathInvalid
    {
        get => (bool)GetValue(IsPathInvalidProperty);
        private set => SetValue(IsPathInvalidProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PathComboSelector)d).UpdateValidation();

    private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PathComboSelector)d).UpdateValidation();

    private void UpdateValidation()
        => IsPathInvalid = PathSuggestionProvider.IsInvalid(Text, Kind);

    private IReadOnlyList<string> BuildSuggestions()
        => PathSuggestionProvider.GetSuggestions(Text, RecentPaths?.Cast<object>().Select(o => o.ToString() ?? string.Empty));

    private void ShowSuggestions()
    {
        IReadOnlyList<string> suggestions = BuildSuggestions();
        SuggestionList.ItemsSource = suggestions;
        if (suggestions.Count == 0)
        {
            SuggestionPopup.IsOpen = false;
            DropToggle.IsChecked = false;
            return;
        }

        SuggestionPopup.IsOpen = true;
    }

    private void PathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Offer children automatically only while the user is typing in the box, right after
        // a separator was entered; other keystrokes just refresh an already-open popup.
        if (!PathBox.IsKeyboardFocusWithin)
        {
            return;
        }

        if (PathSuggestionProvider.EndsWithSeparator(Text) || SuggestionPopup.IsOpen)
        {
            ShowSuggestions();
        }
    }

    private void PathBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape when SuggestionPopup.IsOpen:
                SuggestionPopup.IsOpen = false;
                e.Handled = true;
                break;
            case Key.Down when SuggestionPopup.IsOpen && SuggestionList.Items.Count > 0:
                SuggestionList.SelectedIndex = 0;
                ((ListBoxItem?)SuggestionList.ItemContainerGenerator.ContainerFromIndex(0))?.Focus();
                e.Handled = true;
                break;
            case Key.Down:
                ShowSuggestions();
                e.Handled = true;
                break;
        }
    }

    private void PathBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!SuggestionList.IsKeyboardFocusWithin)
        {
            SuggestionPopup.IsOpen = false;
        }
    }

    private void DropToggle_Checked(object sender, RoutedEventArgs e) => ShowSuggestions();

    private void DropToggle_Unchecked(object sender, RoutedEventArgs e) => SuggestionPopup.IsOpen = false;

    private void SuggestionPopup_Closed(object sender, EventArgs e) => DropToggle.IsChecked = false;

    private void SuggestionList_MouseUp(object sender, MouseButtonEventArgs e) => AcceptSelectedSuggestion();

    private void SuggestionList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                AcceptSelectedSuggestion();
                e.Handled = true;
                break;
            case Key.Escape:
                SuggestionPopup.IsOpen = false;
                PathBox.Focus();
                e.Handled = true;
                break;
        }
    }

    private void AcceptSelectedSuggestion()
    {
        if (SuggestionList.SelectedItem is string selected)
        {
            Text = selected;
            SuggestionPopup.IsOpen = false;
            PathBox.Focus();
            PathBox.CaretIndex = PathBox.Text.Length;
        }
    }
}
