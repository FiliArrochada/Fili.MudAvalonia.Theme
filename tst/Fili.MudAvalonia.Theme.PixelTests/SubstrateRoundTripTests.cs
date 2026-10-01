using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Fili.MudAvalonia.Theme.Gallery;
using Fili.MudAvalonia.Theme.Gallery.Desktop;
using Xunit;

namespace Fili.MudAvalonia.Theme.PixelTests;

/// <summary>
/// Flipping the gallery to Fluent and back must restore the standalone base exactly.
///
/// <para>
/// The base is not rebuilt on the way back: <c>App</c> keeps the instance App.axaml declared and
/// puts it back, because a StyleInclude constructed in code loads its source by reflection, which
/// trimming can strip from the browser build. This is the test that a kept instance, detached
/// and re-attached, still themes everything it did the first time.
/// </para>
/// <para>
/// It compares against the committed baseline, not against a frame rendered earlier in the same
/// test. Every render goes through <c>UseSubstrate</c>, so a broken restore breaks both frames
/// identically and a self-comparison passes - which is exactly what the first version of this
/// test did, with the base deliberately emptied.
/// </para>
/// </summary>
public class SubstrateRoundTripTests
{
    [Fact]
    public Task StandaloneFrameMatchesItsBaselineAfterAFluentRoundTrip() => UiThread.RunAsync(() =>
    {
        var standalone = new GalleryFrame("controls", Substrate.Standalone, ThemeVariant.Light);

        using (GalleryFrames.Render(standalone with { Substrate = Substrate.Fluent }))
        {
        }

        using var rendered = GalleryFrames.Render(standalone);
        using var baselineBitmap = new Bitmap(Path.Combine(Baselines.Directory, standalone.FileName));

        var actual = Frames.Read(rendered.Bitmap);
        var baseline = Frames.Read(baselineBitmap);

        Assert.True(
            actual.Size == baseline.Size,
            $"After flipping to Fluent and back, {standalone} rendered {actual.Size} against a "
            + $"baseline of {baseline.Size}.");

        var comparison = Frames.Compare(actual, baseline, rendered.UnstableRegions);

        Assert.True(
            comparison.Matches,
            $"After flipping to Fluent and back, {comparison.Differing:N0} pixels of {standalone} "
            + $"differ from its baseline, first at {comparison.First}.");
    });
}
