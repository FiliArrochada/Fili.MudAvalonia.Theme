using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace Fili.MudAvalonia.Theme.Gallery.Views;

/// <summary>
/// Every palette token as a swatch, with its resolved hex and its contrast against the
/// surface it sits on. Built in code rather than markup because the rows are generated and
/// because the contrast numbers have to be recomputed whenever the theme variant flips.
/// </summary>
public partial class PaletteView : UserControl
{
    private static readonly (string Group, string Key)[] Tokens =
    [
        ("Brand",      "FiliPrimaryColor"),
        ("Brand",      "FiliSecondaryColor"),
        ("Brand",      "FiliTertiaryColor"),

        ("Semantic",   "FiliInfoColor"),
        ("Semantic",   "FiliSuccessColor"),
        ("Semantic",   "FiliWarningColor"),
        ("Semantic",   "FiliErrorColor"),
        ("Semantic",   "FiliDarkColor"),

        ("Surface",    "FiliBackgroundColor"),
        ("Surface",    "FiliBackgroundGrayColor"),
        ("Surface",    "FiliSurfaceColor"),
        ("Surface",    "FiliAppbarBackgroundColor"),
        ("Surface",    "FiliDrawerBackgroundColor"),

        ("Text",       "FiliTextPrimaryColor"),
        ("Text",       "FiliTextSecondaryColor"),
        ("Text",       "FiliTextDisabledColor"),

        ("Action",     "FiliActionDefaultColor"),
        ("Action",     "FiliActionDisabledColor"),
        ("Action",     "FiliActionDisabledBackgroundColor"),

        ("Lines",      "FiliLinesDefaultColor"),
        ("Lines",      "FiliLinesInputsColor"),
        ("Lines",      "FiliDividerColor"),
        ("Lines",      "FiliTableLinesColor"),

        ("Overlay",    "FiliOverlayHoverColor"),
        ("Overlay",    "FiliOverlayPressedColor"),
        ("Overlay",    "FiliSkeletonColor"),
    ];

    public PaletteView()
    {
        AvaloniaXamlLoader.Load(this);

        Build();
        ActualThemeVariantChanged += (_, _) => Build();
    }

    private void Build()
    {
        var root = this.FindControl<StackPanel>("Root");
        if (root is null)
        {
            return;
        }

        root.Children.Clear();

        var surface = Resolve("FiliSurfaceColor") ?? Colors.White;
        string? currentGroup = null;

        foreach (var (group, key) in Tokens)
        {
            if (group != currentGroup)
            {
                currentGroup = group;
                root.Children.Add(new TextBlock
                {
                    Text = group,
                    Classes = { "overline" },
                    Margin = new Thickness(0, 16, 0, 0),
                });
            }

            var color = Resolve(key);
            if (color is null)
            {
                // A missing key is a real defect, so say so rather than skipping the row.
                root.Children.Add(new TextBlock
                {
                    Text = $"{key} — UNRESOLVED",
                    Classes = { "body2" },
                    Foreground = Brushes.Red,
                });
                continue;
            }

            root.Children.Add(BuildRow(key, color.Value, surface));
        }
    }

    private static Control BuildRow(string key, Color color, Color surface)
    {
        var ratio = Contrast.Ratio(color, surface);

        var swatch = new Border
        {
            Width = 64,
            Height = 40,
            Background = new SolidColorBrush(color),
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromArgb(31, 128, 128, 128)),
        };

        var name = new TextBlock
        {
            Text = key.Replace("Fili", string.Empty).Replace("Color", string.Empty),
            Classes = { "body2" },
            Width = 240,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var hex = new TextBlock
        {
            Text = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}",
            Classes = { "caption" },
            Width = 100,
            FontFamily = new FontFamily("Consolas, Menlo, monospace"),
            VerticalAlignment = VerticalAlignment.Center,
        };

        var contrast = new TextBlock
        {
            Text = $"{ratio:0.00}:1  {Contrast.Grade(ratio)}",
            Classes = { "caption" },
            VerticalAlignment = VerticalAlignment.Center,
        };

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Children = { swatch, name, hex, contrast },
        };
    }

    private Color? Resolve(string key)
    {
        if (this.TryFindResource(key, ActualThemeVariant, out var value) && value is Color color)
        {
            return color;
        }

        return null;
    }
}
