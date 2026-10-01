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
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Fili.MudAvalonia.Theme;
using Fili.MudAvalonia.Theme.Gallery.Views;

namespace Fili.MudAvalonia.Theme.Gallery.Desktop;

/// <summary>One rendered gallery frame: a view, under a substrate, in a theme variant.</summary>
public readonly record struct GalleryFrame(string View, Substrate Substrate, ThemeVariant Variant)
{
    /// <summary>The frame's file name, which is also its baseline's and its identity in a test.</summary>
    public string FileName => $"{View}-{Substrate}-{Variant}.png".ToLowerInvariant();

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

    /// <summary>Every frame the screenshot mode writes.</summary>
    public static IReadOnlyList<GalleryFrame> All { get; } =
    [
        .. from substrate in new[] { Substrate.Standalone, Substrate.Fluent }
           from variant in Variants(substrate)
           from view in Views
           select new GalleryFrame(view.Name, substrate, variant),
    ];

    /// <summary>
    /// The frames with a committed baseline: the standalone substrate only.
    ///
    /// <para>
    /// Fluent is excluded deliberately rather than forgotten. It is a comparison aid, and what it
    /// renders belongs to Avalonia — baselining it would turn every Avalonia upgrade into a
    /// failing test about someone else's theme, which is noise pretending to be coverage.
    /// </para>
    /// </summary>
    public static IReadOnlyList<GalleryFrame> Baselined { get; } =
        [.. All.Where(f => f.Substrate == Substrate.Standalone)];

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
        var app = (App)Application.Current!;

        app.UseSubstrate(frame.Substrate);
        app.RequestedThemeVariant = frame.Variant;

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

        // Match MainWindow. Without this the window falls back to the substrate's own background
        // — near-black under Fluent dark — and every dark capture misrepresents the theme, whose
        // page ground is the much lighter #32333D.
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

        var frameBitmap = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException(
                $"{frame} rendered no frame. The usual cause is headless drawing being left on.");

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
        // key frames; CaptureRenderedFrame then renders that state.
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// The parts of a frame that will not be the same twice, located by asking the live tree
    /// rather than by remembering coordinates.
    ///
    /// <para>
    /// There is exactly one today: an indeterminate ProgressBar. Its band is driven by an
    /// animation clock that follows wall time, so the phase depends on how long the process took
    /// to get here — two runs of the SAME BUILD differ by about eighty pixels, every time.
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
            .OfType<ProgressBar>()
            .Where(bar => bar.IsIndeterminate && bar.Bounds is { Width: > 0, Height: > 0 })
            .Select(bar =>
            {
                var origin = bar.TranslatePoint(default, window) ?? default;

                return new PixelRect(
                    (int)Math.Floor(origin.X) - 2,
                    (int)Math.Floor(origin.Y) - 2,
                    (int)Math.Ceiling(bar.Bounds.Width) + 4,
                    (int)Math.Ceiling(bar.Bounds.Height) + 4);
            }),
    ];

    // High contrast only on the standalone substrate: it is this package's variant, and asking
    // Fluent for it would render its dark theme under a filename that claims otherwise.
    private static ThemeVariant[] Variants(Substrate substrate) =>
        substrate == Substrate.Standalone
            ? [ThemeVariant.Light, ThemeVariant.Dark, FiliThemeVariants.HighContrast]
            : [ThemeVariant.Light, ThemeVariant.Dark];
}
