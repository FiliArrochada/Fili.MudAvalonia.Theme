using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;
using Shapes = Avalonia.Controls.Shapes;

namespace Fili.MudAvalonia.Theme.UnitTests;

/// <summary>
/// MudBlazor's Color and Size on the controls beyond Button, each rule checked for every colour.
/// The values are from _menu / _checkbox / _radio / _switch / _slider and the components' own
/// parameter defaults - which is where most of the surprises were: MudMenu, MudCheckBox, MudRadio
/// and MudSwitch all default to Color.DEFAULT, not primary, while MudSlider and MudLink do default
/// to primary.
/// </summary>
public static class Matrix
{
    public static readonly TheoryData<string> Colours =
        ["Primary", "Secondary", "Tertiary", "Info", "Success", "Warning", "Error", "Dark"];

    public static T Show<T>(T control) where T : Control
    {
        var window = new Window
        {
            Content = new StackPanel { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Children = { control } },
            Width = 600,
            Height = 300,
        };

        window.Show();
        Dispatcher.UIThread.RunJobs();
        return control;
    }

    /// <summary>A real pointer move onto the centre of <paramref name="target"/>.</summary>
    public static void Hover(Control target)
    {
        var window = (Window)TopLevel.GetTopLevel(target)!;
        var centre = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window)!.Value;

        window.MouseMove(new Point(window.Width - 1, window.Height - 1), RawInputModifiers.None);
        window.MouseMove(centre, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.True(target.IsPointerOver, $"The pointer move did not reach the {target.GetType().Name}.");
    }

    public static TPart Part<TPart>(Control control, string name) where TPart : Control =>
        control.GetVisualDescendants().OfType<TPart>().First(p => p.Name == name);

    public static Color Colour(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    public static Color Token(string key)
    {
        Assert.True(Application.Current!.TryFindResource(key, ThemeVariant.Light, out var value), $"{key} did not resolve.");
        return Assert.IsType<Color>(value);
    }
}

/// <summary>DropDownButton is a MudMenu with a button activator: MudButton's matrix.</summary>
public class DropDownButtonMatrixTests
{
    [Fact]
    public Task NoClassIsTextPrimary() => UiThread.RunAsync(() =>
    {
        var button = Matrix.Show(new DropDownButton { Content = "Sort by" });

        Assert.Equal(Matrix.Token("FiliTextPrimaryColor"), Matrix.Colour(button.Foreground));
        Assert.Equal(Matrix.Token("FiliTextPrimaryColor"), Matrix.Colour(Matrix.Part<Shapes.Path>(button, "DropDownGlyph").Fill));

        Matrix.Hover(button);
        Assert.Equal(Matrix.Token("FiliActionDefaultHoverColor"), Matrix.Colour(Matrix.Part<Border>(button, "PART_StateLayer").Background));
    });

    [Theory]
    [MemberData(nameof(Matrix.Colours), MemberType = typeof(Matrix))]
    public Task TextOutlinedAndFilledFollowTheColour(string colour) => UiThread.RunAsync(() =>
    {
        var cls = colour.ToLowerInvariant();

        var text = Matrix.Show(new DropDownButton { Content = "Menu", Classes = { cls } });
        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(text.Foreground));
        Matrix.Hover(text);
        Assert.Equal(Matrix.Token($"Fili{colour}HoverColor"), Matrix.Colour(Matrix.Part<Border>(text, "PART_StateLayer").Background));

        var outlined = Matrix.Show(new DropDownButton { Content = "Menu", Classes = { "outlined", cls } });
        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(Matrix.Part<Border>(outlined, "RootBorder").BorderBrush));
        Assert.Equal(new Thickness(1), Matrix.Part<Border>(outlined, "RootBorder").BorderThickness);

        var filled = Matrix.Show(new DropDownButton { Content = "Menu", Classes = { "filled", cls } });
        var root = Matrix.Part<Border>(filled, "RootBorder");
        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(root.Background));
        Assert.Equal(Matrix.Token($"Fili{colour}ContrastTextColor"), Matrix.Colour(filled.Foreground));
        Matrix.Hover(filled);
        Assert.Equal(Matrix.Token($"Fili{colour}DarkenColor"), Matrix.Colour(root.Background));
    });

    [Theory]
    [InlineData("text", "small", 5, 4)]
    [InlineData("outlined", "large", 21, 7)]
    [InlineData("filled", "small", 10, 4)]
    public Task SizesFollowTheVariant(string variant, string size, double x, double y) => UiThread.RunAsync(() =>
    {
        var button = Matrix.Show(new DropDownButton { Content = "Menu", Classes = { variant, size } });

        Assert.Equal(new Thickness(x, y), button.Padding);
    });
}

/// <summary>
/// MudCheckBox and MudRadio. Color.Default is action-default, checked or not; a colour class paints
/// every glyph and the halo; Size scales MudIcon's 24px to 20 or 36.
/// </summary>
public class SelectionGlyphMatrixTests
{
    [Theory]
    [InlineData(typeof(CheckBox))]
    [InlineData(typeof(RadioButton))]
    public Task NoClassIsActionDefaultInEveryState(Type type) => UiThread.RunAsync(() =>
    {
        var control = Matrix.Show((ToggleButton)Activator.CreateInstance(type)!);

        Assert.Equal(Matrix.Token("FiliActionDefaultColor"), Matrix.Colour(Matrix.Part<Shapes.Path>(control, "PART_Unchecked").Fill));
        Assert.Equal(Matrix.Token("FiliActionDefaultColor"), Matrix.Colour(Matrix.Part<Shapes.Path>(control, "PART_Checked").Fill));

        Matrix.Hover(control);
        Assert.Equal(Matrix.Token("FiliActionDefaultHoverColor"), Matrix.Colour(Matrix.Part<Shapes.Ellipse>(control, "PART_StateLayer").Fill));
    });

    [Theory]
    [MemberData(nameof(Matrix.Colours), MemberType = typeof(Matrix))]
    public Task AColourPaintsEveryGlyphAndTheHalo(string colour) => UiThread.RunAsync(() =>
    {
        foreach (var control in new ToggleButton[] { new CheckBox(), new RadioButton() })
        {
            control.Classes.Add(colour.ToLowerInvariant());
            Matrix.Show(control);

            // UncheckedColor is null in MudBlazor, so the colour reaches the unchecked glyph too.
            Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(Matrix.Part<Shapes.Path>(control, "PART_Unchecked").Fill));
            Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(Matrix.Part<Shapes.Path>(control, "PART_Checked").Fill));

            Matrix.Hover(control);
            Assert.Equal(Matrix.Token($"Fili{colour}HoverColor"), Matrix.Colour(Matrix.Part<Shapes.Ellipse>(control, "PART_StateLayer").Fill));
        }
    });

    [Theory]
    [InlineData("small", 20)]
    [InlineData("large", 36)]
    public Task SizeScalesTheGlyph(string size, double glyph) => UiThread.RunAsync(() =>
    {
        var box = Matrix.Show(new CheckBox { Classes = { size } });
        var scale = Matrix.Part<LayoutTransformControl>(box, "PART_Scale");

        Assert.Equal(glyph, 24 * scale.LayoutTransform!.Value.M11, 2);
    });
}

/// <summary>
/// MudSwitch. The #fafafa thumb is the thumb of every switch with no colour, on and off; a colour
/// paints thumb and track only when ON.
/// </summary>
public class SwitchMatrixTests
{
    [Fact]
    public Task NoClassKeepsTheLightThumbOnAndOff() => UiThread.RunAsync(() =>
    {
        var off = Matrix.Show(new ToggleSwitch());
        var on = Matrix.Show(new ToggleSwitch { IsChecked = true });

        Assert.Equal(Matrix.Token("FiliSwitchThumbColor"), Matrix.Colour(Matrix.Part<Border>(off, "PART_Thumb").Background));
        Assert.Equal(Matrix.Token("FiliSwitchThumbColor"), Matrix.Colour(Matrix.Part<Border>(on, "PART_Thumb").Background));
    });

    [Theory]
    [MemberData(nameof(Matrix.Colours), MemberType = typeof(Matrix))]
    public Task AColourPaintsThumbAndTrackOnlyWhenOn(string colour) => UiThread.RunAsync(() =>
    {
        var cls = colour.ToLowerInvariant();
        var off = Matrix.Show(new ToggleSwitch { Classes = { cls } });
        var on = Matrix.Show(new ToggleSwitch { IsChecked = true, Classes = { cls } });

        Assert.Equal(Matrix.Token("FiliSwitchThumbColor"), Matrix.Colour(Matrix.Part<Border>(off, "PART_Thumb").Background));
        Assert.Equal(Matrix.Token("FiliActionDefaultColor"), Matrix.Colour(Matrix.Part<Border>(off, "PART_Track").Background));

        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(Matrix.Part<Border>(on, "PART_Thumb").Background));
        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(Matrix.Part<Border>(on, "PART_Track").Background));
    });

    [Theory]
    [InlineData("small", 30, 10, 14)]
    [InlineData("large", 38, 18, 26)]
    public Task SizesFollowTheSwitchSpan(string size, double trackWidth, double trackHeight, double thumb) => UiThread.RunAsync(() =>
    {
        var toggle = Matrix.Show(new ToggleSwitch { Classes = { size } });

        Assert.Equal(trackWidth, Matrix.Part<Border>(toggle, "PART_Track").Width);
        Assert.Equal(trackHeight, Matrix.Part<Border>(toggle, "PART_Track").Height);
        Assert.Equal(thumb, Matrix.Part<Border>(toggle, "PART_Thumb").Width);
    });
}

/// <summary>
/// MudSlider. It defaults to Color.Primary and Size.Small - a 2px rail and a 12px thumb - and
/// rings the thumb on hover rather than growing it.
/// </summary>
public class SliderMatrixTests
{
    [Fact]
    public Task NoClassIsASmallPrimarySlider() => UiThread.RunAsync(() =>
    {
        var slider = Matrix.Show(new Slider { Width = 200, Value = 40 });

        Assert.Equal(Matrix.Token("FiliPrimaryColor"), Matrix.Colour(slider.Foreground));
        Assert.Equal(2, Matrix.Part<RepeatButton>(slider, "PART_DecreaseButton").Height);
        Assert.Equal(0.30, Matrix.Part<RepeatButton>(slider, "PART_IncreaseButton").Opacity, 2);
        Assert.Equal(new Thickness(14), Matrix.Part<Thumb>(slider, "PART_Thumb").Padding);
    });

    [Theory]
    [MemberData(nameof(Matrix.Colours), MemberType = typeof(Matrix))]
    public Task AColourPaintsTheThumbAndTheRail(string colour) => UiThread.RunAsync(() =>
    {
        var slider = Matrix.Show(new Slider { Width = 200, Value = 40, Classes = { colour.ToLowerInvariant() } });

        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(Matrix.Part<Thumb>(slider, "PART_Thumb").Background));
        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(Matrix.Part<RepeatButton>(slider, "PART_DecreaseButton").Background));
    });

    [Theory]
    [InlineData("medium", 4, 10)]
    [InlineData("large", 6, 8)]
    public Task SizesSetTheRailAndTheThumb(string size, double rail, double inset) => UiThread.RunAsync(() =>
    {
        var slider = Matrix.Show(new Slider { Width = 200, Value = 40, Classes = { size } });

        Assert.Equal(rail, Matrix.Part<RepeatButton>(slider, "PART_DecreaseButton").Height);
        Assert.Equal(new Thickness(inset), Matrix.Part<Thumb>(slider, "PART_Thumb").Padding);
    });

    /// <summary>
    /// The rail is continuous under the thumb, as MudBlazor's native range input draws it. Each
    /// half reaches 20px - half the thumb's hit area - toward the thumb; without that, a gap showed
    /// on both sides of the knob.
    /// </summary>
    [Fact]
    public Task TheRailHalvesMeetUnderTheThumb() => UiThread.RunAsync(() =>
    {
        var slider = Matrix.Show(new Slider { Width = 200, Value = 40 });
        var decrease = Matrix.Part<RepeatButton>(slider, "PART_DecreaseButton");
        var increase = Matrix.Part<RepeatButton>(slider, "PART_IncreaseButton");
        var thumb = Matrix.Part<Thumb>(slider, "PART_Thumb");

        var centre = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, 0), slider)!.Value.X;
        var decreaseEnd = decrease.TranslatePoint(new Point(decrease.Bounds.Width, 0), slider)!.Value.X;
        var increaseStart = increase.TranslatePoint(default, slider)!.Value.X;

        Assert.Equal(centre, decreaseEnd, 1);
        Assert.Equal(centre, increaseStart, 1);
    });

    /// <summary>The 1px ring: on hover the ring's inset is one pixel less than the knob's.</summary>
    [Fact]
    public Task HoverRingsTheThumbByOnePixel() => UiThread.RunAsync(() =>
    {
        var slider = Matrix.Show(new Slider { Width = 200, Value = 40 });
        var thumb = Matrix.Part<Thumb>(slider, "PART_Thumb");

        Matrix.Hover(thumb);

        Assert.Equal(new Thickness(13), thumb.BorderThickness);
    });
}

/// <summary>ProgressBar now takes every palette colour, tertiary and dark included.</summary>
public class ProgressColourTests
{
    [Theory]
    [MemberData(nameof(Matrix.Colours), MemberType = typeof(Matrix))]
    public Task AColourPaintsTheBar(string colour) => UiThread.RunAsync(() =>
    {
        var bar = Matrix.Show(new ProgressBar { Width = 200, Value = 50, Classes = { colour.ToLowerInvariant() } });

        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(bar.Foreground));
    });
}

/// <summary>MudLink's Color.Inherit is `color: inherit`, now bound rather than approximated.</summary>
public class LinkInheritTests
{
    [Fact]
    public Task InheritTakesTheSurroundingTextColour() => UiThread.RunAsync(() =>
    {
        var link = new HyperlinkButton { Content = "Link", Classes = { "inherit" } };
        var host = new Border { Child = link };
        host.SetValue(TextElement.ForegroundProperty, Brushes.OrangeRed);

        Matrix.Show(host);

        Assert.Equal(Colors.OrangeRed, Matrix.Colour(link.Foreground));
    });
}
