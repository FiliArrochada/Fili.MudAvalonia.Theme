using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avalonia.Threading;
using Fili.MudAvalonia.Theme;
using Fili.MudAvalonia.Theme.Gallery.Views;

namespace Fili.MudAvalonia.Theme.Gallery.Desktop;

/// <summary>
/// Renders the gallery views to PNG without a display, for every substrate and theme variant.
/// <para>
/// A theme is judged by looking at it, and a reviewer cannot always run the app — so the gallery
/// needs to be able to produce its own screenshots. This is also what makes a pixel-regression
/// suite possible later: the same frames, diffed against committed baselines.
/// </para>
/// <para>
/// The one non-obvious requirement is <c>UseHeadlessDrawing = false</c> plus Skia. Headless
/// drawing is the default and it is a no-op renderer: everything "works", every capture comes
/// back blank.
/// </para>
/// </summary>
internal static class Capture
{
    private static readonly (string Name, Func<Control> Build)[] Views =
    [
        ("controls", () => new ControlStatesView()),
        ("screen", () => new SampleScreenView()),
        ("palette", () => new PaletteView()),
    ];

    public static void Run(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithInterFont()
            .SetupWithoutStarting();

        var app = (App)Application.Current!;

        foreach (var substrate in new[] { Substrate.Standalone, Substrate.Fluent })
        {
            app.UseSubstrate(substrate);

            // High contrast only on the standalone substrate: it is this package's variant, and
            // asking Fluent for it would render its dark theme under a filename that claims
            // otherwise.
            var variants = substrate == Substrate.Standalone
                ? new[] { ThemeVariant.Light, ThemeVariant.Dark, FiliThemeVariants.HighContrast }
                : [ThemeVariant.Light, ThemeVariant.Dark];

            foreach (var variant in variants)
            {
                app.RequestedThemeVariant = variant;

                foreach (var (name, build) in Views)
                {
                    var path = Path.Combine(
                        outputDirectory,
                        $"{name}-{substrate}-{variant}.png".ToLowerInvariant());

                    Write(build(), path);
                    Console.WriteLine(path);
                }
            }
        }
    }

    private static void Write(Control view, string path)
    {
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

        using var frame = window.CaptureRenderedFrame();
        frame?.Save(path);

        window.Close();
    }
}
