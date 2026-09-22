using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Shapes = Avalonia.Controls.Shapes;
using Xunit;

namespace Fili.MudAvalonia.Theme.UnitTests;

/// <summary>
/// Right-to-left behaviour, and the one case the framework gets wrong for us.
///
/// <para>
/// The headline finding, measured rather than assumed: <b>Avalonia mirrors an entire subtree with
/// a single transform, applied where the flow direction CHANGES.</b> Grid columns, dock sides and
/// Left/Right alignment therefore flip on their own, and not one template in this package needed
/// rewriting for RTL. MudBlazor reaches the same place with logical CSS properties
/// (<c>margin-inline-end</c> and friends), which is why those have no counterpart here.
/// </para>
///
/// <para>
/// The exception is a glyph that must NOT mirror. A checkmark is not a directional symbol, so
/// Material's bidirectionality guidance leaves it alone in RTL, and the checkbox paths opt out
/// with <c>FlowDirection="LeftToRight"</c>. Avalonia's own Simple theme does the same to its
/// check path, which is how the case was spotted. Arrows are deliberately left to mirror: a
/// tree's disclosure arrow SHOULD point left in RTL.
/// </para>
/// </summary>
public class RightToLeftTests
{
    /// <summary>
    /// The mirror exists, and it sits on the element where the direction changes rather than on
    /// every control beneath it. If that stopped being true, every template here would need
    /// explicit mirroring and none of them have any.
    /// </summary>
    [Fact]
    public Task TheMirrorSitsWhereTheDirectionChanges() => UiThread.RunAsync(() =>
    {
        var combo = new ComboBox { PlaceholderText = "Select", Width = 200 };
        combo.Items.Add(new ComboBoxItem { Content = "One" });

        var panel = new StackPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Children = { combo },
        };

        var window = Render(panel);

        Assert.True(panel.HasMirrorTransform, "The RTL subtree carried no mirror transform.");

        // The control inherits the direction, so it does NOT carry a mirror of its own: one
        // transform covers everything below it.
        Assert.Equal(FlowDirection.RightToLeft, combo.FlowDirection);
        Assert.False(combo.HasMirrorTransform);

        // And the mirror really moves things. The comparison has to CROSS the boundary: measured
        // against the mirrored panel itself, coordinates are still left-to-right, which is what
        // made the first version of this test fail. Against the window the chevron's origin lands
        // to the LEFT of the control's — the reverse of the left-to-right case.
        var chevron = combo.GetVisualDescendants()
            .OfType<Shapes.Path>()
            .First(p => p.Name == "PART_Chevron");

        var chevronX = chevron.TranslatePoint(new Point(0, 0), window)!.Value.X;
        var comboX = combo.TranslatePoint(new Point(0, 0), window)!.Value.X;

        Assert.True(chevronX < comboX, $"chevron at {chevronX} was not mirrored past {comboX}.");
    });

    /// <summary>
    /// The checkbox glyphs opt out of mirroring; the tree's arrow does not.
    /// <para>
    /// Asserted on the property rather than on pixels because that is the mechanism — and because
    /// a reversed tick is exactly the kind of thing that survives review for months.
    /// </para>
    /// </summary>
    [Fact]
    public Task CheckMarksDoNotMirrorButArrowsDo() => UiThread.RunAsync(() =>
    {
        var box = new CheckBox { IsChecked = true, Content = "Enabled" };

        var branch = new TreeViewItem { Header = "Library", IsExpanded = true };
        branch.Items.Add(new TreeViewItem { Header = "Installed" });
        var tree = new TreeView { Width = 200, Height = 100 };
        tree.Items.Add(branch);

        _ = Render(new StackPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Children = { box, tree },
        });

        foreach (var name in new[] { "PART_Unchecked", "PART_Checked", "PART_Indeterminate" })
        {
            var glyph = box.GetVisualDescendants().OfType<Shapes.Path>().First(p => p.Name == name);

            Assert.Equal(FlowDirection.LeftToRight, glyph.FlowDirection);
        }

        // The disclosure arrow is directional, so it inherits the mirror and points the other
        // way. Opting it out would leave it pointing away from the content it opens.
        var arrow = branch.GetVisualDescendants()
            .OfType<Shapes.Path>()
            .First(p => p.Name == "PART_Chevron");

        Assert.Equal(FlowDirection.RightToLeft, arrow.FlowDirection);
    });

    private static Window Render(Control content)
    {
        var window = new Window { Content = content, Width = 400, Height = 300 };

        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(400, 300));
        window.Arrange(new Rect(0, 0, 400, 300));
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
