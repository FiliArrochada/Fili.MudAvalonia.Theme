using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Xunit;

namespace Fili.MudAvalonia.Theme.UnitTests;

/// <summary>
/// Tracks how close this theme is to standing on its own.
///
/// <para>
/// The question this answers: can the substrate be dropped? Every mature Avalonia theme —
/// Semi.Avalonia, Material.Avalonia, Classic.Avalonia — ships a complete ControlTheme set and
/// replaces FluentTheme outright, because layering means inheriting another design system's
/// shapes and paying a permanent tax in <c>:not()</c> guards and version churn.
/// </para>
///
/// <para>
/// The catch is the failure mode. Avalonia has NO implicit default theme, so a control with no
/// ControlTheme in scope does not look wrong — it has no template, measures to nothing and
/// renders NOTHING. Going standalone early does not degrade the app, it deletes controls from
/// it. That makes this a coverage threshold to reach, not a switch to flip, which is what these
/// tests measure.
/// </para>
/// </summary>
public class StandaloneReadinessTests
{
    /// <summary>
    /// Every ControlTheme this package ships. Adding one means adding it here, which keeps the
    /// coverage number honest and makes the list double as an inventory.
    /// </summary>
    internal static readonly string[] ThemeKeys =
    [
        "FiliFilledButton", "FiliTextButton", "FiliOutlinedButton",
        "FiliStandardTextBox", "FiliFilledTextBox", "FiliOutlinedTextBox",
        "FiliCheckBox", "FiliRadioButton", "FiliToggleSwitch",
        "FiliToggleButton", "FiliRepeatButton", "FiliHyperlinkButton",
        "FiliSplitButton", "FiliSplitButtonHalf", "FiliDropDownButton", "FiliChip",
        "FiliComboBox", "FiliFilledComboBox", "FiliOutlinedComboBox", "FiliComboBoxItem",
        "FiliListBox", "FiliListBoxItem",
        "FiliTreeView", "FiliTreeViewItem", "FiliTreeViewChevron",
        "FiliNumericUpDown", "FiliButtonSpinner", "FiliSpinnerButton",
        "FiliAutoCompleteBox",
        "FiliExpander", "FiliExpanderToggle",
        "FiliLabel", "FiliDataValidationErrors",
        "FiliSlider", "FiliTabControl", "FiliTabItem",
        "FiliTabStrip", "FiliTabStripItem", "FiliPipsPager",
        "FiliProgressBar", "FiliSeparator",
        "FiliScrollBar",
        "FiliMenu", "FiliMenuItem", "FiliTopLevelMenuItem", "FiliContextMenu",
        "FiliNotificationCard", "FiliWindowNotificationManager",
        "FiliToolTip", "FiliFlyoutPresenter", "FiliMenuFlyoutPresenter",
    ];

    /// <summary>
    /// The other fifty, each with the reason it is still wearing a forked Simple template.
    ///
    /// <para>
    /// This list exists because "fifty still forked" reads like a backlog and is not one. Every
    /// entry below was checked against MudBlazor rather than assumed, and the honest finding is
    /// that the control work is CLOSED: what remains is either framework plumbing with no design
    /// opinion to express, a bespoke MudBlazor component that would be a rewrite rather than a
    /// restyle, or a control MudBlazor simply does not have. Leaving them forked is the fork
    /// earning its keep.
    /// </para>
    /// <para>
    /// It is asserted exhaustive and mutually exclusive, so an Avalonia version that adds a
    /// control type fails here asking to be classified, instead of quietly joining an anonymous
    /// pile and making the coverage number mean less every release.
    /// </para>
    /// </summary>
    internal static readonly (string Reason, string[] Types)[] Unthemed =
    [
        // Verified by rendering both: identical desired size and descendant count to the type
        // they derive from, because StyleKeyOverride points their theme lookup at it. A theme of
        // their own would be a second copy of one that already applies.
        ("Inherits a themed type's template through StyleKeyOverride",
            ["MaskedTextBox", "ToggleSplitButton"]),

        // No chrome of their own - a presenter and nothing else. Theming them would mean
        // inventing a look for something that has never had one.
        ("Base and container types with nothing to paint",
            [
                "ContentControl", "HeaderedContentControl", "HeaderedItemsControl",
                "HeaderedSelectingItemsControl", "ItemsControl", "PageNavigationHost",
                "SelectingItemsControl", "TemplatedControl", "Thumb",
                "TransitioningContentControl", "UserControl",
            ]),

        // Built by the framework, never by an app author, and mostly invisible when correct.
        ("Framework-instantiated surfaces",
            [
                "EmbeddableControlRoot", "NativeMenuBar", "OverlayPopupHost", "PopupRoot",
                "TextSelectionHandle", "Window", "WindowBase",
            ]),

        // Avalonia's navigation shells. A design system has no opinion about a page host.
        ("Shell page types",
            ["CarouselPage", "ContentPage", "DrawerPage", "NavigationPage", "TabbedPage"]),

        // MudDrawer, and the worked example of the cheapest of the three ways to theme a control:
        // the forked template's structure is already right, so only values needed changing.
        ("Deliberately treated with a Style rather than a ControlTheme", ["SplitView"]),

        // Structural rather than decorative: get a part name wrong and it lays out perfectly and
        // does not scroll. Written once while layering, deleted when the fork made it redundant.
        ("Deliberately deleted", ["ScrollViewer"]),

        // Bespoke MudBlazor components. Matching MudDatePicker or MudTable means a rewrite per
        // template with no shortcut, and a real data grid is explicitly out of scope here.
        ("Out of scope: a rewrite, not a restyle",
            [
                "Calendar", "CalendarButton", "CalendarDatePicker", "CalendarDayButton",
                "CalendarItem", "DatePicker", "DatePickerPresenter", "TimePicker",
                "TimePickerPresenter", "TableView", "TableViewCell", "TableViewColumnHeader",
                "TableViewRow",
            ]),

        // Nothing in MudBlazor to transcribe. Carousel's visible chrome is icon buttons and
        // bullets, themed elsewhere; GroupBox's nearest relatives are MudCard and MudPaper, which
        // are different shapes; a command bar and pull-to-refresh have no counterpart at all.
        ("No MudBlazor counterpart",
            [
                "Carousel", "CommandBar", "CommandBarButton", "CommandBarSeparator",
                "CommandBarToggleButton", "GroupBox", "RefreshContainer", "RefreshVisualizer",
            ]),

        // MudIcon is the counterpart and the only divergence is size - it defaults to Size.Medium,
        // 1.5rem, where Simple's IconElementTheme* keys are 20px. Not worth acting on: every
        // template that uses PathIcon at the default size is itself permanently forked, so the
        // change would be invisible everywhere this theme supports.
        ("Counterpart exists, divergence not worth acting on", ["PathIcon"]),

        // MudSplitter's divider is TRANSPARENT: _splitpanel.scss gives it a cursor, a 12px
        // invisible hit area and a focus outline, and separation comes from the two panels having
        // different backgrounds. Avalonia guarantees no such contrast, so transcribing it exactly
        // would produce an invisible control - the same failure this package has already shipped
        // twice in other costumes. Simple's visible hairline is kept deliberately.
        ("Counterpart exists but transcribing it would make the control invisible",
            ["GridSplitter"]),
    ];

    [Fact]
    public Task EveryDeclaredThemeResolves() => UiThread.RunAsync(() =>
    {
        foreach (var key in ThemeKeys)
        {
            Assert.True(
                Application.Current!.TryFindResource(key, ThemeVariant.Light, out var value),
                $"{key} is listed in ThemeKeys but does not resolve.");

            Assert.IsType<ControlTheme>(value);
        }
    });

    /// <summary>
    /// Reports the gap to standalone, and fails if it moves without the list being updated.
    /// <para>
    /// The assertion is deliberately on the exact set rather than a count: a theme silently
    /// changing TargetType would keep the count and break the coverage claim.
    /// </para>
    /// </summary>
    [Fact]
    public Task CoverageMatchesTheDeclaredSet() => UiThread.RunAsync(() =>
    {
        var themed = ThemeKeys
            .Select(key =>
            {
                Application.Current!.TryFindResource(key, ThemeVariant.Light, out var value);
                return ((ControlTheme)value!).TargetType!;
            })
            .Distinct()
            .OrderBy(t => t.Name)
            .ToList();

        Assert.Equal(
            [
                "AutoCompleteBox", "Button", "ButtonSpinner", "CheckBox", "ComboBox",
                "ComboBoxItem", "ContextMenu", "DataValidationErrors", "DropDownButton", "Expander",
                "FlyoutPresenter", "HyperlinkButton", "Label", "ListBox", "ListBoxItem", "Menu",
                "MenuFlyoutPresenter", "MenuItem", "NotificationCard", "NumericUpDown",
                "PipsPager", "ProgressBar", "RadioButton", "RepeatButton", "ScrollBar",
                "Separator", "Slider", "SplitButton", "TabControl", "TabItem", "TabStrip",
                "TabStripItem",
                "TextBox", "ToggleButton", "ToggleSwitch", "ToolTip", "TreeView", "TreeViewItem",
                "WindowNotificationManager",
            ],
            themed.Select(t => t.Name).ToArray());

        // THE NUMBER THAT SAYS HOW MUCH OF THE FORK IS STILL DOING THE WORK. 39 of 89 templated
        // control types are hand-written here; the other 50 are still wearing the forked Simple
        // templates from Themes/Base. Pinned rather than merely reported, so it cannot drift
        // unnoticed and so an Avalonia version that adds controls surfaces as a failure here.
        var templated = TemplatedControlTypes();

        Assert.Equal(39, themed.Count);
        Assert.Equal(89, templated.Count);
    });

    /// <summary>
    /// The inventory is complete and says each type once.
    ///
    /// <para>
    /// This is what turns the coverage number from a claim into a fact. Without it, "39 of 89"
    /// only says how many themes exist; with it, every one of the other fifty has a stated reason
    /// for not being one, and a control type added by a future Avalonia fails here by name rather
    /// than by making 89 into 90.
    /// </para>
    /// </summary>
    [Fact]
    public Task EveryTemplatedTypeIsThemedOrClassified() => UiThread.RunAsync(() =>
    {
        var classified = Unthemed.SelectMany(group => group.Types).ToList();

        Assert.Equal(classified.Count, classified.Distinct().Count());

        var accounted = ThemedTypeNames().Concat(classified).OrderBy(n => n, StringComparer.Ordinal);
        var templated = TemplatedControlTypes()
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal);

        Assert.Equal(templated, accounted);
    });

    private static IEnumerable<string> ThemedTypeNames() =>
        ThemeKeys
            .Select(key =>
            {
                Application.Current!.TryFindResource(key, ThemeVariant.Light, out var value);
                return ((ControlTheme)value!).TargetType!.Name;
            })
            .Distinct();

    private static List<Type> TemplatedControlTypes() =>
        typeof(Button).Assembly
            .GetTypes()
            .Where(t => t.IsPublic
                && !t.IsAbstract
                && !t.IsGenericTypeDefinition
                && typeof(TemplatedControl).IsAssignableFrom(t))
            .ToList();
}
