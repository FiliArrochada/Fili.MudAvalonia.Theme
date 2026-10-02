using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Fili.MudAvalonia.Theme.UnitTests;

/// <summary>
/// <c>Border.appbar</c> and the colour of the text inside it.
///
/// <para>
/// The app bar's text colour used to be a blanket <c>Border.appbar TextBlock</c> style. A
/// descendant selector walks the LOGICAL tree, and a dropdown's items are logical children of the
/// control on the bar even though they render in a popup on a white surface - so every item in an
/// app-bar ComboBox was painted the app bar's white, on white. The colour is now inherited from
/// the bar instead, which reaches plain text but loses to any control that sets its own.
/// </para>
/// </summary>
public class AppBarTests
{
    [Fact]
    public Task PlainTextOnTheBarIsAppBarText() => UiThread.RunAsync(() =>
    {
        var label = new TextBlock { Text = "Title" };

        Show(new Border { Classes = { "appbar" }, Child = label });

        Assert.Equal(Resolve("FiliAppbarTextColor"), Colour(label.Foreground));
    });

    /// <summary>The reported bug: the open dropdown of a select on the bar.</summary>
    [Fact]
    public Task DropDownItemsOfASelectOnTheBarKeepTheirOwnText() => UiThread.RunAsync(() =>
    {
        var combo = new ComboBox
        {
            Width = 140,
            SelectedIndex = 0,
            ItemsSource = new[] { "Light", "Dark", "High contrast" },
        };

        Show(new Border { Classes = { "appbar" }, Child = combo });

        combo.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        var item = combo.ContainerFromIndex(1);
        Assert.NotNull(item);

        var text = item!.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();
        Assert.NotNull(text);

        Assert.Equal(Resolve("FiliTextPrimaryColor"), Colour(text!.Foreground));
        Assert.NotEqual(Resolve("FiliAppbarTextColor"), Colour(text.Foreground));

        combo.IsDropDownOpen = false;
    });

    private static void Show(Control content)
    {
        var window = new Window { Content = content, Width = 600, Height = 400 };

        window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    private static Color Colour(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static Color Resolve(string key)
    {
        Assert.True(
            Application.Current!.TryFindResource(key, ThemeVariant.Light, out var value),
            $"{key} did not resolve.");

        return Assert.IsType<Color>(value);
    }
}
