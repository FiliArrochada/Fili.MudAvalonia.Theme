using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Fili.MudAvalonia.Theme.PixelTests;

/// <summary>A frame's pixels, normalised to BGRA so two images can be compared byte for byte.</summary>
public sealed record Pixels(byte[] Bytes, PixelSize Size, int Stride)
{
    public bool DiffersAt(Pixels other, int x, int y, int tolerance, out int delta)
    {
        var i = (y * Stride) + (x * 4);
        delta = 0;

        // Alpha is deliberately not compared. An opaque frame carries 255 everywhere, and the one
        // time it would not, the colour channels have already said so.
        for (var channel = 0; channel < 3; channel++)
        {
            delta = Math.Max(delta, Math.Abs(Bytes[i + channel] - other.Bytes[i + channel]));
        }

        return delta > tolerance;
    }
}

/// <summary>The result of comparing a rendered frame with its baseline.</summary>
public sealed record Comparison(int Differing, int MaxDelta, PixelPoint? First, int Total)
{
    public bool Matches => Differing == 0;

    public double Percent => Total == 0 ? 0 : 100.0 * Differing / Total;
}

/// <summary>
/// Reading, comparing and diffing frames.
///
/// <para>
/// Everything here runs on the UI thread, because decoding a PNG goes through the platform's
/// imaging stack and there is no platform off that thread.
/// </para>
/// </summary>
public static class Frames
{
    /// <summary>
    /// How far a channel may drift before a pixel counts as different.
    ///
    /// <para>
    /// Zero would be ideal and is nearly true — two runs of the same build produce identical
    /// frames once the animated region is masked. One is kept as headroom for a Skia point
    /// release rounding an antialiased edge differently, which is a change in someone else's
    /// code rather than a regression in this theme.
    /// </para>
    /// </summary>
    public const int Tolerance = 1;

    public static Pixels Read(Bitmap bitmap)
    {
        var size = bitmap.PixelSize;

        // Copying through a known format is what makes the comparison meaningful: a decoded PNG
        // and a captured frame do not necessarily arrive in the same pixel format, and comparing
        // their raw buffers would be comparing two different encodings of the same colour.
        using var target = new WriteableBitmap(size, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using var buffer = target.Lock();

        bitmap.CopyPixels(buffer);

        var bytes = new byte[buffer.RowBytes * size.Height];
        Marshal.Copy(buffer.Address, bytes, 0, bytes.Length);

        return new Pixels(bytes, size, buffer.RowBytes);
    }

    public static Comparison Compare(Pixels actual, Pixels baseline, IReadOnlyList<PixelRect> masked)
    {
        var size = actual.Size;
        var differing = 0;
        var maxDelta = 0;
        PixelPoint? first = null;

        for (var y = 0; y < size.Height; y++)
        {
            for (var x = 0; x < size.Width; x++)
            {
                if (IsMasked(masked, x, y))
                {
                    continue;
                }

                if (!actual.DiffersAt(baseline, x, y, Tolerance, out var delta))
                {
                    continue;
                }

                differing++;
                maxDelta = Math.Max(maxDelta, delta);
                first ??= new PixelPoint(x, y);
            }
        }

        return new Comparison(differing, maxDelta, first, size.Width * size.Height);
    }

    /// <summary>
    /// Writes a diff image: the baseline drained of colour, with every differing pixel in red and
    /// every masked region in blue. The point is to be able to SEE what moved — a number in a test
    /// failure says something changed, not what.
    /// </summary>
    public static void WriteDiff(string path, Pixels actual, Pixels baseline, IReadOnlyList<PixelRect> masked)
    {
        var size = actual.Size;

        using var image = new WriteableBitmap(size, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);

        using (var buffer = image.Lock())
        {
            var bytes = new byte[buffer.RowBytes * size.Height];

            for (var y = 0; y < size.Height; y++)
            {
                for (var x = 0; x < size.Width; x++)
                {
                    var i = (y * buffer.RowBytes) + (x * 4);
                    var source = (y * baseline.Stride) + (x * 4);

                    if (IsMasked(masked, x, y))
                    {
                        (bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3]) = ((byte)200, (byte)90, (byte)0, (byte)255);
                    }
                    else if (actual.DiffersAt(baseline, x, y, Tolerance, out _))
                    {
                        (bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3]) = ((byte)0, (byte)0, (byte)255, (byte)255);
                    }
                    else
                    {
                        var grey = (byte)(((baseline.Bytes[source] + baseline.Bytes[source + 1] + baseline.Bytes[source + 2]) / 3) / 3);
                        (bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3]) = (grey, grey, grey, (byte)255);
                    }
                }
            }

            Marshal.Copy(bytes, 0, buffer.Address, bytes.Length);
        }

        Save(image, path);
    }

    /// <summary>
    /// Avalonia 12.1.2 deprecates <c>Save(string)</c> in favour of an overload taking
    /// BitmapEncoderOptions, a type that ships with no public surface to construct. Until it has
    /// one, the deprecated call is the only one that writes a PNG.
    /// </summary>
#pragma warning disable CS0618
    public static void Save(Bitmap bitmap, string path) => bitmap.Save(path);
#pragma warning restore CS0618

    private static bool IsMasked(IReadOnlyList<PixelRect> masked, int x, int y)
    {
        for (var i = 0; i < masked.Count; i++)
        {
            if (masked[i].Contains(new PixelPoint(x, y)))
            {
                return true;
            }
        }

        return false;
    }
}
