using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Fili.MudAvalonia.Theme.Gallery;

public partial class MainWindow : Window
{
    public MainWindow() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Flips the application theme variant at runtime. This is the single most useful thing
    /// the gallery does: every token that was wired with StaticResource instead of
    /// DynamicResource stops following the theme here, visibly, in one click.
    /// </summary>
    private void OnThemeToggled(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is null || sender is not ToggleSwitch toggle)
        {
            return;
        }

        Application.Current.RequestedThemeVariant =
            toggle.IsChecked == true ? ThemeVariant.Dark : ThemeVariant.Light;
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
