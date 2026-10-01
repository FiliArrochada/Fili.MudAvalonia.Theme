using System;
using System.IO;
using Avalonia.Media.Imaging;

namespace Fili.MudAvalonia.Theme.Gallery.Desktop;

/// <summary>
/// Writes every gallery frame to PNG without a display.
///
/// <para>
/// A theme is judged by looking at it, and a reviewer cannot always run the app — so the gallery
/// produces its own screenshots. The frames and the rendering live in <see cref="GalleryFrames"/>
/// because the pixel-regression suite renders exactly the same ones; this file is only the
/// command-line skin over them.
/// </para>
/// </summary>
internal static class Capture
{
    public static void Run(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        GalleryFrames.Configure().SetupWithoutStarting();

        foreach (var frame in GalleryFrames.All)
        {
            var path = Path.Combine(outputDirectory, frame.FileName);

            using (var rendered = GalleryFrames.Render(frame))
            {
                rendered.Bitmap.Save(path, PngBitmapEncoderOptions.Default);
            }

            Console.WriteLine(path);
        }
    }
}
