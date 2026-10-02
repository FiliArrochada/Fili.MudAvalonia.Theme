using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Fili.MudAvalonia.Theme.UnitTests;

/// <summary>
/// MudInputControl's helper line - helper text, the error in its place, and the counter - plus
/// Margin.Dense and Variant on a select. Values from _inputcontrol.scss, _input.scss and
/// _inputlabel.scss; the counter's text is MudTextField.GetCounterText.
/// </summary>
public class FieldDetailTests
{
    private static TextBlock Block(Control field, string name) =>
        field.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == name);

    private static T Named<T>(Control field, string name) where T : Control =>
        field.GetVisualDescendants().OfType<T>().Single(t => t.Name == name);

    /// <summary>The visible texts under the field, in the helper line.</summary>
    private static List<string?> HelperTexts(Control field) =>
        Named<Grid>(field, "PART_HelperLine").GetVisualDescendants()
            .OfType<TextBlock>()
            .Where(t => t.IsEffectivelyVisible)
            .Select(t => t.Text)
            .ToList();

    // --------------------------------------------------------------------------------------
    // Helper text
    // --------------------------------------------------------------------------------------

    [Fact]
    public Task HelperTextIsCaptionTextSecondaryUnderTheField() => UiThread.RunAsync(() =>
    {
        var box = new TextBox { Width = 220, PlaceholderText = "Email" };
        AutomationProperties.SetHelpText(box, "We never share it");
        Matrix.Show(box);

        var helper = Block(box, "PART_HelperText");
        Assert.True(helper.IsEffectivelyVisible);
        Assert.Equal("We never share it", helper.Text);
        Assert.Equal(12, helper.FontSize);
        Assert.Equal(20, helper.LineHeight);
        Assert.Equal(Matrix.Token("FiliTextSecondaryColor"), Matrix.Colour(helper.Foreground));
        Assert.Equal(3, helper.Margin.Top);

        // Under the field, not beside or over it.
        var root = Named<Border>(box, "PART_Root");
        Assert.True(helper.TranslatePoint(default, box)!.Value.Y >= root.Bounds.Height);
    });

    /// <summary>An empty helper line takes no room: a plain field is exactly as tall as before.</summary>
    [Fact]
    public Task NoHelperTakesNoRoom() => UiThread.RunAsync(() =>
    {
        var box = Matrix.Show(new TextBox { Width = 220, PlaceholderText = "Email" });

        Assert.Equal(Named<Border>(box, "PART_Root").Bounds.Height, box.Bounds.Height);
        Assert.Empty(HelperTexts(box));
    });

    /// <summary>MudInputControl: the error takes the helper text's place, and the row goes red.</summary>
    [Fact]
    public Task AnErrorReplacesTheHelperText() => UiThread.RunAsync(() =>
    {
        var box = new TextBox { Width = 220, PlaceholderText = "Email", Classes = { "counter" }, MaxLength = 50, Text = "abc" };
        AutomationProperties.SetHelpText(box, "We never share it");
        DataValidationErrors.SetError(box, new InvalidOperationException("Must be an email"));
        Matrix.Show(box);

        var texts = HelperTexts(box);
        Assert.Contains("Must be an email", texts);
        Assert.DoesNotContain("We never share it", texts);
        Assert.Equal(Matrix.Token("FiliErrorColor"), Matrix.Colour(Block(box, "PART_CounterLimit").Foreground));
    });

    [Fact]
    public Task TheErrorClassHidesTheHelperTextToo() => UiThread.RunAsync(() =>
    {
        var box = new TextBox { Width = 220, Classes = { "error" } };
        AutomationProperties.SetHelpText(box, "We never share it");
        Matrix.Show(box);

        Assert.False(Block(box, "PART_HelperText").IsEffectivelyVisible);
    });

    /// <summary>
    /// HelperTextOnFocus: the help keeps its room and shows only while the field has focus, so
    /// focusing a field never pushes the layout below it down.
    /// </summary>
    [Fact]
    public Task HelperOnFocusShowsOnlyWhileFocused() => UiThread.RunAsync(() =>
    {
        var box = new TextBox { Width = 220, Classes = { "helper-on-focus" } };
        AutomationProperties.SetHelpText(box, "Shown while focused");
        Matrix.Show(box);
        var helper = Block(box, "PART_HelperText");
        var height = box.Bounds.Height;

        Assert.Equal(0, helper.Opacity);
        Assert.True(helper.IsEffectivelyVisible, "The help keeps its room while hidden.");

        box.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, helper.Opacity);
        Assert.Equal(height, box.Bounds.Height);
    });

    [Theory]
    [InlineData("filled", 4)]
    [InlineData("outlined", 8)]
    public Task TheHelperLineIsInsetUnderFilledAndOutlinedFields(string variant, double inset) => UiThread.RunAsync(() =>
    {
        var box = new TextBox { Width = 220, Classes = { variant } };
        AutomationProperties.SetHelpText(box, "Help");
        Matrix.Show(box);

        Assert.Equal(new Thickness(inset, 0), Named<Grid>(box, "PART_HelperLine").Margin);
    });

    // --------------------------------------------------------------------------------------
    // Counter
    // --------------------------------------------------------------------------------------

    [Fact]
    public Task TheCounterCountsAgainstMaxLength() => UiThread.RunAsync(() =>
    {
        var box = Matrix.Show(new TextBox { Width = 220, Classes = { "counter" }, MaxLength = 50, Text = "hello" });

        Assert.Equal(["5", " / 50"], HelperTexts(box));

        box.Text = "hello there";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["11", " / 50"], HelperTexts(box));
    });

    /// <summary>MudBlazor's Counter="0": the length alone. Here, a counter with no MaxLength.</summary>
    [Fact]
    public Task WithoutMaxLengthTheCounterIsTheLengthAlone() => UiThread.RunAsync(() =>
    {
        var empty = Matrix.Show(new TextBox { Width = 220, Classes = { "counter" } });
        Assert.Equal(["0"], HelperTexts(empty));

        var typed = Matrix.Show(new TextBox { Width = 220, Classes = { "counter" }, Text = "abc" });
        Assert.Equal(["3"], HelperTexts(typed));
    });

    [Fact]
    public Task NoCounterWithoutTheClass() => UiThread.RunAsync(() =>
    {
        var box = Matrix.Show(new TextBox { Width = 220, MaxLength = 50, Text = "hello" });

        Assert.Empty(HelperTexts(box));
    });

    // --------------------------------------------------------------------------------------
    // Margin.Dense
    // --------------------------------------------------------------------------------------

    /// <summary>
    /// The dense deltas, not absolute heights: a standard field is as tall as its content wants
    /// (51px), past its 48px minimum, so only the change is MudBlazor's - 3px off the standard
    /// field, 4px top and bottom off filled, 8px top and bottom off outlined.
    /// </summary>
    [Theory]
    [InlineData(null, 3)]
    [InlineData("filled", 8)]
    [InlineData("outlined", 16)]
    public Task DenseFieldsAreShorter(string? variant, double delta) => UiThread.RunAsync(() =>
    {
        TextBox Field(params string[] extra)
        {
            var box = new TextBox { Width = 220, PlaceholderText = "Label" };
            if (variant is not null) box.Classes.Add(variant);
            box.Classes.AddRange(extra);
            return Matrix.Show(box);
        }

        ComboBox Select(params string[] extra)
        {
            var combo = new ComboBox { Width = 220, PlaceholderText = "Label" };
            combo.Items.Add(new ComboBoxItem { Content = "One" });
            if (variant is not null) combo.Classes.Add(variant);
            combo.Classes.AddRange(extra);
            return Matrix.Show(combo);
        }

        Assert.Equal(delta, Field().Bounds.Height - Field("dense").Bounds.Height, 1);
        Assert.Equal(delta, Select().Bounds.Height - Select("dense").Bounds.Height, 1);
    });

    /// <summary>
    /// _input.scss pads filled content 12px and outlined 14px from the side, label included.
    /// <para>
    /// Both shipped flush to the edge: the template wrote the content margin inline, which binds
    /// at Template priority, and the variants' plain <c>^ /template/</c> styles cannot beat that
    /// - while their <c>.dense</c> styles, carrying an activator, could. Asserting the property
    /// rather than eyeballing a frame is what catches it.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData(null, 0, 20)]
    [InlineData("filled", 12, 20)]
    [InlineData("outlined", 14, 16)]
    public Task VariantsPadTheirContentFromTheSide(string? variant, double side, double top) => UiThread.RunAsync(() =>
    {
        var box = new TextBox { Width = 220, PlaceholderText = "Label" };
        var combo = new ComboBox { Width = 220, PlaceholderText = "Label" };
        if (variant is not null)
        {
            box.Classes.Add(variant);
            combo.Classes.Add(variant);
        }
        Matrix.Show(box);
        Matrix.Show(combo);

        Assert.Equal(side, Named<DockPanel>(box, "PART_ContentArea").Margin.Left);
        Assert.Equal(top, Named<DockPanel>(box, "PART_ContentArea").Margin.Top);
        Assert.Equal(side, Block(box, "PART_FloatingLabel").Margin.Left);
        Assert.Equal(side, Named<Grid>(combo, "PART_ContentArea").Margin.Left);
        Assert.Equal(top, Named<Grid>(combo, "PART_ContentArea").Margin.Top);
        Assert.Equal(side, Block(combo, "PART_FloatingLabel").Margin.Left);
    });

    // --------------------------------------------------------------------------------------
    // Variant on a select
    // --------------------------------------------------------------------------------------

    private static ComboBox ShowSelect(string? cls, bool selected)
    {
        var combo = new ComboBox { Width = 220, PlaceholderText = "Label" };
        combo.Items.Add(new ComboBoxItem { Content = "One" });
        if (cls is not null) combo.Classes.Add(cls);
        if (selected) combo.SelectedIndex = 0;
        return Matrix.Show(combo);
    }

    [Fact]
    public Task AFilledSelectIsAFilledField() => UiThread.RunAsync(() =>
    {
        var combo = ShowSelect("filled", selected: true);
        var root = Named<Border>(combo, "PART_Root");

        Assert.Equal(Matrix.Token("FiliInputFilledColor"), Matrix.Colour(root.Background));
        Assert.Equal(new CornerRadius(4, 4, 0, 0), root.CornerRadius);
        Assert.Equal(new Thickness(12, 0), Block(combo, "PART_SelectedLabel").Margin);
        Assert.Equal(-10, ((TransformOperations)Block(combo, "PART_SelectedLabel").RenderTransform!).Value.M32);
    });

    /// <summary>
    /// The selected label's resting position is written INLINE in the template, which a plain
    /// style cannot override - so this is the check that the outlined theme actually moves it
    /// onto the stroke, and masks it there.
    /// </summary>
    [Fact]
    public Task AnOutlinedSelectIsBoxedWithItsLabelOnTheStroke() => UiThread.RunAsync(() =>
    {
        var combo = ShowSelect("outlined", selected: true);
        var root = Named<Border>(combo, "PART_Root");
        var label = Block(combo, "PART_SelectedLabel");

        Assert.Equal(Matrix.Token("FiliLinesInputsColor"), Matrix.Colour(root.BorderBrush));
        Assert.Equal(new Thickness(1), root.BorderThickness);
        Assert.False(Named<Border>(combo, "PART_Underline").IsVisible);
        Assert.Equal(-26, ((TransformOperations)label.RenderTransform!).Value.M32);
        Assert.Equal(Matrix.Token("FiliSurfaceColor"), Matrix.Colour(label.Background));
    });

    [Fact]
    public Task ADenseOutlinedSelectFloatsLower() => UiThread.RunAsync(() =>
    {
        var combo = new ComboBox { Width = 220, PlaceholderText = "Label", Classes = { "outlined", "dense" } };
        combo.Items.Add(new ComboBoxItem { Content = "One" });
        combo.SelectedIndex = 0;
        Matrix.Show(combo);

        Assert.Equal(-18, ((TransformOperations)Block(combo, "PART_SelectedLabel").RenderTransform!).Value.M32);
    });

    /// <summary>A select used to have no error host at all: a failing binding showed nothing.</summary>
    [Fact]
    public Task AnInvalidSelectShowsItsErrorAndRedRule() => UiThread.RunAsync(() =>
    {
        var combo = new ComboBox { Width = 220, PlaceholderText = "Library" };
        combo.Items.Add(new ComboBoxItem { Content = "One" });
        AutomationProperties.SetHelpText(combo, "Where new files go");
        DataValidationErrors.SetError(combo, new InvalidOperationException("Pick a library"));
        Matrix.Show(combo);

        var texts = HelperTexts(combo);
        Assert.Contains("Pick a library", texts);
        Assert.DoesNotContain("Where new files go", texts);
        Assert.Equal(Matrix.Token("FiliErrorColor"), Matrix.Colour(Named<Border>(combo, "PART_Underline").Background));
    });

    [Fact]
    public Task ASelectShowsHelperText() => UiThread.RunAsync(() =>
    {
        var combo = new ComboBox { Width = 220, PlaceholderText = "Library" };
        AutomationProperties.SetHelpText(combo, "Where new files go");
        Matrix.Show(combo);

        Assert.Equal(["Where new files go"], HelperTexts(combo));
    });
}
