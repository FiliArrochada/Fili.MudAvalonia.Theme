using Avalonia.Media;
using Avalonia.Styling;
using Xunit;
using GalleryApp = Fili.MudAvalonia.Theme.Gallery.App;

namespace Fili.MudAvalonia.Theme.UnitTests;

/// <summary>
/// The substrate is the theme this package layers over: Avalonia ships no implicit default, so
/// something must supply templates for the ~65 controls that have no ControlTheme here.
/// </summary>
public class SubstrateTests
{
    /// <summary>
    /// Regression test for a bug that shipped in the docs before it was caught by rendering.
    /// <para>
    /// FluentTheme derives its accent ramp from its own <c>Palettes</c> collection, seeded from
    /// platform settings. It never looks up a <c>SystemAccentColor</c> resource, so overriding
    /// one fails <em>silently</em>: every unthemed control simply stays the OS accent blue, and
    /// nothing anywhere reports a problem.
    /// </para>
    /// <para>
    /// This asserts the mechanism as well as the values, because the values being right in a
    /// resource dictionary nobody reads is exactly the failure being guarded against.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Runs on the headless UI thread like every other test here. Constructing a FluentTheme
    /// touches the compositor, and doing that from the xunit thread throws
    /// "The calling thread cannot access this object" — and then poisons the shared session, so
    /// every later test in the assembly fails too.
    /// </remarks>
    [Theory]
    [InlineData("Light", "#594AE2")]
    [InlineData("Dark", "#776BE7")]
    public Task FluentAccentIsTheMudPrimary(string variantName, string expected) => UiThread.RunAsync(() =>
    {
        var variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

        var palettes = GalleryApp.CreateFluentSubstrate().Palettes;

        Assert.True(palettes.ContainsKey(variant), $"No palette registered for {variantName}.");
        Assert.Equal(Color.Parse(expected), palettes[variant].Accent);
    });

    /// <summary>
    /// The primary differs per variant, so a single shared palette would render the wrong brand
    /// colour in one of them.
    /// </summary>
    [Fact]
    public Task FluentAccentDiffersBetweenVariants() => UiThread.RunAsync(() =>
    {
        var palettes = GalleryApp.CreateFluentSubstrate().Palettes;

        Assert.NotEqual(palettes[ThemeVariant.Light].Accent, palettes[ThemeVariant.Dark].Accent);
    });
}
