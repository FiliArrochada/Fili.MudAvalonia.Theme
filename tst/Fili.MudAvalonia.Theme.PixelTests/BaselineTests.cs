using Avalonia.Media.Imaging;
using Fili.MudAvalonia.Theme.Gallery.Desktop;
using Xunit;

namespace Fili.MudAvalonia.Theme.PixelTests;

/// <summary>
/// Pixel regression: every baselined gallery frame, rendered and compared with the PNG committed
/// beside this file.
///
/// <para>
/// This is the only test in the repository that can notice a control theme restyling a control
/// nobody was looking at. Everything else asserts that a key resolves, that a setter is present,
/// that a size is not zero — all of which stay true while a frame changes completely. Control
/// themes here are keyed by TYPE, so one edit reaches every screen, and the two defects found by
/// hand in adopting apps — headings clipped by a line height, a field measuring to nothing —
/// were both invisible to every assertion in the suite and obvious in a picture.
/// </para>
///
/// <para>
/// <b>When this fails and the change was intended</b>, look at the diff image whose path the
/// failure prints, then accept the new frames:
/// <code>FILI_PIXEL_BASELINES=accept dotnet test tst/Fili.MudAvalonia.Theme.PixelTests</code>
/// Accepting rewrites the PNGs in the source tree, so the new look arrives as a reviewable diff
/// in the same commit as the change that caused it. That review is the entire value of this
/// suite; accepting without looking throws it away.
/// </para>
///
/// <para>
/// <b>Baselines are platform-specific.</b> They were rendered on Windows with Skia, and text
/// rasterisation is not identical across platforms — a Linux CI runner would fail every frame on
/// glyph edges alone. Committing a second set per platform is the answer if that day comes; a
/// tolerance wide enough to cover it would be wide enough to hide real changes.
/// </para>
/// </summary>
public class BaselineTests
{
    private const string AcceptVariable = "FILI_PIXEL_BASELINES";
    private const string DiffVariable = "FILI_PIXEL_DIFF_DIR";

    public static TheoryData<string> BaselinedFrames
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var frame in GalleryFrames.All)
            {
                data.Add(frame.ToString());
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(BaselinedFrames))]
    public Task FrameMatchesItsBaseline(string name) => UiThread.RunAsync(() =>
    {
        var frame = GalleryFrames.All.Single(f => f.ToString() == name);
        var baselinePath = Path.Combine(Baselines.Directory, frame.FileName);

        using var rendered = GalleryFrames.Render(frame);

        if (Accepting)
        {
            Directory.CreateDirectory(Baselines.Directory);
            Frames.Save(rendered.Bitmap, baselinePath);

            return;
        }

        Assert.True(
            File.Exists(baselinePath),
            $"No baseline for {name}. Create it with {AcceptVariable}=accept and review the PNG "
            + "before committing it.");

        var actual = Frames.Read(rendered.Bitmap);

        using var baselineBitmap = new Bitmap(baselinePath);
        var baseline = Frames.Read(baselineBitmap);

        // Size first, and on its own. A frame that changed height differs in every row below the
        // change, so a pixel count would report a million differences and say nothing; the height
        // IS the finding, and it usually means a control's measurement changed.
        Assert.True(
            actual.Size == baseline.Size,
            $"{name} rendered {actual.Size} against a baseline of {baseline.Size}. "
            + "Something changed how the view measures, not only how it paints.");

        var comparison = Frames.Compare(actual, baseline, rendered.UnstableRegions);

        if (comparison.Matches)
        {
            return;
        }

        Directory.CreateDirectory(DiffDirectory);

        var actualPath = Path.Combine(DiffDirectory, $"{name}-actual.png");
        var diffPath = Path.Combine(DiffDirectory, $"{name}-diff.png");

        Frames.Save(rendered.Bitmap, actualPath);
        Frames.WriteDiff(diffPath, actual, baseline, rendered.UnstableRegions);

        Assert.Fail(
            $"""
             {name} does not match its baseline.

               {comparison.Differing:N0} of {comparison.Total:N0} pixels differ ({comparison.Percent:N4}%),
               the largest by {comparison.MaxDelta} of 255, first at {comparison.First}.

               rendered: {actualPath}
               diff:     {diffPath}   (red = changed, orange = masked animation)
               baseline: {baselinePath}

             If the change was intended, accept it with {AcceptVariable}=accept and commit the
             new PNGs alongside it.
             """);
    });

    /// <summary>
    /// Where a failure leaves the rendered frame and the diff image.
    ///
    /// <para>
    /// Overridable because a CI runner has to collect them: the system temp directory is not
    /// somewhere an artifact upload can reach, and a failure whose evidence nobody can open is a
    /// failure that will be re-run rather than read.
    /// </para>
    /// </summary>
    private static string DiffDirectory =>
        Environment.GetEnvironmentVariable(DiffVariable) is { Length: > 0 } configured
            ? configured
            : Path.Combine(Path.GetTempPath(), "fili-pixel-diff");

    private static bool Accepting =>
        string.Equals(
            Environment.GetEnvironmentVariable(AcceptVariable),
            "accept",
            StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Where the committed baselines live.
/// </summary>
/// <remarks>
/// Found by walking up from the test binary to the solution, rather than by a path in a config
/// file: the tests have to write accepted baselines back into the SOURCE tree, and a copy under
/// bin would be accepted, admired and then deleted by the next clean.
/// </remarks>
public static class Baselines
{
    public static string Directory { get; } = Locate();

    private static string Locate()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Fili.MudAvalonia.Theme.sln")))
            {
                return Path.Combine(
                    directory.FullName, "tst", "Fili.MudAvalonia.Theme.PixelTests", "Baselines");
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not find Fili.MudAvalonia.Theme.sln above the test binary, so there is no "
            + "source tree to read baselines from.");
    }
}
