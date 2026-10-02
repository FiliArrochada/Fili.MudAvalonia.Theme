using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Fili.MudAvalonia.Theme.UnitTests;

// Declares the headless Avalonia application the UI-thread session boots from
// (https://docs.avaloniaui.net/docs/concepts/headless/headless-testing).
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]
// One shared application for the whole run. The default per-test isolation tears the app scope
// down after every dispatch, which disposes the FontManager and breaks the next test.
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerAssembly)]

namespace Fili.MudAvalonia.Theme.UnitTests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

/// <summary>
/// Runs test bodies on the shared headless Avalonia UI thread. This is what
/// Avalonia.Headless.XUnit's [AvaloniaFact] does under the hood, driven directly because that
/// integration package is xunit-v2-only.
/// </summary>
public static class UiThread
{
    private static readonly HeadlessUnitTestSession Session =
        HeadlessUnitTestSession.GetOrStartForAssembly(typeof(TestApp).Assembly);

    public static Task RunAsync(Action action) => Session.Dispatch(action, CancellationToken.None);
}

/// <summary>
/// The harness application. It must load the theme exactly as a consuming app does: the forked
/// base first for the control templates, then FiliTheme over it. A harness that composes itself
/// differently does not fail — it silently measures a theme nobody renders.
/// </summary>
public class TestApp : Application
{
    public override void Initialize()
    {
        // Same two lines a consuming app writes, in the same order. No FluentTheme anywhere.
        var baseUri = new Uri("avares://Fili.MudAvalonia.Theme.UnitTests");

        Styles.Add(new StyleInclude(baseUri)
        {
            Source = new Uri("avares://Fili.MudAvalonia.Theme/Themes/Base/FiliBaseTheme.axaml"),
        });

        Styles.Add(new StyleInclude(baseUri)
        {
            Source = new Uri("avares://Fili.MudAvalonia.Theme/FiliTheme.axaml"),
        });
    }
}
