using Avalonia.Styling;

namespace Fili.Theme.MudAvalonia;

/// <summary>
/// The theme variants this package defines beyond Avalonia's own
/// <see cref="ThemeVariant.Light"/> and <see cref="ThemeVariant.Dark"/>.
/// </summary>
public static class FiliThemeVariants
{
    /// <summary>
    /// The high-contrast variant: black ground, opaque lines, and a hard ring in place of every
    /// elevation shadow. Select it the same way as any other variant, on the application or on
    /// any control:
    /// <code>Application.Current.RequestedThemeVariant = FiliThemeVariants.HighContrast;</code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// It inherits from <see cref="ThemeVariant.Dark"/>, which is doing real work rather than
    /// naming a family: a token this variant does not declare resolves to the dark one instead
    /// of resolving to nothing. That is what lets the eighty-one forked control themes, which
    /// declare Light and Dark only, keep working here untouched.
    /// </para>
    /// <para>
    /// IN XAML, NAME THIS PROPERTY. <c>x:Key="HighContrast"</c> does not work and does not fail
    /// quietly: Avalonia's ThemeVariant type converter accepts the built-in variants only, and
    /// throws <c>NotSupportedException</c> while the merged dictionary is being built, which
    /// takes the rest of the theme down with it. The form that works is
    /// <c>&lt;ResourceDictionary x:Key="{x:Static theme:FiliThemeVariants.HighContrast}"&gt;</c>
    /// with <c>xmlns:theme="using:Fili.Theme.MudAvalonia"</c>.
    /// </para>
    /// <para>
    /// The inheritance is also the trap for an adopting app. Overriding palette tokens in Light
    /// and Dark and stopping there does not produce a high-contrast version of the app's colours.
    /// It produces its DARK ones, silently, because that is what this variant falls back to.
    /// Overriding tokens for a variant means declaring that variant.
    /// </para>
    /// </remarks>
    public static ThemeVariant HighContrast { get; } = new("HighContrast", ThemeVariant.Dark);
}
