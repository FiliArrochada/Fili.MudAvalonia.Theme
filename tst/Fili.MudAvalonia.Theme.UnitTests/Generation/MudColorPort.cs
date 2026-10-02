using Avalonia.Media;

namespace Fili.MudAvalonia.Theme.UnitTests.Generation;

/// <summary>
/// MudBlazor's colour derivation, ported line for line from <c>src/MudBlazor/Utilities/MudColor.cs</c>:
/// the RGB to HSL round trip, and <c>ColorRgbDarken</c> / <c>ColorRgbLighten</c>.
///
/// <para>
/// Palette.cs derives every <c>{Color}Darken</c> as <c>Color.ColorRgbDarken()</c>: HSL lightness
/// minus 0.075, with hue rounded to a degree and saturation and lightness to two places on the way
/// in and on the way out. Lighten is the same with plus 0.075. The rounding is .NET's own
/// <see cref="Math.Round(double, int)"/>, which is why this is C#: it scales by 100 in double
/// arithmetic and rounds that half-to-even, so 0.465 becomes 0.46 and 0.575 becomes 0.57 - and
/// rounding the decimal or the binary value instead each gets some colours wrong.
/// </para>
/// </summary>
public static class MudColorPort
{
    private const double Epsilon = 0.000000000000001;

    public static Color Darken(Color color) => ChangeLightness(color, -0.075);

    public static Color Lighten(Color color) => ChangeLightness(color, +0.075);

    private static Color ChangeLightness(Color color, double amount)
    {
        var (h, s, l) = RgbToHsl(color.R, color.G, color.B);
        var (r, g, b) = HslToRgb(h, s, Math.Max(0, Math.Min(1, l + amount)));

        return Color.FromRgb(r, g, b);
    }

    private static (double H, double S, double L) RgbToHsl(byte r, byte g, byte b)
    {
        var h = 0d;
        var s = 0d;

        var rn = r / 255d;
        var gn = g / 255d;
        var bn = b / 255d;

        var max = Math.Max(rn, Math.Max(gn, bn));
        var min = Math.Min(rn, Math.Min(gn, bn));

        if (Math.Abs(max - min) < Epsilon)
        {
            h = 0d;
        }
        else if (Math.Abs(max - rn) < Epsilon && gn >= bn)
        {
            h = 60d * (gn - bn) / (max - min);
        }
        else if (Math.Abs(max - rn) < Epsilon && gn < bn)
        {
            h = (60d * (gn - bn) / (max - min)) + 360d;
        }
        else if (Math.Abs(max - gn) < Epsilon)
        {
            h = (60d * (bn - rn) / (max - min)) + 120d;
        }
        else if (Math.Abs(max - bn) < Epsilon)
        {
            h = (60d * (rn - gn) / (max - min)) + 240d;
        }

        var l = (max + min) / 2d;

        if (Math.Abs(l) < Epsilon || Math.Abs(max - min) < Epsilon)
        {
            s = 0d;
        }
        else if (l is > 0d and <= .5d)
        {
            s = (max - min) / (max + min);
        }
        else if (l > .5d)
        {
            s = (max - min) / (2d - (max + min));
        }

        return (Math.Round(Range(h, 360), 0), Math.Round(Range(s, 1), 2), Math.Round(Range(l, 1), 2));
    }

    private static (byte R, byte G, byte B) HslToRgb(double h, double s, double l)
    {
        // MudColor's HSL constructor rounds again before converting.
        h = Math.Round(Range(h, 360), 0);
        s = Math.Round(Range(s, 1), 2);
        l = Math.Round(Range(l, 1), 2);

        if (Math.Abs(s) < Epsilon)
        {
            var grey = (byte)Math.Max(0, Math.Min(255, (int)Math.Ceiling(l * 255d)));
            return (grey, grey, grey);
        }

        var hn = h / 360d;
        var t2 = l <= 0.5d ? l * (1.0d + s) : l + s - (l * s);
        var t1 = (2.0d * l) - t2;

        return (Channel(HueToRgb(t1, t2, hn + (1.0d / 3.0d))),
                Channel(HueToRgb(t1, t2, hn)),
                Channel(HueToRgb(t1, t2, hn - (1.0d / 3.0d))));

        static byte Channel(double value) => (byte)Math.Max(0, Math.Min(255, (int)Math.Round(value * 255d)));
    }

    private static double HueToRgb(double t1, double t2, double hue)
    {
        if (hue < 0.0d)
        {
            hue += 1.0d;
        }

        if (hue > 1.0d)
        {
            hue -= 1.0d;
        }

        return hue switch
        {
            < 1.0d / 6.0d => t1 + ((t2 - t1) * 6.0d * hue),
            < 1.0d / 2.0d => t2,
            < 2.0d / 3.0d => t1 + ((t2 - t1) * ((2.0d / 3.0d) - hue) * 6.0d),
            _ => t1,
        };
    }

    private static double Range(double value, double max) => Math.Max(0, Math.Min(max, value));
}
