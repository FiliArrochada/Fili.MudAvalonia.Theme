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
/// MudButton's Variant x Color x Size matrix, transcribed from _button.scss.
///
/// <para>
/// Colour is only a colour, as <c>Color</c> is in MudBlazor: <c>primary</c> on its own is a
/// primary TEXT button. Each test states the rule for one variant once and checks it for every
/// colour, because the failure this guards against is one colour's block quietly differing from
/// the others - a typo in a key resolves to nothing and leaves the default in place.
/// </para>
/// </summary>
public class ButtonMatrixTests
{
    public static readonly TheoryData<string> Colours =
        ["Primary", "Secondary", "Tertiary", "Info", "Success", "Warning", "Error", "Dark"];

    [Theory]
    [MemberData(nameof(Colours))]
    public Task TextButtonIsTheColourAndTintsWithItsHover(string colour) => UiThread.RunAsync(() =>
    {
        var button = Show(colour.ToLowerInvariant());

        Assert.Equal(Token($"Fili{colour}Color"), Colour(button.Foreground));
        Assert.Equal(Colors.Transparent, Colour(Part(button, "PART_Root").Background));

        Hover(button);
        Assert.Equal(Token($"Fili{colour}HoverColor"), Colour(Part(button, "PART_StateLayer").Background));
    });

    [Theory]
    [MemberData(nameof(Colours))]
    public Task OutlinedButtonAddsALineInTheColour(string colour) => UiThread.RunAsync(() =>
    {
        var button = Show("outlined", colour.ToLowerInvariant());

        Assert.Equal(Token($"Fili{colour}Color"), Colour(button.Foreground));
        Assert.Equal(Token($"Fili{colour}Color"), Colour(Part(button, "PART_Root").BorderBrush));
        Assert.Equal(new Thickness(1), Part(button, "PART_Root").BorderThickness);

        Hover(button);
        Assert.Equal(Token($"Fili{colour}HoverColor"), Colour(Part(button, "PART_StateLayer").Background));
    });

    [Theory]
    [MemberData(nameof(Colours))]
    public Task FilledButtonFillsAndDarkensOnHover(string colour) => UiThread.RunAsync(() =>
    {
        var button = Show("filled", colour.ToLowerInvariant());
        var root = Part(button, "PART_Root");

        Assert.Equal(Token($"Fili{colour}Color"), Colour(root.Background));
        Assert.Equal(Token($"Fili{colour}ContrastTextColor"), Colour(button.Foreground));

        Hover(button);
        Assert.Equal(Token($"Fili{colour}DarkenColor"), Colour(root.Background));
        Assert.Equal(Colors.Transparent, Colour(Part(button, "PART_StateLayer").Background));
    });

    /// <summary>Color.Default: text-primary, and action-default-hover as the tint.</summary>
    [Theory]
    [InlineData("text")]
    [InlineData("outlined")]
    public Task NoColourIsTextPrimary(string variant) => UiThread.RunAsync(() =>
    {
        var button = Show(variant);

        Assert.Equal(Token("FiliTextPrimaryColor"), Colour(button.Foreground));

        if (variant == "outlined")
        {
            Assert.Equal(Token("FiliTextPrimaryColor"), Colour(Part(button, "PART_Root").BorderBrush));
        }

        Hover(button);
        Assert.Equal(Token("FiliActionDefaultHoverColor"), Colour(Part(button, "PART_StateLayer").Background));
    });

    [Fact]
    public Task FilledWithNoColourIsGrey() => UiThread.RunAsync(() =>
    {
        var button = Show("filled");
        var root = Part(button, "PART_Root");

        Assert.Equal(Token("FiliActionDefaultHoverColor"), Colour(root.Background));
        Assert.Equal(Token("FiliTextPrimaryColor"), Colour(button.Foreground));

        Hover(button);
        Assert.Equal(Token("FiliActionDisabledBackgroundColor"), Colour(root.Background));
    });

    /// <summary>
    /// An unclassed Button is MudButton with every parameter at its default: Variant.Text and
    /// Color.Default. It used to be primary text, which is Material's default and not MudBlazor's.
    /// </summary>
    [Fact]
    public Task AnUnclassedButtonIsTheDefaultTextButton() => UiThread.RunAsync(() =>
    {
        var button = Show();

        Assert.Equal(Token("FiliTextPrimaryColor"), Colour(button.Foreground));
        Assert.Equal(new Thickness(8, 6), button.Padding);
    });

    /// <summary>
    /// Color.Inherit is <c>color: inherit</c>: the button takes the colour of whatever it sits in,
    /// which is how an icon button reads correctly on an app bar. Outlined's line follows it.
    /// </summary>
    [Theory]
    [InlineData("text")]
    [InlineData("outlined")]
    [InlineData("filled")]
    public Task InheritTakesTheSurroundingTextColour(string variant) => UiThread.RunAsync(() =>
    {
        var button = new Button { Content = "Inherit", Classes = { variant, "inherit" } };
        var host = new Border { Child = button };
        host.SetValue(TextElement.ForegroundProperty, Brushes.OrangeRed);

        Mount(host);

        Assert.Equal(Colors.OrangeRed, Colour(button.Foreground));

        if (variant == "outlined")
        {
            Assert.Equal(Colors.OrangeRed, Colour(Part(button, "PART_Root").BorderBrush));
        }
    });

    /// <summary>The padding and type of each size, per variant, as _button.scss gives them.</summary>
    [Theory]
    [InlineData("text", "small", 5, 4, 13)]
    [InlineData("text", "large", 11, 8, 15)]
    [InlineData("outlined", "small", 9, 3, 13)]
    [InlineData("outlined", "large", 21, 7, 15)]
    [InlineData("filled", "small", 10, 4, 13)]
    [InlineData("filled", "large", 22, 8, 15)]
    public Task SizesFollowTheVariant(string variant, string size, double x, double y, double fontSize) =>
        UiThread.RunAsync(() =>
        {
            var button = Show(variant, size);

            Assert.Equal(new Thickness(x, y), button.Padding);
            Assert.Equal(fontSize, button.FontSize);
        });

    /// <summary>
    /// A filled button's text must stay readable on the darker fill it switches to on hover, at
    /// the 7:1 the variant holds every other filled colour to.
    /// </summary>
    [Theory]
    [MemberData(nameof(Colours))]
    public Task HighContrastTextIsReadableOnTheHoverFill(string colour) => UiThread.RunAsync(() =>
    {
        var variant = FiliThemeVariants.HighContrast;
        var ratio = Contrast(Token($"Fili{colour}DarkenColor", variant), Token($"Fili{colour}ContrastTextColor", variant));

        Assert.True(ratio >= 7, $"Text on Fili{colour}DarkenColor is {ratio:N1}:1 in high contrast.");
    });

    private static Button Show(params string[] classes)
    {
        var button = new Button { Content = "Button" };
        button.Classes.AddRange(classes);

        Mount(button);

        return button;
    }

    private static Window Mount(Control content)
    {
        var window = new Window
        {
            Content = new StackPanel { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Children = { content } },
            Width = 400,
            Height = 200,
        };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }

    /// <summary>A real pointer move, so :pointerover is set the way it is in an app.</summary>
    private static void Hover(Button button)
    {
        var window = (Window)TopLevel.GetTopLevel(button)!;
        var centre = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;

        window.MouseMove(centre, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.True(button.IsPointerOver, "The pointer move did not reach the button.");
    }

    private static Border Part(Button button, string name) =>
        button.GetVisualDescendants().OfType<Border>().Single(b => b.Name == name);

    private static Color Colour(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static Color Token(string key, ThemeVariant? variant = null)
    {
        Assert.True(
            Application.Current!.TryFindResource(key, variant ?? ThemeVariant.Light, out var value),
            $"{key} did not resolve.");

        return Assert.IsType<Color>(value);
    }

    private static double Contrast(Color a, Color b)
    {
        static double Linear(byte channel)
        {
            var c = channel / 255d;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        static double L(Color c) => (0.2126 * Linear(c.R)) + (0.7152 * Linear(c.G)) + (0.0722 * Linear(c.B));

        var (x, y) = (L(a), L(b));
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }
}
