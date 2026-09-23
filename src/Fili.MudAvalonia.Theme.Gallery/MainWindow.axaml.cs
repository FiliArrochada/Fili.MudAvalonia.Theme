using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Fili.MudAvalonia.Theme;

namespace Fili.MudAvalonia.Theme.Gallery;

public partial class MainWindow : Window
{
    public MainWindow() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Switches the application theme variant at runtime. This is the single most useful thing
    /// the gallery does: every token that was wired with StaticResource instead of
    /// DynamicResource stops following the theme here, visibly, in one click.
    /// </summary>
    /// <remarks>
    /// The variant goes on the APPLICATION and not on this window, and that is not a shortcut.
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
    /// Worth watching while it flips: the five themed controls do not change at all, because they
    /// carry full templates. Everything else in the window does. That difference is the honest
    /// measure of how much of the look is still borrowed.
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
