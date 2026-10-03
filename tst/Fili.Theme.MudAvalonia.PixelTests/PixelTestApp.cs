using Avalonia;
using Avalonia.Headless;
using Fili.Theme.MudAvalonia.Gallery.Desktop;
using Fili.Theme.MudAvalonia.PixelTests;

// The headless application this suite renders in. It is the gallery's own builder, not a copy:
// Skia with headless drawing turned off, which is the only configuration that produces a frame
// rather than a blank image.
[assembly: AvaloniaTestApplication(typeof(PixelTestAppBuilder))]
// One application for the whole run. Per-test isolation tears the app scope down after every
// dispatch, which disposes the FontManager — and a suite that renders text cannot survive that.
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerAssembly)]

namespace Fili.Theme.MudAvalonia.PixelTests;

public class PixelTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => GalleryFrames.Configure();
}

/// <summary>
/// Runs test bodies on the shared headless UI thread, exactly as the unit-test suite does.
/// Constructing an Avalonia object off that thread throws and then poisons the session for every
/// test that follows, so there are no exceptions to this.
/// </summary>
public static class UiThread
{
    private static readonly HeadlessUnitTestSession Session =
        HeadlessUnitTestSession.GetOrStartForAssembly(typeof(PixelTestAppBuilder).Assembly);

    public static Task RunAsync(Action action) => Session.Dispatch(action, CancellationToken.None);
}
