using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Fili.MudAvalonia.Theme;
using Fili.MudAvalonia.Theme.Gallery.Views;

namespace Fili.MudAvalonia.Theme.Gallery.Desktop;

/// <summary>One rendered gallery frame: a view, in a theme variant.</summary>
public readonly record struct GalleryFrame(string View, ThemeVariant Variant)
{
    /// <summary>The frame's file name, which is also its baseline's and its identity in a test.</summary>
    public string FileName => $"{View}-{Variant}.png".ToLowerInvariant();

    public override string ToString() => FileName[..^4];
}

/// <summary>
/// A rendered frame, and the regions of it that are not reproducible — see
/// <see cref="GalleryFrames.Render"/>.
/// </summary>
public sealed record RenderedFrame(WriteableBitmap Bitmap, IReadOnlyList<PixelRect> UnstableRegions)
    : IDisposable
{
    public void Dispose() => Bitmap.Dispose();
}

/// <summary>
/// The frame catalogue and the one routine that renders them, shared by the screenshot mode and
/// the pixel-regression suite.
///
/// <para>
/// Shared on purpose. Two copies of "set up a window, measure it, take the frame" drift, and the
/// day they drift is the day the baselines stop describing what <c>--capture</c> produces — which
/// is the only thing that makes either of them worth having.
/// </para>
/// </summary>
public static class GalleryFrames
{
    private static readonly (string Name, Func<Control> Build)[] Views =
    [
        ("controls", () => new ControlStatesView()),
        ("screen", () => new SampleScreenView()),
        ("palette", () => new PaletteView()),
    ];

    private static readonly ThemeVariant[] Variants =
        [ThemeVariant.Light, ThemeVariant.Dark, FiliThemeVariants.HighContrast];

    /// <summary>
    /// Every frame: each view in each variant. The screenshot mode writes all of them, and every
    /// one has a committed baseline.
    /// </summary>
    public static IReadOnlyList<GalleryFrame> All { get; } =
    [
        .. from variant in Variants
           from view in Views
           select new GalleryFrame(view.Name, variant),
    ];

    /// <summary>
    /// The app builder both callers use.
    ///
    /// <para>
    /// The one non-obvious requirement is <c>UseHeadlessDrawing = false</c> plus Skia. Headless
    /// drawing is the default and it is a no-op renderer: everything "works", every capture comes
    /// back blank, and a pixel suite built on it would compare two blank images and pass forever.
    /// </para>
    /// </summary>
    public static AppBuilder Configure() =>
        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithInterFont()
            .WithGalleryFonts();

    /// <summary>
    /// Renders one frame. The application must already be set up — by the screenshot mode's own
    /// call to <c>SetupWithoutStarting</c>, or by the headless session in a test.
    /// </summary>
    public static RenderedFrame Render(GalleryFrame frame)
    {
        // Rendered under the invariant culture, so a frame is the same on every machine. The
        // palette tab formats contrast ratios with the current culture - "6,00:1" on a pt-PT
        // machine, "6.00:1" on an en-US CI runner - and the baselines, recorded on the first,
        // failed on the second while nothing about the theme had changed. The gallery itself
        // keeps the reader's own number format; only the capture is pinned.
        var (culture, uiCulture) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            return RenderUnderCurrentCulture(frame);
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, uiCulture);
        }
    }

    private static RenderedFrame RenderUnderCurrentCulture(GalleryFrame frame)
    {
        Application.Current!.RequestedThemeVariant = frame.Variant;

        var view = Views.Single(v => v.Name == frame.View).Build();

        // The window is sized to the CONTENT rather than to a constant.
        //
        // A fixed height quietly truncates: every time a view grew, the capture kept rendering
        // and simply stopped showing the new rows, which is the least useful way for a screenshot
        // harness to fail. Worse, the outer ScrollViewer scrolls on load — a ListBox with a
        // selection brings its container into view — so a too-short window does not even start at
        // the top. Measuring first means neither can happen again.
        var window = new Window
        {
            Content = view,
            Width = 1180,
            Height = 800,
        };

        // Match the gallery page, which MainView paints with this brush behind every tab. A bare
        // window would show the base theme's own window background instead, and every capture
        // would misrepresent the page ground the views are designed against.
        window[!Window.BackgroundProperty] = new DynamicResourceExtension("FiliBackgroundGrayBrush");

        window.Show();
        var shown = Stopwatch.StartNew();
        Dispatcher.UIThread.RunJobs();

        // Now that the templates exist, ask the content how tall it actually wants to be and
        // grow the window to fit. The cap is a guard against a runaway measurement producing a
        // gigabyte of PNG, not a layout decision.
        view.Measure(new Size(window.Width, double.PositiveInfinity));
        window.Height = Math.Clamp(Math.Ceiling(view.DesiredSize.Height), 800, 8000);

        // Two passes: the first builds templates, the second lets the styles that those templates
        // triggered settle before the frame is taken.
        for (var i = 0; i < 2; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.Measure(new Size(window.Width, window.Height));
            window.Arrange(new Rect(0, 0, window.Width, window.Height));
            Dispatcher.UIThread.RunJobs();
        }

        SettleAnimations(shown);

        var unstable = UnstableRegions(window);
        var frameBitmap = Capture(window);

        window.Close();

        return new RenderedFrame(frameBitmap, unstable);
    }

    /// <summary>
    /// Longer than every finite animation that starts when a view is shown. The longest is the
    /// snackbar's 0.45s enter (Themes/Controls/Notifications.axaml); the 0.75s and 1.25s ones
    /// run only while a card closes, which no gallery frame does.
    /// </summary>
    private static readonly TimeSpan SettleTime = TimeSpan.FromMilliseconds(600);

    /// <summary>
    /// Lets every finite animation reach its last key frame before the frame is taken.
    ///
    /// <para>
    /// The animation clock follows wall time, so a one-shot animation is wherever the clock had
    /// got to when the capture happened. The snackbar's enter fade was about 99% done when its
    /// baselines were recorded, and a faster or slower run leaves it at some other percentage —
    /// a deterministic failure on one machine that passes on another. Masking the cards instead
    /// would hide the one region whose colours this suite most needs to watch. Waiting is exact:
    /// with <c>FillMode="Forward"</c> a finished animation holds its final value for good.
    /// </para>
    /// <para>
    /// The infinite ones never settle, which is what <see cref="UnstableRegions"/> is for.
    /// </para>
    /// </summary>
    private static void SettleAnimations(Stopwatch shown)
    {
        var remaining = SettleTime - shown.Elapsed;

        if (remaining > TimeSpan.Zero)
        {
            Thread.Sleep(remaining);
        }

        // One tick after the wait, so the clock observes the elapsed time and applies the final
        // key frames; Capture then draws that state.
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Draws the window's whole tree, once, into a fresh bitmap.
    ///
    /// <para>
    /// NOT <c>window.CaptureRenderedFrame()</c>, which this used to be. That returns the
    /// compositor's own frame, and the compositor repaints only what changed since the frame
    /// before - so the captured pixels are the sum of every partial redraw since the window
    /// opened, and how many there were depends on how the render timer happened to interleave
    /// with layout. On a loaded machine the antialiased ends of a few pill shapes - a large
    /// slider's rail and knob, a large switch's track - came out up to 36 levels off on roughly one
    /// run in three, and CI's slower runners failed a build and then a release on it. A
    /// RenderTargetBitmap has no history: under the same load it produced identical frames fifteen
    /// times out of fifteen, and pixel for pixel the same frames as before outside the masked
    /// animations, so no baseline changed.
    /// </para>
    /// </summary>
    private static WriteableBitmap Capture(Window window)
    {
        var size = new PixelSize((int)Math.Ceiling(window.Bounds.Width), (int)Math.Ceiling(window.Bounds.Height));
        var dpi = new Vector(96, 96);

        using var drawn = new RenderTargetBitmap(size, dpi);
        drawn.Render(window);

        var frame = new WriteableBitmap(size, dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var buffer = frame.Lock())
        {
            drawn.CopyPixels(new PixelRect(size), buffer.Address, buffer.RowBytes * size.Height, buffer.RowBytes);
        }

        return frame;
    }

    /// <summary>
    /// The parts of a frame that will not be the same twice, located by asking the live tree
    /// rather than by remembering coordinates.
    ///
    /// <para>
    /// There are two today, both animated for ever: an indeterminate ProgressBar, and a pulsing
    /// skeleton. A `no-animation` skeleton holds still, so it is compared like everything else. Each is driven by an animation clock that follows wall time, so
    /// the phase depends on how long the process took to get here — two runs of the SAME BUILD
    /// differ, every time, and no settle time can wait out an infinite animation.
    /// </para>
    /// <para>
    /// Masking a rectangle is not the same as loosening the comparison, and the difference
    /// matters. A tolerance budget large enough to absorb this would also absorb a small real
    /// change — a glyph redrawn, a one-pixel border appearing — and would hide it everywhere in
    /// the frame. This hides it only where the animation actually is, leaves the rest strict, and
    /// still catches a regression that MOVES the bar, because the old position stops being masked.
    /// </para>
    /// <para>
    /// The coordinates come from the control, so they follow the layout and cannot rot. The two
    /// pixels of inflation cover the antialiased edge of the band.
    /// </para>
    /// </summary>
    private static IReadOnlyList<PixelRect> UnstableRegions(Window window) =>
    [
        .. window.GetVisualDescendants()
            .OfType<Control>()
            .Where(c => c is ProgressBar { IsIndeterminate: true }
                || (c is Border && c.Classes.Contains("skeleton") && !c.Classes.Contains("no-animation")))
            .Where(c => c.Bounds is { Width: > 0, Height: > 0 })
            .Select(c =>
            {
                var origin = c.TranslatePoint(default, window) ?? default;

                return new PixelRect(
                    (int)Math.Floor(origin.X) - 2,
                    (int)Math.Floor(origin.Y) - 2,
                    (int)Math.Ceiling(c.Bounds.Width) + 4,
                    (int)Math.Ceiling(c.Bounds.Height) + 4);
            }),
    ];
}
