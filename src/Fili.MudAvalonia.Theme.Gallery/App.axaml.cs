using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;

namespace Fili.MudAvalonia.Theme.Gallery;

/// <summary>
/// Which set of templates sits underneath.
/// </summary>
public enum Substrate
{
    /// <summary>
    /// This package's own forked base — no external theme. The shipping configuration.
    /// </summary>
    Standalone,

    /// <summary>
    /// FluentTheme, kept only so the two can still be compared side by side in the gallery.
    /// Consuming apps do not need it and should not reference it.
    /// </summary>
    Fluent,
}

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        UseSubstrate(Substrate.Standalone);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Swaps the substrate theme in place, keeping FiliTheme on top of it.
    /// <para>
    /// Avalonia ships no implicit default theme: a control with no ControlTheme in scope has no
    /// template and renders nothing at all. So something must sit underneath, and this package
    /// only themes five controls. Being able to flip the substrate here is the point of the
    /// control — it makes visible exactly which parts of the window are borrowed, and what
    /// dropping Fluent would actually cost.
    /// </para>
    /// <para>
    /// Index 0 only. Order is load-bearing: the substrate must stay below FiliTheme, because
    /// styles are evaluated in order and later ones win for overlapping setters.
    /// </para>
    /// </summary>
    public void UseSubstrate(Substrate substrate)
    {
        Styles[0] = substrate switch
        {
            Substrate.Fluent => CreateFluentSubstrate(),
            _ => CreateStandaloneBase(),
        };
    }

    /// <summary>
    /// The shipping base: this package's fork of Avalonia's Simple templates, repaletted through
    /// <c>Themes/Base/Accents.axaml</c>. No external theme package is involved.
    /// </summary>
    public static IStyle CreateStandaloneBase() =>
        new StyleInclude(new Uri("avares://Fili.MudAvalonia.Theme.Gallery/"))
        {
            Source = new Uri("avares://Fili.MudAvalonia.Theme/Themes/Base/FiliBaseTheme.axaml"),
        };

    /// <summary>
    /// FluentTheme carrying the Mud accent.
    /// <para>
    /// Overriding a <c>SystemAccentColor</c> resource does NOT work: FluentTheme derives its
    /// accent ramp from its own <see cref="FluentTheme.Palettes"/> collection, seeded from
    /// platform settings, and never consults a resource of that name. Doing it the wrong way
    /// fails silently — every unthemed control simply stays the OS accent blue, which is exactly
    /// what the first capture of this gallery showed.
    /// </para>
    /// </summary>
    public static FluentTheme CreateFluentSubstrate()
    {
        var theme = new FluentTheme();

        theme.Palettes[ThemeVariant.Light] = new ColorPaletteResources
        {
            Accent = Color.Parse("#594AE2"),
        };

        theme.Palettes[ThemeVariant.Dark] = new ColorPaletteResources
        {
            Accent = Color.Parse("#776BE7"),
        };

        return theme;
    }
}
