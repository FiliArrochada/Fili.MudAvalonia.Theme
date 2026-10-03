using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Styling;
using Xunit;

namespace Fili.Theme.MudAvalonia.UnitTests;

/// <summary>
/// The high-contrast variant.
///
/// <para>
/// Every failure this guards against is a SILENT one, and they all have the same shape: the
/// variant inherits from Dark, so a token it forgets to declare does not blow up and does not
/// go missing — it quietly comes back with the dark value. A #1FFFFFFF divider over a black
/// ground is not a visible bug, it is an invisible divider, and nothing in a screenshot or a
/// resolution test would say so. These tests exist because looking cannot find it.
/// </para>
/// </summary>
public class HighContrastTests
{
    private static ThemeVariant HighContrast => FiliThemeVariants.HighContrast;

    /// <summary>Every palette token, in the order Palette.axaml declares them.</summary>
    private static readonly string[] PaletteTokens =
    [
        "FiliPrimaryColor", "FiliPrimaryContrastTextColor",
        "FiliSecondaryColor", "FiliSecondaryContrastTextColor",
        "FiliTertiaryColor", "FiliTertiaryContrastTextColor",
        "FiliInfoColor", "FiliInfoContrastTextColor",
        "FiliSuccessColor", "FiliSuccessContrastTextColor",
        "FiliWarningColor", "FiliWarningContrastTextColor",
        "FiliErrorColor", "FiliErrorContrastTextColor",
        "FiliDarkColor", "FiliBlackColor", "FiliWhiteColor",
        "FiliTextPrimaryColor", "FiliTextSecondaryColor", "FiliTextDisabledColor",
        "FiliActionDefaultColor", "FiliActionDisabledColor", "FiliActionDisabledBackgroundColor",
        "FiliBackgroundColor", "FiliBackgroundGrayColor", "FiliSurfaceColor",
        "FiliAppbarBackgroundColor", "FiliAppbarTextColor",
        "FiliDrawerBackgroundColor", "FiliDrawerTextColor", "FiliDrawerIconColor",
        "FiliDrawerBorderColor",
        "FiliLinesDefaultColor", "FiliLinesInputsColor", "FiliDividerColor",
        "FiliTableLinesColor", "FiliTableStripedColor", "FiliTableHoverColor",
        "FiliSkeletonColor", "FiliPrimaryHoverColor",
        "FiliOverlayHoverColor", "FiliOverlayPressedColor", "FiliInputFilledColor",
        "FiliOverlayDarkColor", "FiliOverlayLightColor", "FiliTooltipBackgroundColor",
        "FiliSwitchThumbColor",
        "FiliDividerLightColor",
        "FiliDarkContrastTextColor", "FiliActionDefaultHoverColor",
        "FiliSecondaryHoverColor", "FiliTertiaryHoverColor", "FiliInfoHoverColor", "FiliSuccessHoverColor",
        "FiliWarningHoverColor", "FiliErrorHoverColor", "FiliDarkHoverColor",
        "FiliPrimaryDarkenColor", "FiliSecondaryDarkenColor", "FiliTertiaryDarkenColor", "FiliInfoDarkenColor",
        "FiliSuccessDarkenColor", "FiliWarningDarkenColor", "FiliErrorDarkenColor", "FiliDarkDarkenColor",
        "FiliPrimaryLightenColor", "FiliSecondaryLightenColor", "FiliTertiaryLightenColor", "FiliInfoLightenColor",
        "FiliSuccessLightenColor", "FiliWarningLightenColor", "FiliErrorLightenColor", "FiliDarkLightenColor",
    ];

    /// <summary>
    /// The sixteen raw Colors the forked Simple templates read directly. The rest of the bridge
    /// is brushes bound to Fili tokens, which follow the variant without being restated.
    /// </summary>
    private static readonly string[] BridgeTokens =
    [
        "ThemeForegroundColor", "ThemeForegroundLowColor", "ThemeForegroundMidColor",
        "ThemeDisabledColor",
        "ThemeBorderLowColor", "ThemeBorderMidColor", "ThemeBorderHighColor",
        "ThemeControlLowColor", "ThemeControlMidColor", "ThemeControlMidHighColor",
        "ThemeControlHighColor", "ThemeControlHighlightMidColor",
        "ThemeAccentColor", "ThemeAccentColor2", "ThemeAccentColor3",
        "HighlightForegroundColor",
    ];

    /// <summary>
    /// The only tokens whose high-contrast value is legitimately the dark one. White is white,
    /// and white is also the text on Dark's fill in every variant.
    /// </summary>
    private static readonly string[] SharedWithDark = ["FiliWhiteColor", "FiliDarkContrastTextColor"];

    /// <summary>
    /// Tokens allowed to keep an alpha channel here. All of them are overlays laid OVER content
    /// rather than content themselves: the state layers (including each colour's hover tint), the
    /// input fill, the selection tint and the two scrims. Everything else must be opaque, because a translucent line or label
    /// composites against the ground and loses exactly the contrast this variant is for.
    /// </summary>
    private static readonly string[] MayBeTranslucent =
    [
        "FiliOverlayHoverColor", "FiliOverlayPressedColor", "FiliInputFilledColor",
        "FiliPrimaryHoverColor", "FiliOverlayDarkColor", "FiliOverlayLightColor",
        "FiliActionDefaultHoverColor",
        "FiliSecondaryHoverColor", "FiliTertiaryHoverColor", "FiliInfoHoverColor", "FiliSuccessHoverColor",
        "FiliWarningHoverColor", "FiliErrorHoverColor", "FiliDarkHoverColor",
    ];

    /// <summary>The bridge equivalents: the accent ramp and Simple's own highlight overlay.</summary>
    private static readonly string[] BridgeMayBeTranslucent =
    [
        "ThemeAccentColor2", "ThemeAccentColor3", "ThemeControlHighlightMidColor",
    ];

    [Fact]
    public Task EveryPaletteTokenResolves() => UiThread.RunAsync(() =>
    {
        foreach (var key in PaletteTokens)
        {
            Resolve(key, HighContrast);
        }
    });

    /// <summary>
    /// The test that actually proves the dictionary is complete. A token the high-contrast
    /// dictionary does not declare still RESOLVES — to the dark value — so resolution alone
    /// proves nothing. Differing from dark is the only evidence the key was written.
    /// </summary>
    [Fact]
    public Task EveryPaletteTokenHasItsOwnValue() => UiThread.RunAsync(() =>
    {
        foreach (var key in PaletteTokens.Except(SharedWithDark))
        {
            Assert.True(
                Resolve(key, HighContrast) != Resolve(key, ThemeVariant.Dark),
                $"{key} is not declared for high contrast: it fell back to the dark value.");
        }

        foreach (var key in SharedWithDark)
        {
            Assert.Equal(Resolve(key, ThemeVariant.Dark), Resolve(key, HighContrast));
        }
    });

    /// <summary>
    /// The same argument for the Simple bridge. Miss that file and the forty-three control types
    /// still wearing Simple's shapes paint themselves in dark-mode alpha over black.
    /// </summary>
    [Fact]
    public Task EveryBridgeColourHasItsOwnValue() => UiThread.RunAsync(() =>
    {
        foreach (var key in BridgeTokens)
        {
            Assert.True(
                Resolve(key, HighContrast) != Resolve(key, ThemeVariant.Dark),
                $"{key} is not declared for high contrast: it fell back to the dark value.");
        }
    });

    [Fact]
    public Task ContentTokensAreOpaque() => UiThread.RunAsync(() =>
    {
        foreach (var key in PaletteTokens.Except(MayBeTranslucent))
        {
            Assert.True(
                Resolve(key, HighContrast).A == 255,
                $"{key} is translucent in high contrast, where nothing may be conveyed by alpha.");
        }

        foreach (var key in BridgeTokens.Except(BridgeMayBeTranslucent))
        {
            Assert.True(
                Resolve(key, HighContrast).A == 255,
                $"{key} is translucent in high contrast.");
        }
    });

    /// <summary>
    /// Every filled colour against the text painted on it, at WCAG AAA (7:1).
    ///
    /// <para>
    /// Declaring a token is not the same as declaring a readable one. The status colours were
    /// declared here and differed from dark — every test above passed — while snackbars still
    /// painted white on them at about 1.6:1, because there was no contrast-text token to declare.
    /// Light and dark are not held to this: they carry MudBlazor's own white, which is the point
    /// of transcribing it.
    /// </para>
    /// </summary>
    [Fact]
    public Task TextOnEveryFilledColourIsReadable() => UiThread.RunAsync(() =>
    {
        foreach (var fill in new[] { "Primary", "Secondary", "Tertiary", "Info", "Success", "Warning", "Error" })
        {
            var ratio = ContrastRatio(
                Resolve($"Fili{fill}Color", HighContrast),
                Resolve($"Fili{fill}ContrastTextColor", HighContrast));

            Assert.True(ratio >= 7, $"Text on Fili{fill}Color is {ratio:N1}:1 in high contrast.");
        }
    });

    /// <summary>
    /// The snackbar paints its text in the contrast colour of its OWN type, not one shared
    /// white. In light and dark the two are the same value, so only this variant can tell a
    /// setter that is missing from one that is present.
    /// </summary>
    [Theory]
    [InlineData(NotificationType.Information, "FiliInfoContrastTextColor")]
    [InlineData(NotificationType.Success, "FiliSuccessContrastTextColor")]
    [InlineData(NotificationType.Warning, "FiliWarningContrastTextColor")]
    [InlineData(NotificationType.Error, "FiliErrorContrastTextColor")]
    public Task EverySnackbarTypePaintsItsOwnContrastText(NotificationType type, string token) =>
        UiThread.RunAsync(() =>
        {
            var application = Application.Current!;
            var previous = application.RequestedThemeVariant;

            try
            {
                application.RequestedThemeVariant = HighContrast;

                var card = new NotificationCard { NotificationType = type, Content = "Text" };
                var window = new Window { Content = card };

                window.Show();
                card.ApplyTemplate();

                Assert.Equal(
                    Resolve(token, HighContrast),
                    Assert.IsAssignableFrom<ISolidColorBrush>(card.Foreground).Color);
            }
            finally
            {
                application.RequestedThemeVariant = previous;
            }
        });

    /// <summary>
    /// A hard ring replaces every shadow. This is what separates a card, a menu or a dialog from
    /// the page here, and it is bought entirely in Elevation.axaml — no control template knows
    /// the variant exists.
    /// </summary>
    [Fact]
    public Task EveryRaisedLevelIsASingleRing() => UiThread.RunAsync(() =>
    {
        Assert.Equal(0, Elevation(0, HighContrast).Count);

        for (var level = 1; level <= 24; level++)
        {
            var shadows = Elevation(level, HighContrast);

            Assert.True(shadows.Count == 1, $"FiliElevation{level} is not a single ring.");
            Assert.Equal(0d, shadows[0].Blur);
            Assert.Equal(1d, shadows[0].Spread);
            Assert.Equal(0d, shadows[0].OffsetX);
            Assert.Equal(0d, shadows[0].OffsetY);
        }
    });

    /// <summary>
    /// Light and Dark hold two copies of the same ladder, which exist only because a token
    /// declared outside the theme dictionaries cannot be overridden by one. Two copies can
    /// drift; this is what stops them.
    /// </summary>
    [Fact]
    public Task LightAndDarkShareTheSameLadder() => UiThread.RunAsync(() =>
    {
        for (var level = 0; level <= 24; level++)
        {
            Assert.Equal(
                Elevation(level, ThemeVariant.Light).ToString(),
                Elevation(level, ThemeVariant.Dark).ToString());
        }
    });

    /// <summary>
    /// The rule that forced the elevation ladder into the theme dictionaries, pinned so a later
    /// "simplification" back to one global copy plus a high-contrast override cannot pass: a
    /// dictionary's OWN entries are found before its ThemeDictionaries, so the global value wins
    /// in every variant and the override does nothing at all. Silently, of course.
    /// </summary>
    [Fact]
    public Task OwnEntriesWinOverThemeDictionaries() => UiThread.RunAsync(() =>
    {
        var dictionary = new ResourceDictionary
        {
            ["Token"] = Colors.Black,
            ThemeDictionaries =
            {
                [HighContrast] = new ResourceDictionary { ["Token"] = Colors.White },
            },
        };

        Assert.True(dictionary.TryGetResource("Token", HighContrast, out var value));
        Assert.Equal(Colors.Black, value);
    });

    /// <summary>
    /// The hazard an adopting app walks into. Overriding tokens in Light and Dark and stopping
    /// there does not produce a high-contrast version of the app's colours — it produces the
    /// app's DARK colours, because that is what this variant inherits. Overriding tokens for a
    /// variant means declaring that variant.
    /// </summary>
    [Fact]
    public Task AnAppThatDeclaresOnlyLightAndDarkGetsItsDarkColours() => UiThread.RunAsync(() =>
    {
        var appPalette = new ResourceDictionary
        {
            ThemeDictionaries =
            {
                [ThemeVariant.Light] = new ResourceDictionary { ["FiliPrimaryColor"] = Colors.Red },
                [ThemeVariant.Dark] = new ResourceDictionary { ["FiliPrimaryColor"] = Colors.Green },
            },
        };

        Assert.True(appPalette.TryGetResource("FiliPrimaryColor", HighContrast, out var value));
        Assert.Equal(Colors.Green, value);
    });

    /// <summary>
    /// This property is not a convenience. A theme dictionary keyed
    /// <c>x:Key="HighContrast"</c> does not compile to a custom variant — it throws
    /// <c>NotSupportedException: ThemeVariant type converter supports only build in variants</c>
    /// while the merged dictionary is being built, which takes the whole theme down with it.
    /// XAML reaches a custom variant only through <c>x:Static</c>, so this is the single object
    /// every dictionary in the package and every adopting app has to name.
    /// </summary>
    [Fact]
    public void TheVariantIsASingletonReachableFromXaml()
    {
        Assert.Same(FiliThemeVariants.HighContrast, FiliThemeVariants.HighContrast);
        Assert.Equal("HighContrast", HighContrast.Key);
        Assert.Equal(ThemeVariant.Dark, HighContrast.InheritVariant);

        // Equality is still by key, which is what lets the inheritance above do its job.
        Assert.Equal(new ThemeVariant("HighContrast", null), HighContrast);
    }

    /// <summary>
    /// The drawer's edge, and the priority rule behind it.
    ///
    /// <para>
    /// The rectangle is the forked Simple template's own <c>HCPaneBorder</c>, which ships
    /// <c>Fill="Transparent"</c>. A value written inside a ControlTemplate binds at Template
    /// priority, which OUTRANKS an ordinary Style — so the obvious
    /// <c>SplitView /template/ Rectangle#HCPaneBorder</c> setter does nothing, silently, and
    /// reads like a selector that failed to match. It takes an activator to win, and this test
    /// fails the moment one is dropped from that selector.
    /// </para>
    /// </summary>
    [Fact]
    public Task TheDrawerGainsAnEdgeInHighContrastOnly() => UiThread.RunAsync(() =>
    {
        // Painted in every variant; it is the TOKEN that is transparent in two of them. Asserting
        // the token rather than a literal is what proves the setter ran at all: a selector that
        // silently failed to match would leave the template's own Transparent behind, and
        // Transparent is #00FFFFFF, not the #00000000 this token carries.
        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark, HighContrast })
        {
            Assert.Equal(Resolve("FiliDrawerBorderColor", variant), PaneBorderFill(variant));
        }

        Assert.Equal(0, PaneBorderFill(ThemeVariant.Light).A);
        Assert.Equal(0, PaneBorderFill(ThemeVariant.Dark).A);
        Assert.Equal(255, PaneBorderFill(HighContrast).A);
    });

    /// <remarks>
    /// The variant goes on the APPLICATION, not on the window, and that is not incidental. Every
    /// brush in this theme is one shared object declared once at application level whose Color is
    /// a DynamicResource, so the colour it reports follows the application's variant — a window
    /// asking for a different one gets the same brush and therefore the same colour. It is the
    /// reason a per-window variant does not work in this package, and it applies to every token,
    /// not just this one.
    /// </remarks>
    private static Color PaneBorderFill(ThemeVariant variant)
    {
        var application = Application.Current!;
        var previous = application.RequestedThemeVariant;

        try
        {
            application.RequestedThemeVariant = variant;

            var split = new SplitView { IsPaneOpen = true, DisplayMode = SplitViewDisplayMode.Inline };
            var window = new Window { Content = split };

            window.Show();
            split.ApplyTemplate();

            var rectangle = split.GetVisualDescendants()
                .OfType<Rectangle>()
                .Single(r => r.Name == "HCPaneBorder");

            return Assert.IsAssignableFrom<ISolidColorBrush>(rectangle.Fill).Color;
        }
        finally
        {
            application.RequestedThemeVariant = previous;
        }
    }

    private static Color Resolve(string key, ThemeVariant variant)
    {
        Assert.True(
            Application.Current!.TryFindResource(key, variant, out var value),
            $"{key} did not resolve under {variant}.");

        return Assert.IsType<Color>(value);
    }

    /// <summary>The WCAG 2 contrast ratio of two opaque colours.</summary>
    private static double ContrastRatio(Color a, Color b)
    {
        var (lighter, darker) = (Luminance(a), Luminance(b)) is var (x, y) && x > y ? (x, y) : (y, x);

        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double Luminance(Color color)
    {
        static double Linear(byte channel)
        {
            var c = channel / 255d;

            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Linear(color.R)) + (0.7152 * Linear(color.G)) + (0.0722 * Linear(color.B));
    }

    private static BoxShadows Elevation(int level, ThemeVariant variant)
    {
        Assert.True(
            Application.Current!.TryFindResource($"FiliElevation{level}", variant, out var value),
            $"FiliElevation{level} did not resolve under {variant}.");

        return Assert.IsType<BoxShadows>(value);
    }
}
