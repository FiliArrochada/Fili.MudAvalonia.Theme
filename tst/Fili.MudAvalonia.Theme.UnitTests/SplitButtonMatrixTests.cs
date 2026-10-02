using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Fili.MudAvalonia.Theme.UnitTests;

/// <summary>
/// SplitButton is MudButtonGroup, with Button's classes: text by default, <c>outlined</c>,
/// <c>filled</c>, a colour class that is only a colour, and the sizes. Values from
/// _buttongroup.scss.
///
/// <para>
/// It used to treat <c>primary</c> as "filled primary", the one place in the theme where a colour
/// class also chose the variant. These tests are what keep it on the same rule as Button.
/// </para>
/// </summary>
public class SplitButtonMatrixTests
{
    public static readonly TheoryData<string> Colours =
        ["Primary", "Secondary", "Tertiary", "Info", "Success", "Warning", "Error", "Dark"];

    [Theory]
    [MemberData(nameof(Colours))]
    public Task AColourAloneIsATextGroupInThatColour(string colour) => UiThread.RunAsync(() =>
    {
        var split = Show(colour.ToLowerInvariant());

        Assert.Equal(Token($"Fili{colour}Color"), Colour(split.Foreground));
        Assert.Equal(Token($"Fili{colour}Color"), Colour(Separator(split).Background));
        Assert.Equal(Colors.Transparent, Colour(Half(split, "PART_PrimaryButton").Background));
        Assert.Equal(new Thickness(0), Root(split).BorderThickness);

        Hover(Half(split, "PART_PrimaryButton"));
        Assert.Equal(Token($"Fili{colour}HoverColor"), Colour(StateLayer(Half(split, "PART_PrimaryButton")).Background));
    });

    [Theory]
    [MemberData(nameof(Colours))]
    public Task OutlinedDrawsTheColourRoundBothHalves(string colour) => UiThread.RunAsync(() =>
    {
        var split = Show("outlined", colour.ToLowerInvariant());

        Assert.Equal(new Thickness(1), Root(split).BorderThickness);
        Assert.Equal(Token($"Fili{colour}Color"), Colour(Root(split).BorderBrush));
        Assert.Equal(Token($"Fili{colour}Color"), Colour(Separator(split).Background));
        Assert.Equal(new Thickness(15, 5), split.Padding);
    });

    [Theory]
    [MemberData(nameof(Colours))]
    public Task FilledFillsDividesWithLightenAndDarkensOnHover(string colour) => UiThread.RunAsync(() =>
    {
        var split = Show("filled", colour.ToLowerInvariant());
        var main = Half(split, "PART_PrimaryButton");

        Assert.Equal(Token($"Fili{colour}Color"), Colour(main.Background));
        Assert.Equal(Token($"Fili{colour}ContrastTextColor"), Colour(split.Foreground));
        Assert.Equal(Token($"Fili{colour}LightenColor"), Colour(Separator(split).Background));

        Hover(main);
        Assert.Equal(Token($"Fili{colour}DarkenColor"), Colour(main.Background));
    });

    [Fact]
    public Task NoClassIsATextGroupInTextPrimary() => UiThread.RunAsync(() =>
    {
        var split = Show();

        Assert.Equal(Token("FiliTextPrimaryColor"), Colour(split.Foreground));
        Assert.Equal(Token("FiliTextPrimaryColor"), Colour(Separator(split).Background));

        Hover(Half(split, "PART_PrimaryButton"));
        Assert.Equal(Token("FiliActionDefaultHoverColor"), Colour(StateLayer(Half(split, "PART_PrimaryButton")).Background));
    });

    [Fact]
    public Task FilledWithNoColourIsGreyWithADividerLine() => UiThread.RunAsync(() =>
    {
        var split = Show("filled");

        Assert.Equal(Token("FiliActionDefaultHoverColor"), Colour(Half(split, "PART_PrimaryButton").Background));
        Assert.Equal(Token("FiliDividerColor"), Colour(Separator(split).Background));
    });

    [Theory]
    [InlineData("text", "small", 5, 4)]
    [InlineData("outlined", "large", 21, 7)]
    [InlineData("filled", "small", 10, 4)]
    public Task SizesFollowTheVariant(string variant, string size, double x, double y) => UiThread.RunAsync(() =>
    {
        var split = Show(variant, size);

        Assert.Equal(new Thickness(x, y), split.Padding);
    });

    private static SplitButton Show(params string[] classes)
    {
        var split = new SplitButton { Content = "Deploy" };
        split.Classes.AddRange(classes);

        var window = new Window
        {
            Content = new StackPanel { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Children = { split } },
            Width = 400,
            Height = 200,
        };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        return split;
    }

    private static void Hover(Control target)
    {
        var window = (Window)TopLevel.GetTopLevel(target)!;
        var centre = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window)!.Value;

        // Away first: the headless pointer keeps its position between tests, and a move to where
        // it already is raises nothing.
        window.MouseMove(new Point(window.Width - 1, window.Height - 1), RawInputModifiers.None);
        window.MouseMove(centre, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        // The centre is on the label. That spot used to fall straight through to the window,
        // because the half's template root had no background - so hovering, and pointing at, the
        // label of a split button reached nothing. This assertion is what keeps it fixed.
        Assert.True(target.IsPointerOver, "The pointer over the half's label did not reach the half.");
    }

    private static Border Root(SplitButton split) =>
        split.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Root");

    private static Border Separator(SplitButton split) =>
        split.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "SeparatorBorder");

    private static Button Half(SplitButton split, string name) =>
        split.GetVisualDescendants().OfType<Button>().Single(b => b.Name == name);

    private static Border StateLayer(Button half) =>
        half.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_StateLayer");

    private static Color Colour(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static Color Token(string key)
    {
        Assert.True(Application.Current!.TryFindResource(key, ThemeVariant.Light, out var value), $"{key} did not resolve.");
        return Assert.IsType<Color>(value);
    }
}

/// <summary>
/// MudText's Color. A colour class is the palette colour on text as everywhere else -
/// <c>secondary</c> is the secondary colour, not grey; it used to be text-secondary.
/// </summary>
public class TextColourTests
{
    [Theory]
    [MemberData(nameof(SplitButtonMatrixTests.Colours), MemberType = typeof(SplitButtonMatrixTests))]
    public Task AColourClassPaintsThePaletteColour(string colour) => UiThread.RunAsync(() =>
    {
        var text = new TextBlock { Text = "Text", Classes = { "body2", colour.ToLowerInvariant() } };
        var window = new Window { Content = text };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.True(Application.Current!.TryFindResource($"Fili{colour}Color", ThemeVariant.Light, out var expected));
        Assert.Equal((Color)expected!, Assert.IsAssignableFrom<ISolidColorBrush>(text.Foreground).Color);
    });

    /// <summary>A colour beats a type style's own default colour, which overline has.</summary>
    [Fact]
    public Task AColourBeatsTheOverlinesGrey() => UiThread.RunAsync(() =>
    {
        var text = new TextBlock { Text = "Text", Classes = { "overline", "error" } };
        var window = new Window { Content = text };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.True(Application.Current!.TryFindResource("FiliErrorColor", ThemeVariant.Light, out var expected));
        Assert.Equal((Color)expected!, Assert.IsAssignableFrom<ISolidColorBrush>(text.Foreground).Color);
    });
}
