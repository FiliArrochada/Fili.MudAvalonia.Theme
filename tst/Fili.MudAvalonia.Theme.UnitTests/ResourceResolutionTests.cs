using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace Fili.MudAvalonia.Theme.UnitTests;

/// <summary>
/// The acceptance test for the token layer.
/// <para>
/// A misspelt or missing resource key is SILENT in Avalonia: the lookup resolves to nothing
/// and whatever was there before simply stays. That turns a typo into a colour which quietly
/// never changed, rather than into a failure. These tests are what make it a failure.
/// </para>
/// </summary>
public class ResourceResolutionTests
{
    private static readonly ThemeVariant[] Variants = [ThemeVariant.Light, ThemeVariant.Dark];

    private static readonly string[] ColorTokens =
    [
        "FiliPrimaryColor", "FiliPrimaryContrastTextColor",
        "FiliSecondaryColor", "FiliSecondaryContrastTextColor",
        "FiliTertiaryColor", "FiliTertiaryContrastTextColor",
        "FiliInfoColor", "FiliSuccessColor", "FiliWarningColor", "FiliErrorColor",
        "FiliDarkColor", "FiliBlackColor", "FiliWhiteColor",
        "FiliTextPrimaryColor", "FiliTextSecondaryColor", "FiliTextDisabledColor",
        "FiliActionDefaultColor", "FiliActionDisabledColor", "FiliActionDisabledBackgroundColor",
        "FiliBackgroundColor", "FiliBackgroundGrayColor", "FiliSurfaceColor",
        "FiliAppbarBackgroundColor", "FiliAppbarTextColor",
        "FiliDrawerBackgroundColor", "FiliDrawerTextColor", "FiliDrawerIconColor",
        "FiliLinesDefaultColor", "FiliLinesInputsColor", "FiliDividerColor",
        "FiliTableLinesColor", "FiliTableStripedColor", "FiliTableHoverColor",
        "FiliSkeletonColor", "FiliOverlayHoverColor", "FiliOverlayPressedColor",
        "FiliSwitchThumbOffColor", "FiliSwitchTrackOffColor", "FiliInputFilledColor",
    ];

    private static readonly string[] BrushTokens =
    [
        "FiliPrimaryBrush", "FiliSecondaryBrush", "FiliTertiaryBrush",
        "FiliInfoBrush", "FiliSuccessBrush", "FiliWarningBrush", "FiliErrorBrush", "FiliDarkBrush",
        "FiliTextPrimaryBrush", "FiliTextSecondaryBrush", "FiliTextDisabledBrush",
        "FiliBackgroundBrush", "FiliBackgroundGrayBrush", "FiliSurfaceBrush",
        "FiliAppbarBackgroundBrush", "FiliAppbarTextBrush",
        "FiliDrawerBackgroundBrush", "FiliDrawerTextBrush", "FiliDrawerIconBrush",
        "FiliLinesDefaultBrush", "FiliLinesInputsBrush", "FiliDividerBrush",
        "FiliOverlayHoverBrush", "FiliOverlayPressedBrush",
        // These two exist because a Border.Background bound to a Color silently renders nothing:
        // DynamicResource is not type-checked, so the mistake compiles and ships.
        "FiliSwitchThumbOffBrush", "FiliSwitchTrackOffBrush", "FiliInputFilledBrush",
    ];

    [Fact]
    public Task EveryColourTokenResolvesInBothVariants() => UiThread.RunAsync(() =>
    {
        foreach (var variant in Variants)
        {
            foreach (var key in ColorTokens)
            {
                Assert.True(
                    Application.Current!.TryFindResource(key, variant, out var value),
                    $"{key} did not resolve under {variant}.");

                Assert.IsType<Color>(value);
            }
        }
    });

    [Fact]
    public Task EveryBrushTokenResolvesInBothVariants() => UiThread.RunAsync(() =>
    {
        foreach (var variant in Variants)
        {
            foreach (var key in BrushTokens)
            {
                Assert.True(
                    Application.Current!.TryFindResource(key, variant, out var value),
                    $"{key} did not resolve under {variant}.");

                Assert.IsAssignableFrom<IBrush>(value);
            }
        }
    });

    [Fact]
    public Task EveryElevationLevelResolves() => UiThread.RunAsync(() =>
    {
        for (var level = 0; level <= 24; level++)
        {
            var key = $"FiliElevation{level}";

            Assert.True(
                Application.Current!.TryFindResource(key, ThemeVariant.Light, out var value),
                $"{key} did not resolve.");

            Assert.IsType<BoxShadows>(value);
        }
    });

    /// <summary>
    /// The brand colour differs between variants, and getting this backwards is the classic
    /// mistake: #594AE2 is the LIGHT primary, #776BE7 the dark one. An app pinned to one
    /// variant that seeds from the other renders the wrong brand colour everywhere.
    /// </summary>
    [Fact]
    public Task PrimaryDiffersBetweenVariants() => UiThread.RunAsync(() =>
    {
        Application.Current!.TryFindResource("FiliPrimaryColor", ThemeVariant.Light, out var light);
        Application.Current!.TryFindResource("FiliPrimaryColor", ThemeVariant.Dark, out var dark);

        Assert.Equal(Color.Parse("#594AE2"), Assert.IsType<Color>(light));
        Assert.Equal(Color.Parse("#776BE7"), Assert.IsType<Color>(dark));
    });

    /// <summary>
    /// Surfaces must actually differ from the page ground, or the elevation ladder has nothing
    /// to separate. This catches a merge that lost the dark dictionary.
    /// </summary>
    [Fact]
    public Task DarkSurfaceSitsAboveDarkBackground() => UiThread.RunAsync(() =>
    {
        Application.Current!.TryFindResource("FiliBackgroundColor", ThemeVariant.Dark, out var background);
        Application.Current!.TryFindResource("FiliSurfaceColor", ThemeVariant.Dark, out var surface);

        Assert.Equal(Color.Parse("#32333D"), Assert.IsType<Color>(background));
        Assert.Equal(Color.Parse("#373740"), Assert.IsType<Color>(surface));
        Assert.NotEqual(background, surface);
    });
}
