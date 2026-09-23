using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Xunit;

namespace Fili.MudAvalonia.Theme.UnitTests;

/// <summary>
/// Measurement floors: the sizes a control needs in order to exist at all when nothing stretches
/// it.
///
/// <para>
/// These are the numbers MudBlazor cannot supply, and the reason is structural rather than an
/// oversight. Its components are sized in CSS as a percentage of their container, so "what is
/// this control's intrinsic width?" is a question the stylesheet never has to answer. Avalonia
/// asks it of every control that is not stretched, and answers zero unless someone says
/// otherwise.
/// </para>
/// </summary>
public class ControlSizingTests
{
    /// <summary>
    /// A slider that opts out of stretching must still measure to something a person can drag.
    ///
    /// <para>
    /// Without a floor, a <c>Track</c>'s desired width is zero, so the control measures to its
    /// thumb: a single dot where a slider should be, with nothing thrown, no missing resource and
    /// nothing in the visual tree to suggest a problem. Fili.PlaySphere never hit it because its
    /// sliders stretch; Fili.Plex's interface-scale slider — <c>MaxWidth="320"</c> with
    /// <c>HorizontalAlignment="Left"</c>, which asks for a desired width — shipped as that dot.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public Task AnUnstretchedHorizontalSliderStillHasAWidth(HorizontalAlignment alignment) =>
        UiThread.RunAsync(() =>
        {
            var slider = new Slider
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = alignment,
                Minimum = 0,
                Maximum = 1,
            };

            Measure(slider);

            Assert.True(
                slider.DesiredSize.Width >= 120,
                $"A {alignment}-aligned slider measured to {slider.DesiredSize.Width}px, which is "
                + "a thumb rather than a control.");
        });

    /// <summary>
    /// And the floor must not become a size: a stretched slider still fills what it is given,
    /// which is the behaviour that matches MudBlazor's <c>width: 100%</c>.
    /// </summary>
    [Fact]
    public Task AStretchedHorizontalSliderStillFillsItsContainer() => UiThread.RunAsync(() =>
    {
        var slider = new Slider { Minimum = 0, Maximum = 1 };
        var host = new Border { Width = 600, Child = slider };

        Measure(host);
        host.Arrange(new Rect(0, 0, 600, host.DesiredSize.Height));

        Assert.Equal(600d, slider.Bounds.Width);
    });

    /// <summary>
    /// The same floor for the field family, and this is the one that shipped visibly broken.
    ///
    /// <para>
    /// An EMPTY field is the case that matters: its content is nothing, so it measures to
    /// nothing, so it draws nothing — no box, no underline, no label. Fili.Plex's debrid API
    /// token box was a row reading "API token" followed by blank space, which is precisely the
    /// failure this repo keeps warning about in another costume: nothing threw, nothing was
    /// missing, and there was nothing in the tree to find.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData(typeof(TextBox))]
    [InlineData(typeof(NumericUpDown))]
    [InlineData(typeof(ComboBox))]
    [InlineData(typeof(AutoCompleteBox))]
    public Task AnUnstretchedEmptyFieldStillHasAWidth(Type type) => UiThread.RunAsync(() =>
    {
        var field = (Control)Activator.CreateInstance(type)!;
        field.HorizontalAlignment = HorizontalAlignment.Left;

        Measure(field);

        Assert.True(
            field.DesiredSize.Width >= 120,
            $"An empty {type.Name} measured to {field.DesiredSize.Width}px, which is not a field.");
    });

    private static void Measure(Control control)
    {
        // A control measures against the theme only once it is in a window: the ControlTheme is
        // resolved through the visual tree, and a detached control has no tree to resolve it in.
        var window = new Window { Content = control, SizeToContent = SizeToContent.Manual };

        window.Show();
        window.Measure(new Size(800, 600));
        window.Arrange(new Rect(0, 0, 800, 600));
    }
}
