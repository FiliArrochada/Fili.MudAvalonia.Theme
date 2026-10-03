using Avalonia.Media;

namespace Fili.Theme.MudAvalonia.Gallery;

/// <summary>
/// WCAG 2.x contrast maths, so the palette tab reports real numbers instead of an opinion.
/// AA wants 4.5:1 for body text and 3:1 for large text; AAA wants 7:1.
/// </summary>
internal static class Contrast
{
    /// <summary>
    /// Flattens a possibly translucent foreground onto an opaque background. Several palette
    /// tokens (TextSecondary, Divider, the state overlays) are alpha values, and their
    /// contrast is meaningless until they are composited.
    /// </summary>
    public static Color Composite(Color foreground, Color background)
    {
        if (foreground.A == 255)
        {
            return foreground;
        }

        var alpha = foreground.A / 255.0;

        return Color.FromRgb(
            (byte)Math.Round(foreground.R * alpha + background.R * (1 - alpha)),
            (byte)Math.Round(foreground.G * alpha + background.G * (1 - alpha)),
            (byte)Math.Round(foreground.B * alpha + background.B * (1 - alpha)));
    }

    public static double Ratio(Color foreground, Color background)
    {
        var flattened = Composite(foreground, background);

        var a = RelativeLuminance(flattened);
        var b = RelativeLuminance(background);

        var lighter = Math.Max(a, b);
        var darker = Math.Min(a, b);

        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>AA for normal-size body text.</summary>
    public static string Grade(double ratio) => ratio switch
    {
        >= 7.0 => "AAA",
        >= 4.5 => "AA",
        >= 3.0 => "AA large",
        _ => "fail",
    };

    private static double RelativeLuminance(Color color)
    {
        static double Channel(byte raw)
        {
            var v = raw / 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(color.R))
             + (0.7152 * Channel(color.G))
             + (0.0722 * Channel(color.B));
    }
}
