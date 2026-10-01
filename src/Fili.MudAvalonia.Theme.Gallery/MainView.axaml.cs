using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Fili.MudAvalonia.Theme;

namespace Fili.MudAvalonia.Theme.Gallery;

/// <summary>
/// The gallery itself: the app bar with its substrate and variant selectors, and the tabs.
/// Desktop hosts it in <see cref="MainWindow"/>; the browser build sets it as the single view.
/// </summary>
public partial class MainView : UserControl
{
    public MainView() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Switches the application theme variant at runtime. This is the single most useful thing
    /// the gallery does: every token that was wired with StaticResource instead of
    /// DynamicResource stops following the theme here, visibly, in one click.
    /// </summary>
    /// <remarks>
    /// The variant goes on the APPLICATION and not on the window, and that is not a shortcut.
    /// Every brush in the theme is one shared application-level object whose Color is a
    /// DynamicResource, so a window asking for a different variant gets the same brush instance
    /// and therefore the same colour.
    /// </remarks>
    private void OnVariantChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Application.Current is null || sender is not ComboBox selector)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = selector.SelectedIndex switch
        {
            1 => ThemeVariant.Dark,
            2 => FiliThemeVariants.HighContrast,
            _ => ThemeVariant.Light,
        };
    }

    /// <summary>
    /// Swaps the substrate theme underneath this one, live.
    /// <para>
    /// Worth watching while it flips: the 39 control types with hand-written themes do not change
    /// at all, because they carry full templates. The rest wear the forked Simple templates in
    /// Standalone and Fluent's in Fluent, so they are the part of the page that moves.
    /// </para>
    /// </summary>
    private void OnSubstrateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Application.Current is not App app || sender is not ComboBox selector)
        {
            return;
        }

        app.UseSubstrate(selector.SelectedIndex == 1 ? Substrate.Fluent : Substrate.Standalone);
    }
}
