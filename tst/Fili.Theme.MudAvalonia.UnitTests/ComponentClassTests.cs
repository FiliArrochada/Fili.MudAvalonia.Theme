using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Xunit;

namespace Fili.Theme.MudAvalonia.UnitTests;

/// <summary>
/// MudBlazor components that are a CLASS on a control Avalonia already has, not new controls:
/// MudIconButton (<c>Button.icon</c>), MudChip (<c>Button.chip</c>, <c>ToggleButton.chip</c>),
/// MudAlert (<c>Border.alert</c>), MudSkeleton (<c>Border.skeleton</c>), MudDivider.Light
/// (<c>Separator.light</c>) and DropShadow="false" (<c>flat</c>).
/// </summary>
public class ComponentClassTests
{
    // --------------------------------------------------------------------------------------
    // MudIconButton
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// Round, 12px round a 24px icon, and action-default: `.mud-icon-button` comes after
    /// `.mud-button` in MudBlazor's bundle, so its colour beats the text button's text-primary.
    /// </summary>
    [Fact]
    public Task AnIconButtonIsRoundAndActionDefault() => UiThread.RunAsync(() =>
    {
        var icon = new PathIcon { Data = Geometry.Parse("M3 18h18v-2H3v2z") };
        var button = Matrix.Show(new Button { Classes = { "icon" }, Content = icon });

        Assert.Equal(new Thickness(12), button.Padding);
        Assert.True(button.CornerRadius.TopLeft >= 24, "A text icon button is round.");
        Assert.Equal(Matrix.Token("FiliActionDefaultColor"), Matrix.Colour(button.Foreground));
        Assert.Equal(24, icon.Width);
        Assert.Equal(48, button.Bounds.Width, 1);
    });

    [Theory]
    [InlineData("small", 3, 18)]
    [InlineData("large", 12, 36)]
    public Task IconButtonSizes(string size, double padding, double iconSize) => UiThread.RunAsync(() =>
    {
        var icon = new PathIcon { Data = Geometry.Parse("M3 18h18v-2H3v2z") };
        var button = Matrix.Show(new Button { Classes = { "icon", size }, Content = icon });

        Assert.Equal(new Thickness(padding), button.Padding);
        Assert.Equal(iconSize, icon.Width);
    });

    [Theory]
    [MemberData(nameof(Matrix.Colours), MemberType = typeof(Matrix))]
    public Task AColourBeatsTheIconButtonsGrey(string colour) => UiThread.RunAsync(() =>
    {
        var button = Matrix.Show(new Button { Classes = { "icon", colour.ToLowerInvariant() }, Content = "x" });

        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(button.Foreground));
    });

    /// <summary>An outlined or filled icon button is a square-cornered button again, padded 5px.</summary>
    [Theory]
    [InlineData("outlined")]
    [InlineData("filled")]
    public Task OutlinedAndFilledIconButtonsAreSquareCornered(string variant) => UiThread.RunAsync(() =>
    {
        var button = Matrix.Show(new Button { Classes = { "icon", variant }, Content = "x" });

        Assert.Equal(new Thickness(5), button.Padding);
        Assert.Equal(4, button.CornerRadius.TopLeft);
    });

    // --------------------------------------------------------------------------------------
    // flat: DropShadow="false"
    // --------------------------------------------------------------------------------------

    [Fact]
    public Task AFlatFilledButtonHasNoShadowEvenWhenHovered() => UiThread.RunAsync(() =>
    {
        var button = Matrix.Show(new Button { Classes = { "filled", "primary", "flat" }, Content = "Flat" });
        var root = Matrix.Part<Border>(button, "PART_Root");

        Assert.Equal(0, root.BoxShadow.Count);
        Matrix.Hover(button);
        Assert.Equal(0, root.BoxShadow.Count);
    });

    [Fact]
    public Task FlatAppliesToSplitAndDropDownButtons() => UiThread.RunAsync(() =>
    {
        var split = Matrix.Show(new SplitButton { Classes = { "filled", "primary", "flat" }, Content = "Flat" });
        var dropDown = Matrix.Show(new DropDownButton { Classes = { "filled", "primary", "flat" }, Content = "Flat" });
        var raised = Matrix.Show(new DropDownButton { Classes = { "filled", "primary" }, Content = "Raised" });

        Assert.Equal(0, Matrix.Part<Border>(split, "PART_Root").BoxShadow.Count);
        Assert.Equal(0, Matrix.Part<Border>(dropDown, "RootBorder").BoxShadow.Count);
        Assert.NotEqual(0, Matrix.Part<Border>(raised, "RootBorder").BoxShadow.Count);
    });

    // --------------------------------------------------------------------------------------
    // MudChip
    // --------------------------------------------------------------------------------------

    /// <summary>MudChip defaults to Variant.Filled and Color.Default: a grey 32px pill.</summary>
    [Fact]
    public Task AChipIsAGreyFilledPill() => UiThread.RunAsync(() =>
    {
        var chip = Matrix.Show(new Button { Classes = { "chip" }, Content = "Chip" });

        Assert.Equal(32, chip.Bounds.Height, 1);
        Assert.Equal(16, chip.CornerRadius.TopLeft);
        Assert.Equal(Matrix.Token("FiliActionDisabledBackgroundColor"), Matrix.Colour(Matrix.Part<Border>(chip, "PART_Root").Background));

        Matrix.Hover(chip);
        Assert.Equal(Matrix.Token("FiliActionDisabledColor"), Matrix.Colour(Matrix.Part<Border>(chip, "PART_Root").Background));
    });

    [Theory]
    [MemberData(nameof(Matrix.Colours), MemberType = typeof(Matrix))]
    public Task ChipVariantsFollowTheColour(string colour) => UiThread.RunAsync(() =>
    {
        var cls = colour.ToLowerInvariant();

        var filled = Matrix.Show(new Button { Classes = { "chip", cls }, Content = "Chip" });
        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(Matrix.Part<Border>(filled, "PART_Root").Background));
        Assert.Equal(Matrix.Token($"Fili{colour}ContrastTextColor"), Matrix.Colour(filled.Foreground));
        Matrix.Hover(filled);
        Assert.Equal(Matrix.Token($"Fili{colour}DarkenColor"), Matrix.Colour(Matrix.Part<Border>(filled, "PART_Root").Background));

        var outlined = Matrix.Show(new Button { Classes = { "chip", "outlined", cls }, Content = "Chip" });
        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(outlined.BorderBrush));
        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(outlined.Foreground));

        // The text chip's 12% hover: the colour itself on the state layer, at Opacity 0.12.
        var text = Matrix.Show(new Button { Classes = { "chip", "text", cls }, Content = "Chip" });
        Assert.Equal(Matrix.Token($"Fili{colour}HoverColor"), Matrix.Colour(Matrix.Part<Border>(text, "PART_Root").Background));
        Matrix.Hover(text);
        var layer = Matrix.Part<Border>(text, "PART_StateLayer");
        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(layer.Background));
        Assert.Equal(0.12, layer.Opacity, 3);
    });

    /// <summary>MudChip.GetVariant: a selected filled chip draws as text, a selected text chip as filled.</summary>
    [Fact]
    public Task SelectionSwapsFilledAndText() => UiThread.RunAsync(() =>
    {
        var filledSelected = Matrix.Show(new ToggleButton { Classes = { "chip", "primary" }, IsChecked = true, Content = "Chip" });
        Assert.Equal(Matrix.Token("FiliPrimaryHoverColor"), Matrix.Colour(Matrix.Part<Border>(filledSelected, "PART_Root").Background));
        Assert.Equal(Matrix.Token("FiliPrimaryColor"), Matrix.Colour(filledSelected.Foreground));

        var textSelected = Matrix.Show(new ToggleButton { Classes = { "chip", "text", "primary" }, IsChecked = true, Content = "Chip" });
        Assert.Equal(Matrix.Token("FiliPrimaryColor"), Matrix.Colour(Matrix.Part<Border>(textSelected, "PART_Root").Background));
        Assert.Equal(Matrix.Token("FiliPrimaryContrastTextColor"), Matrix.Colour(textSelected.Foreground));
    });

    [Theory]
    [InlineData("small", 24, 12)]
    [InlineData("large", 40, 20)]
    public Task ChipSizes(string size, double height, double radius) => UiThread.RunAsync(() =>
    {
        var chip = Matrix.Show(new Button { Classes = { "chip", size }, Content = "Chip" });

        Assert.Equal(height, chip.Bounds.Height, 1);
        Assert.Equal(radius, chip.CornerRadius.TopLeft);
    });

    // --------------------------------------------------------------------------------------
    // MudAlert
    // --------------------------------------------------------------------------------------

    /// <summary>Text and outlined alerts draw their TEXT in the darken shade and the icon in the colour.</summary>
    [Theory]
    [MemberData(nameof(Matrix.Colours), MemberType = typeof(Matrix))]
    public Task AlertVariantsFollowTheColour(string colour) => UiThread.RunAsync(() =>
    {
        var cls = colour.ToLowerInvariant();

        var text = new TextBlock { Text = "Message" };
        var icon = new PathIcon { Data = Geometry.Parse("M3 18h18v-2H3v2z") };
        var alert = Matrix.Show(new Border { Classes = { "alert", cls }, Child = new StackPanel { Children = { icon, text } } });

        Assert.Equal(Matrix.Token($"Fili{colour}HoverColor"), Matrix.Colour(alert.Background));
        Assert.Equal(Matrix.Token($"Fili{colour}DarkenColor"), Matrix.Colour(text.Foreground));
        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(icon.Foreground));

        var filledText = new TextBlock { Text = "Message" };
        var filled = Matrix.Show(new Border { Classes = { "alert", "filled", cls }, Child = filledText });
        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(filled.Background));
        Assert.Equal(Matrix.Token($"Fili{colour}ContrastTextColor"), Matrix.Colour(filledText.Foreground));
        Assert.Equal(FontWeight.Medium, filledText.FontWeight);

        var outlined = Matrix.Show(new Border { Classes = { "alert", "outlined", cls }, Child = new TextBlock() });
        Assert.Equal(Matrix.Token($"Fili{colour}Color"), Matrix.Colour(outlined.BorderBrush));
        Assert.Equal(new Thickness(1), outlined.BorderThickness);
    });

    [Fact]
    public Task ANormalAlertIsTextPrimaryOnDarkHover() => UiThread.RunAsync(() =>
    {
        var text = new TextBlock { Text = "Message" };
        var alert = Matrix.Show(new Border { Classes = { "alert" }, Child = text });

        Assert.Equal(Matrix.Token("FiliDarkHoverColor"), Matrix.Colour(alert.Background));
        Assert.Equal(Matrix.Token("FiliTextPrimaryColor"), Matrix.Colour(text.Foreground));
        Assert.Equal(new Thickness(16, 6), alert.Padding);
    });

    // --------------------------------------------------------------------------------------
    // MudSkeleton, MudDivider.Light
    // --------------------------------------------------------------------------------------

    [Fact]
    public Task SkeletonTypes() => UiThread.RunAsync(() =>
    {
        var text = Matrix.Show(new Border { Classes = { "skeleton" }, Width = 100 });
        var circle = Matrix.Show(new Border { Classes = { "skeleton", "circle" }, Width = 40, Height = 40 });
        var rectangle = Matrix.Show(new Border { Classes = { "skeleton", "rectangle" }, Width = 40, Height = 40 });

        Assert.Equal(Matrix.Token("FiliSkeletonColor"), Matrix.Colour(text.Background));
        Assert.Equal(4, text.CornerRadius.TopLeft);
        Assert.True(circle.CornerRadius.TopLeft >= 20, "A circle skeleton is round.");
        Assert.Equal(0, rectangle.CornerRadius.TopLeft);
    });

    [Fact]
    public Task ALightSeparatorIsDividerLight() => UiThread.RunAsync(() =>
    {
        var separator = Matrix.Show(new Separator { Classes = { "light" }, Width = 100 });

        Assert.Equal(Matrix.Token("FiliDividerLightColor"), Matrix.Colour(separator.Background));
    });
}
