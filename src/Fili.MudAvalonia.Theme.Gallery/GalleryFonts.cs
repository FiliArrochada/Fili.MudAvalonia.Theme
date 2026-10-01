using Avalonia;
using Avalonia.Media;

namespace Fili.MudAvalonia.Theme.Gallery;

/// <summary>
/// The fonts every gallery head registers, so they all draw the same glyphs.
/// </summary>
public static class GalleryFonts
{
    /// <summary>
    /// Registers Noto Sans Arabic as the fallback for characters the requested font lacks.
    ///
    /// <para>
    /// The gallery's right-to-left section is Arabic, and neither Roboto nor Inter has those
    /// glyphs. Without a registered fallback, the browser build drew those labels as nothing,
    /// because a browser has no system fonts, and the desktop drew them with whatever Arabic
    /// face the OS happened to have, so a pixel baseline recorded on one Windows machine was only
    /// a guess about another. Registered here, every head and the pixel suite use the same face.
    /// </para>
    /// </summary>
    public static AppBuilder WithGalleryFonts(this AppBuilder builder) =>
        builder.With(new FontManagerOptions
        {
            FontFallbacks =
            [
                new FontFallback
                {
                    FontFamily = new FontFamily(
                        "avares://Fili.MudAvalonia.Theme.Gallery/Assets/Fonts#Noto Sans Arabic"),
                },
            ],
        });
}
