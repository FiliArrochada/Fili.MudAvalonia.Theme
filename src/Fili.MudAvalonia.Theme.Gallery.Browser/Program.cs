using Avalonia;
using Avalonia.Browser;
using Avalonia.Media;

namespace Fili.MudAvalonia.Theme.Gallery.Browser;

internal static class Program
{
    // "out" is the id of the element in wwwroot/index.html that the app renders into.
    private static Task Main(string[] args) =>
        BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            // Consulted for any character the requested font does not have. On desktop the OS
            // answers that; in the browser nothing does unless the app ships a face itself.
            .With(new FontManagerOptions
            {
                FontFallbacks =
                [
                    new FontFallback
                    {
                        FontFamily = new FontFamily(
                            "avares://Fili.MudAvalonia.Theme.Gallery.Browser/Assets/Fonts#Noto Sans Arabic"),
                    },
                ],
            });
}
