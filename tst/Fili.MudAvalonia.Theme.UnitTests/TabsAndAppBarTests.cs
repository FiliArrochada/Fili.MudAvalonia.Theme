using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace Fili.MudAvalonia.Theme.UnitTests;

/// <summary>
/// MudTabs' Color, Border, Outlined, Rounded and Centered on TabControl and TabStrip, and MudAppBar's
/// Dense, Color and Gutters on <c>Border.appbar</c>. Values from _tabs.scss, _appbar.scss and
/// _toolbar.scss, defaults from MudTabs.razor.cs and MudAppBar.razor.cs.
/// </summary>
public class TabsAndAppBarTests
{
    private static T Named<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static TabControl Tabs(params string[] classes)
    {
        var tabs = new TabControl { Width = 560 };
        tabs.Classes.AddRange(classes);
        tabs.Items.Add(new TabItem { Header = "One", Content = "First" });
        tabs.Items.Add(new TabItem { Header = "Two", Content = "Second" });
        tabs.Items.Add(new TabItem { Header = "Off", IsEnabled = false });
        tabs.SelectedIndex = 0;
        return Matrix.Show(tabs);
    }

    private static TabStrip Strip(params string[] classes)
    {
        var strip = new TabStrip { Width = 560 };
        strip.Classes.AddRange(classes);
        strip.Items.Add(new TabStripItem { Content = "One" });
        strip.Items.Add(new TabStripItem { Content = "Two" });
        strip.SelectedIndex = 0;
        return Matrix.Show(strip);
    }

    private static TabItem Item(TabControl tabs, int index) => (TabItem)tabs.Items[index]!;

    // --------------------------------------------------------------------------------------
    // MudTabs defaults
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// MudTabs' defaults, each of which this theme used to differ on: a SURFACE bar with no rule
    /// (Border defaults to false), tabs that inherit text-primary rather than text-secondary, and
    /// MinimumTabWidth's 160px.
    /// </summary>
    [Fact]
    public Task AnUnclassedTabBarIsSurfaceWithNoRule() => UiThread.RunAsync(() =>
    {
        var tabs = Tabs();
        var bar = Named<Border>(tabs, "PART_TabBar");

        Assert.Equal(Matrix.Token("FiliSurfaceColor"), Matrix.Colour(bar.Background));
        Assert.Equal(default, bar.BorderThickness);
        Assert.Equal(Matrix.Token("FiliPrimaryColor"), Matrix.Colour(Item(tabs, 0).Foreground));
        Assert.Equal(Matrix.Token("FiliTextPrimaryColor"), Matrix.Colour(Item(tabs, 1).Foreground));
        Assert.Equal(Matrix.Token("FiliTextDisabledColor"), Matrix.Colour(Item(tabs, 2).Foreground));
        Assert.True(Item(tabs, 1).Bounds.Width >= 160);
    });

    [Fact]
    public Task TabHoverIsActionDefaultAndPrimaryWhenActive() => UiThread.RunAsync(() =>
    {
        var tabs = Tabs();

        Matrix.Hover(Item(tabs, 1));
        Assert.Equal(Matrix.Token("FiliActionDefaultHoverColor"), Matrix.Colour(Named<Border>(Item(tabs, 1), "PART_StateLayer").Background));

        Matrix.Hover(Item(tabs, 0));
        Assert.Equal(Matrix.Token("FiliPrimaryHoverColor"), Matrix.Colour(Named<Border>(Item(tabs, 0), "PART_StateLayer").Background));
    });

    [Fact]
    public Task BorderOutlinedAndRoundedShapeTheBar() => UiThread.RunAsync(() =>
    {
        var border = Named<Border>(Tabs("border"), "PART_TabBar");
        Assert.Equal(new Thickness(0, 0, 0, 1), border.BorderThickness);
        Assert.Equal(Matrix.Token("FiliLinesDefaultColor"), Matrix.Colour(border.BorderBrush));

        Assert.Equal(new Thickness(1), Named<Border>(Tabs("outlined"), "PART_TabBar").BorderThickness);

        var rounded = Named<Border>(Strip("rounded"), "PART_TabBar");
        Assert.Equal(new CornerRadius(4), rounded.CornerRadius);
        Assert.True(rounded.ClipToBounds);
    });

    [Fact]
    public Task CenteredCentresTheTabRow() => UiThread.RunAsync(() =>
    {
        var tabs = Tabs("centered");
        var row = Named<ItemsPresenter>(tabs, "PART_ItemsPresenter");

        Assert.Equal(HorizontalAlignment.Center, row.HorizontalAlignment);
        var left = row.TranslatePoint(default, tabs)!.Value.X;
        Assert.Equal(tabs.Bounds.Width - row.Bounds.Width, left * 2, 1);
    });

    [Fact]
    public Task HideSliderHidesTheIndicator() => UiThread.RunAsync(() =>
    {
        var tabs = Tabs("hide-slider", "primary");
        var strip = Strip("hide-slider");

        Assert.False(Named<Border>(Item(tabs, 0), "PART_Indicator").IsVisible);
        Assert.False(Named<Border>((TabStripItem)strip.Items[0]!, "PART_Indicator").IsVisible);
        Assert.True(Named<Border>(Item(Tabs(), 0), "PART_Indicator").IsVisible);
    });

    // --------------------------------------------------------------------------------------
    // MudTabs.Color
    // --------------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Matrix.Colours), MemberType = typeof(Matrix))]
    public Task AColouredBarCarriesItsContrastText(string colour) => UiThread.RunAsync(() =>
    {
        var tabs = Tabs(colour.ToLowerInvariant());
        var contrast = Matrix.Token($"Fili{colour}ContrastTextColor");

        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(Named<Border>(tabs, "PART_TabBar").Background));
        Assert.Equal(contrast, Matrix.Colour(Item(tabs, 0).Foreground));
        Assert.Equal(contrast, Matrix.Colour(Item(tabs, 1).Foreground));
        Assert.Equal(Matrix.Token("FiliTextDisabledColor"), Matrix.Colour(Item(tabs, 2).Foreground));
        Assert.Equal(contrast, Matrix.Colour(Named<Border>(Item(tabs, 0), "PART_Indicator").Background));

        Matrix.Hover(Item(tabs, 0));
        Assert.Equal(Matrix.Token($"Fili{colour}LightenColor"), Matrix.Colour(Named<Border>(Item(tabs, 0), "PART_StateLayer").Background));
    });

    [Theory]
    [MemberData(nameof(Matrix.Colours), MemberType = typeof(Matrix))]
    public Task ATabStripTakesTheSameColours(string colour) => UiThread.RunAsync(() =>
    {
        var strip = Strip(colour.ToLowerInvariant());
        var first = (TabStripItem)strip.Items[0]!;

        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(Named<Border>(strip, "PART_TabBar").Background));
        Assert.Equal(Matrix.Token($"Fili{colour}ContrastTextColor"), Matrix.Colour(first.Foreground));
        Assert.Equal(Matrix.Token($"Fili{colour}ContrastTextColor"), Matrix.Colour(((TabStripItem)strip.Items[1]!).Foreground));
        Assert.Equal(Matrix.Token($"Fili{colour}ContrastTextColor"), Matrix.Colour(Named<Border>(first, "PART_Indicator").Background));
    });

    /// <summary>
    /// The colour reaches the bar's OWN tabs only. A child selector, because a descendant one
    /// would also repaint the tabs of a TabControl nested in the content.
    /// </summary>
    [Fact]
    public Task ABarColourDoesNotReachNestedTabs() => UiThread.RunAsync(() =>
    {
        var inner = new TabControl();
        inner.Items.Add(new TabItem { Header = "Inner" });
        inner.Items.Add(new TabItem { Header = "Other" });
        inner.SelectedIndex = 0;

        var outer = new TabControl { Width = 560, Classes = { "primary" } };
        outer.Items.Add(new TabItem { Header = "Outer", Content = inner });
        outer.SelectedIndex = 0;
        Matrix.Show(outer);

        Assert.Equal(Matrix.Token("FiliTextPrimaryColor"), Matrix.Colour(((TabItem)inner.Items[1]!).Foreground));
        Assert.Equal(Matrix.Token("FiliSurfaceColor"), Matrix.Colour(Named<Border>(inner, "PART_TabBar").Background));
    });

    // --------------------------------------------------------------------------------------
    // MudAppBar
    // --------------------------------------------------------------------------------------

    [Fact]
    public Task AnAppBarHasGuttersAndADenseHeight() => UiThread.RunAsync(() =>
    {
        var bar = Matrix.Show(new Border { Classes = { "appbar" }, Width = 400, Child = new TextBlock { Text = "Title" } });
        var dense = Matrix.Show(new Border { Classes = { "appbar", "dense" }, Width = 400 });

        Assert.Equal(64, bar.Bounds.Height);
        Assert.Equal(new Thickness(24, 0), bar.Padding);
        Assert.Equal(48, dense.Bounds.Height);
    });

    [Theory]
    [MemberData(nameof(Matrix.Colours), MemberType = typeof(Matrix))]
    public Task AnAppBarColourIsTheGroundAndItsContrastText(string colour) => UiThread.RunAsync(() =>
    {
        var title = new TextBlock { Text = "Title" };
        var bar = Matrix.Show(new Border { Classes = { "appbar", colour.ToLowerInvariant() }, Width = 400, Child = title });

        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(bar.Background));
        Assert.Equal(Matrix.Token($"Fili{colour}ContrastTextColor"), Matrix.Colour(title.Foreground));
    });
}
