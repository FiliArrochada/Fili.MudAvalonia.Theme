using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Fili.MudAvalonia.Theme.UnitTests;

/// <summary>
/// Covers the two things that resource-resolution tests cannot see: whether the embedded font
/// actually loads, and whether the control themes actually apply once a button is templated.
/// </summary>
public class ThemeCompositionTests
{
    [Fact]
    public Task FontFamilyPointsAtTheEmbeddedRoboto() => UiThread.RunAsync(() =>
    {
        Assert.True(
            Application.Current!.TryFindResource("FiliFontFamily", ThemeVariant.Light, out var value));

        var family = Assert.IsType<FontFamily>(value);

        Assert.Equal("Roboto", family.Name);

        // A bare "Roboto" would silently fall back to whatever the host has installed, which is
        // the whole thing embedding was meant to stop.
        Assert.NotNull(family.Key);
        Assert.Contains("Fili.MudAvalonia.Theme", family.Key!.ToString());
    });

    /// <summary>
    /// The three static instances must each resolve to their own face. This is what a variable
    /// font would fail: Avalonia matches a weight by picking a face, not by setting an axis, so
    /// a single variable file renders Light and Medium as Regular.
    /// </summary>
    [Theory]
    [InlineData(300)] // Light  — h1, h2
    [InlineData(400)] // Regular — body
    [InlineData(500)] // Medium  — h6, subtitle2, button
    public Task EveryUsedWeightHasItsOwnFace(int weight) => UiThread.RunAsync(() =>
    {
        Application.Current!.TryFindResource("FiliFontFamily", ThemeVariant.Light, out var value);
        var family = Assert.IsType<FontFamily>(value);

        var typeface = new Typeface(family, FontStyle.Normal, (FontWeight)weight);

        Assert.True(
            FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface),
            $"Roboto weight {weight} did not resolve to a glyph typeface.");

        // Asserting the weight alone would pass vacuously: if the embedded font failed to load,
        // Avalonia falls back to a system face and may still report the requested weight. The
        // family name is what proves the bytes in Assets/Fonts were the ones used.
        //
        // StartsWith, not Equal, because Roboto's static instances carry LEGACY name tables:
        // Roboto-Light reports family "Roboto Light" and Roboto-Medium reports "Roboto Medium",
        // each with subfamily "Regular", rather than one "Roboto" family with three weights.
        // Avalonia's embedded font collection groups them correctly anyway — which is exactly
        // what this test is here to keep true.
        Assert.StartsWith("Roboto", glyphTypeface!.FamilyName);
        Assert.Equal((FontWeight)weight, glyphTypeface.Weight);
    });

    [Theory]
    [InlineData("FiliContainedButton")]
    [InlineData("FiliTextButton")]
    [InlineData("FiliOutlinedButton")]
    public Task ButtonControlThemesResolve(string key) => UiThread.RunAsync(() =>
    {
        Assert.True(
            Application.Current!.TryFindResource(key, ThemeVariant.Light, out var value),
            $"{key} did not resolve.");

        var theme = Assert.IsType<ControlTheme>(value);
        Assert.Equal(typeof(Button), theme.TargetType);
    });

    /// <summary>
    /// The raised button must actually carry a shadow once templated. A ControlTheme that fails
    /// to apply is silent — the button simply keeps Fluent's flat look.
    /// </summary>
    [Fact]
    public Task ContainedButtonIsElevated() => UiThread.RunAsync(() =>
    {
        var button = Templated(new Button { Classes = { "primary" }, Content = "Save" });

        var root = button.GetVisualDescendants()
            .OfType<Border>()
            .FirstOrDefault(b => b.Name == "PART_Root");

        Assert.NotNull(root);
        Assert.True(root!.BoxShadow.Count > 0, "The raised button rendered without a shadow.");
    });

    /// <summary>
    /// Regression test for a real bug in the first cut.
    /// <para>
    /// A blanket <c>Selector="TextBlock"</c> style that set Foreground also matched the TextBlock
    /// a ContentPresenter generates for a button's string content. A style setter outranks an
    /// inherited value, so white-on-primary button text silently rendered in body-text grey.
    /// The inheritable defaults now live on Window/UserControl instead.
    /// </para>
    /// </summary>
    [Fact]
    public Task ButtonContentKeepsItsContrastForeground() => UiThread.RunAsync(() =>
    {
        var button = Templated(new Button { Classes = { "primary" }, Content = "Save" });

        var text = button.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();
        Assert.NotNull(text);

        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(text!.Foreground);

        Assert.Equal(Colors.White, brush.Color);
    });

    [Theory]
    [InlineData("FiliFilledTextBox")]
    [InlineData("FiliOutlinedTextBox")]
    public Task TextBoxControlThemesResolve(string key) => UiThread.RunAsync(() =>
    {
        Assert.True(
            Application.Current!.TryFindResource(key, ThemeVariant.Light, out var value),
            $"{key} did not resolve.");

        var theme = Assert.IsType<ControlTheme>(value);
        Assert.Equal(typeof(TextBox), theme.TargetType);
    });

    /// <summary>
    /// The floating label is the whole point of the field, and it only exists in the template.
    /// If the ControlTheme silently fails to apply, Fluent's template has no such part.
    /// </summary>
    [Theory]
    [InlineData("filled")]
    [InlineData("outlined")]
    public Task TextFieldHasAFloatingLabel(string variant) => UiThread.RunAsync(() =>
    {
        var box = Templated(new TextBox { Classes = { variant }, PlaceholderText = "Label" });

        var label = box.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(t => t.Name == "PART_FloatingLabel");

        Assert.NotNull(label);
        Assert.Equal("Label", label!.Text);
    });

    /// <summary>
    /// Regression test for the second instance of the blanket-style trap.
    /// <para>
    /// A Style always outranks a ControlTheme setter, so the shared input style setting
    /// CornerRadius would flatten the filled field's top-only rounding to a full 4. The
    /// <c>:not(.filled):not(.outlined)</c> guard on that selector is what prevents it.
    /// </para>
    /// </summary>
    [Fact]
    public Task FilledTextFieldKeepsTopOnlyRounding() => UiThread.RunAsync(() =>
    {
        var box = Templated(new TextBox { Classes = { "filled" }, PlaceholderText = "Label" });

        Assert.Equal(new CornerRadius(4, 4, 0, 0), box.CornerRadius);
    });

    /// <summary>
    /// Filled fields carry rules; boxed ones do not. This is the visible difference between the
    /// two variants, and it is expressed only as IsVisible on two template parts.
    /// </summary>
    [Theory]
    [InlineData("filled", true)]
    [InlineData("outlined", false)]
    public Task OnlyFilledFieldsShowAnUnderline(string variant, bool expected) => UiThread.RunAsync(() =>
    {
        var box = Templated(new TextBox { Classes = { variant }, PlaceholderText = "Label" });

        var underline = box.GetVisualDescendants()
            .OfType<Border>()
            .FirstOrDefault(b => b.Name == "PART_Underline");

        Assert.NotNull(underline);
        Assert.Equal(expected, underline!.IsVisible);
    });

    [Fact]
    public Task TextButtonHasNoFillAndNoShadow() => UiThread.RunAsync(() =>
    {
        var button = Templated(new Button { Classes = { "text" }, Content = "Learn more" });

        var root = button.GetVisualDescendants()
            .OfType<Border>()
            .FirstOrDefault(b => b.Name == "PART_Root");

        Assert.NotNull(root);
        Assert.Equal(0, root!.BoxShadow.Count);
    });

    [Theory]
    [InlineData("FiliCheckBox", typeof(CheckBox))]
    [InlineData("FiliRadioButton", typeof(RadioButton))]
    [InlineData("FiliToggleSwitch", typeof(ToggleSwitch))]
    public Task SelectionControlThemesResolve(string key, Type target) => UiThread.RunAsync(() =>
    {
        Assert.True(
            Application.Current!.TryFindResource(key, ThemeVariant.Light, out var value),
            $"{key} did not resolve.");

        Assert.Equal(target, Assert.IsType<ControlTheme>(value).TargetType);
    });

    [Fact]
    public Task CheckedCheckBoxFillsItsBox() => UiThread.RunAsync(() =>
    {
        var box = Templated(new CheckBox { Classes = { "fili" }, IsChecked = true });

        var chip = box.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Box");

        Assert.Equal(Primary(), Assert.IsAssignableFrom<ISolidColorBrush>(chip.Background).Color);
    });

    /// <summary>
    /// The difference that separates a radio from a round checkbox: selecting it recolours the
    /// ring and grows a separate inner disc, it does NOT fill the ring. Filling it is the usual
    /// mistake and the result reads as a checkbox that happens to be round.
    /// </summary>
    [Fact]
    public Task CheckedRadioButtonDoesNotFillItsRing() => UiThread.RunAsync(() =>
    {
        var radio = Templated(new RadioButton { Classes = { "fili" }, IsChecked = true });

        var ring = radio.GetVisualDescendants().OfType<Ellipse>().First(e => e.Name == "PART_Ring");
        var dot = radio.GetVisualDescendants().OfType<Ellipse>().First(e => e.Name == "PART_Dot");

        Assert.Equal(Primary(), Assert.IsAssignableFrom<ISolidColorBrush>(ring.Stroke).Color);
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(ring.Fill).Color);
        Assert.Equal(Primary(), Assert.IsAssignableFrom<ISolidColorBrush>(dot.Fill).Color);
    });

    /// <summary>
    /// ToggleSwitch.OnApplyTemplate looks these two parts up to wire knob dragging, and the
    /// lookups are null-safe — so renaming or dropping one costs the drag gesture silently,
    /// leaving a switch that only responds to clicks.
    /// </summary>
    [Theory]
    [InlineData("PART_SwitchKnob")]
    [InlineData("PART_MovingKnobs")]
    public Task ToggleSwitchKeepsItsDragParts(string part) => UiThread.RunAsync(() =>
    {
        var toggle = Templated(new ToggleSwitch { Classes = { "fili" } });

        Assert.Contains(
            toggle.GetVisualDescendants().OfType<Panel>(),
            p => p.Name == part);
    });

    /// <summary>
    /// Proves the <c>:checked</c> styles reached the template.
    /// <para>
    /// Deliberately asserted on the track rather than on the thumb's travel: the thumb transform
    /// carries a TransformOperationsTransition, so at the instant a test reads it the value is
    /// still mid-interpolation and compares equal to the resting one. Track opacity has no
    /// transition, so it flips synchronously. The travel itself is a visual check, in the gallery.
    /// </para>
    /// </summary>
    [Fact]
    public Task CheckedToggleSwitchTintsItsTrack() => UiThread.RunAsync(() =>
    {
        static Border Track(Control c) => c.GetVisualDescendants()
            .OfType<Border>()
            .First(b => b.Name == "PART_Track");

        var off = Track(Templated(new ToggleSwitch { Classes = { "fili" } }));
        var on = Track(Templated(new ToggleSwitch { Classes = { "fili" }, IsChecked = true }));

        Assert.Equal(1.0, off.Opacity);
        Assert.Equal(0.5, on.Opacity);
        Assert.Equal(Primary(), Assert.IsAssignableFrom<ISolidColorBrush>(on.Background).Color);
    });

    [Theory]
    [InlineData("FiliSlider", typeof(Slider))]
    [InlineData("FiliTabControl", typeof(TabControl))]
    [InlineData("FiliTabItem", typeof(TabItem))]
    public Task RangeAndNavigationThemesResolve(string key, Type target) => UiThread.RunAsync(() =>
    {
        Assert.True(
            Application.Current!.TryFindResource(key, ThemeVariant.Light, out var value),
            $"{key} did not resolve.");

        Assert.Equal(target, Assert.IsType<ControlTheme>(value).TargetType);
    });

    /// <summary>
    /// Slider.OnApplyTemplate requires a <c>Track</c> named PART_Track, and Track in turn requires
    /// a Thumb and both RepeatButtons. Those two buttons *are* the active and inactive halves of
    /// the rail — there is no separate fill element — so losing one loses half the slider.
    /// </summary>
    [Fact]
    public Task SliderKeepsTheTrackPartsAvaloniaRequires() => UiThread.RunAsync(() =>
    {
        var slider = Templated(new Slider { Classes = { "fili" }, Width = 200, Value = 40 });

        var track = slider.GetVisualDescendants().OfType<Track>().FirstOrDefault();

        Assert.NotNull(track);
        Assert.NotNull(track!.Thumb);
        Assert.NotNull(track.DecreaseButton);
        Assert.NotNull(track.IncreaseButton);
    });

    /// <summary>
    /// ItemContainerTheme is what carries the header theme down to a bare
    /// <c>&lt;TabItem&gt;</c>. Without it the TabControl is themed and its headers are not, which
    /// looks like the theme half-applied.
    /// </summary>
    [Fact]
    public Task ThemedTabControlThemesItsHeaders() => UiThread.RunAsync(() =>
    {
        var tabs = new TabControl { Classes = { "fili" }, Width = 300, Height = 120 };
        tabs.Items.Add(new TabItem { Header = "One" });
        tabs.Items.Add(new TabItem { Header = "Two" });

        Templated(tabs);

        var indicator = tabs.GetVisualDescendants()
            .OfType<Border>()
            .FirstOrDefault(b => b.Name == "PART_Indicator");

        Assert.NotNull(indicator);
    });

    private static Color Primary()
    {
        Application.Current!.TryFindResource("FiliPrimaryColor", ThemeVariant.Light, out var value);
        return (Color)value!;
    }

    /// <summary>
    /// Renders a control inside a headless window far enough that its template is built and its
    /// visual children exist.
    /// </summary>
    private static T Templated<T>(T control) where T : Control
    {
        var window = new Window { Content = control, Width = 400, Height = 200 };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.Measure(Size.Infinity);
        window.Arrange(new Rect(window.DesiredSize));
        Dispatcher.UIThread.RunJobs();

        return control;
    }
}
