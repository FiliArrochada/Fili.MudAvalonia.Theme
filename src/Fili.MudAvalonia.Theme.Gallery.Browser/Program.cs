using Avalonia;
using Avalonia.Browser;

namespace Fili.MudAvalonia.Theme.Gallery.Browser;

internal static class Program
{
    // "out" is the id of the element in wwwroot/index.html that the app renders into.
    private static Task Main(string[] args) =>
        BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
}
