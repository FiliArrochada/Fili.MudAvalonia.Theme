using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Shapes = Avalonia.Controls.Shapes;
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
    [InlineData("FiliFilledButton")]
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
        var button = Templated(new Button { Classes = { "filled", "primary" }, Content = "Save" });

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
        var button = Templated(new Button { Classes = { "filled", "primary" }, Content = "Save" });

        var text = button.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();
        Assert.NotNull(text);

        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(text!.Foreground);

        Assert.Equal(Colors.White, brush.Color);
    });

    [Theory]
    [InlineData("FiliStandardTextBox")]
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
    [InlineData("")]  // standard - the type-keyed default, no class at all
    public Task TextFieldHasAFloatingLabel(string variant) => UiThread.RunAsync(() =>
    {
        var box = new TextBox { PlaceholderText = "Label" };
        if (variant.Length > 0)
        {
            box.Classes.Add(variant);
        }

        Templated(box);

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

    /// <summary>
    /// Selection is a GLYPH SWAP, not a box that fills.
    /// <para>
    /// MudCheckBox renders <c>Icons.Material.Filled.CheckBox</c> / <c>CheckBoxOutlineBlank</c> /
    /// <c>IndeterminateCheckBox</c> through MudIcon — it does not draw a box. An earlier version
    /// of this theme built an 18px Border with a stroked tick, which is Material's *spec* rather
    /// than MudBlazor's *implementation*, and could match neither the glyph corners nor the
    /// indeterminate bar. These assertions pin the swap so that cannot quietly come back.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData(true, "PART_Checked", "PART_Unchecked")]
    [InlineData(false, "PART_Unchecked", "PART_Checked")]
    public Task CheckBoxSwapsGlyphs(bool isChecked, string shown, string hidden) =>
        UiThread.RunAsync(() =>
        {
            var box = Templated(new CheckBox { IsChecked = isChecked });

            var glyphs = box.GetVisualDescendants().OfType<Shapes.Path>().ToList();

            Assert.True(glyphs.Single(p => p.Name == shown).IsVisible, $"{shown} should be shown.");
            Assert.False(glyphs.Single(p => p.Name == hidden).IsVisible, $"{hidden} should be hidden.");
        });

    [Fact]
    public Task CheckedCheckBoxGlyphIsPrimary() => UiThread.RunAsync(() =>
    {
        var box = Templated(new CheckBox { IsChecked = true });

        var glyph = box.GetVisualDescendants().OfType<Shapes.Path>().Single(p => p.Name == "PART_Checked");

        Assert.Equal(Primary(), Assert.IsAssignableFrom<ISolidColorBrush>(glyph.Fill).Color);
    });

    /// <summary>
    /// Same correction for the radio: MudRadio swaps RadioButtonChecked for
    /// RadioButtonUnchecked. The checked glyph is ONE path carrying both ring and dot, so there
    /// is no separate inner disc to grow — the earlier version's scale transition was Material
    /// spec, not MudBlazor.
    /// </summary>
    [Fact]
    public Task RadioButtonSwapsGlyphs() => UiThread.RunAsync(() =>
    {
        var radio = Templated(new RadioButton { IsChecked = true });

        var glyphs = radio.GetVisualDescendants().OfType<Shapes.Path>().ToList();

        Assert.True(glyphs.Single(p => p.Name == "PART_Checked").IsVisible);
        Assert.False(glyphs.Single(p => p.Name == "PART_Unchecked").IsVisible);
        Assert.Equal(
            Primary(),
            Assert.IsAssignableFrom<ISolidColorBrush>(
                glyphs.Single(p => p.Name == "PART_Checked").Fill).Color);
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
        var toggle = Templated(new ToggleSwitch());

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

        var off = Track(Templated(new ToggleSwitch()));
        var on = Track(Templated(new ToggleSwitch { IsChecked = true }));

        // MudBlazor _switch.scss holds the track at opacity .48 in BOTH states and changes only
        // its colour — action-default when off, the accent when on. An earlier version stepped
        // 1.0 -> 0.5, which is Material spec rather than what MudBlazor renders.
        Assert.Equal(0.48, off.Opacity);
        Assert.Equal(0.48, on.Opacity);

        Assert.Equal(
            ActionDefault(),
            Assert.IsAssignableFrom<ISolidColorBrush>(off.Background).Color);
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
        var slider = Templated(new Slider { Width = 200, Value = 40 });

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
        var tabs = new TabControl { Width = 300, Height = 120 };
        tabs.Items.Add(new TabItem { Header = "One" });
        tabs.Items.Add(new TabItem { Header = "Two" });

        Templated(tabs);

        var indicator = tabs.GetVisualDescendants()
            .OfType<Border>()
            .FirstOrDefault(b => b.Name == "PART_Indicator");

        Assert.NotNull(indicator);
    });

    /// <summary>
    /// The select's label floats when it HOLDS A VALUE, not when it has items.
    /// <para>
    /// The first version of this theme floated it with <c>:not(:empty)</c>, which reads like
    /// "has a selection" and means "has items" — so every select rendered with a permanently
    /// raised label and no resting state at all. There is no pseudoclass for a selection, so the
    /// theme swaps two TextBlocks on a null check instead, and this is what pins that.
    /// </para>
    /// </summary>
    [Fact]
    public Task SelectFloatsItsLabelOnlyWhenSomethingIsSelected() => UiThread.RunAsync(() =>
    {
        static (bool Resting, bool Floated) Labels(ComboBox box)
        {
            var blocks = box.GetVisualDescendants().OfType<TextBlock>().ToList();

            return (blocks.Single(b => b.Name == "PART_FloatingLabel").IsVisible,
                    blocks.Single(b => b.Name == "PART_SelectedLabel").IsVisible);
        }

        var empty = new ComboBox { PlaceholderText = "Label", Width = 190 };
        empty.Items.Add(new ComboBoxItem { Content = "One" });
        Templated(empty);

        var chosen = new ComboBox { PlaceholderText = "Label", Width = 190 };
        chosen.Items.Add(new ComboBoxItem { Content = "One" });
        chosen.SelectedIndex = 0;
        Templated(chosen);

        Assert.Equal((true, false), Labels(empty));
        Assert.Equal((false, true), Labels(chosen));
    });

    /// <summary>
    /// MudProgressLinear's Size defaults to Size.Small, so a bare bar is 4px — not the 8px that
    /// looking at a MudBlazor screenshot would suggest, and not the 16px the forked base sets.
    /// </summary>
    [Theory]
    [InlineData(null, 4d)]
    [InlineData("medium", 8d)]
    [InlineData("large", 12d)]
    public Task ProgressBarHeightFollowsTheMudSize(string? size, double expected) =>
        UiThread.RunAsync(() =>
        {
            var bar = new ProgressBar { Value = 50, Width = 220 };
            if (size is not null)
            {
                bar.Classes.Add(size);
            }

            Templated(bar);

            Assert.Equal(expected, bar.Bounds.Height);
        });

    /// <summary>
    /// The indeterminate bars are sized by a converter, and a converter that returns
    /// <c>UnsetValue</c> fails the way everything in this package fails — silently, leaving the
    /// bar at its natural size. This renders one and checks a bar actually got a width.
    /// <para>
    /// It also covers the class of bug that found the converter in the first place: a key frame
    /// value is parsed at RUNTIME, so a malformed one compiles cleanly and throws only when the
    /// template is instantiated.
    /// </para>
    /// </summary>
    [Fact]
    public Task IndeterminateBarsAreSizedFromTheRail() => UiThread.RunAsync(() =>
    {
        var bar = Templated(new ProgressBar { IsIndeterminate = true, Width = 200 });

        var bars = bar.GetVisualDescendants()
            .OfType<Border>()
            .Where(b => b.Name is "PART_IndeterminateIndicator" or "PART_IndeterminateIndicator2")
            .ToList();

        Assert.Equal(2, bars.Count);

        // ContainerWidth is 40% of the rail, so 200px gives 80.
        Assert.Equal(80d, bar.TemplateSettings.ContainerWidth);

        // Bar 1 runs from 0.875 of that (70px — MudBlazor's 35% of the whole rail) to 2.25 of it
        // (180px, the 90% frame). The assertion is a RANGE, not 70: the animation is already
        // interpolating by the time the test reads it, the same reason transitioned properties
        // cannot be asserted exactly anywhere else in this suite. If the converter returned
        // UnsetValue the width would be NaN, which is outside any range.
        var first = bars.Single(b => b.Name == "PART_IndeterminateIndicator");
        Assert.InRange(first.Width, 70d, 180d);

        // Bar 2 is still inside its 1.15s delay, so it rests at zero rather than at its natural
        // size. That resting value is the thing being pinned: without it, every indeterminate bar
        // flashes a solid full-width block for the first second.
        var second = bars.Single(b => b.Name == "PART_IndeterminateIndicator2");
        Assert.Equal(0d, second.Width);
    });

    /// <summary>
    /// <c>.mud-divider</c> is <c>margin: 0</c>. The forked Simple template ships
    /// <c>Margin="29,1,0,1"</c> — a menu-shaped indent baked into every divider — so this pins
    /// that the hand-written theme is the one in force.
    /// </summary>
    [Fact]
    public Task DividerHasNoMarginOfItsOwn() => UiThread.RunAsync(() =>
    {
        var separator = Templated(new Separator());

        Assert.Equal(default, separator.Margin);
        Assert.Equal(1d, separator.Bounds.Height);
    });

    /// <summary>
    /// A failing binding shows its MESSAGE under the field, not its exception type.
    /// <para>
    /// Both halves of this were bugs. The template had no <c>DataValidationErrors</c> host at
    /// all, so an invalid field showed nothing; and once it did, binding straight to the error
    /// object rendered <c>"System.InvalidOperationException: ..."</c>, because an entry in that
    /// collection is an exception and <c>ToString()</c> on one carries the type name.
    /// </para>
    /// </summary>
    [Fact]
    public Task AnInvalidFieldShowsTheMessageAndNotTheExceptionType() => UiThread.RunAsync(() =>
    {
        var box = new TextBox { Width = 220, PlaceholderText = "Email" };
        DataValidationErrors.SetError(box, new InvalidOperationException("Must be an email"));

        Templated(box);

        var texts = box.GetVisualDescendants()
            .OfType<TextBlock>()
            .Select(t => t.Text)
            .ToList();

        Assert.Contains("Must be an email", texts);
        Assert.DoesNotContain(texts, t => t?.Contains("InvalidOperationException") == true);
    });

    /// <summary>
    /// The spin column is 24px because _inputcontrol.scss reserves exactly that much padding for
    /// it — "This must be the same width of the spinners". A different width here would leave the
    /// number either colliding with the arrows or floating short of them.
    /// </summary>
    [Fact]
    public Task NumericFieldSpinColumnIsTwentyFourWide() => UiThread.RunAsync(() =>
    {
        var numeric = Templated(new NumericUpDown { Value = 42, Width = 190 });

        var spinners = numeric.GetVisualDescendants()
            .OfType<UniformGrid>()
            .Single(g => g.Name == "PART_SpinnerPanel");

        Assert.Equal(24d, spinners.Bounds.Width);
        Assert.Equal(2, spinners.Rows);
    });

    /// <summary>
    /// Keyboard focus lights the state layer, and a pointer click does not.
    /// <para>
    /// This is the test that matters most in this file, because <c>:focus-visible</c> compiles
    /// whether or not it ever matches — a selector that silently never fires is the same class of
    /// failure as a misspelt resource key, and it would leave the theme with no keyboard focus
    /// indication at all while looking entirely fine.
    /// </para>
    /// <para>
    /// The negative half is the point of using <c>:focus-visible</c> rather than <c>:focus</c>:
    /// clicking a button should not leave it tinted after the pointer goes away.
    /// </para>
    /// </summary>
    [Fact]
    public Task KeyboardFocusLightsTheStateLayerAndPointerFocusDoesNot() => UiThread.RunAsync(() =>
    {
        // TWO buttons, not one focused twice. Re-focusing an element that already has focus does
        // not revisit the focus-visible flag, so a single button focused by pointer and then by
        // Tab stays untinted and the test passes for the wrong reason.
        var keyboard = new Button { Content = "Save" };
        var pointer = new Button { Content = "Cancel" };

        Templated(new StackPanel { Children = { keyboard, pointer } });

        static Border Layer(Control c) => c.GetVisualDescendants()
            .OfType<Border>()
            .Single(b => b.Name == "PART_StateLayer");

        static Color Of(IBrush? brush) => ((ISolidColorBrush)brush!).Color;

        keyboard.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();

        // A button's tint is MudBlazor's action-default-hover (_button.scss), not the generic
        // Material overlay the circular halos below use.
        Assert.True(Application.Current!.TryFindResource("FiliActionDefaultHoverColor", ThemeVariant.Light, out var tint));
        Assert.Equal((Color)tint!, Of(Layer(keyboard).Background));

        pointer.Focus(NavigationMethod.Pointer);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Colors.Transparent, Of(Layer(pointer).Background));
    });

    /// <summary>
    /// The same affordance, on the controls whose state layer is a circular halo rather than a
    /// rounded rectangle. Parameterised because each one wires its own part, and a theme that
    /// forgot one would still pass the Button test.
    /// </summary>
    [Theory]
    [InlineData(typeof(CheckBox), "PART_StateLayer")]
    [InlineData(typeof(RadioButton), "PART_StateLayer")]
    [InlineData(typeof(ToggleSwitch), "PART_ThumbStateLayer")]
    public Task KeyboardFocusLightsTheHalo(Type controlType, string part) => UiThread.RunAsync(() =>
    {
        var control = Templated((Control)Activator.CreateInstance(controlType)!);

        var halo = control.GetVisualDescendants()
            .OfType<Shapes.Ellipse>()
            .Single(e => e.Name == part);

        ((InputElement)control).Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(OverlayHover(), ((ISolidColorBrush)halo.Fill!).Color);
    });

    /// <summary>
    /// Depth is 17px per level, and a leaf shows no arrow.
    /// <para>
    /// Both are runtime mechanisms that fail silently. The indent is a MultiBinding through
    /// Avalonia's own <c>TreeViewItemIndentConverter</c> — if the resource key it multiplies is
    /// wrong, every level sits at the same depth and the tree still renders. The arrow is hidden
    /// by an attribute selector on <c>ItemCount</c>, which compiles whether or not it matches.
    /// </para>
    /// </summary>
    [Fact]
    public Task TreeIndentsSeventeenPerLevelAndHidesLeafArrows() => UiThread.RunAsync(() =>
    {
        var leaf = new TreeViewItem { Header = "Installed" };
        var branch = new TreeViewItem { Header = "Library", IsExpanded = true };
        branch.Items.Add(leaf);

        var tree = new TreeView { Width = 220, Height = 140 };
        tree.Items.Add(branch);

        Templated(tree);

        static Grid Header(Control c) => c.GetVisualDescendants()
            .OfType<Grid>()
            .First(g => g.Name == "PART_Header");

        // Level 0 gets no indent, level 1 gets one step of 17. _treeview.scss puts that 17px on
        // .mud-treeview-group; Avalonia reaches the same place by multiplying Level instead.
        Assert.Equal(0d, Header(branch).Margin.Left);
        Assert.Equal(17d, Header(leaf).Margin.Left);

        static ToggleButton Arrow(Control c) => c.GetVisualDescendants()
            .OfType<ToggleButton>()
            .First(t => t.Name == "PART_ExpandCollapseChevron");

        Assert.True(Arrow(branch).IsVisible);
        Assert.False(Arrow(leaf).IsVisible);
    });

    /// <summary>
    /// The drawer is themed with STYLES rather than a ControlTheme, and this pins that it works.
    /// <para>
    /// A Style outranks a ControlTheme setter — normally the trap this repo warns about, relied
    /// on deliberately here so the forked SplitView template keeps the pane sliding and the
    /// display modes while painting MudDrawer's values. If Avalonia ever reversed that
    /// precedence, every drawer would silently revert to Simple's 320px grey pane.
    /// </para>
    /// </summary>
    [Fact]
    public Task DrawerTakesMudDrawerMetricsOverTheForkedTemplate() => UiThread.RunAsync(() =>
    {
        var drawer = Templated(new SplitView
        {
            IsPaneOpen = true,
            Pane = new TextBlock { Text = "Library" },
            Content = new TextBlock { Text = "Games" },
        });

        // LayoutProperties.cs: DrawerWidthLeft "240px", DrawerMiniWidthLeft "56px".
        // Simple's forked theme sets 320 and 48.
        Assert.Equal(240d, drawer.OpenPaneLength);
        Assert.Equal(56d, drawer.CompactPaneLength);

        Application.Current!.TryFindResource("FiliDrawerBackgroundColor", ThemeVariant.Light, out var drawerBackground);
        Assert.Equal(
            (Color)drawerBackground!,
            Assert.IsAssignableFrom<ISolidColorBrush>(drawer.PaneBackground).Color);
    });

    /// <summary>
    /// An unclassed TextBlock must keep the font's natural line height.
    /// <para>
    /// This package used to set body2's LineHeight on a blanket <c>TextBlock</c> selector, which
    /// silently CLIPPED any text larger than 20px — a 28px heading got a 20px line box and lost
    /// its descenders. It was invisible here, because this package's own gallery labels
    /// everything with a ramp class that sets its own LineHeight, and it cost Fili.PlaySphere
    /// every page title before anyone rendered it.
    /// </para>
    /// <para>
    /// So: the ramp classes carry the metrics, and bare text is left alone. An adopter's type
    /// scale will never know to opt out of a rule it does not know exists.
    /// </para>
    /// </summary>
    [Fact]
    public Task UnclassedTextKeepsItsNaturalLineHeight() => UiThread.RunAsync(() =>
    {
        var bare = Templated(new TextBlock { Text = "A heading an app styled itself", FontSize = 28 });

        Assert.True(
            double.IsNaN(bare.LineHeight),
            $"A bare TextBlock got LineHeight {bare.LineHeight}, which clips any font taller than it.");

        // The ramp still carries its own metrics, which is where they belong.
        var body = Templated(new TextBlock { Classes = { "body2" }, Text = "Body" });

        Assert.False(double.IsNaN(body.LineHeight));
    });

    private static Color OverlayHover()
    {
        Application.Current!.TryFindResource("FiliOverlayHoverColor", ThemeVariant.Light, out var value);
        return (Color)value!;
    }

    private static Color ActionDefault()
    {
        Application.Current!.TryFindResource("FiliActionDefaultColor", ThemeVariant.Light, out var value);
        return (Color)value!;
    }

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
