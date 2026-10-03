using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace Fili.Theme.MudAvalonia.UnitTests.Generation;

/// <summary>
/// Every <c>Fili{Color}Darken</c> and <c>Fili{Color}Lighten</c> token is what MudBlazor's own
/// algorithm makes of <c>Fili{Color}Color</c>, in every variant.
///
/// <para>
/// The darken and lighten shades are derived, not transcribed: MudBlazor computes them at runtime
/// from each colour. Pasted into Palette.axaml once, they would silently stop matching the moment
/// someone changed a colour and forgot its shades. This test is the derivation, run every build.
/// </para>
/// </summary>
public class PaletteDerivationTests
{
    public static readonly TheoryData<string> Colours =
        ["Primary", "Secondary", "Tertiary", "Info", "Success", "Warning", "Error", "Dark"];

    /// <summary>
    /// The port against MudBlazor itself: the darken and lighten CSS variables mudblazor.com emits
    /// for its default palette. Dark is left out because that site customises it.
    /// </summary>
    [Theory]
    [InlineData("#594AE2", "#3E2CDD", "#766AE7")]
    [InlineData("#FF4081", "#FF1F69", "#FF6699")]
    [InlineData("#1EC8A5", "#19A98C", "#2ADFBB")]
    [InlineData("#2196F3", "#0C80DF", "#47A7F5")]
    [InlineData("#00C853", "#00A344", "#00EB62")]
    [InlineData("#FF9800", "#D68100", "#FFA724")]
    [InlineData("#F44336", "#F21C0D", "#F66055")]
    public void ThePortReproducesMudBlazorsPublishedValues(string colour, string darken, string lighten)
    {
        Assert.Equal(Color.Parse(darken), MudColorPort.Darken(Color.Parse(colour)));
        Assert.Equal(Color.Parse(lighten), MudColorPort.Lighten(Color.Parse(colour)));
    }

    [Theory]
    [MemberData(nameof(Colours))]
    public Task LightAndDarkShadesAreDerived(string colour) => UiThread.RunAsync(() =>
    {
        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            var baseColour = Token($"Fili{colour}Color", variant);

            Assert.Equal(MudColorPort.Darken(baseColour), Token($"Fili{colour}DarkenColor", variant));
            Assert.Equal(MudColorPort.Lighten(baseColour), Token($"Fili{colour}LightenColor", variant));
        }
    });

    /// <summary>
    /// High contrast derives its shades the same way, with one deliberate exception: its Dark is
    /// black, darkening black is black, and a filled dark button would have no hover at all - so
    /// Dark darkens UP to the variant's table-hover grey instead.
    /// </summary>
    [Theory]
    [MemberData(nameof(Colours))]
    public Task HighContrastShadesAreDerived(string colour) => UiThread.RunAsync(() =>
    {
        var variant = FiliThemeVariants.HighContrast;
        var baseColour = Token($"Fili{colour}Color", variant);

        var expectedDarken = colour == "Dark" ? Token("FiliTableHoverColor", variant) : MudColorPort.Darken(baseColour);

        Assert.Equal(expectedDarken, Token($"Fili{colour}DarkenColor", variant));
        Assert.Equal(MudColorPort.Lighten(baseColour), Token($"Fili{colour}LightenColor", variant));
    });

    private static Color Token(string key, ThemeVariant variant)
    {
        Assert.True(Application.Current!.TryFindResource(key, variant, out var value), $"{key} did not resolve under {variant}.");
        return Assert.IsType<Color>(value);
    }
}
