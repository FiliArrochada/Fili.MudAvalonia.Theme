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
    private static readonly string[] ThemeKeys =
    [
        "FiliContainedButton", "FiliTextButton", "FiliOutlinedButton",
        "FiliStandardTextBox", "FiliFilledTextBox", "FiliOutlinedTextBox",
        "FiliCheckBox", "FiliRadioButton", "FiliToggleSwitch",
        "FiliToggleButton", "FiliRepeatButton", "FiliHyperlinkButton",
        "FiliSplitButton", "FiliSplitButtonHalf", "FiliDropDownButton",
        "FiliComboBox", "FiliComboBoxItem",
        "FiliListBox", "FiliListBoxItem",
        "FiliTreeView", "FiliTreeViewItem", "FiliTreeViewChevron",
        "FiliNumericUpDown", "FiliButtonSpinner", "FiliSpinnerButton",
        "FiliAutoCompleteBox",
        "FiliExpander", "FiliExpanderToggle",
        "FiliLabel", "FiliDataValidationErrors",
        "FiliSlider", "FiliTabControl", "FiliTabItem",
        "FiliProgressBar", "FiliSeparator",
        "FiliScrollBar",
        "FiliMenu", "FiliMenuItem", "FiliTopLevelMenuItem", "FiliContextMenu",
        "FiliNotificationCard", "FiliWindowNotificationManager",
        "FiliToolTip", "FiliFlyoutPresenter", "FiliMenuFlyoutPresenter",
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
                "ComboBoxItem", "ContextMenu", "DataValidationErrors", "DropDownButton",
                "Expander",
                "FlyoutPresenter", "HyperlinkButton", "Label", "ListBox", "ListBoxItem", "Menu",
                "MenuFlyoutPresenter", "MenuItem", "NotificationCard", "NumericUpDown",
                "ProgressBar", "RadioButton",
                "RepeatButton", "ScrollBar", "Separator", "Slider", "SplitButton", "TabControl",
                "TabItem",
                "TextBox", "ToggleButton", "ToggleSwitch", "ToolTip", "TreeView", "TreeViewItem",
                "WindowNotificationManager",
            ],
            themed.Select(t => t.Name).ToArray());

        // THE NUMBER THAT SAYS HOW MUCH OF THE FORK IS STILL DOING THE WORK. 36 of 89 templated
        // control types are hand-written here; the other 53 are still wearing the forked Simple
        // templates from Themes/Base. Pinned rather than merely reported, so it cannot drift
        // unnoticed and so an Avalonia version that adds controls surfaces as a failure here.
        var templated = TemplatedControlTypes();

        Assert.Equal(36, themed.Count);
        Assert.Equal(89, templated.Count);
    });

    private static List<Type> TemplatedControlTypes() =>
        typeof(Button).Assembly
            .GetTypes()
            .Where(t => t.IsPublic
                && !t.IsAbstract
                && !t.IsGenericTypeDefinition
                && typeof(TemplatedControl).IsAssignableFrom(t))
            .ToList();
}
