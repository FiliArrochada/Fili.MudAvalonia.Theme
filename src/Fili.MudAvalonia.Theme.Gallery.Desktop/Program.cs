using Avalonia;

namespace Fili.MudAvalonia.Theme.Gallery.Desktop;

internal static class Program
{
    // Initialization code. Nothing before AppMain may use any Avalonia type or SynchronizationContext.
    [STAThread]
    public static void Main(string[] args)
    {
        // dotnet run --project ... -- --capture <dir>
        // Renders every view, substrate and variant to PNG without a display. See Capture.cs.
        var capture = Array.IndexOf(args, "--capture");
        if (capture >= 0)
        {
            Capture.Run(capture + 1 < args.Length ? args[capture + 1] : "screenshots");
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .WithGalleryFonts()
            .LogToTrace();
}
