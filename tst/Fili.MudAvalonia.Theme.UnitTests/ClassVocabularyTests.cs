using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Xunit;

namespace Fili.MudAvalonia.Theme.UnitTests;

/// <summary>
/// Every style class this theme claims, pinned.
///
/// <para>
/// This is the one risk the gallery cannot show, because the gallery has no app styles to collide
/// with. The class names are MudBlazor's vocabulary on purpose — <c>primary</c> is
/// <c>Color.Primary</c>, <c>filled</c> is <c>Variant.Filled</c> — and they are deliberately not
/// namespaced, so an adopter that already uses one of these words has its controls restyled the
/// moment it includes the theme.
/// </para>
///
/// <para>
/// A collision does not fail loudly. The app wins for the setters it declares, and every setter
/// it does NOT declare leaks through from the theme. Fili.PlaySphere hit this twice: `primary` in
/// 20 places, and `h2` rendering 19px headings inside a 72px line box, because the app set
/// FontSize while the theme supplied LineHeight.
/// </para>
///
/// <para>
/// So the list below is a published interface rather than an implementation detail. Adding a
/// class costs adopters something, and this test is what makes that deliberate — as well as
/// giving an adopter one place to grep against. Note how ordinary the words are: <c>small</c>,
/// <c>flat</c>, <c>middle</c>, <c>vertical</c>, <c>error</c>. That is the whole hazard.
/// </para>
/// </summary>
public class ClassVocabularyTests
{
    private static readonly string[] Vocabulary =
    [
        // Type ramp, on TextBlock.
        "body1", "body2", "caption", "h1", "h2", "h3", "h4", "h5", "h6",
        "overline", "subtitle1", "subtitle2",

        // Surfaces, on Border.
        "appbar", "surface",
        "elevation0", "elevation1", "elevation2", "elevation4", "elevation6",
        "elevation8", "elevation12", "elevation16", "elevation24",

        // Colour, from MudBlazor's Color enum.
        "dark", "error", "info", "inherit", "primary", "secondary", "success", "tertiary",
        "warning",

        // Shape, from MudBlazor's Variant enum and the per-control parameters.
        "filled", "flat", "outlined", "rounded", "text",

        // Size, from MudBlazor's Size enum.
        "dense", "large", "medium", "small",

        // Placement and decoration, from MudDivider and MudLink.
        "inset", "light", "middle", "no-underline", "underline", "vertical",

        // Components that are a class on an existing control: MudIconButton and MudChip on a
        // Button, MudAlert and MudSkeleton on a Border, with MudSkeleton's SkeletonType.
        "alert", "chip", "circle", "counter", "icon", "rectangle", "skeleton",
    ];

    /// <summary>
    /// The classes the theme actually uses must match the declared list exactly, in both
    /// directions. An undeclared class is a collision nobody signed off on; a declared one that
    /// no selector uses means the list is lying to adopters.
    /// </summary>
    [Fact]
    public Task TheClaimedVocabularyIsExactlyWhatTheThemeUses() => UiThread.RunAsync(() =>
    {
        var selectors = new List<string>();

        // Top-level styles: the type ramp, the surface classes, the drawer, the pickers.
        Walk(Application.Current!.Styles, selectors);

        // And the classes declared INSIDE control themes, which live in a resource dictionary
        // rather than in the styles collection. Those are the variant names, which are most of
        // them — walking only Application.Styles finds 28 of the 44.
        foreach (var key in StandaloneReadinessTests.ThemeKeys)
        {
            Application.Current!.TryFindResource(key, ThemeVariant.Light, out var value);
            Walk(((ControlTheme)value!).Children, selectors);
        }

        var used = selectors
            // Attribute selectors carry a dotted property path —
            // [(DataValidationErrors.HasErrors)=True] — which otherwise reads as a class.
            .Select(s => Regex.Replace(s, @"\[[^\]]*\]", string.Empty))
            .SelectMany(s => Regex.Matches(s, @"\.([A-Za-z][A-Za-z0-9-]*)").Select(m => m.Groups[1].Value))
            .Distinct()
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(Vocabulary.OrderBy(c => c, StringComparer.Ordinal).ToArray(), used);
    });

    /// <summary>
    /// Walks every style in the tree, including those behind a StyleInclude and those nested in
    /// a Style or a ControlTheme.
    /// </summary>
    private static void Walk(IEnumerable<IStyle> styles, List<string> into)
    {
        foreach (var style in styles)
        {
            switch (style)
            {
                case Style s:
                    if (s.Selector is not null)
                    {
                        into.Add(s.Selector.ToString()!);
                    }

                    Walk(s.Children, into);
                    break;

                case ControlTheme theme:
                    Walk(theme.Children, into);
                    break;

                // A StyleInclude is a placeholder until it resolves, and its Loaded style is the
                // real content. Missing this case is why the first version of this test walked
                // the whole application and found nothing at all.
                case StyleInclude include when include.Loaded is { } loaded:
                    Walk([loaded], into);
                    break;

                case Styles group:
                    Walk(group, into);
                    break;
            }
        }
    }
}
