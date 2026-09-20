using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace Fili.MudAvalonia.Theme.Gallery.Views;

public partial class ElevationView : UserControl
{
    private const int MaxElevation = 24;

    public ElevationView()
    {
        AvaloniaXamlLoader.Load(this);

        Build();
        ActualThemeVariantChanged += (_, _) => Build();
    }

    private void Build()
    {
        var root = this.FindControl<WrapPanel>("Root");
        if (root is null)
        {
            return;
        }

        root.Children.Clear();

        for (var level = 0; level <= MaxElevation; level++)
        {
            var key = $"FiliElevation{level}";

            if (!this.TryFindResource(key, ActualThemeVariant, out var value) || value is not BoxShadows shadow)
            {
                root.Children.Add(new TextBlock
                {
                    Text = $"{key} — UNRESOLVED",
                    Classes = { "caption" },
                    Foreground = Brushes.Red,
                });
                continue;
            }

            root.Children.Add(BuildTile(level, shadow));
        }
    }

    private Control BuildTile(int level, BoxShadows shadow)
    {
        var surface = new Border
        {
            Width = 110,
            Height = 76,
            CornerRadius = new CornerRadius(4),
            BoxShadow = shadow,
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock
            {
                Text = level.ToString(),
                Classes = { "h6" },
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };

        // Bound rather than assigned, so the surface follows a runtime theme flip.
        surface[!Border.BackgroundProperty] = new DynamicResourceExtension("FiliSurfaceBrush");

        return new StackPanel
        {
            Margin = new Thickness(8),
            Spacing = 6,
            Children =
            {
                surface,
                new TextBlock
                {
                    Text = $"elevation{level}",
                    Classes = { "caption" },
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
            },
        };
    }
}
